using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Corium.Utils;

namespace Corium
{
    public static class CoriumOptions
    {
        public static void AddCommandsOptions(RootCommand rootCommand, Command hideCommand, Command extractCommand)
        {
            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.AddGlobalOption(new Option<bool>(
                new[] { "--verbose", "-v" },
                description: "Turn on verbose mode which will show more debug information..",
                getDefaultValue: () => false));

            rootCommand.AddGlobalOption(new Option<bool>(
                new[] { "--silent", "-s" },
                description:
                "Turn on silent mode (no messages will appear on the console, if return code was 0 then operation was successful)..",
                getDefaultValue: () => false));


            // hide options //

            hideCommand.AddOption(new Option<FileSystemInfo[]>(
                    new[] { "--images", "-i" },
                    "The path(s) to the image(s) or directory(s) containing images that is to be used by " +
                    "Corium for hiding files.")
                .Chain(o => o.AddValidator(ValidatePathsExist))
                .Chain(o => o.IsRequired = true));

            hideCommand.AddOption(new Option<FileSystemInfo[]>(
                    new[] { "--data", "-d" },
                    "The path to the file(s) or directory(s) that is to be hidden inside the images.")
                .Chain(o => o.AddValidator(ValidatePathsExist))
                .Chain(o => o.IsRequired = true));

            hideCommand.AddOption(new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo("processed"),
                    "The name or the path of the output directory(processed image" +
                    " collections will be dropped in this output directory)."
                )
                .Chain(o => o.AddValidator(ValidateDirectoryIsWritable)));

            hideCommand.AddOption(new Option<bool>(
                new[] { "--alpha", "-a" },
                description: "Use alpha channels in the output images (increases image capacity).",
                getDefaultValue: () => false));


            hideCommand.AddOption(new Option<int>(
                new[] { "--bits", "-b" },
                description: "Set how many bits to be used (1-8) from every pixel channel default 3.",
                getDefaultValue: () => 3));


            hideCommand.AddOption(new Option<string>(
                    new[] { "--collection", "-c" },
                    description: "Set the collection number of the output images.",
                    getDefaultValue: RandomCollectionNumber)
                .Chain(o => o.AddValidator(ValidCollectionNumber)));
            hideCommand.AddGlobalOption(new Option<bool>(
                new[] { "--no-sub-directory", "-n" },
                description: "by default processed images will be placed in a folder" +
                             " whose name is the collection name of the images" +
                             "enabling this option will disable this feature and all " +
                             "images will be put in the same output directory.",
                getDefaultValue: () => false));
            // extract options //
            extractCommand.AddOption(new Option<FileSystemInfo[]>(
                    new[] { "--images", "-i" },
                    "The path(s) to the image(s) or directory(s) containing images that is to be used by " +
                    "Corium to extract files.")
                .Chain(o => o.AddValidator(ValidatePathsExist))
                .Chain(o => o.IsRequired = true));
            extractCommand.AddOption(new Option<string>(
                    new[] { "--collection", "-c" },
                    description:
                    "Set the target collection number to be extracted from the input images if the images contains more than one collection. " +
                    "this will cause Corium to only extract from the specified collection if it was found. " +
                    "if no collection number was specified corium will extract all found collections",
                    getDefaultValue: () => "")
                .Chain(o => o.AddValidator(ValidCollectionNumber)));
            extractCommand.AddOption(new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo("extracted"),
                    "The name or the path of the output directory(extracted files " +
                    "will be dropped in this directory).")
                .Chain(o => o.AddValidator(ValidateDirectoryIsWritable)));

            extractCommand.AddOption(new Option<int>(
                new[] { "--bits", "-b" },
                description: "Set how many bits to be used (1-8) from every pixel channel default 3, " +
                             "(must be same as when data was hidden inside images otherwise extraction will fail).",
                getDefaultValue: () => 3));
            extractCommand.AddOption(new Option<bool>(
                new[] { "--alpha", "-a" },
                description: "(DEFAULT None) Use alpha channels in the input images (set to true if images were " +
                             "used by corium with alpha option set to true when hiding files).",
                getDefaultValue: () => false));
            extractCommand.AddGlobalOption(new Option<bool>(
                new[] { "--no-collection-directory", "-ncd" },
                description: "by default extracted files will be placed in a folder whose name" +
                             " is the collection name of the images" +
                             "enabling this option will disable this feature and all collection " +
                             "files will be put in the same output directory.",
                getDefaultValue: () => false));
        }

        private static string ValidCollectionNumber(OptionResult arg)
        {
            var s = arg.Tokens.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(s)) return null;
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Convert.ToInt32(s, 16);
            }
            catch (Exception e)
            {
                Writer.VerboseException(e.ToString());
                // ReSharper disable once StringLiteralTypo
                return $"Expected a valid Hexadecimal number but got [{s}], " +
                       $"allowed symbols in Hex number are [ABCDEF1234567890]";
            }

            return null;
        }

        private static int NonZeroRandomNumber()
        {
            var r = new Random();
            int v;
            do
                v = r.Next(int.MinValue, int.MaxValue);
            while (v == 0);
            return v;
        }

        private static string ValidatePathsExist(OptionResult arg)
        {
            return (from token in arg.Tokens
                where !Directory.Exists(token.Value) && !File.Exists(token.Value)
                select $"Path does not Exist: {token.Value}").FirstOrDefault();
        }

        private static string ValidateDirectoryIsWritable(OptionResult arg)
        {
            var outPath = arg.Tokens.Count != 0
                ? arg.Tokens.First().Value
                : arg.Option.GetDefaultValue()?.ToString();
            try
            {
                // should not happen since we have a default value
                if (outPath == null) return "output directory not set";
                var outDir = new DirectoryInfo(outPath);
                // try to create and delete the directory to make sure we have access to the path
                if (!outDir.Exists)
                {
                    try
                    {
                        outDir.Create();
                    }
                    catch
                    {
                        return $"output directory can't be created: {outDir}";
                    }
                }

                try
                {
                    // try to create and delete a file to make sure we have access to the output directory
                    var f = new FileInfo(Path.Combine(outPath, new Random().Next().ToString()));
                    f.OpenWrite().Close();
                    if (!f.Exists) throw new Exception($"cant create test file in directory {outDir}");
                    f.Delete();
                    return null;
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.ToString());
                    return $"unable to write to directory {outDir}";
                }
            }
            catch (Exception e)
            {
                Writer.VerboseException(e.ToString());
                // if we got an exception it probably means we can't write to that path (or path does not exist)
                return arg.Option.Aliases.First() + ", Directory can't be created or is write-protected: " +
                       outPath;
            }
        }

        private static string RandomCollectionNumber() => Convert.ToString(NonZeroRandomNumber(), 16).ToUpper();
    }
}