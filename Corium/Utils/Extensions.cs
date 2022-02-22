using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Corium.Utils
{
    /// <summary>
    ///     Extension Helpers used by corium
    /// </summary>
    public static class Extensions
    {
        private static readonly IEnumerable<string> ReadableExtensions =
            (from codec in ImageCodecInfo.GetImageEncoders()
                where codec.FilenameExtension != null
                from c in codec.FilenameExtension!.ToLowerInvariant().Split(';')
                select c).ToArray();

        /// <summary>
        ///     create a new bit iterator which yields the items of the current
        ///     iterator and then yields the items of the other iterator,
        ///     and then yields random bits
        /// </summary>
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public static IEnumerator<int> Then(this IEnumerator<int> me, IEnumerator<int> other)
        {
            while (me.MoveNext()) yield return me.Current;

            // we tried to move in the loop above, so we dont need to move here
            do
                yield return other.Current;
            while (other.MoveNext());

            // fake data, served as needed
            var r = new Random();
            while (true) yield return r.Next(2) & 1;
        }

        /**
         * humans are lazy and dump they cant convert byte units so we do it for them
         */
        public static string HumanReadableSize(this long byteCount)
        {
            //Longs run out around EB, like we ever gonna have images with that size, unless...
            var units = new[] {"B", "KB", "MB", "GB", "TB", "PB", "EB"};
            if (byteCount == 0) return "0" + units[0];
            var bytes = Math.Abs(byteCount);
            var @base = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, @base), 1);
            return Math.Sign(byteCount) * num + units[@base];
        }

        public static string HumanReadableSize(this int byteCount)
        {
            return ((long) byteCount).HumanReadableSize();
        }

        // idea stolen from https://stackoverflow.com/questions/7027022/java-how-to-efficiently-predict-if-data-is-compressible
        // essentially read small parts of the stream and check if they can be compressed
        public static bool IsCompressible(this FileStream stream)
        {
            var data = new List<byte>();
            var buffer = new byte[4.KB()];
            for (int read; (read = stream.Read(buffer)) != 0;)
            {
                data.AddRange(buffer.Take(read));
                stream.Seek(200.MB(), SeekOrigin.Current); // jump 200MB
            }

            var len = data.Count;
            var sum = new int[16];
            data.ForEach(byteData => sum[(byteData & 0xff) >> 4]++);
            return len * sum.Select(x => ((long) x << 32) / len)
                       .Select(v => 63 - BitOperations.LeadingZeroCount((ulong) (v + 1))).Sum()
                   < 438 * len;
        }

        public static void CopyTo(this DirectoryInfo src, DirectoryInfo dest)
        {
            foreach (var dir in src.GetDirectories())
                dir.CopyTo(dest.CreateSubdirectory(dir.Name));
            foreach (var file in src.GetFiles())
                file.CopyTo(Path.Combine(dest.FullName, file.Name));
        }

        public static IEnumerable<FileInfo> GetAllFilesRecursively(this FileSystemInfo path)
        {
            if (File.Exists(path.FullName) && (path.Attributes & FileAttributes.System) == 0)
            {
                yield return new FileInfo(path.FullName);
            }
            else if((path.Attributes & FileAttributes.System) == 0)
            {
                string[] files = { };
                try
                {
                    files = Directory.GetFiles(path.FullName);
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Warning($"Access to {path} was denied by the system");
                }

                foreach (var f in files) yield return new FileInfo(f);
                string[] directories = { };
                try
                {
                    directories = Directory.GetDirectories(path.FullName);
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Warning($"Access to {path} was denied by the system");
                }

                foreach (var d in directories)
                foreach (var f in GetAllFilesRecursively(new DirectoryInfo(d)))
                    yield return f;
            }
            else
            {
                Writer.VerboseFeedBack($"Skipped system entry {path}");
            }
        }

        public static List<T> Sorted<T>(this List<T> list, Comparison<T> comparision)
        {
            list.Sort(comparision);
            return list;
        }

        /// <summary>
        ///     similar to the method tap, it will give the value to the callable and call it and then return the value back,
        ///     useful for chaining methods on an object which has methods that return void
        /// </summary>
        /// <param name="value">any type of value</param>
        /// <param name="callable">a callback which will consume the value</param>
        /// <returns>value</returns>
        public static T Chain<T>([AllowNull] this T value, [NotNull] Action<T> callable)
        {
            callable(value);
            return value;
        }


        public static byte[] Bytes(this int n)
        {
            return new[]
            {
                (byte) ((n >> (3 * 8)) & 0xFF),
                (byte) ((n >> (2 * 8)) & 0xFF),
                (byte) ((n >> (1 * 8)) & 0xFF),
                (byte) ((n >> (0 * 8)) & 0xFF)
            };
        }

        public static byte[] Bytes(this byte n)
        {
            return new[] {n};
        }

        public static byte[] Bytes(this long n)
        {
            return new[]
            {
                (byte) ((n >> (7 * 8)) & 0xFF),
                (byte) ((n >> (6 * 8)) & 0xFF),
                (byte) ((n >> (5 * 8)) & 0xFF),
                (byte) ((n >> (4 * 8)) & 0xFF),
                (byte) ((n >> (3 * 8)) & 0xFF),
                (byte) ((n >> (2 * 8)) & 0xFF),
                (byte) ((n >> (1 * 8)) & 0xFF),
                (byte) ((n >> (0 * 8)) & 0xFF)
            };
        }

        public static long ToInt64(this byte[] b)
        {
            return ((long) b[0] << (7 * 8)) |
                   ((long) b[1] << (6 * 8)) |
                   ((long) b[2] << (5 * 8)) |
                   ((long) b[3] << (4 * 8)) |
                   ((long) b[4] << (3 * 8)) |
                   ((long) b[5] << (2 * 8)) |
                   ((long) b[6] << (1 * 8)) |
                   ((long) b[7] << (0 * 8))
                ;
        }

        [SuppressMessage("ReSharper", "RedundantCast")]
        public static int ToInt32(this byte[] b)
        {
            return ((int) b[0] << (3 * 8)) |
                   ((int) b[1] << (2 * 8)) |
                   ((int) b[2] << (1 * 8)) |
                   ((int) b[3] << (0 * 8))
                ;
        }

        /// <returns>n * 1024</returns>
        // ReSharper disable once InconsistentNaming
        public static int KB(this int n)
        {
            return n * 1024;
        }

        /// <returns>n * 1024 * 1024</returns>
        // ReSharper disable once InconsistentNaming
        public static int MB(this int n)
        {
            return n.KB() * 1024;
        }

        /// <returns>n * 1024 * 1024 * 1024</returns>
        // ReSharper disable once InconsistentNaming
        public static int GB(this int n)
        {
            return n.MB() * 1024;
        }

        public static bool IsRecognisedImageFile(this FileInfo file)
        {
            var extension = Path.GetExtension(file.FullName);
            if (string.IsNullOrEmpty(extension)) return false;
            extension = "*" + extension.ToLowerInvariant();
            return ReadableExtensions.Contains(extension);
        }
    }
}