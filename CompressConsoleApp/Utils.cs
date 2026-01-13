using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressConsoleApp
{
    public class Utils
    {
        const int TRUNC_LENGTH = 72; // for print info
        static string TruncStr(string data, bool truncate)
        {
            if (!truncate)
                return data;
            if (data.Length <= TRUNC_LENGTH)
                return data;
            return $"{data[..TRUNC_LENGTH]} ... (total {data.Length})";
        }
        /// <summary>
        /// Special method to print compression info
        /// </summary>
        /// <param name="originalBytes">Data</param>
        /// <param name="compressedBytes">Compression result</param>
        /// <param name="decompressedBytes">Decompression result</param>
        /// <param name="showData">Disable all data</param>
        /// <param name="includeCompressedText">Disable compressed text only</param>
        /// <param name="includeBytes">Disable original and compressed bytes</param>
        /// <param name="includeDecompressedBytes">Disable Decompressed bytes only</param>
        /// <param name="trunc">Truncate long lines</param>
        public static void PrintInfo(byte[] originalBytes, byte[] compressedBytes, byte[] decompressedBytes, bool showData = true, bool includeCompressedText = true, bool includeBytes = true, bool includeDecompressedBytes = false, bool trunc = false)
        {
            Console.WriteLine("---------------------------------");
            if (showData)
            {
                if (includeCompressedText)
                    Console.WriteLine($"\n Compressed text:  {TruncStr(Encoding.UTF8.GetString(compressedBytes), trunc)}");
                if (includeBytes)
                {
                    Console.WriteLine();
                    Console.WriteLine($" Original bytes:     {TruncStr(BitConverter.ToString(originalBytes).Replace("-", " "), trunc)}");
                    Console.WriteLine();
                    Console.WriteLine($" Compressed bytes:   {TruncStr(BitConverter.ToString(compressedBytes).Replace("-", " "), trunc)}");
                }
                if (includeDecompressedBytes)
                    Console.WriteLine($"\n Decompressed bytes: {TruncStr(BitConverter.ToString(decompressedBytes).Replace("-", " "), trunc)}");
                Console.WriteLine();
                Console.WriteLine($" Decompressed text:  {TruncStr(Encoding.UTF8.GetString(decompressedBytes), trunc)}");
            }
            Console.WriteLine();
            Console.WriteLine($" Original Size: {originalBytes.Length} bytes");
            Console.WriteLine($" Compressed Size: {compressedBytes.Length} bytes");
            Console.WriteLine($" Compression ratio: {(double)originalBytes.Length / compressedBytes.Length:F2}");
            if (!decompressedBytes.SequenceEqual(originalBytes))
            {
                Console.WriteLine($" Output bytes DOES NOT match input");
                Console.WriteLine($" Decompressed Size: {decompressedBytes.Length} bytes");
            }
            Console.WriteLine("---------------------------------");
        }

        public static int SizeOfList(List<string> list)
        {
            int length = 0, size = 0;
            foreach (var str in list)
            {
                length += (str+",").Length;
                size += Encoding.UTF8.GetByteCount(str + ",");
            }
            if (length != size)
                Console.WriteLine($"Warning: Length ({length}) and UTF8 byte size ({size}) differ.");
            if (size > 0)
                size--;
            return size;
        }

        public static List<List<string>> CsvRead(string filePath)
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

        /// <summary>
        /// LHS, RHS, Leading white-space, Trailing white-space, Trailing zeros
        public static int[] IdentifyFormat(List<string> input)
        {
            int startWhiteSpace = input[0].Length - input[0].TrimStart().Length;
            int endWhiteSpace = input[0].Length - input[0].TrimEnd().Length;
            int[] format = [0, 0, startWhiteSpace, endWhiteSpace, 100];
            foreach (var value in input)
            {
                var parts = value.Split(".");

                if (parts[0].Length - startWhiteSpace > format[0])
                    format[0] = parts[0].Length - startWhiteSpace;

                if (parts.Length != 2) 
                    continue;
                if (parts[1].Length - endWhiteSpace > format[1])
                    format[1] = parts[1].Length - endWhiteSpace;

                if (parts[1].Length - endWhiteSpace < format[4])
                    format[4] = parts[1].Length - endWhiteSpace;
            }
            if (format[1] == 0)
                format[4] = 0;
            return format;
        }

        public static byte[] FormatAndConvertToBytes(List<string> input, int[] format)
        {
            int stepSize = format[0] + format[1];
            int lhs = format[0];
            int rhs = format[1];

            byte[] bytes = new byte[input.Count * stepSize];
            string value;
            for (int i = 0; i < input.Count; i++)
            {
                if (rhs == 0)
                    value = input[i].TrimStart().PadLeft(lhs, '0');
                else
                    value = input[i].Split(".")[0].TrimStart().PadLeft(lhs, '0') + input[i].Split(".")[1].TrimEnd().PadRight(rhs, '0');

                for (int j = 0; j < value.Length; j++)
                    bytes[i * stepSize + j] = (byte)value[j];
            }
            return bytes;
        }
    }
}
