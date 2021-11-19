using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Corium.Utils;

namespace Corium.Core
{
    /// <summary>
    /// Helps hide data inside images or extract data from images
    /// </summary>
    public static class ImageCollection
    {
        /// <summary>
        /// hide the stream bytes into the collection images
        /// </summary>
        public static void WriteToStream(FileStream stream, List<ImageWrapper> collection, ImageInfo baseInfo)
        {
            var bits = Bits.OfStream(stream);
            var remainingVolume = stream.Length;
            foreach (var image in collection)
            {
                baseInfo.ImageIndex++;
                baseInfo.StoredDataLength = (int) Math.Min(remainingVolume, image.Capacity);
                remainingVolume -= Math.Max(0, remainingVolume - image.Capacity);

                using var modified = image
                    .OpenBitmap()
                    .Clone(new Rectangle(0, 0, image.Width, image.Height), Context.PixelFormat);
                image.Dispose();
                WriteBits(modified, baseInfo.GetBits().Concat(bits));
                Save(modified, image.FileName(Context.OutExtension));
            }
        }

        /// <summary>
        /// save the image using the context's settings
        /// </summary>
        private static void Save(Image image, string fileName)
        {
            var path = Path.Combine(Context.OutDir.FullName, fileName);
            image.Save(path, Context.Codec, Context.Encoder);
        }

        /// <summary>
        ///     Write bits into the image, using the context's settings
        /// </summary>
        private static void WriteBits(Bitmap bitmap, IEnumerator<int> bits)
        {
            bits.MoveNext();
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var channel = bitmap.GetPixel(x, y).ToArgb();
                    for (var channelOffset = Context.ChannelCount - 1; channelOffset >= 0; channelOffset--)
                    {
                        var shift = 8 * channelOffset;
                        channel &= ~(((1 << Context.Bits) - 1) << shift);
                        for (var offset = Context.Bits - 1; offset >= 0; offset--)
                        {
                            channel |= bits.Current << (shift + offset);
                            bits.MoveNext();
                        }
                    }

                    bitmap.SetPixel(x, y, Color.FromArgb(channel));
                }
            }
        }

        /// <summary>
        ///     extract the collection data to a temporary file
        /// </summary>
        public static FileInfo ExtractCollection(List<ImageWrapper> collection)
        {
            var temp = new FileInfo(Path.GetTempFileName());
            var stream = temp.OpenWrite();
            collection.Sorted((im1, im2) => im1.ReadInfo().ImageIndex - im2.ReadInfo().ImageIndex);
            foreach (var image in collection)
                using (image)
                using (var bitmap = image.OpenBitmap())
                {
                    WriteToStream(bitmap, stream, image.Info.StoredDataLength);
                }

            stream.Close();
            return temp;
        }

        /// <summary>
        /// Read hidden image bits to the stream
        /// </summary>
        private static void WriteToStream(Bitmap image, Stream stream, int length)
        {
            using var bits = ImageInfo.SkipInfoArea(Bits.OfImage(image));
            stream.Write(Bits.ToByteArray(bits, length));
        }
    }
}