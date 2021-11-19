using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoriumTest
{
    [TestClass]
    public class UnitTests
    {
        // a big non-compressible single file
        private const string BigMp4Video = @"C:\Users\me\Downloads\BrBa.S01E02.720p.BrRip.x264.350MB-Pahe.in.mkv";

        // small txt file
        private const string TxtFile = @"C:\Users\me\Desktop\input.txt";

        // folder which contains a bunch of random images
        private const string ImagesFolder = @"D:\restored\memes\";
        private readonly Random _r = new Random();

        /// <returns>true if random number was greater than p</returns>
        private bool P(double p)
        {
            return _r.NextDouble() > p;
        }

        /// <returns>50/50 chance of true or false</returns>
        private bool P()
        {
            return _r.NextDouble() > .5;
        }

        private int R(int lower, int upper)
        {
            return _r.Next(lower, upper + 1);
        }

        [TestInitialize]
        public void CleanBeforeTest()
        {
            var dir = new DirectoryInfo("output");
            if (dir.Exists) dir.Delete(true);
        }

        [TestMethod]
        public void WorkMinimumArgs()
        {
            var t1 = Corium.Corium.Main("hide", "-v", "-i", ImagesFolder, "-d", TxtFile);
            Assert.AreEqual(0, t1.Result);
            var t2 = Corium.Corium.Main("extract", "-v", "-i", @"output");
            Assert.AreEqual(0, t2.Result);
        }

        [TestMethod]
        public void WorkRandomChannelAndAlpha()
        {
            var alpha = P();
            var bit = R(1, 8).ToString();
            var args = new List<string>
            {
                "hide",
                "-v",
                "-i",
                @"D:\restored\memes\",
                "-d",
                @"C:\Users\me\Desktop\input.txt",
                alpha ? "-a" : "",
                "-b",
                bit
            };
            args.RemoveAll(string.IsNullOrEmpty);
            var t1 = Corium.Corium.Main(args.ToArray());
            Assert.AreEqual(0, t1.Result, "HIDE FAILED");
            args = new List<string>
            {
                "extract",
                "-i",
                @"output",
                alpha ? "-a" : "",
                "-b",
                bit
            };
            args.RemoveAll(string.IsNullOrEmpty);
            var t2 = Corium.Corium.Main(args.ToArray());
            Assert.AreEqual(0, t2.Result, "EXTRACT FAILED");
        }
    }
}