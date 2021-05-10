using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Corium
{
    class ImageChannelsIterator
    {
        public static IEnumerable<byte> ParseChannelsForImage(Bitmap image, IEnumerable<int> bits)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color Pixel = image.GetPixel(x, y);
                    int c = Pixel.R;
                    c = (c >> ProgramOptions.BitsUsage);
                    for(int i = 0; i < ProgramOptions.BitsUsage; i++)
                    {
                        bits.GetEnumerator().MoveNext();
                        c = c << 1;
                        c = c | bits.GetEnumerator().Current;
                    }
                    c = Pixel.G;
                    c = (c >> ProgramOptions.BitsUsage);
                    for (int i = 0; i < ProgramOptions.BitsUsage; i++)
                    {
                        bits.GetEnumerator().MoveNext();
                        c = c << 1;
                        c = c | bits.GetEnumerator().Current;
                    }
                    c = Pixel.B;
                    c = (c >> ProgramOptions.BitsUsage);
                    for (int i = 0; i < ProgramOptions.BitsUsage; i++)
                    {
                        bits.GetEnumerator().MoveNext();
                        c = c << 1;
                        c = c | bits.GetEnumerator().Current;
                    }
                    if (ProgramOptions.Alpha)
                    {
                        c = Pixel.A;
                        c = (c >> ProgramOptions.BitsUsage);
                        for (int i = 0; i < ProgramOptions.BitsUsage; i++)
                        {
                            bits.GetEnumerator().MoveNext();
                            c = c << 1;
                            c = c | bits.GetEnumerator().Current;
                        }
                    }
                    yield return 
                }
            }
        }
    }
}
