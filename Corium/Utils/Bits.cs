using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Corium.Utils
{
    /// <summary>
    /// contains helper methods to generate bit iterators from byte arrays or generate arrays from bit iterators
    /// </summary>
    public static class Bits
    {
        /// <summary>
        /// convert surfaceStream to a stream of bits
        /// </summary>
        public static IEnumerator<int> ByteArrayToBitIterator(IEnumerable<byte> surfaceStream)
        {
            foreach (var surface in surfaceStream)
            {
                for (var offset = 7; offset >= 0; offset--)
                {
                    yield return (surface >> offset) & 1;
                }
            }
        }
        /// <summary>
        /// convert an (ugly)stream of bytes into a stream of (nice)bits
        /// </summary>
        public static IEnumerator<int> StreamToBitIterator(Stream stream)
        {
            var available = stream.Length;
            var read = 0;
            while(read++ < available)
            {
                var b = stream.ReadByte();
                for (var offset = 7; offset >= 0; offset--)
                {
                    yield return (b >> offset) & 1;
                }
            }
        }
        /// <summary>
        /// Get bits of image channels
        /// </summary>
        public static IEnumerator<int> ImageToBitIterator(Bitmap image)
        {
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var p = image.GetPixel(x, y);
                    var channels = new [] { p.A, p.R, p.G, p.B };
                    for (var i = 4 - Context.ChannelCount; i < 4; i++)
                    {
                        for (var offset = Context.Bits - 1; offset >= 0; offset--)
                        {
                            yield return (channels[i] >> offset) & 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read n bytes from the current position of the bit stream
        /// </summary>
        public static byte[] ToByteArray(IEnumerator<int> bits, int n)
        {
            var data = new byte[n];
            var index = 0;
            bits.MoveNext();
            while (n-- > 0)
            {
                var b = 0;
                for(var shift = 7; shift >= 0; shift--)
                {
                    b |= bits.Current << shift;
                    var moved = bits.MoveNext();
                    Debug.Assert(moved, "bit iterator has reached end but n is still larger than 0");
                }
                data[index++] = (byte)b;
            }
            return data;
        }
    }
}
