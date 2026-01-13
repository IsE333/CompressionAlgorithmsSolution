namespace CompressionAlgorithms
{
    /// <summary>
    /// Lempel–Ziv–Welch (LZW) implementation with array hashing.
    /// </summary>
    public class LZWOptimized3 : IAlgorithm
    {
        const int BUFFER_LIMIT = 4095 - 255; // 12 bit
        const int HashSize = 4096 * 8;

        public string AlgorithmName => "LZW Optimized 3";

        public byte[] Compress(byte[] data, int dataSize)
        {
            int [] _hashPrefix = new int[HashSize];
            byte [] _hashNext = new byte[HashSize];
            int [] _hashCode = new int[HashSize];
            Array.Fill(_hashCode, -1);

            List<byte> compressed = [];
            int temp = -1;
            
            int codeCounter = 256;

            for (int i = 0; i < dataSize; i++)
            {
                bool found = false;
                int code = data[i];
                bool searchEnd = false;
                int x = 1;
                while (!searchEnd)
                {
                    if (i + x == dataSize)
                        break;
                    int idx = GetHash(code, data[i + x]);
                    while (true)
                    {
                        if (_hashCode[idx] == -1)
                        {
                            if (codeCounter < BUFFER_LIMIT)
                            {
                                _hashPrefix[idx] = code;
                                _hashNext[idx] = data[i + x];
                                _hashCode[idx] = codeCounter;
                                codeCounter++;
                            }
                            searchEnd = true;
                            break;
                        }
                        else if (_hashPrefix[idx] == code && _hashNext[idx] == data[i + x])
                        {
                            code = _hashCode[idx];
                            x++;
                            found = true;
                            break;
                        }
                        idx++;
                        if (idx == HashSize)
                            idx = 0;
                    }
                }
                i += x - 1;
                
                if (!found)
                    code = data[i];

                if (temp == -1)
                    temp = code;
                else
                {
                    write3Bytes(temp, code, compressed);
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

        int GetHash(int prefix, byte next)
        {
            return (prefix << 3) ^ next;
        }

        public byte[] Decompress(byte[] compressedData)
        {
            List<byte> decompressed = [];
            List<byte[]> searchBuffer = [];

            byte[] prevBytes = [];

            int[] temp = [-1,-1];
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