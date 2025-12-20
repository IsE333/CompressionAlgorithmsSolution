using CompressionAlgorithms;
using System.Text;

namespace CompressConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            ExampleRLE("aabbbccddddee");
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
    }
}
