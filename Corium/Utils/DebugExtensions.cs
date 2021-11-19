using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Corium.Core;

namespace Corium.Utils
{
    /// <summary>
    /// Helper extensions used just for development and testing
    /// </summary>
    [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class DebugExtensions
    {
        public static string BinaryString(this int n)
        {
            return n.Bytes().BinaryString();
        }

        public static string BinaryString(this long n)
        {
            return n.Bytes().BinaryString();
        }

        public static string BinaryString(this byte n)
        {
            return n.Bytes().BinaryString();
        }

        public static string GetChannelsBytes(this ImageWrapper image, long count)
        {
            return image.OpenBitmap().GetChannelsBytes(count);
        }

        public static string GetChannelsBytes(this Bitmap im, long count)
        {
            var bits = count * 8;
            var s = "";
            for (var y = 0; y < im.Height; y++)
            {
                for (var x = 0; x < im.Width; x++)
                {
                    var p = im.GetPixel(x, y);
                    var channels = new[] {p.A, p.R, p.G, p.B};
                    for (var i = 4 - Context.ChannelCount; i < 4; i++)
                    {
                        for (var offset = Context.Bits - 1; offset >= 0; offset--)
                        {
                            if (bits-- <= 0)
                            {
                                goto ret;
                            }

                            s += ((channels[i] >> offset) & 1) == 1 ? 1 : 0;
                        }
                    }
                }
            }

            ret:
            return s;
        }

        /// <summary>
        /// hash the stream bytes with MD5
        /// </summary>
        /// <returns>hexadecimal hash pairs separated by hyphens ie "7D-AF-6D..."</returns>
        public static string GetHash(this FileStream stream)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(stream));
        }
        
        public static string BinaryString(this IEnumerable<byte> arr)
        {
            return string.Join(" ", 
                arr.Select(x => 
                    Convert.ToString(x, 2).PadLeft(8, '0')));
        }
        public static T[] ToArray<T>(this IEnumerator<T> iter)
        {
            var l = new List<T>();
            while (iter.MoveNext())
            {
                l.Add(iter.Current);
            }

            return l.ToArray();
        }
        
        
    }
}