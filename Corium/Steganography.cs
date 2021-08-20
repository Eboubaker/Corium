using System;
using System.Collections;
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
        private static int bitBuffer = 0;
        private static int bitBufferOffset = ProgramOptions.BitsUsage - 1;

        internal static void InsertZip(FileInfo file, Stack<ImageStruct> images)
        {
            var zip = file.OpenRead();
            int imageIndex = 0;
            var zipBits = BitsIterator.fromStream(zip);

            foreach (var imstruct in images)
            {
                Bitmap image = new Bitmap(imstruct.File.FullName);
                // copy original pixels
                Bitmap newImage = image.Clone(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ProgramOptions.Alpha ? System.Drawing.Imaging.PixelFormat.Format32bppArgb : image.PixelFormat
                );
                var header = new List<byte>();
                header.AddRange(ProgramIdentifier);
                header.AddRange(BitConverter.GetBytes(imageIndex));
                header.AddRange(BitConverter.GetBytes(images.Count));
                header.AddRange(BitConverter.GetBytes(file.Length));

                var dataBits = BitsIterator.FromBuffer(header.ToArray(), 0, header.Count);

                int wroteBits = 0;
                WriteImage(image, newImage, dataBits, ref wroteBits);
                int headerBitsCount = header.Count * 8;

                WriteImage(image, newImage, zipBits, ref wroteBits);

                newImage.Save(Path.Combine(ProgramOptions.Ouput.FullName, imstruct.FileName + (ProgramOptions.Alpha ? ".png" : imstruct.File.Extension)));
                imageIndex++;
            }


        }
        private static void WriteImage(Bitmap image, Bitmap newimage, IEnumerator<int> dataBitsIterator, ref int insertionSkip)
        {
            int channelIndex = (insertionSkip / ProgramOptions.BitsUsage) % ProgramOptions.ChannelCount;
            int channelBitOffset = ProgramOptions.BitsUsage - 1 - insertionSkip % ProgramOptions.BitsUsage;
            int pixelIndex = channelIndex / ProgramOptions.ChannelCount;
            bool hasNext = false;
            int pixelCount = image.Width * image.Height;
            int x = 0;
            int y = 0;
            while (x * y > pixelCount)
            {
                x = pixelIndex % image.Width;
                y = pixelIndex / image.Height;
                var op = newimage.GetPixel(x, y); // Original Pixel
                var channels = new int[] { op.R, op.G, op.B, op.A };
                for (; channelIndex < ProgramOptions.ChannelCount; channelIndex++)
                {
                    for (; channelBitOffset >= 0 && (hasNext=dataBitsIterator.MoveNext()); channelBitOffset--)
                    {
                        int cindx = channelIndex % ProgramOptions.ChannelCount;
                        channels[cindx] &= ~(1 << channelBitOffset); // clear the channelBitOffset'th bit
                        channels[cindx] |= (dataBitsIterator.Current << channelBitOffset);// set the channelBitOffset'th bit
                        insertionSkip++;
                    }
                    channelBitOffset = ProgramOptions.BitsUsage - 1;
                    newimage.SetPixel(x, y, Color.FromArgb(channels[3], channels[0], channels[1], channels[2]));
                    if (!hasNext)
                    {
                        return;
                    }
                }
                pixelIndex++;
                channelBitOffset = ProgramOptions.BitsUsage - 1;
                channelIndex = 0;
            }
        }

        internal static Stream extractZip(List<ImageStruct> images)
        {
            var zip = File.OpenWrite(Path.GetTempFileName());
            int imageIndex = 0;

            foreach (var imstruct in images)
            {
                Bitmap image = new Bitmap(imstruct.File.FullName);
                var header = new List<byte>();
                header.AddRange(ProgramIdentifier);
                header.AddRange(BitConverter.GetBytes(imageIndex));
                header.AddRange(BitConverter.GetBytes(images.Count));
                header.AddRange(BitConverter.GetBytes(file.Length));

                var dataBits = BitsIterator.FromBuffer(header.ToArray(), 0, header.Count);

                int wroteBits = 0;
                WriteImage(image, newImage, dataBits, ref wroteBits);
                int headerBitsCount = header.Count * 8;

                WriteImage(image, newImage, zipBits, ref wroteBits);

                newImage.Save(Path.Combine(ProgramOptions.Ouput.FullName, imstruct.FileName + (ProgramOptions.Alpha ? ".png" : imstruct.File.Extension)));
                imageIndex++;
            }
            return zip;
        }



        private static void ReadImage(Bitmap image , Stream zipFile)
        {
            for(int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    var channels = new int[] { pixel.R, pixel.G, pixel.B, pixel.A };
                    for (int channelIndex=0; channelIndex < ProgramOptions.ChannelCount; channelIndex++)
                    {
                        bitBuffer = channels[channelIndex];
                        var arr = new BitArray(8);
                        arr.and
                        for (int channelBitOffset = ProgramOptions.BitsUsage-1; channelBitOffset >= 0 ; channelBitOffset--)
                        {
                            
                        }
                        channelBitOffset = ProgramOptions.BitsUsage - 1;
                        newimage.SetPixel(x, y, Color.FromArgb(channels[3], channels[0], channels[1], channels[2]));
                        if (!hasNext)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}