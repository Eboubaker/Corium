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
using System.IO.Compression;
using System.Drawing;
using Microsoft.VisualBasic;

namespace Corium
{
    class Corium
    {
        public static async Task<int> Main(params string[] args)
        {
            RootCommand rootCommand = new RootCommand(description: "Image Steganography Utility (Hide data  inside images)");
            Command hideCommand = new Command(name: "hide", description: "Hide data inside images");
            Command extractCommand = new Command(name: "extract", description: "Extract hidden data from images");

            hideCommand.TreatUnmatchedTokensAsErrors = true;
            extractCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            rootCommand.AddCommand(hideCommand);
            rootCommand.AddCommand(extractCommand);

            Option inputOption = new Option<FileSystemInfo[]>(
              aliases: new string[] { "--images", "-i" },
              description: "The path to the image files/ directories containing images that is to be used.");
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


            Option dataOption = new Option<FileSystemInfo[]>(
              aliases: new string[] { "--data", "-d" },
              description: "The path to the files/ directories that is to be hidden inside the images.");
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


            Option outputOption = new Option<string>(
              aliases: new string[] { "--output", "-o" },
              description: "Set an output directory for the processed images/output files",
              getDefaultValue: () => "output");
            outputOption.AddValidator(optResult =>
            {
                string msg = null;
                string dir = null;
                if(optResult.Tokens.Count != 0)
                {
                    dir = optResult.Tokens.First().Value;
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
            rootCommand.AddGlobalOption(outputOption);


            Option alphaOption = new Option<bool>(
              aliases: new string[] { "--alpha", "-a" }
              , description: "Use alpha channels in the images",
              getDefaultValue: () => false);
            hideCommand.AddOption(alphaOption);


            Option bitsOption = new Option<int>(
              aliases: new string[] { "--bits", "-b" }
              , description: "Set how many bits to be used (1-8) from every image",
              getDefaultValue: () => 3);
            hideCommand.AddOption(bitsOption);

            Option extractImagesOption = new Option<FileSystemInfo[]>(
              aliases: new string[] { "--images", "-m" },
              description: "Set files/folders that contains images to extract data from");
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

            Option verbosity = new Option<bool>(
              aliases: new string[] { "--verbose", "-v" }
              , description: "Turn on verbose mode with more debug informations.",
              getDefaultValue: () => false);

            hideCommand.AddOption(verbosity);
            extractCommand.AddOption(verbosity);

           
            hideCommand.Handler =
              CommandHandler.Create<FileSystemInfo[], FileSystemInfo[], string, bool, int, bool>(RunHideOptions);
            extractCommand.Handler =
              CommandHandler.Create<FileSystemInfo[], bool>(RunExtractOptions);
            return await rootCommand.InvokeAsync(args);
        }

        
        static int RunExtractOptions(FileSystemInfo[] images, bool verbose)
        {
            ProgramOptions.InputImages = images;
            ProgramOptions.Verbose = verbose;
            Writer.VerboseInfo("Verbose mode is active");
            Console.WriteLine(images.Length);
            return 0;
        }
        static int RunHideOptions(FileSystemInfo[] images, FileSystemInfo[] data, string output, bool alpha, int bits, bool verbose)
        {
            ProgramOptions.InputImages = images;
            ProgramOptions.InputData = data;
            ProgramOptions.Ouput = new DirectoryInfo(output);
            ProgramOptions.Alpha = alpha;
            ProgramOptions.ChannelCount = Convert.ToByte(alpha) + 3;
            ProgramOptions.BitsUsage = bits;
            ProgramOptions.Verbose = verbose;

            if (!ProgramOptions.Ouput.Exists)
            {
                ProgramOptions.Ouput.Create();
            }
            Writer.VerboseInfo("Verbose mode is active");

            Writer.VerboseInfo("Locating and parsing Images");
            var imagestructs = new List<ImageStruct>();
            foreach(var path in images)
            {
                foreach (var f in path.AllContainedFiles())
                {
                    try
                    {
                        imagestructs.Add(new ImageStruct(f));
                    }
                    catch (OutOfMemoryException e)
                    {
                        Writer.VerboseException(e.Message);
                        Writer.Warning($"Unsupported Image Format in file [{f}]");
                    }
                    catch (Exception e)
                    {
                        Writer.VerboseException(e.Message);
                        Writer.Warning($"Access denied to the file [{f}]");
                    }
                }
            }
            if (imagestructs.Count == 0)
            {
                Writer.Error("No supported images were found in the given paths");
                images.ToList().ForEach(e => Console.WriteLine(e));
                return 1;
            }
            else
            {
                Writer.VerboseInfo("Sorting Images");
                imagestructs.Sort((a, b) => a.Capacity - b.Capacity);
                Writer.Info($"Found {imagestructs.Count} Images");
                var tdirname = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
                try
                {
                    Writer.VerboseDebug($"Creating temporary directory [{tdirname}]");
                    var tempDirectory = Directory.CreateDirectory(tdirname);
                    Writer.VerboseInfo($"Copying data to directory");
                    foreach (var source in data)
                    {
                        string dest = "";
                        try
                        {
                            if (source is FileInfo)
                            {
                                dest = tempDirectory.FullName + Path.DirectorySeparatorChar + source.Name;
                                File.Copy(source.FullName, dest);
                                if(!File.Exists(dest))
                                {
                                    throw new IOException("Unkown IO Exception can't copy file");
                                }
                            }
                            else if (source is DirectoryInfo)
                            {
                                dest = tempDirectory.FullName;
                                Helper.CopyDirectory((DirectoryInfo)source, tempDirectory);
                            }
                        }catch(Exception e)
                        {
                            Writer.VerboseException(e.Message);
                            Writer.Error($"Failed to copy item [{source}] to [{dest}]");
                            return 2;
                        }
                    }
                    try
                    {
                        Writer.VerboseInfo($"Compressing data");
                        string tmpname = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        ZipFile.CreateFromDirectory(tempDirectory.FullName, tmpname);
                        var archive = new FileInfo(tmpname);
                        Writer.Info($"Data size [{Helper.HumanReadableBytes(archive.Length)}] [{archive.Length} Bytes]");
                        var pickedImages = new List<ImageStruct>();
                        var remainingBytes = archive.Length;

                        Writer.VerboseInfo($"Picking Best Image from collection");
                        var leftJoin = imagestructs.Where(im => im.Capacity > remainingBytes);
                        if (leftJoin.Count() > 0)
                        {
                            var selection = leftJoin.Select(image => new KeyValuePair<long, ImageStruct>(remainingBytes - image.Capacity, image));
                            var min = selection.Max(e => e.Key);
                            var selected = selection.First(e => e.Key == min);
                            pickedImages.Add(selected.Value);
                            remainingBytes = selected.Key;
                        }
                        else
                        {
                            foreach (var im in Helper.FastReverse(imagestructs))
                            {
                                remainingBytes -= im.Capacity;
                                pickedImages.Add(im);
                                if (remainingBytes <= 0)
                                {
                                    break;
                                }
                            }
                        }
                        if (remainingBytes <= 0)
                        {
                            Writer.VerboseInfo($"selected {pickedImages.Count} Images");
                            foreach (var selected in pickedImages)
                            {
                                Writer.VerboseInfo($"[{Helper.HumanReadableBytes(selected.Capacity)}] {selected.File.FullName} ");
                            }
                            try
                            {
                                Writer.VerboseInfo($"Starting image processing");
                                HashSet<string> uniqueNames = new HashSet<string>();
                                foreach (var im in pickedImages)
                                {
                                    while (uniqueNames.Contains(im.FileName))
                                    {
                                        im.FileName = im.FileName + "_copy";
                                    }
                                }
                                Steganography.InsertZip(archive, new Stack<ImageStruct>(pickedImages));
                            }catch(Exception e)
                            {
                                Writer.VerboseException(e.Message);
                                Writer.Error("Failed to process the images");
                                return 4;
                            }
                        }
                        else
                        {
                            long total = 0;
                            imagestructs.ForEach(im => total += im.Capacity);
                            Writer.Info($"The given data size is {Helper.HumanReadableBytes(archive.Length)} ({archive.Length} Bytes)");
                            Writer.Info($"The given images capacity is {Helper.HumanReadableBytes(total)} ({total} Bytes)");
                            Writer.Error($"images capacity is less than data size (need more {remainingBytes} Bytes), try increasing bit count or add more images");
                            return 5;
                        }
                    }
                    catch(Exception e)
                    {
                        Writer.VerboseException(e.Message);
                        Writer.Error($"Failed to compress data");
                        return 3;
                    }

                    
                }
                catch(Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Error($"Could not create a temporary directory [{tdirname}]");
                    return 5;
                }
            }
            return 0;
        }
    }
}
