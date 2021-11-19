using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Corium
{
    /// <summary>
    /// Contains the program cli settings and other settings that depend on the cli settings
    /// this makes the whole program context-aware of settings.
    /// </summary>
    public static class Context
    {
        // ReSharper disable once UnusedMember.Global
        public const int MaxThreads = 2;// will be used later when i add multithreading to the program
        public static bool Verbose { get; set; }
        public static int Bits { get; set; }
        public static bool Alpha { get; set; }
        public static int ChannelCount { get; set; }

        //--- Lazy getters ---//
        public static PixelFormat SkinType => Alpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
        public static string OutSignature => Alpha ? "png" : "jpg";


        private static ImageCodecInfo _factory;

        /// <summary>
        /// get the skin planter factory according to the current context settings
        /// </summary>
        public static ImageCodecInfo OutputFactory
        {
            get
            {
                if (_factory != null) return _factory;
                _factory = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e =>
                    (e.MimeType == "image/jpeg" && !Alpha) || e.MimeType == "image/png" && Alpha);
                return _factory;
            }
        }

        public static bool Silent { get; set; }
        public static int CollectionNumber { get; set; }
        public static string CollectionString { get; set; }
        public static DirectoryInfo OutDir { get; set; }
    }
}