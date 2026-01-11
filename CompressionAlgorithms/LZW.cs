using System.Collections;

namespace CompressionAlgorithms
{
    public class LZW : IAlgorithm
    {
        const int BUFFER_LIMIT = 4095-255; // 12 bit

        public string AlgorithmName => "LZW";

        public byte[] Compress(byte[] data, int dataSize)
        {
            List<bool> compressed = [];
            List<byte[]> searchBuffer = [];
            int currentPos = -1;
            byte[] prevBytes = [];
            for (int i = 0; i < dataSize; i++)
            {
                int lenOfEntry = -1;
                for (int j = searchBuffer.Count - 1; j >= 0; j--)
                {
                    if (i + searchBuffer[j].Length > dataSize)
                        continue;
                    if (searchBuffer[j].Length <= lenOfEntry)
                        continue;
                    /*if (searchBuffer[j].SequenceEqual(data[i..(i + searchBuffer[j].Length)]))
                    {
                        currentPos = j;
                        lenOfEntry = searchBuffer[j].Length;
                        break;
                    }*/
                    for (int k = 0; k < searchBuffer[j].Length; k++)
                    {
                        if (i + k >= data.Length)
                            continue;
                        if (data[i + k] != searchBuffer[j][k])
                            break;
                        if (k == searchBuffer[j].Length - 1)
                        {
                            currentPos = j;
                            lenOfEntry = searchBuffer[j].Length;
                        }
                    }
                }

                int currentByte = currentPos == -1 ? data[i] : 256 + currentPos; // skip first 255
                BitArray currentValue = new([currentByte]);
                for (int k = 0; k < 12; k++) // 12 bit
                    compressed.Add(currentValue[k]);

                byte[] currentBytes = currentPos == -1 ? [data[i]] : searchBuffer[currentPos];
                if (prevBytes.Length != 0)
                {
                    byte[] newEntry = [.. prevBytes, currentBytes[0]];
                    searchBuffer.Add(newEntry);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                        searchBuffer.RemoveAt(0);
                    i += currentBytes.Length - 1;
                }
                prevBytes = currentBytes;


                currentPos = -1;
            }

            int size = compressed.Count + 1;
            List<bool> paddingBits = [];
            for (int i = 0; i < 8 - size % 8; i++)
                paddingBits.Add(false);
            paddingBits.Add(true);

            Console.WriteLine($"Padding Size:       {paddingBits.Count} bits");

            byte[] result = new byte[(paddingBits.Count + compressed.Count) / 8];
            BitArray bitArray = new([..paddingBits.Concat(compressed)]);
            bitArray.CopyTo(result, 0);
            return result;
        }

        public byte[] Decompress(byte[] compressedData)
        {
            BitArray bitArray = new(compressedData);
            List<byte> decompressed = [];
            List<byte[]> searchBuffer = [];

            bool initialPaddingDone = false;
            byte[] prevBytes = [];

            for (int i = 0; i < bitArray.Length; i++)
            {
                // Remove initial Padding
                if (!initialPaddingDone)
                {
                    if (bitArray[i] == true)
                        initialPaddingDone = true;
                    continue;
                }

                Int16 currentByte = BitConverter.ToInt16(ExtractBits(bitArray, i, 12, 0), 0);
                byte[] currentBytes = currentByte < 256 ? [ (byte)currentByte ] : searchBuffer[currentByte - 256];
                decompressed.AddRange(currentBytes);

                if (prevBytes.Length != 0)
                {
                    searchBuffer.Add([..prevBytes, currentBytes[0]]);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                        searchBuffer.RemoveAt(0);
                }
                prevBytes = currentBytes;
                i += 11;
            }
            return [.. decompressed];
        }

        byte[] ExtractBits(BitArray bitArray, int start, int len = 8, int padding = 0)
        {
            BitArray buffer = new(padding + len);
            for (int i = 0; i < padding; i++)
                buffer[i] = false;
            for (int j = 0; j < len; j++)
                buffer[padding + j] = bitArray[start + j];
            var bytes = new byte[(int)Math.Ceiling((padding + len) / 8.0)];
            buffer.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
