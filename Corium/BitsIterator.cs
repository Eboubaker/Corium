using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Corium
{
    class BitsIterator
    {

        private static int BitBuffer;
        private static int BitBufferOffset;
        /// <summary>
        /// returns bits from a buffer if no more data is available return random generated bits
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerator<int> FromBuffer(byte[] buffer, int start, int length)
        {
            if(BitBufferOffset >= 0)
            {
                for (int index = BitBufferOffset; index >= 0; index--)
                {
                    yield return (BitBuffer & (1 << index)) >> index;
                }
            }
            for(int i = start; i < start+length; i++)
            {
                BitBuffer = buffer[i];
                BitBufferOffset = 7;
                for (int index = 7; index >= 0; index--)
                {
                    yield return (BitBuffer & (1 << index)) >> index;
                    BitBufferOffset--;
                }
            }
            Random random = new Random();
            while (true)
            {
                yield return random.Next() & 1;
            }
        }
        public static IEnumerator<int> fromStream(Stream stream)
        {
            if (BitBufferOffset >= 0)
            {
                for (int index = BitBufferOffset; index >= 0; index--)
                {
                    yield return (BitBuffer & (1 << index)) >> index;
                }
            }
            int read = 0;
            long available = stream.Length;
            while(read < available)
            {
                BitBuffer = stream.ReadByte();
                BitBufferOffset = 7;
                read++;
                for (int index = 7; index >= 0; index--)
                {
                    yield return (BitBuffer & (1 << index)) >> index;
                }
                
            }
            Random random = new Random();
            while (true)
            {
                yield return random.Next() & 1;
            }
        }
        public static IEnumerator<int> fromImageChannels(Bitmap image)
        {

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color Pixel = image.GetPixel(x, y);
                    byte[] channels = new byte[ProgramOptions.ChannelCount];
                    channels[0] = Pixel.R;
                    channels[1] = Pixel.G;
                    channels[2] = Pixel.B;
                    if (ProgramOptions.Alpha)
                        channels[3] = Pixel.A;
                    for (int i = 0; i < channels.Length; i++)
                    {
                        for (int index = 0; index < 8; index++)
                        {
                            yield return (channels[i] & (1 << index)) >> index;
                        }
                    }
                }
            }
        }

    }
}
