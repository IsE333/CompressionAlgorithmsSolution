using CompressionAlgorithms;
using System.Text;

namespace CompressConsoleApp
{
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;


            Console.WriteLine("Enter buffer size in bytes (e.g., 65536): ");
            if (!int.TryParse(Console.ReadLine(), out int bufferSize))
                bufferSize = 65536;


            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            compressFile(bufferSize);
            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            Console.WriteLine("-------------");
            timer.Restart();
            decompressFile();
            timer.Stop();

            Console.WriteLine($"FILE LZW Optimized COMPRESSION Elapsed Time: {elapsedMs} ms");
            Console.WriteLine($"FILE LZW Optimized DECompression Elapsed Time: {timer.ElapsedMilliseconds} ms");


            //ExampleRLE("aabbbccddddee");
            //ExampleDeltaEncoding();
            //ExampleHuffmanCoding();
            //ExampleDeltaHuffmanCodingCombined();
            //ExampleLZ77();
            timer.Restart();
            ExampleLZWOptimized(false);
            timer.Stop();
            Console.WriteLine($"Elapsed Time: {timer.ElapsedMilliseconds} ms");
            timer.Restart();
            ExampleLZW(false);
            timer.Stop();
            Console.WriteLine($"Elapsed Time: {timer.ElapsedMilliseconds} ms");
        }

        static void compressFile(int bufferSize = 65536, string inputPath = "test.txt", string outputPath = "test_lzwO_compressed.bin")
        {
            using (FileStream fsWrite = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;
                    List<Task<byte[]>> taskList = [];
                    bool endOfReading = false;
                    bool endOfWriting = false;

                    Task.Run(() =>
                    {
                        while (!endOfReading || taskList.Count > 0)
                        {
                            if (taskList.Count == 0)
                            {
                                Thread.Sleep(TimeSpan.FromMilliseconds(10));
                                continue;
                            }
                            taskList.First().Wait();
                            var completedTask = taskList.First().Result;
                            fsWrite.Write(BitConverter.GetBytes((uint)completedTask.Length), 0, 4);
                            fsWrite.Write(completedTask, 0, completedTask.Length);
                            taskList.RemoveAt(0);
                            fsWrite.Flush();
                        }
                        fsWrite.Write(BitConverter.GetBytes((uint)0), 0, 4);
                        endOfWriting = true;
                    });
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] temp = new byte[bytesRead];
                        Array.Copy(buffer, 0, temp, 0, bytesRead);
                        var lzwO = new LZWOptimized();

                        taskList.Add(Task.Run(() =>
                        {
                            if (temp.Length == bufferSize)
                                return lzwO.Compress(temp);
                            else
                                return lzwO.Compress(temp[..temp.Length]);
                        }));
                    }
                    endOfReading = true;

                    fs.Close();
                    if (!endOfWriting)
                    {
                        while (!endOfWriting)
                            Thread.Sleep(1);
                    }
                }
                fsWrite.Close();
            }
        }
        static void decompressFile(int bufferSize = 65536, string inputPath = "test_lzwO_compressed.bin", string outputPath = "test_lzwO_DEcompressed.txt") 
        {
            using (FileStream fsWrite = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                {
                    byte[] chunkSizeBytes = [0, 0, 0, 0];
                    fs.ReadExactly(chunkSizeBytes, 0, 4);
                    byte[] chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
                    int bytesRead;
                    List<Task<byte[]>> taskList = [];
                    bool endOfReading = false;
                    bool endOfWriting = false;

                    Task.Run(() =>
                    {
                        while (!endOfReading || taskList.Count > 0)
                        {
                            if (taskList.Count == 0)
                            {
                                Thread.Sleep(TimeSpan.FromMilliseconds(10));
                                continue;
                            }
                            taskList.First().Wait();
                            var completedTask = taskList.First().Result;
                            fsWrite.Write(completedTask, 0, completedTask.Length);
                            taskList.RemoveAt(0);
                            fsWrite.Flush();
                        }
                        endOfWriting = true;
                    });
                    while ((bytesRead = fs.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        byte[] temp = new byte[bytesRead];
                        Array.Copy(chunk, 0, temp, 0, bytesRead);
                        var lzwO = new LZWOptimized();

                        taskList.Add(Task.Run(() =>
                        {
                            return lzwO.Decompress(temp);
                        }));

                        fs.ReadExactly(chunkSizeBytes, 0, 4);
                        chunk = new byte[BitConverter.ToUInt32(chunkSizeBytes)];
                    }
                    endOfReading = true;

                    fs.Close();
                    if (!endOfWriting)
                    {
                        while (!endOfWriting)
                            Thread.Sleep(1);
                    }
                }
                fsWrite.Close();
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
