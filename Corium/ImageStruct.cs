using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Corium
{
    class ImageStruct
    {
        public FileInfo File { get;}
        public string FileName { get; set; }
        public Size Size { get; }
        public int Capacity { get; }
        public int PixelCount { get; set; }
        public ImageStruct(FileInfo file)
        {
            
            var image = Image.FromFile(file.FullName);

            PixelCount = Size.Width * Size.Height;
            Capacity = PixelCount * (3 + ProgramOptions.Alpha) * ProgramOptions.BitsUsage / 8;
            if (Capacity < Steganography.ImageHeaderSize + 1)
                throw new IndexOutOfRangeException("Image is too small");

            Size = image.Size;
            File = file;
            FileName = Path.GetFileNameWithoutExtension(File.FullName);
            image.Dispose();
        }
    }
}
