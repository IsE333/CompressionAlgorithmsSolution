using CompressionAlgorithms;
using CompressionAlgorithms.Common;
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
            var a = new HuffmanCoding();
            timer.Restart();
            FileUtility.CompressFile(a.Compress, null, bufferSize).Wait();
            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            Console.WriteLine("-------------");
            timer.Restart();
            FileUtility.DecompressFile(a.Decompress, null).Wait();
            timer.Stop();

            Console.WriteLine($"FILE {a.AlgorithmName} Compression: {elapsedMs} ms");
            Console.WriteLine($"FILE {a.AlgorithmName} Decompression: {timer.ElapsedMilliseconds} ms");

            // check files are identical
            var originalBytes = File.ReadAllBytes("test.txt");
            var decompressedBytes = File.ReadAllBytes("test_decompressed.txt");
            Console.WriteLine($"Files are identical: {originalBytes.SequenceEqual(decompressedBytes)}");

            //ExampleRLE("aabbbccddddee");
            ExampleDeltaEncoding();
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
        static void ExampleRLE(string text)
        {
            Console.WriteLine("----------------------RunLengthEncoding----------------------");
            var input = Encoding.UTF8.GetBytes(text);
            var rle = new RunLengthEncoding();

            var compressed = rle.Compress(input, input.Length);
            var decompressed = rle.Decompress(compressed);

            Utils.PrintInfo(input, compressed, decompressed, includeCompressedText: false);

        }

        static void ExampleDeltaEncoding()
        {
            Console.WriteLine("------------------------DeltaEncoding------------------------");
            List<string> input = Utils.CsvRead("test2.csv")[2]; // Only third column
            input.RemoveAt(0); // Remove column name
            int[] format = Utils.IdentifyFormat(input);
            byte[] inputBytes = Utils.FormatAndConvertToBytes(input, format);
            
            DeltaEncoding delta = new(format);

            byte[] resultCompress = delta.Compress(inputBytes, inputBytes.Length);
            byte[] resultDecompress = delta.Decompress(resultCompress);

            int stepSize = format[0] + format[1];
            int count = resultDecompress.Length / stepSize;
            string[] outputStrings = new string[count];
            for (int i = 0; i < count; i++)
                outputStrings[i] = Encoding.UTF8.GetString(resultDecompress[(i * stepSize)..(i * stepSize + stepSize)]);

            string[] output = new string[count];
            for (int i = 0; i < count; i++)
            {
                string value;
                if (format[1] == 0)
                    value = outputStrings[i].TrimStart('0');
                else
                {
                    string lhs = outputStrings[i][..format[0]].TrimStart('0');
                    string rhs = outputStrings[i][format[0]..].TrimEnd('0');
                    value = "".PadLeft(format[2], ' ') + lhs.PadLeft(1, '0') + "." + rhs.PadRight(format[4], '0') + "".PadRight(format[3], ' ');
                }
                
                if (value.EndsWith('.')) 
                    value += "0";

                output[i] = value;
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
            var compressed = huffman.Compress(inputBytes, inputBytes.Length);
            var decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed);
        }

        static void ExampleDeltaHuffmanCodingCombined()
        {
            List<string> input = Utils.CsvRead("test1.csv")[2];
            input.RemoveAt(0); // Remove column name
            int[] format = Utils.IdentifyFormat(input);
            byte[] inputBytes = Utils.FormatAndConvertToBytes(input, format);

            DeltaEncoding delta = new(format);
            byte[] resultCompress = delta.Compress(inputBytes, inputBytes.Length);
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
            var compressed = huffman.Compress(inputBytes3, inputBytes3.Length);
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
            compressed = huffman.Compress(inputBytes2, inputBytes2.Length);
            decompressed = huffman.Decompress(compressed);
            Utils.PrintInfo(inputBytes2, compressed, decompressed, false);
            Utils.PrintInfo(inputBytes, compressed, decompressed, false);
        }

        static void ExampleLZ77()
        {
            Console.WriteLine("-----------------------LempelZiv(LZ77)-----------------------");
            var lz77 = new LZ77();
            var inputBytes = Encoding.UTF8.GetBytes("uzum uzume baka baka kararir");
            var compressed = lz77.Compress(inputBytes, inputBytes.Length);
            var decompressed = lz77.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false);
        }

        static void ExampleLZW(bool showData)
        {
            Console.WriteLine("-----------------------LempelZiv(LZW)-----------------------");
            var lzw = new LZW();
            var inputBytes = Encoding.UTF8.GetBytes("üzüm üzüme baka baka kararır");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var compressed = lzw.Compress(inputBytes, inputBytes.Length);
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
            var compressed = lzw.Compress(inputBytes, inputBytes.Length);
            timer.Stop();
            var decompressed = lzw.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false, showData: showData);
            Console.WriteLine($"Compression took: {timer.ElapsedMilliseconds} ms");
        }
    }
}
