using System;
using System.Collections.Generic;
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
            var remainingBytes = stream.Length;
            foreach (var image in collection)
            {
                baseInfo.StoredDataLength = (int) Math.Min(remainingBytes, image.Capacity);
                remainingBytes = Math.Max(0, remainingBytes - image.Capacity);
                using var bitmap = image.CloneBitmap();
                using var bits = baseInfo.GetBits().Then(Bits.OfStream(stream, baseInfo.StoredDataLength));
                Bits.WriteToImage(bits, bitmap);
                var name = image.FileName(Context.OutExtension);
                var path = Path.Combine(Context.OutDir.FullName, name);
                bitmap.Save(path, Context.Codec, Context.Encoder);
                baseInfo.ImageIndex++;
            }
        }

        /// <summary>
        /// extract the collection data to a temporary file
        /// </summary>
        public static FileInfo ExtractCollection(List<ImageWrapper> collection)
        {
            var temp = new FileInfo(Path.GetTempFileName());
            var stream = temp.OpenWrite();
            collection.Sorted((im1, im2) => im1.ReadInfo().ImageIndex - im2.ReadInfo().ImageIndex);
            foreach (var image in collection)
                using (image)
                {
                    Bits.CopyToStream(image.OpenBitmap(), stream, image.Info.StoredDataLength);
                }

            stream.Close();
            return temp;
        }
    }
}