using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using CommandLine;
using System.IO;

using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Corium
{
    class Corium
    {
        public static async Task<int> Main(params string[] args)
        {
            Console.WriteLine(String.Join(' ', args));
            RootCommand rootCommand = new RootCommand(description: "Image Steganography Utility (Hide data  inside images)");
            Command hideCommand = new Command(name: "hide", description: "Converts an image file from one format to another.");
            Command extractCommand = new Command(name: "extract", description: "Converts an image file from one format to another.");

            rootCommand.AddCommand(hideCommand);
            rootCommand.AddCommand(extractCommand);

            Option inputOption = new Option(
              aliases: new string[] { "--images", "-i" }
              , description: "The path to the image files/ directories that is to be used.",
              argumentType: typeof(FileInfo[]));
            inputOption.IsRequired = true;
            inputOption.AddValidator(optResult =>
            {
                foreach(var i in optResult.Tokens)
                {
                    if(!Directory.Exists(i.Value) && !File.Exists(i.Value))
                    {
                        return optResult.Option.Aliases.First() + ", Path does not Exist: " + i.Value;
                    }
                }
                return null;
            });
            hideCommand.AddOption(inputOption);


            Option dataOption = new Option(
              aliases: new string[] { "--data", "-d" }
              , description: "The path to the files/ directories that is to be hidden inside the images.",
              argumentType: typeof(FileInfo[]));
            dataOption.AddValidator(optResult =>
            {
                foreach (var i in optResult.Tokens)
                {
                    if (!Directory.Exists(i.Value) && !File.Exists(i.Value))
                    {
                        return optResult.Option.Aliases.First() + ", Path does not Exist: " + i.Value;
                    }
                }
                return null;
            });
            dataOption.IsRequired = true;
            hideCommand.AddOption(dataOption);


            Option outputOption = new Option(
              aliases: new string[] { "--output", "-o" }
              , description: "set an output directory for the processed images"
              , argumentType: typeof(string),
              getDefaultValue: () => "output");
            outputOption.AddValidator(optResult =>
            {
                Console.WriteLine("running");
                string msg = null;
                string dir = null;
                if(optResult.Token != null)
                {
                    dir = optResult.Token.Value;
                }
                else
                {
                    dir = optResult.Option.GetDefaultValue().ToString();
                }
                DirectoryInfo dr = null;
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        dr = Directory.CreateDirectory(dir);
                        dr.Delete();
                    }
                    catch
                    {
                        msg = optResult.Option.Aliases.First() + ", Directory can't be created: " + dir;
                    }
                }
                return msg;
            });
            hideCommand.AddOption(outputOption);


            Option alphaOption = new Option(
              aliases: new string[] { "--alpha", "-a" }
              , description: "use alpha channels in the images"
              , argumentType: typeof(bool),
              getDefaultValue: () => false);
            hideCommand.AddOption(alphaOption);


            Option bitsOption = new Option(
              aliases: new string[] { "--bits", "-b" }
              , description: "set how many bits to be used (1-8) from every image"
              , argumentType: typeof(short),
              getDefaultValue: () => 3);
            hideCommand.AddOption(bitsOption);

            Option extractImagesOption = new Option(
              aliases: new string[] { "--images", "-m" }
              , description: "set files/folders that contains images to extract data from"
              , argumentType: typeof(string));
            extractCommand.AddOption(extractImagesOption);
            extractImagesOption.AddValidator(optResult =>
            {
                foreach (var i in optResult.Tokens)
                {
                    if (!Directory.Exists(i.Value) && !File.Exists(i.Value))
                    {
                        return optResult.Option.Aliases.First() + ", Path does not Exist: " + i.Value;
                    }
                }
                return null;
            });

            Option verbosity = new Option(
              aliases: new string[] { "--verbose", "-v" }
              , description: "set verbosity active.",
              argumentType: typeof(bool),
              getDefaultValue: () => false);
            rootCommand.AddGlobalOption(verbosity);

            hideCommand.Handler =
              CommandHandler.Create<FileInfo[], FileInfo[], byte[], bool, int>(RunHideOptions);
            extractCommand.Handler =
              CommandHandler.Create<FileInfo>(RunExtractOptions);
            return await rootCommand.InvokeAsync(args);
        }
        static int RunExtractOptions(FileInfo targetPath)
        {

            Console.WriteLine(targetPath.FullName);
            return 1;
        }
        static int RunHideOptions(FileInfo[] imagePaths, FileInfo[] FileInfo, byte[] outputPath, bool alpha, int bits)
        {
            Console.WriteLine(outputPath.Length);
            //opts.InputImages = opts.InputImages.Select(i => i.Trim('"'));
            //Console.WriteLine(opts.InputImages.FirstOrDefault());
            //var files = new List<string>();
            //foreach(var f in opts.InputImages)
            //    files.AddRange(Helper.GetFiles(f));
            //files = Helper.FilterImages(files);
            //foreach (var f in files)
            //    Console.WriteLine(f);
            return 0;
        }

    }
}
