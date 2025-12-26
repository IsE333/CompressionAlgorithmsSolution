using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressionAlgorithms
{
    public class RunLengthEncoding : IAlgorithm
    {
        public byte[] Compress(byte[] data)
        {
            var compressed = new List<byte>();
            byte prev = data[0];
            int count = 1;
            for (int i = 1; i <= data.Length; i++)
            {
                byte current = i == data.Length ? data[i - 1]: data[i];
                if (current != prev || i == data.Length) 
                {
                    compressed.Add((byte)count);
                    compressed.Add(prev);
                    count = 1;
                } else {                     
                    count++;
                }
                prev = current;
            }
            return [.. compressed];
        }

        public byte[] Decompress(byte[] compressedData)
        {
            var decompressed = new List<byte>();
            for (int i = 0; i < compressedData.Length; i += 2)
            {
                byte count = compressedData[i];
                byte value = compressedData[i + 1];
                decompressed.AddRange(Enumerable.Repeat(value, count));
            }
            return [.. decompressed];
        }
    }
}
