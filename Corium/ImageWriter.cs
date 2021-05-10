using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Corium
{
    class ImageWriter: IDisposable
    {
        private Bitmap Image;

        public ImageWriter(ImageStruct image)
        {
            Image = new Bitmap(image.File.FullName);
        }

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
