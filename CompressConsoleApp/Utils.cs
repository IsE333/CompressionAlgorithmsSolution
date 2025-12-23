using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressConsoleApp
{
    public class Utils
    {
        public static int SizeOfList(List<string> list)
        {
            int length = 0, size = 0;
            foreach (var str in list)
            {
                length += str.Length;
                size += Encoding.UTF8.GetByteCount(str);
            }
            if (length != size)
                Console.WriteLine($"Warning: Length ({length}) and UTF8 byte size ({size}) differ.");
            
            return size;
        }
    }
}
