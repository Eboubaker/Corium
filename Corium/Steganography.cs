using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Corium
{
    /**
     * bits = 3
     * 255      255      255      255
     * 11111111 11111111 11111111 11111111
     * 11111000 11111000 11111000 11111000
     */
    internal class Steganography
    {
        private static byte[] ProgramIdentifier { get; } = BitConverter.GetBytes(Convert.ToInt32(
                "11101111 10110011 01010101 10110111".Replace(" ", "")
            , 2));
        public static int ImageHeaderSize { get; } =
            4 // program identifier
            +
            4 // image index
            +
            4 // total images
            +
            sizeof(long) // data size
            ;
        public static int HeaderPixelsSize { get; } = 0;
        internal static void InsertZip(FileInfo file, Stack<ImageStruct> images)
        {
            var zip = file.OpenRead();
            var buffer = new byte[32768];
            byte[] totalImagesInBytes = BitConverter.GetBytes(images.Count);
            
            ImageStruct imstruct = null;
            Bitmap image = null, newimage = null;
            int imageIndex = 0;
            int remainingBitsCapacity = 0;
            IEnumerator<int> bitsIterator = BitsIterator.fromStream(zip);
            IEnumerator<int> channelsIterator = null;
            MemoryStream stream = new MemoryStream();
            int available = zip.Read(buffer, 0, buffer.Length);
            stream.Write(buffer);
            int remainingBits = available * 8;
            long remainingTotalBits = file.Length * 8;
            stream.Write(buffer);
            
            while (remainingTotalBits > 0)
            {
                if (pixelIndex >= imstruct.PixelCount)
                {
                    imstruct = images.Pop();
                    imageIndex++;
                    image = new Bitmap(imstruct.File.FullName);
                    newimage = new Bitmap(image.Width, image.Height, ProgramOptions.Alpha ? System.Drawing.Imaging.PixelFormat.Format32bppArgb : image.PixelFormat);
                    var tempStream = stream;
                    stream = new MemoryStream();
                    stream.Write(ProgramIdentifier, 0, ProgramIdentifier.Length);
                    var len = BitConverter.GetBytes(imageIndex);
                    stream.Write(len, 0, len.Length);
                    stream.Write(totalImagesInBytes, 0, totalImagesInBytes.Length);
                    stream.Write(BitConverter.GetBytes(file.Length), 0, sizeof(long));
                    tempStream.WriteTo(stream);

                    remainingBitsCapacity -= (imstruct.Capacity - ImageHeaderSize) * 8;
                    remainingBits += ImageHeaderSize * 8;
                    channelsIterator = BitsIterator.fromImageChannels(image);
                    bitsIterator.Dispose();
                    bitsIterator = BitsIterator.fromStream(stream);
                }
                if(remainingBits <= 0)
                {
                    remainingBits = zip.Read(buffer, 0, buffer.Length) * 8;
                    stream = new MemoryStream();
                    stream
                }
                int channelIndex = 0;
                int[] channels = new int[ProgramOptions.ChannelCount]; ;
                bool done = false;
                IEnumerator<int> channelIterator = ChannelIterator.FromImage(image);
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
                                done = bitsIterator.MoveNext();
                                newData |= bitsIterator.Current;
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
                        newimage.SetPixel(pixelIndex % newimage.Width, pixelIndex / newimage.Width, color);
                        channels = new int[ProgramOptions.ChannelCount];
                        if (done)
                        {
                            if (pixelIndex < newimage.Width * newimage.Height - 1)
                            {

                            }
                            break;
                        }
                    }
                }
                newimage.Save(Path.Combine(ProgramOptions.Ouput.FullName, imstruct.FileName + (ProgramOptions.Alpha ? ".png" : imstruct.File.Extension)));
            }
            

        }
        private static long WriteBytesInImage(Bitmap image, Bitmap newimage, byte[] bytes, int startingChannel, long startBit, int startIndex, int lengthBytes)
        {
            int pixel = startBit
            long wrote = 0;

            return wrote;
        }

        internal static Stream extractZip(List<ImageStruct> images)
        {
            throw new NotImplementedException();
        }
    }
}