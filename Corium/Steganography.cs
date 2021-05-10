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
            ;
        internal static void InsertZip(FileInfo file, Stack<ImageStruct> images)
        {
            var zip = file.OpenRead();
            var buffer = new byte[32768];
            int offset = 0;
            byte[] totalImagesInBytes = BitConverter.GetBytes(images.Count);

            ImageStruct imstruct = null;
            Bitmap image = null, newimage = null;
            var imageChannels = new List<byte>();
            int pixelIndex = int.MaxValue;
            int imageIndex = -1;
            int remainingCapacity = 0;
            byte[] headerBuffer = new byte[ImageHeaderSize];
            int ImageByteIndex = 0;
            long wroteBits = 0;
            bytes: while (available > 0)
            {
                if (pixelIndex >= imstruct.PixelCount)
                {
                    imstruct = images.Pop();
                    imageIndex++;
                    image = new Bitmap(imstruct.File.FullName);
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            Color Pixel = image.GetPixel(x, y);
                            imageChannels.Add(Pixel.R);
                            imageChannels.Add(Pixel.G);
                            imageChannels.Add(Pixel.B);
                            if(ProgramOptions.Alpha)
                                imageChannels.Add(Pixel.A);
                        }
                    }
                    newimage = new Bitmap(image.Width, image.Height, ProgramOptions.Alpha ? System.Drawing.Imaging.PixelFormat.Format32bppArgb : image.PixelFormat);
                    Array.Copy(ProgramIdentifier, 0, headerBuffer, 0, ProgramIdentifier.Length);
                    var len = BitConverter.GetBytes(imageIndex);
                    Array.Copy(len, 0, headerBuffer, ProgramIdentifier.Length, len.Length);
                    Array.Copy(totalImagesInBytes, 0, headerBuffer, ProgramIdentifier.Length+len.Length, totalImagesInBytes.Length);
                    remainingCapacity = imstruct.Capacity - ImageHeaderSize;

                    foreach(var bit in BitsIterator.FromBuffer(headerBuffer, 0, headerBuffer.Length))
                    {
                        
                    }
                }
                int available = zip.Read(buffer, 0, buffer.Length);
                // HELP
                wroteBits = WriteBytesInImage(image, newimage, buffer, ImageHeaderSize*ProgramOptions.ChannelCount*ProgramOptions.BitsUsage/8, 0, 0, available);

            }
            newimage.Save(Path.Combine(ProgramOptions.Ouput.FullName, imstruct.FileName + (ProgramOptions.Alpha ? ".png" : imstruct.File.Extension)));

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