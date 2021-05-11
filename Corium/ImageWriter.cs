using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Corium
{
    class ImageWriter: IDisposable
    {
        private Bitmap Image;
        private static byte buffer;
        public ImageWriter(ImageStruct image)
        {
            Image = new Bitmap(image.File.FullName);
        }

        public void Dispose()
        {
            Image.Dispose();
        }

        public static void Write(Bitmap newImage, IEnumerator<int> channelIterator, IEnumerator<int> bits)
        {
            int channelIndex = 0;
            int[] channels = new int[ProgramOptions.ChannelCount]; ;
            bool done = false;
            while (channelIterator.MoveNext())
            {
                
                for (int i = 0; i < ProgramOptions.ChannelCount; i++)
                {
                    int newData = 0;
                    for (int _ = 0; _ < ProgramOptions.BitsUsage; _++)
                    {
                        newData <<= 1;
                        if (!done)
                        {
                            done = bits.MoveNext();
                            newData |= bits.Current;
                        }
                    }
                    channels[channelIndex % ProgramOptions.ChannelCount] = channelIterator.Current | newData;
                }
                if (channelIndex % ProgramOptions.ChannelCount == 0)
                {
                    int pixelIndex = channelIndex / ProgramOptions.ChannelCount;
                    Color color;
                    if (ProgramOptions.Alpha)
                        color = Color.FromArgb(channels[3], channels[0], channels[1], channels[2]);
                    else
                        color = Color.FromArgb(channels[0], channels[1], channels[2]);
                    newImage.SetPixel(pixelIndex % newImage.Width, pixelIndex / newImage.Width, color);
                    channels = new int[ProgramOptions.ChannelCount];
                    if (done)
                    {
                        if(pixelIndex < newImage.Width*newImage.Height-1)
                        {

                        }
                        break;
                    }
                }
            }
        }
    }
}
