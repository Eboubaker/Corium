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
                Writer.FeedBack($"processing image part {image.OriginFile.FullName}");
                baseInfo.StoredDataLength = (int)Math.Min(remainingBytes, image.Capacity);
                remainingBytes = Math.Max(0, remainingBytes - image.Capacity);
                using var bitmap = image.CloneBitmap();
                using var bits = baseInfo.GetBits().Then(Bits.OfStream(stream, baseInfo.StoredDataLength));
                Bits.WriteToImage(bits, bitmap);
                var name = image.FileName(Context.OutExtension);
                var path = Path.Combine(Context.OutDir.FullName, name);
                bitmap.Save(path, Context.Codec, Context.Encoder);
                baseInfo.ImageIndex++;
                if (remainingBytes > 0) Writer.FeedBack($"{remainingBytes.HumanReadableSize()} Remaining");
            }
        }

        /// <summary>
        /// extract the collection data to a temporary file
        /// </summary>
        public static FileInfo ExtractCollection(List<ImageWrapper> collection)
        {
            var temp = new FileInfo(Path.GetTempFileName());
            var stream = temp.OpenWrite();
            collection.Sort((im1, im2) => im1.ReadInfo().ImageIndex - im2.ReadInfo().ImageIndex);
            long processed = 0;
            foreach (var image in collection)
                using (image)
                {
                    Writer.FeedBack($"Processing image: {image.OriginFile.FullName}");
                    Bits.CopyToStream(image.OpenBitmap(), stream, image.Info.StoredDataLength);
                    processed += image.Info.StoredDataLength;
                    Writer.FeedBack($"{processed.HumanReadableSize()} was processed so far");
                }

            stream.Close();
            return temp;
        }
    }
}