using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Corium
{
    /// <summary>
    /// Contains the program cli settings and other settings that depend on the cli settings
    /// this makes the whole program settings-aware everywhere.
    /// </summary>
    public static class Context
    {
        // ReSharper disable once UnusedMember.Global
        public const int MaxThreads = 2; // will be used later when i add multithreading to the program


        private static ImageCodecInfo _codec;

        private static EncoderParameters _encoder;
        public static bool Verbose { get; set; }
        public static int Bits { get; set; }
        public static bool Alpha { get; set; }
        public static int ChannelCount { get; set; }

        //--- Lazy getters ---//
        public static PixelFormat PixelFormat => Alpha ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
        public static string OutExtension => Alpha ? "png" : "jpg";

        /// <summary>
        /// get the appropriate image codec in relation to the current settings
        /// </summary>
        public static ImageCodecInfo Codec
        {
            get
            {
                if (_codec != null) return _codec;
                _codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.MimeType == "image/png");
                return _codec;
            }
        }

        public static bool Silent { get; set; }
        public static int CollectionNumber { get; set; }
        public static string CollectionString { get; set; }
        public static DirectoryInfo OutDir { get; set; }

        public static EncoderParameters Encoder
        {
            get
            {
                if (_encoder != null) return _encoder;
                _encoder = new EncoderParameters(1);
                _encoder.Param = new[] { new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L) };
                return _encoder;
            }
        }

        public static bool NoCollectionFolder { get; set; }
    }
}