using System;
using System.Drawing;
using System.IO;
using Corium.Utils;

namespace Corium.Core
{
    public class ImageWrapper : IDisposable
    {
        /// <summary>
        ///     how much bytes can this image contain (without the head info)
        /// </summary>
        public readonly int Capacity;

        public readonly FileInfo OriginFile;

        //-- lazy getters --//
        public readonly string OriginName;
        private Bitmap _bitmap;

        public ImageWrapper(FileInfo originFile)
        {
            using var image = Image.FromFile(originFile.FullName);
            Capacity = image.Width * image.Height * Context.ChannelCount * Context.Bits / 8 - ImageInfo.Size;
            if (Capacity <= 0)
                throw new IndexOutOfRangeException("Image is too small");
            OriginFile = originFile;
            Name = Path.GetFileNameWithoutExtension(OriginFile.Name);
            OriginName = new string(Name);
        }

        /// <summary>
        ///     the meta data of this image
        /// </summary>
        public ImageInfo Info { get; private set; }

        public string Name { get; set; }

        /// <summary>
        ///     dispose the bitmap if it is still open
        /// </summary>
        public void Dispose()
        {
            _bitmap?.Dispose();
        }

        public string FileName(string extension)
        {
            return Name + "." + extension;
        }


        /// <summary>
        /// read the meta data of this image
        /// </summary>
        /// <exception cref="InvalidDataException">if the image has an invalid signature</exception>
        public ImageInfo ReadInfo()
        {
            if (Info != null) return Info;
            using var bitmap = OpenBitmap();
            Info = ImageInfo.FromBits(Bits.OfImage(bitmap));
            if (Info.Fingerprint != ImageInfo.CoriumFingerprint)
            {
                throw new InvalidDataException(
                    "Image Signature Mismatch (the image probably was not created by this application)");
            }

            return Info;
        }

        public Bitmap OpenBitmap()
        {
            _bitmap?.Dispose();
            _bitmap = new Bitmap(OriginFile.FullName);
            return _bitmap;
        }

        /// <summary>
        ///     clones this image's bitmap with a new one which has the context's pixel format
        /// </summary>
        /// <returns></returns>
        public Bitmap CloneBitmap()
        {
            using var bm = OpenBitmap();
            return bm.Clone(new Rectangle(0, 0, bm.Width, bm.Height), Context.PixelFormat);
        }
    }
}