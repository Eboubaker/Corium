using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Corium.Utils;

namespace Corium.Core
{
    /// <summary>
    /// Enter the world of imageWrapper layers, where i can flex my wasted 3 years of biology studies :)
    /// </summary>
    public static class Tyrozyna
    {
        /// <summary>
        /// insert the cancer cells into the corium(dermis) layer
        /// </summary>
        public static void InsertCancer(FileStream stream, List<ImageWrapper> images, ImageInfo baseInfo)
        {
            var bits = Bits.StreamToBitIterator(stream);
            var remainingVolume = stream.Length;
            foreach (var skin in images)
            {
                baseInfo.ImageIndex++;
                baseInfo.StoredDataLength = (int) Math.Min(remainingVolume, skin.Capacity);
                remainingVolume -= Math.Max(0, remainingVolume - skin.Capacity);

                using var corium = skin
                    .OpenBitmap()
                    .Clone(new Rectangle(0, 0, skin.Width, skin.Height), Context.SkinType);
                skin.Dispose();
                InsertCells(corium, baseInfo.GetBits().Concat(bits));
                InsertToBody(corium, skin);
            }
        }
        /// <summary>
        /// plant the modified imageWrapper in the body using the context insertion factory
        /// </summary>
        // ReSharper disable once SuggestBaseTypeForParameter
        private static void InsertToBody(Image corium, ImageWrapper imageWrapper)
        {
            var targetBody = Path.Combine(Context.OutDir.FullName, imageWrapper.FileName(Context.OutSignature));
            var insertionMethod = new EncoderParameters();
            insertionMethod.Param = new[] {new EncoderParameter(Encoder.Quality, (long) 100)};
            corium.Save(targetBody, Context.OutputFactory, insertionMethod);
        }

        private static void InsertCells(Bitmap corium, IEnumerator<int> cells)
        {
            cells.MoveNext();
            for (var y = 0; y < corium.Height; y++)
            {
                for (var x = 0; x < corium.Width; x++)
                {
                    var channel = corium.GetPixel(x, y).ToArgb();
                    for (var channelOffset = Context.ChannelCount - 1; channelOffset >= 0; channelOffset--)
                    {
                        var shift = 8 * channelOffset;
                        channel &= ~(((1 << Context.Bits) - 1) << shift);
                        for (var offset = Context.Bits - 1; offset >= 0; offset--)
                        {
                            channel |= (cells.Current << (shift + offset));
                            cells.MoveNext();
                        }
                    }

                    corium.SetPixel(x, y, Color.FromArgb(channel));
                }
            }
        }

        public static FileInfo ExtractCollection(List<ImageWrapper> collection)
        {
            var file = new FileInfo(Path.GetTempFileName());
            var zip = file.OpenWrite();
            collection.Sorted((im1, im2) => im1.ReadInfo().ImageIndex - im2.ReadInfo().ImageIndex);
            foreach (var image in collection)
                using (image)
                {
                    ImageReadToStream(image, zip);
                }

            zip.Close();
            return file;
        }

        /// <summary>
        /// Read hidden image bits to the stream
        /// </summary>
        private static void ImageReadToStream(ImageWrapper wrapper, Stream stream)
        {
            var iter = ImageInfo.SkipInfoArea(Bits.ImageToBitIterator(wrapper.OpenBitmap()));
            stream.Write(Bits.ToByteArray(iter, wrapper.Info.StoredDataLength));
        }
    }
}