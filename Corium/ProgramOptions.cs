using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Corium
{
    public static class ProgramOptions
    {
        public static FileSystemInfo[] InputImages { get; set; }
        public static FileSystemInfo[] InputData { get; set; }
        public static bool Verbose { get; set; }
        public static DirectoryInfo Ouput { get; set; }
        public static int BitsUsage { get; set; }
        public static bool Alpha { get; set; }
        public static int ChannelCount { get; set; }
        public static PixelFormat PixelFormat { get; set; }
    }
}
