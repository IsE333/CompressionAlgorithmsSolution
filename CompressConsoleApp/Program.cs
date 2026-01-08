using CompressionAlgorithms;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace CompressConsoleApp
{
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            Console.WriteLine(Environment.ProcessorCount + " logical processors detected.");
            Console.WriteLine("Leave empty for default 0.5 MB");
            Console.WriteLine("Enter buffer size in megabytes (e.g., 4.0): ");
            if (!double.TryParse(Console.ReadLine(), out double size))
                size = 0.5;
            int bufferSize = (int)(size * 1024 * 1024);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            CompressFile<LZWOptimized>(bufferSize).Wait();
            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            Console.WriteLine("-------------");
            timer.Restart();
            DecompressFile<LZWOptimized>().Wait();
            timer.Stop();

            Console.WriteLine($"FILE LZW Optimized Compression Elapsed Time: {elapsedMs} ms");
            Console.WriteLine($"FILE LZW Optimized DeCompression Elapsed Time: {timer.ElapsedMilliseconds} ms");


            //ExampleRLE("aabbbccddddee");
            //ExampleDeltaEncoding();
            //ExampleHuffmanCoding();
            //ExampleDeltaHuffmanCodingCombined();
            //ExampleLZ77();
            /*
            timer.Restart();
            ExampleLZWOptimized(false);
            timer.Stop();
            Console.WriteLine($"Elapsed Time: {timer.ElapsedMilliseconds} ms");
            timer.Restart();
            ExampleLZW(false);
            timer.Stop();
            Console.WriteLine($"Elapsed Time: {timer.ElapsedMilliseconds} ms");*/
        }
        static async Task CompressFile<T>(int bufferSize = 65536, string inputPath = "test.txt", string outputPath = "test_lzwO_compressed.bin") where T : IAlgorithm, new()
        {
            using (FileStream fsWrite = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;
                    BlockingCollection<Task<byte[]>> taskList = [];
                    bool endOfWriting = false;

                    _ = Task.Run(async () =>
                    {
                        while (!taskList.IsCompleted)
                        {
                            if (taskList.TryTake(out var task))
                            {
                                var result = await task;
                                fsWrite.Write(BitConverter.GetBytes((uint)result.Length), 0, 4);
                                fsWrite.Write(result, 0, result.Length);
                            } 
                            else
                                await Task.Delay(1);
                        }
                        fsWrite.Write(BitConverter.GetBytes((uint)0), 0, 4); // EOF
                        endOfWriting = true;
                    });
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int bytesReadTemp = bytesRead;
                        byte[] temp = ArrayPool<byte>.Shared.Rent(bytesRead);
                        Array.Copy(buffer, 0, temp, 0, bytesRead);

                        while (taskList.Count >= Environment.ProcessorCount)
                            await Task.Delay(1);
                        
                        taskList.Add(Task.Run(() =>
                        {
                            try
                            {
                                //Console.WriteLine($"Compressing chunk {taskList.Count}");
                                var lzwO = new T();
                                if (temp.Length > bytesReadTemp)
                                    return lzwO.Compress(temp[..bytesReadTemp]);
                                return lzwO.Compress(temp);
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(temp);
                            }
                        }));
                    }
                    taskList.CompleteAdding();
                    while (!endOfWriting)
                        await Task.Delay(10);
                }
            }
        }
        static async Task DecompressFile<T>(int bufferSize = 65536, string inputPath = "test_lzwO_compressed.bin", string outputPath = "test_lzwO_DEcompressed.txt") where T : IAlgorithm, new()
        {
            using (FileStream fsWrite = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                {
                    byte[] chunkSizeBytes = [0, 0, 0, 0];
                    fs.ReadExactly(chunkSizeBytes, 0, 4);
                    byte[] chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
                    int bytesRead;
                    BlockingCollection<Task<byte[]>> taskList = [];
                    bool endOfWriting = false;

                    _ = Task.Run(async () =>
                    {
                        while (!taskList.IsCompleted)
                        {
                            if (taskList.TryTake(out var task))
                            {
                                task.Wait();
                                var result = task.Result;
                                fsWrite.Write(result, 0, result.Length);
                            } 
                            else
                                await Task.Delay(10);
                        }
                        endOfWriting = true;
                    });
                    while ((bytesRead = fs.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        byte[] temp = new byte[chunk.Length];
                        Array.Copy(chunk, 0, temp, 0, chunk.Length);
                        var lzwO = new T();

                        while (taskList.Count >= Environment.ProcessorCount)
                            await Task.Delay(1);

                        taskList.Add(Task.Run(() =>
                        {
                            return lzwO.Decompress(temp);
                        }));

                        fs.ReadExactly(chunkSizeBytes, 0, 4);
                        chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
                    }
                    taskList.CompleteAdding();
                    while (!endOfWriting)
                        await Task.Delay(1);
                }
            }
        }

        static void ExampleRLE(string text)
        {
            Console.WriteLine("----------------------RunLengthEncoding----------------------");
            var input = Encoding.UTF8.GetBytes(text);
            var rle = new RunLengthEncoding();

            var compressed = rle.Compress(input);
            var decompressed = rle.Decompress(compressed);

            Utils.PrintInfo(input, compressed, decompressed, includeCompressedText: false);

        }

        static void ExampleDeltaEncoding()
        {
            Console.WriteLine("------------------------DeltaEncoding------------------------");
            List<string> input = Utils.CsvRead("test1.csv")[2]; // Only third column
            input.RemoveAt(0); // Remove column name
            byte[] inputBytes = Utils.FormatAndConvertToBytes(input);
            
            DeltaEncoding delta = new(lengthLHS: 2, lengthRHS: 3);

            byte[] resultCompress = delta.Compress(inputBytes);
            byte[] resultDecompress = delta.Decompress(resultCompress);

            List<string> outputStrings = [];
            for (int i = 0; i < resultDecompress.Length / 6; i++)
                outputStrings.Add(Encoding.UTF8.GetString(resultDecompress[(i * 6)..(i * 6 + 6)]));

            List<string> output = [];
            for (int i = 0; i < outputStrings.Count; i++)
            {
                string value = outputStrings[i].Split(".")[0].TrimStart('0') + "." + outputStrings[i].Split(".")[1].TrimEnd('0');
                if (value.EndsWith('.')) value += "0";
                output.Add(value);
                if (input[i] != output[i])
                    Console.WriteLine($"Original: {input[i]}  Output: {output[i]}");
            }

            Console.WriteLine($"Formatting:     {Utils.SizeOfList(input)} -> {inputBytes.Length} bytes");
            Console.WriteLine($"Output bytes match input: {resultDecompress.SequenceEqual(inputBytes)}");
            Console.WriteLine($"Output strings match input: {output.SequenceEqual(input)}");
            Utils.PrintInfo(inputBytes, resultCompress, resultDecompress, true, includeBytes: false, trunc: true);
        }

        static void ExampleHuffmanCoding()
        {
            Console.WriteLine("-----------------------HuffmanEncoding-----------------------");
            var huffman = new HuffmanCoding();
            var inputBytes = Encoding.UTF8.GetBytes("GGGGGDDDDDDDDDDDBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCCCFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            var compressed = huffman.Compress(inputBytes);
            var decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed);
        }

        static void ExampleDeltaHuffmanCodingCombined()
        {
            List<string> input = Utils.CsvRead("test1.csv")[2];
            input.RemoveAt(0); // Remove column name
            byte[] inputBytes = Utils.FormatAndConvertToBytes(input);

            DeltaEncoding delta = new(lengthLHS: 2, lengthRHS: 3);
            byte[] resultCompress = delta.Compress(inputBytes);
            byte[] resultDecompress = delta.Decompress(resultCompress);

            List<string> outputStrings = [];
            for (int i = 0; i < resultDecompress.Length / 6; i++)
                outputStrings.Add(Encoding.UTF8.GetString(resultDecompress[(i * 6)..(i * 6 + 6)]));

            List<string> output = [];
            for (int i = 0; i < outputStrings.Count; i++)
            {
                string value = outputStrings[i].Split(".")[0].TrimStart('0') + "." + outputStrings[i].Split(".")[1].TrimEnd('0');
                if (value.EndsWith('.')) value += "0";
                output.Add(value);
                if (input[i] != output[i])
                    Console.WriteLine($"Original: {input[i]}  Output: {output[i]}");
            }

            Console.WriteLine("-----------------------------OnlyRLE-----------------------------");
            Console.WriteLine($"Formatting:     {Utils.SizeOfList(input)} -> {inputBytes.Length} bytes");
            Console.WriteLine($"Output bytes match input: {resultDecompress.SequenceEqual(inputBytes)}");
            Console.WriteLine($"Output strings match input: {output.SequenceEqual(input)}");
            Utils.PrintInfo(inputBytes, resultCompress, resultDecompress, true, includeBytes: false, trunc: true);

            Console.WriteLine("-----------------------OnlyHuffmanEncoding-----------------------");
            var huffman = new HuffmanCoding();
            var inputBytes3 = inputBytes;
            var compressed = huffman.Compress(inputBytes3);
            var decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo(inputBytes3, compressed, decompressed, false);

            /*Console.WriteLine("--------------------RawCSVOnlyHuffmanEncoding--------------------");
            List<byte> inputBytes4 = [];
            for (int i = 1; i < input.Count; i++)
            {
                var value = input[i] + ",";
                inputBytes4.AddRange(Encoding.UTF8.GetBytes(value));
            }
            compressed = huffman.Compress([.. inputBytes4]);
            decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo([.. inputBytes4], compressed, decompressed, false);*/

            Console.WriteLine("-----------------------RLE+HuffmanEncoding-----------------------");
            var inputBytes2 = resultCompress;
            compressed = huffman.Compress(inputBytes2);
            decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo(inputBytes2, compressed, decompressed, false);
            Utils.PrintInfo(inputBytes, compressed, decompressed, false);
        }

        static void ExampleLZ77()
        {
            Console.WriteLine("-----------------------LempelZiv(LZ77)-----------------------");
            var lz77 = new LZ77();
            var inputBytes = Encoding.UTF8.GetBytes("uzum uzume baka baka kararir");
            var compressed = lz77.Compress(inputBytes);
            var decompressed = lz77.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false);
        }

        static void ExampleLZW(bool showData)
        {
            Console.WriteLine("-----------------------LempelZiv(LZW)-----------------------");
            var lzw = new LZW();
            var inputBytes = Encoding.UTF8.GetBytes("üzüm üzüme baka baka kararır");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var compressed = lzw.Compress(inputBytes);
            timer.Stop();
            var decompressed = lzw.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false, showData: showData);
            Console.WriteLine($"Compression took: {timer.ElapsedMilliseconds} ms");
        }
        static void ExampleLZWOptimized(bool showData)
        {
            Console.WriteLine("------------------LempelZiv(LZW) Optimized------------------");
            var lzw = new LZWOptimized();
            var inputBytes = Encoding.UTF8.GetBytes("üzüm üzüme baka baka kararır");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var compressed = lzw.Compress(inputBytes);
            timer.Stop();
            var decompressed = lzw.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false, showData: showData);
            Console.WriteLine($"Compression took: {timer.ElapsedMilliseconds} ms");
        }
    }
}
