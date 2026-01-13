using System.Collections;

namespace CompressionAlgorithms
{
    /// <summary>
    /// Lempel–Ziv–Welch (LZW) implementation with dictionary hashing. The search buffer is constant after full.
    /// </summary>
    public class LZWOptimized2 : IAlgorithm
    {
        const int BUFFER_LIMIT = 4095 - 255; // 12 bit

        public string AlgorithmName => "LZW Optimized 2";

        public byte[] Compress(byte[] data, int dataSize)
        {
            Dictionary<int, List<int>> hashtable = [];
            List<byte> compressed = [];
            List<byte[]> searchBuffer = [];

            int temp = -1;
            int currentPos = -1;
            byte[] prevBytes = [];
            for (int i = 0; i < dataSize; i++)
            {
                int[] hashes = GetSearchHashArray(data, dataSize, i);
                foreach (int hash in hashes)
                {
                    if (hashtable.TryGetValue(hash, out List<int>? list))
                    {
                        int lenOfEntry = -1;
                        foreach (int index in list)
                        {
                            byte[] entry = searchBuffer[index];
                            if (entry.Length <= lenOfEntry)
                                continue;
                            if (i + entry.Length > dataSize)
                                continue;
                            if (entry.SequenceEqual(data[i..(i + entry.Length)]))
                            {
                                currentPos = index;
                                lenOfEntry = entry.Length;
                            }
                        }
                    }
                    if (currentPos != -1)
                        break;
                }

                int currentByte = currentPos == -1 ? data[i] : 256 + currentPos; // skip first 255
                byte[] currentBytes = currentPos == -1 ? [data[i]] : searchBuffer[currentPos];
                if (searchBuffer.Count < BUFFER_LIMIT && prevBytes.Length != 0)
                {
                    byte[] newEntry = [.. prevBytes, currentBytes[0]];

                    //AddToHashTable(GetHash(newEntry), bufferOffset + searchBuffer.Count);
                    if (hashtable.TryGetValue(GetHash(newEntry), out List<int>? list))
                        list.Add(searchBuffer.Count);
                    else
                        hashtable.Add(GetHash(newEntry), [searchBuffer.Count]);

                    searchBuffer.Add(newEntry);
                }
                i += currentBytes.Length - 1;
                prevBytes = currentBytes;
                currentPos = -1;

                if (temp == -1)
                    temp = currentByte;
                else
                {
                    write3Bytes(temp, currentByte, compressed);
                    temp = -1;
                }
            }

            if (temp != -1)
                write3Bytes(temp, -1, compressed);

            return [.. compressed];
        }
        void write3Bytes(int code1, int code2, List<byte> compressed)
        {
            bool emptyCode2 = code2 == -1;
            if (emptyCode2)
                code2 = 0;
            int combined = (code1 << 12) | code2;
            byte byte1 = (byte)(combined >> 16);
            byte byte2 = (byte)(combined >> 8);
            byte byte3 = (byte)combined;
            compressed.Add(byte1);
            compressed.Add(byte2);
            if (!emptyCode2)
                compressed.Add(byte3);
        }

        int GetHash(byte[] arr) // first 3 bytes are used
        {
            int hash = 0;
            for (int i = 0; i < Math.Min(arr.Length, 3); i++)
                hash = (hash << 8) | arr[i];
            return hash;
        }

        int[] GetSearchHashArray(byte[] data, int dataSize, int pos)
        {
            List<int> hashes = [];
            for (int len = 3; len > 0; len--)
            {
                if (pos + len > dataSize)
                    continue;
                byte[] subArray = data[pos..(pos + len)];
                hashes.Add(GetHash(subArray));
            }
            return [.. hashes];
        }

        public byte[] Decompress(byte[] compressedData)
        {
            BitArray bitArray = new(compressedData);
            List<byte> decompressed = [];
            List<byte[]> searchBuffer = [];

            byte[] prevBytes = [];
            int[] temp = [-1, -1];
            int tempIdx = 2;

            for (int i = 0; i < compressedData.Length; i++)
            {
                if (tempIdx == 2)
                {
                    temp[0] = compressedData[i] << 4 ^ (compressedData[i + 1] >> 4);
                    if (i + 2 < compressedData.Length)
                        temp[1] = (compressedData[i + 1] << 8 & 3840) ^ compressedData[i + 2];
                    i++;
                    tempIdx = 0;
                }
                int currentByte = temp[tempIdx];

                byte[] currentBytes = currentByte < 256 ? [(byte)currentByte] : currentByte - 256 == searchBuffer.Count ? [..prevBytes, prevBytes[0]] : searchBuffer[currentByte - 256];
                //byte[] currentBytes = currentByte < 256 ? [(byte)currentByte] : searchBuffer[currentByte - 256];
                decompressed.AddRange(currentBytes);

                if (searchBuffer.Count < BUFFER_LIMIT && prevBytes.Length != 0)
                    searchBuffer.Add([.. prevBytes, currentBytes[0]]);

                prevBytes = currentBytes;
                tempIdx++;
            }
            return [.. decompressed];
        }
    }
}