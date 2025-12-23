using CompressionAlgorithms;
using System.Text;

namespace CompressConsoleApp
{
    internal class Program
    {
        static void Main()
        {

            ExampleDeltaEncoding();
            //ExampleRLE("aabbbccddddee");
        }

        static List<List<string>> CsvRead(string filePath)
        {
            using var sr = new StreamReader(filePath);
            var parser = new SmallestCSV.SmallestCSVParser(sr);
            List<string>? row = parser.ReadNextRow(removeEnclosingQuotes: false);
            if (row == null)
                throw new Exception("CSV file is empty");

            List<List<string>> columns = [];
            for (int i = 0; i < row.Count; i++) // Initialize lists for each column
                columns.Add([]);

            while (true)
            {
                if (row == null)
                    break;

                for (int i = 0; i < row.Count; i++)
                    columns[i].Add(row[i]);

                row = parser.ReadNextRow(removeEnclosingQuotes: false);
            }
            return columns;
        }

        static void ExampleRLE(string text)
        {
            Console.WriteLine("Run Length Encoding!");
            var input = Encoding.UTF8.GetBytes(text);
            var rle = new RunLengthEncoding();
            var compressed = rle.Compress(input);

            Console.WriteLine($"Original text:      {text}");
            Console.WriteLine($"Original bytes:     {BitConverter.ToString(input).Replace("-", " ")}");
            Console.WriteLine($"Compressed bytes:   {BitConverter.ToString(compressed).Replace("-", " ")}");

            var decompressed = rle.Decompress(compressed);
            var decompressedText = Encoding.UTF8.GetString(decompressed);
            Console.WriteLine();
            Console.WriteLine($"Decompressed bytes: {BitConverter.ToString(decompressed).Replace("-", " ")}");
            Console.WriteLine($"Decompressed text:  {decompressedText}");
        }

        static void ExampleDeltaEncoding()
        {
            List<string> input = CsvRead("test1.csv")[2];
            Console.WriteLine($"Original size: {Utils.SizeOfList(input)} bytes");

            List<byte> inputBytes = [];
            for (int i = 1; i < input.Count; i++)
            {
                var value = input[i].Split(".")[0].PadLeft(2, '0') + "." + input[i].Split(".")[1].PadRight(3, '0');
                inputBytes.AddRange(Encoding.UTF8.GetBytes(value));
            }

            DeltaEncoding delta = new(lengthLHS: 2, lengthRHS: 3);

            byte[] resultCompress = delta.Compress([.. inputBytes]);
            Console.WriteLine($"Delta Encoded result: {Encoding.UTF8.GetString(resultCompress)}");
            Console.WriteLine($"Delta Encoded size: {resultCompress.Length} bytes");
            Console.WriteLine("\n\n\n\n");

            byte[] resultDecompress = delta.Decompress(resultCompress);
            Console.WriteLine($"Delta Decoded result: {Encoding.UTF8.GetString(resultDecompress)}");
            Console.WriteLine($"Delta Decoded size: {resultDecompress.Length} bytes");

            List<string> outputStrings = [];
            for (int i = 0; i < resultDecompress.Length / 6; i++)
            {
                outputStrings.Add(Encoding.UTF8.GetString(resultDecompress[(i * 6)..(i * 6 + 6)]));
            }

            List<string> output = [];
            for (int i = 0; i < outputStrings.Count; i++)
            {
                string value = outputStrings[i].Split(".")[0].TrimStart('0') + "." + outputStrings[i].Split(".")[1].TrimEnd('0');
                if (value.EndsWith('.')) value += "0";
                output.Add(value);
                if (input[i + 1] != output[i])
                    Console.WriteLine($"Original: {input[i + 1]}  Output: {output[i]}");
            }

            Console.WriteLine($"\nOriginal size: {Utils.SizeOfList(input)} bytes");
            Console.WriteLine($"Input bytes size: {inputBytes.Count} bytes");
            Console.WriteLine($"Delta Encoded size: {resultCompress.Length} bytes");
            Console.WriteLine($"Delta Decoded size: {resultDecompress.Length} bytes");
            Console.WriteLine($"Output bytes size: {Utils.SizeOfList(output)} bytes");

            Console.WriteLine($"\nCompression ratio: {(double)Utils.SizeOfList(input) / resultCompress.Length:F2}");
            Console.WriteLine($"Output bytes match input: {resultDecompress.SequenceEqual(inputBytes)}");
            Console.WriteLine($"Output strings match input: {output.SequenceEqual(input[1..])}");
        }
    }
}
