using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CompressionAlgorithms
{
    public class LZWOptimized : IAlgorithm
    {
        const int BUFFER_LIMIT = 4095 - 255; // 12 bit
        Dictionary<int, List<int>> hashtable = [];

        public byte[] Compress(byte[] data)
        {
            hashtable = [];
            List<bool> compressed = [];
            List<byte[]> searchBuffer = [];
            int bufferOffset = 0;

            int currentPos = -1;
            byte[] prevBytes = [];
            for (int i = 0; i < data.Length; i++)
            {
                int[] hashes = GetSearchHashArray(data, i);
                foreach (int hash in hashes)
                {
                    if (hashtable.TryGetValue(hash, out List<int>? list))
                    {
                        int lenOfEntry = -1;
                        List<int> toRemove = [];
                        foreach (int index in list)
                        {
                            if (index < bufferOffset)
                            {
                                toRemove.Add(index);
                                continue;
                            }
                            byte[] entry = searchBuffer[index - bufferOffset];
                            if (entry.Length <= lenOfEntry)
                                continue;
                            if (i + entry.Length > data.Length)
                                continue;
                            if (entry.SequenceEqual(data[i..(i + entry.Length)]))
                            {
                                currentPos = index - bufferOffset;
                                lenOfEntry = entry.Length;
                                //break;
                            }
                        }
                        foreach (int rem in toRemove)
                            list.Remove(rem);
                    }
                    if (currentPos != -1)
                        break;
                }
                /*
                for (int j = searchBuffer.Count - 1; j >= 0; j--)
                {
                    if (i + searchBuffer[j].Length >= data.Length)
                        continue;
                    if (searchBuffer[j].SequenceEqual(data[i..(i + searchBuffer[j].Length)]))
                    {
                        currentPos = j;
                        break;
                    }
                }*/

                int currentByte = currentPos == -1 ? data[i] : 256 + currentPos; // skip first 255
                BitArray currentValue = new([currentByte]);
                for (int k = 0; k < 12; k++) // 12 bit
                    compressed.Add(currentValue[k]);

                byte[] currentBytes = currentPos == -1 ? [data[i]] : searchBuffer[currentPos];
                if (prevBytes.Length != 0)
                {
                    byte[] newEntry = [.. prevBytes, currentBytes[0]];
                    AddToHashTable(GetHash(newEntry), bufferOffset + searchBuffer.Count);
                    searchBuffer.Add(newEntry);
                    if (searchBuffer.Count > BUFFER_LIMIT)
                    {
                        searchBuffer.RemoveAt(0);
                        bufferOffset++; // for hash table
                    }
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

            //Console.WriteLine($"Padding Size:       {paddingBits.Count} bits");

            byte[] result = new byte[(paddingBits.Count + compressed.Count) / 8];
            BitArray bitArray = new([.. paddingBits.Concat(compressed)]);
            bitArray.CopyTo(result, 0);
            return result;
        }

        int GetHash(byte[] arr) // first 3 bytes are used
        {
            int hash = 0;
            for (int i = 0; i < Math.Min(arr.Length, 3); i++)
                hash = (hash << 8) | arr[i];
            return hash;
        }

        int[] GetSearchHashArray(byte[] data, int pos)
        {
            List<int> hashes = [];
            for (int len = 3; len > 0; len--)
            {
                if (pos + len > data.Length)
                    continue;
                byte[] subArray = data[pos..(pos + len)];
                hashes.Add(GetHash(subArray));
            }
            return [.. hashes];
        }

        void AddToHashTable(int hash, int value)
        {
            if (hashtable.TryGetValue(hash, out List<int>? list))
                list.Add(value);
            else
                hashtable.Add(hash, [value]);
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
                byte[] currentBytes = currentByte < 256 ? [(byte)currentByte] : searchBuffer[currentByte - 256];
                decompressed.AddRange(currentBytes);

                if (prevBytes.Length != 0)
                {
                    searchBuffer.Add([.. prevBytes, currentBytes[0]]);
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