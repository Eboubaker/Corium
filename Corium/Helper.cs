using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Corium
{
    class Helper
    {
        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".TIF", "JFIF", ".PNG" };
        public static List<string> FilterImages(List<string> files)
        {
            return files.FindAll(file =>
            {
                if(Directory.Exists(file))
                {
                    return false;
                }
                if(!File.Exists(file))
                {
                    return false;
                }
                return ImageExtensions.Contains(Path.GetExtension(file).ToUpper());
            });
        }
        public static List<string> GetFiles(string path)
        {
            var list = new List<string>();
            if (Directory.Exists(path))
            {
                list.AddRange(Directory.GetFiles(path));
                foreach (var p in Directory.GetDirectories(path))
                {
                    list.AddRange(GetFiles(p));
                }
            }else if(File.Exists(path))
            {
                list.Add(path);
            }
            return list;
        }
    }
}
