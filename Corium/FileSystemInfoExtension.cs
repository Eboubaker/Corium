using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Corium
{
    public static class FileSystemInfoExtension
    {
        public static IEnumerable<FileInfo> AllContainedFiles(this FileSystemInfo path)
        {
            if (File.Exists(path.FullName))
            {
                yield return new FileInfo(path.FullName);
            }
            else if (Directory.Exists(path.FullName))
            {
                foreach (var f in Directory.GetFiles(path.FullName))
                {
                    yield return new FileInfo(f);
                }
                foreach (var d in Directory.GetDirectories(path.FullName))
                {
                    foreach (var f in AllContainedFiles(new DirectoryInfo(d)))
                        yield return f;
                }
            }
        }
    }
}
