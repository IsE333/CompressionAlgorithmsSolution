using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressionAlgorithms
{
    public class DeltaEncoding : IAlgorithm
    {
        readonly int stepSize = 1;
        readonly int lenLHS = 1;
        readonly int lenRHS = 0;
        public DeltaEncoding(int lengthLHS = 1, int lengthRHS = 0)
        {
            stepSize = lengthRHS == 0 ? lengthLHS : lengthLHS + lengthRHS + 1;
            lenLHS = lengthLHS;
            lenRHS = lengthRHS;
        }

        public byte[] Compress(byte[] data)
        {
            List<byte[]> compressedData = [];
            if (data.Length == 0) return [];

            compressedData.Add(data[0..stepSize]);

            for (int i = stepSize; i < data.Length; i+= stepSize)
                compressedData.Add(SubstractStrings(data[i..(i + stepSize)], data[(i - stepSize)..i] ));
            
            return [.. compressedData.SelectMany(b => b)];
        }

        public byte[] Decompress(byte[] compressedData)
        {
            List<byte[]> decompressedData = [];
            
            bool isFirst = true;
            byte[] current = [];
            byte[] diff = [];

            for (int i= 0; i<compressedData.Length; i++)
            {
                if (isFirst)
                {
                    if (compressedData[i] == 43 || compressedData[i] == 45) // + or -
                        isFirst = false;
                    else
                    {
                        current = [.. current, compressedData[i]];
                        continue;
                    }
                }

                if (compressedData[i] == 43 || compressedData[i] == 45) // + or - 
                {
                    decompressedData.Add(AddStrings(current,diff));
                    current = decompressedData.Last();
                    diff = [];
                } 
                diff = [.. diff, compressedData[i]];
            }
            decompressedData.Add(AddStrings(current, diff));
            return [.. decompressedData.SelectMany(b => b)];
        }

        // 0 -> 00.000
        // TODO negative values
        public byte[] IntToByte(int value) 
        {
            string str = value.ToString();
            str = str.PadLeft(stepSize - 1, '0');
            str = str.Insert(lenLHS, ".");
            return Encoding.UTF8.GetBytes(str);
        }

        // 00.000 -> 0
        // 00 -> 0
        int ByteToInt(byte[] data)
        {
            if (lenLHS == stepSize) return int.Parse(Encoding.UTF8.GetString(data[..lenLHS]));
            return int.Parse(Encoding.UTF8.GetString(data[..lenLHS]) + Encoding.UTF8.GetString(data[(lenLHS + 1)..]));
        }

        // 10.000 - 09.000 = +1.000
        // 09.000 - 10.000 = -1.000
        public byte[] SubstractStrings(byte[] arrA, byte[] arrB)
        {
            int a = ByteToInt(arrA);
            int b = ByteToInt(arrB);
            int c = a - b;
            string res = c < 0 ? c.ToString() : "+" + c.ToString();
            return Encoding.UTF8.GetBytes(res);
        }

        // 09.000 + 1.000 = 10.000
        // 10.000 + [] = 10.000
        public byte[] AddStrings(byte[] arrA, byte[] arrB)
        {
            int a = ByteToInt(arrA);
            int b = arrB.Length == 0 ? 0 : int.Parse(Encoding.UTF8.GetString(arrB));
            int c = a + b;
            return IntToByte(c);
        }
    }
}
