namespace CompressionAlgorithms
{
    public class RunLengthEncoding : IAlgorithm
    {
        public string AlgorithmName => "Run Length Encoding";

        public byte[] Compress(byte[] data, int dataSize)
        {
            var compressed = new List<byte>();
            byte prev = data[0];
            int count = 1;
            for (int i = 1; i <= dataSize; i++)
            {
                byte current = i == dataSize ? data[i - 1]: data[i];
                if (current != prev || i == dataSize) 
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
