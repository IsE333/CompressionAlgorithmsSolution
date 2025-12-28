using CompressionAlgorithms;
using System.Text;

namespace CompressConsoleApp
{
    internal class Program
    {
        static void Main()
        {
            //ExampleRLE("aabbbccddddee");
            //ExampleDeltaEncoding();
            //ExampleHuffmanCoding();
            //ExampleDeltaHuffmanCodingCombined();
            ExampleLZ77();
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
            var inputBytes = Encoding.UTF8.GetBytes("3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930381964428810975665933446128475648233786783165271201909145648566");
            var compressed = lz77.Compress(inputBytes);
            var decompressed = lz77.Decompress(compressed);
            Utils.PrintInfo(inputBytes, compressed, decompressed, includeCompressedText: false);
        }
    }
}
