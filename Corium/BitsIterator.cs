using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Corium
{
    class BitsIterator
    {
        public static IEnumerable<int> FromBuffer(byte[] buffer, int start, int length)
        {
            for(int i = start; i < start+length; i++)
            {
                for(int index = 0; index < 8; index++)
                {
                    yield return (buffer[i] & (1 << index)) >> index;
                }
            }
        }
        public static IEnumerable<int> fromStream(Stream stream)
        {
            int read = 0;
            long available = stream.Length;
            while(read < available)
            {
                int b = stream.ReadByte();
                read++;
                for (int index = 0; index < 8; index++)
                {
                    yield return (b & (1 << index)) >> index;
                }
                
            }
        }
    }
}
