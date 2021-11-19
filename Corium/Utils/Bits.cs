﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Corium.Utils
{
    /// <summary>
    /// responsible for converting bytes into bits or bits into bytes
    /// </summary>
    public static class Bits
    {
        /// <summary>
        /// convert an array of bytes into bit iterator
        /// </summary>
        public static IEnumerator<int> OfBytes(IEnumerable<byte> bytes)
        {
            foreach (var @byte in bytes)
            {
                for (var offset = 7; offset >= 0; offset--)
                {
                    yield return (@byte >> offset) & 1;
                }
            }
        }

        /// <summary>
        /// convert a stream of bytes into a bit iterator
        /// </summary>
        public static IEnumerator<int> OfStream(Stream stream)
        {
            var available = stream.Length;
            var read = 0;
            while (read++ < available)
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
        public static IEnumerator<int> OfImage(Bitmap image)
        {
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var p = image.GetPixel(x, y);
                    var channels = new[] {p.A, p.R, p.G, p.B};
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
        /// Read n bytes from the current position of the bit iterator
        /// </summary>
        public static byte[] ToByteArray(IEnumerator<int> bits, int n)
        {
            var data = new byte[n];
            var index = 0;
            bits.MoveNext();
            while (n-- > 0)
            {
                var b = 0;
                for (var shift = 7; shift >= 0; shift--)
                {
                    b |= bits.Current << shift;
                    var moved = bits.MoveNext();
                    Debug.Assert(moved, "bit iterator has reached end but n is still larger than 0");
                }

                data[index++] = (byte) b;
            }

            return data;
        }
    }
}