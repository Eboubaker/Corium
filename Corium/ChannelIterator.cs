using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Corium
{
    class ChannelIterator
    {
        public static IEnumerator<int> FromImage(Bitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color Pixel = bitmap.GetPixel(x, y);
                    yield return Pixel.R >> ProgramOptions.BitsUsage << ProgramOptions.BitsUsage;
                    yield return Pixel.G >> ProgramOptions.BitsUsage << ProgramOptions.BitsUsage;
                    yield return Pixel.B >> ProgramOptions.BitsUsage << ProgramOptions.BitsUsage;
                    if (ProgramOptions.Alpha)
                        yield return Pixel.A >> ProgramOptions.BitsUsage << ProgramOptions.BitsUsage;
                }
            }
        }
    }
}
