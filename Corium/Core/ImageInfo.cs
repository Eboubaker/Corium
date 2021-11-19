using System;
using System.Collections.Generic;
using System.Linq;
using Corium.Utils;

namespace Corium.Core
{
    /// <summary>
    ///     The head of an image, it contains meta data of a single image
    /// </summary>
    public class ImageInfo
    {
        public const long CoriumFingerprint = ~0xC0fe0fC0de;

        public const int Size =
                8 // Fingerprint
                +
                4 // data unique identifier
                +
                4 // tissue index
                +
                4 // total layers
                +
                4 // contained data size
                +
                1 // options flags
            ;

        public long Fingerprint { get; set; }
        public int DataIdentifier { get; set; }
        public int ImageIndex { get; set; }
        public int TotalImages { get; set; }
        public int StoredDataLength { get; set; }
        public bool IsCompressed { get; set; }

        /// <summary>
        ///     Skip the imageInfo part from the bits iterator,
        ///     iterator position must be at 0
        /// </summary>
        public static IEnumerator<int> SkipInfoArea(IEnumerator<int> bits)
        {
            FromBits(bits);
            return bits;
        }

        /// <summary>
        ///     convert the imageInfo data into a bit iterator
        /// </summary>
        public IEnumerator<int> GetBits()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Fingerprint.Bytes());
            bytes.AddRange(DataIdentifier.Bytes());
            bytes.AddRange(ImageIndex.Bytes());
            bytes.AddRange(TotalImages.Bytes());
            bytes.AddRange(StoredDataLength.Bytes());
            bytes.Add((byte) (0 | Convert.ToByte(IsCompressed)));
            return Bits.OfBytes(bytes);
        }

        /// <summary>
        ///     Read the imageInfo part from a bit iterator
        /// </summary>
        public static ImageInfo FromBits(IEnumerator<int> bits)
        {
            var bytes = Bits.ToByteArray(bits, Size);
            var info = new ImageInfo
            {
                Fingerprint = bytes.Take(8).ToArray().ToInt64(),
                DataIdentifier = bytes.Skip(8).Take(4).ToArray().ToInt32(),
                ImageIndex = bytes.Skip(12).Take(4).ToArray().ToInt32(),
                TotalImages = bytes.Skip(16).Take(4).ToArray().ToInt32(),
                StoredDataLength = bytes.Skip(20).Take(4).ToArray().ToInt32()
            };
            var b = bytes.Skip(24).First();
            info.IsCompressed = (b & (1 << 0)) == 1;
            return info;
        }
    }
}