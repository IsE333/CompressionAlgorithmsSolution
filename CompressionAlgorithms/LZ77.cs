using System.Collections;

namespace CompressionAlgorithms
{
    public class LZ77 : IAlgorithm
    {
        const int BUFFER_LIMIT = 4095; // 12 bit
        const int LEN_LIMIT = 63; // 6 bit
        const int BUFFER_SIZE = 255; // 8 bit
        const int LEN_SIZE = 15; // 4 bit
        public string AlgorithmName => "LZ77";

        public byte[] Compress(byte[] data, int dataSize)
        {
            List<bool> compressed = [];
            List<byte> searchBuffer = [];
            int currentLen = 0;
            int currentPos = 0;
            for (int i = 0; i < dataSize; i++)
            {
                for (int j = searchBuffer.Count - 1; j >= 0; j--)
                {
                    if (j + currentLen == searchBuffer.Count)
                        break;
                    if (i + currentLen == dataSize)
                        break;
                    if (currentLen == LEN_LIMIT)
                        break;
                    /*if (searchBuffer[j..(j + currentLen + 1)].ToArray().SequenceEqual(data[i..(i + currentLen + 1)]))
                    {
                        currentLen++;
                        currentPos = searchBuffer.Count - j;
                        j++;
                    }*/
                    bool match = true;
                    for (int k = 0; k < currentLen + 1; k++)
                    {
                        if (j + k == searchBuffer.Count)
                            break;
                        if (data[i + k] != searchBuffer[j + k])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        currentLen++;
                        currentPos = searchBuffer.Count - j;
                        j++;
                    }
                }

                if (currentLen > 1)
                {
                    compressed.Add(false);
                    if (currentLen > LEN_SIZE || currentPos > BUFFER_SIZE)
                    {
                        compressed.Add(true);
                        byte[] pos = BitConverter.GetBytes(currentPos); // 12 bit
                        byte len = (byte)currentLen; // 6 bit
                        BitArray bits = new(bytes: [pos[0], pos[1], len]);
                        for (int b = 0; b < 22; b++)
                        {
                            if (b == 12) b = 16; // skip 4 bits
                            compressed.Add(bits[b]);
                        }
                    } 
                    else 
                    {
                        compressed.Add(false);
                        byte pos = (byte)currentPos; // 8 bit
                        byte len = (byte)currentLen; // 4 bit
                        BitArray bits = new(bytes: [pos, len]);
                        for (int b = 0; b < 12; b++)
                            compressed.Add(bits[b]);
                    }
                }
                else
                {
                    compressed.Add(true);
                    BitArray value = new BitArray(bytes: [data[i]]);
                    foreach (bool bit in value)
                        compressed.Add(bit);
                    currentLen = 1;
                }

                for (int j = 0; j < currentLen; j++)
                {
                    if (i + j == dataSize) break;
                    searchBuffer.Add(data[i + j]);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                        searchBuffer.RemoveAt(0);
                }

                i += currentLen - 1;
                currentLen = 0;
                currentPos = 0;
            }

            int size = compressed.Count + 1;
            List<bool> paddingBits = [];
            for (int i = 0; i < 8 - size % 8; i++)
                paddingBits.Add(false);
            paddingBits.Add(true);

            Console.WriteLine($"Padding Size:       {paddingBits.Count} bits");

            byte[] result = new byte[(paddingBits.Count + compressed.Count) / 8];
            BitArray bitArray = new(paddingBits.Concat(compressed).ToArray());
            bitArray.CopyTo(result, 0);
            return result;
        }

        public byte[] Decompress(byte[] compressedData)
        {
            List<byte> decompressed = [];
            List<byte> searchBuffer = [];
            List<char> searchBufferDebug = [];
            BitArray bitArray = new(compressedData);

            bool initialPaddingDone = false;
            int currentLen;
            int currentPos;

            for (int i = 0; i < bitArray.Length; i++)
            {
                // Remove initial Padding
                if (!initialPaddingDone)
                {
                    if (bitArray[i] == true)
                        initialPaddingDone = true;
                    continue;
                }

                if (bitArray[i])
                {
                    byte b = GetBytes(bitArray, i + 1, 8)[0];
                    decompressed.Add(b);
                    searchBuffer.Add(b);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                        searchBuffer.RemoveAt(0);
                    i += 8;
                    continue;
                }
                i++;

                if (bitArray[i])
                {
                    i++;
                    currentPos = BitConverter.ToInt16(GetBytes(bitArray, i, 12, 0), 0);
                    i += 12;
                    currentLen = GetBytes(bitArray, i, 6)[0];
                    i += 6;
                }
                else
                {
                    i++;
                    currentPos = GetBytes(bitArray, i, 8)[0];
                    i += 8;
                    currentLen = GetBytes(bitArray, i, 4)[0];
                    i += 4;
                }


                int cursor = searchBuffer.Count;
                for (int j = 0; j < currentLen; j++)
                {
                    byte value = searchBuffer[cursor - currentPos + j];
                    decompressed.Add(value);
                    searchBuffer.Add(value);
                    searchBufferDebug.Add((char)value);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                    {
                        searchBuffer.RemoveAt(0);
                        searchBufferDebug.RemoveAt(0);
                        cursor--;
                    }
                }
                i--;
            }
            return [.. decompressed];
        }

        byte[] GetBytes(BitArray bitArray, int start, int len = 8, int padding = 0)
        {
            BitArray buffer = new(padding + len);
            for (int i = 0; i < padding; i++)
                buffer[i] = false;
            for (int j = 0; j < len; j++)
                buffer[padding + j] = bitArray[start + j];
            var bytes = new byte[(int)Math.Ceiling((padding + len)/8.0)];
            buffer.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
