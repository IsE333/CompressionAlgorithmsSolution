using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CompressionAlgorithms
{
    public class LZ77 : IAlgorithm
    {
        const int BUFFER_SIZE = 255;
        const int LEN_SIZE = 15;
        public byte[] Compress(byte[] data)
        {
            List<bool> compressed = [];

            List<byte> searchBuffer = new();
            int currentLen = 0;
            int currentPos = 0;
            string debug;
            byte[] debug2;
            byte[] debug3;
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < searchBuffer.Count; j++)
                {
                    if (j + currentLen == searchBuffer.Count)
                        break;
                    if (i + currentLen == data.Length)
                        break;
                    if (currentLen == LEN_SIZE)
                        break;
                    debug = Encoding.ASCII.GetString(data[i..(i + currentLen + 1)]);
                    debug3 = data[i..(i + currentLen + 1)];
                    if (searchBuffer[j..(j + currentLen + 1)].ToArray().SequenceEqual(data[i..(i + currentLen + 1)]))
                    {
                        debug2 = searchBuffer[j..(j + currentLen + 1)].ToArray();
                        currentLen++;
                        currentPos = searchBuffer.Count - j;
                        j--;
                    }
                }

                if (currentLen > 1)
                {
                    compressed.Add(false);
                    byte pos = (byte)currentPos;
                    byte len = (byte)currentLen; // 4 bit
                    BitArray bits = new BitArray(bytes: [pos, len]);
                    for (int b = 0; b < 12; b++)
                        compressed.Add(bits[b]);
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
                    if (i + j == data.Length) break;
                    searchBuffer.Add(data[i + j]);
                    if (searchBuffer.Count > BUFFER_SIZE)
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

            byte[] result = new byte[(paddingBits.Count + compressed.Count) / 8];
            BitArray bitArray = new(paddingBits.Concat(compressed).ToArray());
            bitArray.CopyTo(result, 0);
            return result;
        }

        public byte[] Decompress(byte[] compressedData)
        {
            List<byte> decompressed = [];
            List<byte> searchBuffer = new();
            List<char> searchBufferDebug = new();
            BitArray bitArray = new(compressedData);

            bool initialPaddingDone = false;

            int currentLen = 0;
            int currentPos = 0;

            string debug;
            byte[] debug2;
            byte[] debug3;

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
                    BitArray byteBits = new(8);
                    for (int j = 0; j < 8; j++)
                        byteBits[j] = bitArray[i + 1 + j];
                    var b = new byte[1];
                    byteBits.CopyTo(b, 0);
                    decompressed.Add(b[0]);
                    searchBuffer.Add(b[0]);
                    if (searchBuffer.Count > BUFFER_SIZE)
                        searchBuffer.RemoveAt(0);
                    i += 8;
                    continue;
                }
                i += 1;

                BitArray byteBits0 = new(8);
                for (int j = 0; j < 8; j++)
                    byteBits0[j] = bitArray[i + j];
                var b0 = new byte[1];
                byteBits0.CopyTo(b0, 0);
                currentPos = b0[0];
                i += 8;

                byteBits0 = new(8);
                for (int j = 0; j < 4; j++)
                    byteBits0[j] = bitArray[i + j];
                b0 = new byte[1];
                byteBits0.CopyTo(b0, 0);
                currentLen = b0[0];
                i += 4;

                int last = searchBuffer.Count;
                for (int j = 0; j < currentLen; j++)
                {
                    byte value = searchBuffer[last - currentPos + j];
                    decompressed.Add(value);
                    searchBuffer.Add(value);
                    searchBufferDebug.Add((char)value);
                    if (searchBuffer.Count > BUFFER_SIZE)
                    {
                        last--;
                        searchBuffer.RemoveAt(0);
                        searchBufferDebug.RemoveAt(0);
                    }
                }
                i--;
            }

            return [.. decompressed];
        }
    }
}
