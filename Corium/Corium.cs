using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Corium.Core;
using Corium.Utils;

namespace Corium
{
    public static class Corium
    {
        public static async Task<int> Main(params string[] args)
        {
            Context.Silent = args.Count(arg => arg == "-s" || arg == "--silent") > 0;
            if (Context.Silent)
            {
                Console.SetOut(TextWriter.Null);
                Console.SetError(TextWriter.Null);
            }

            var rootCommand = new RootCommand("Corium is an image steganography utility " +
                                              "which allows to hide data inside images");
            var hideCommand = new Command("hide", "Hide files inside images");
            var extractCommand = new Command("extract", "Extract hidden files from images");

            rootCommand.AddCommand(hideCommand);
            rootCommand.AddCommand(extractCommand);

            CoriumOptions.AddCommandsOptions(rootCommand, hideCommand, extractCommand);

            // ReSharper disable once ConvertToLocalFunction
            Func<IEnumerable<FileSystemInfo>, IEnumerable<FileSystemInfo>, DirectoryInfo, string, int, bool, bool, bool,
                int> hideAction = (images, data, output,
                collection, bits, alpha,
                verbose, silent) =>
            {
                var r = RegisterGlobalOptions(verbose, alpha, bits, output, collection);
                return r != 0 ? r : RunHideOptions(images, data);
            };

            // ReSharper disable once ConvertToLocalFunction
            Func<IEnumerable<FileSystemInfo>, DirectoryInfo, string, int, bool, bool, bool, int> extractAction = (
                images, output, collection,
                bits, alpha, verbose, silent) =>
            {
                var r = RegisterGlobalOptions(verbose, alpha, bits, output, collection);
                return r != 0 ? r : RunExtractOptions(images);
            };

            hideCommand.Handler = CommandHandler.Create(hideAction);
            extractCommand.Handler = CommandHandler.Create(extractAction);

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception e)
            {
                Writer.Error("Program could not run due to unknown error");
                Writer.VerboseException(e.Message);
                return Error.UNKNOWN;
            }
        }

        private static int RegisterGlobalOptions(bool verbose, bool alpha, int bits, DirectoryInfo output,
            string collection)
        {
            Context.Bits = bits;
            Context.Verbose = verbose;
            Context.Alpha = alpha;
            Context.ChannelCount = Convert.ToByte(alpha) + 3;
            Context.CollectionNumber = Convert.ToInt32(collection);
            Context.CollectionString = collection.ToUpper();
            Context.OutDir = output;

            Writer.VerboseFeedBack("Verbose mode is active");

            if (Context.OutDir.Exists) return 0;
            Writer.VerboseFeedBack("Output directory does not exist, trying to create output directory");
            try
            {
                Context.OutDir.Create();
            }
            catch (Exception e)
            {
                Writer.VerboseException(e.Message);
                Writer.Error("Could not create output directory");
                return Error.OUT_DIR_CREATE_FAIL;
            }

            return 0;
        }

        private static int RunExtractOptions(IEnumerable<FileSystemInfo> searchPaths)
        {
            Writer.FeedBack("Locating and parsing Images");
            var images = new List<ImageWrapper>();
            foreach (var path in searchPaths)
            {
                foreach (var f in path.GetAllFilesRecursively())
                {
                    try
                    {
                        images.Add(new ImageWrapper(f));
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Writer.VerboseException(e.Message);
                        Writer.Error($"Image is too small [{f}]");
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

            if (images.Count == 0)
            {
                Writer.Error("No supported images were found in the given paths");
                images.ToList().ForEach(Console.WriteLine);
                return Error.NO_IMAGE_FOUND;
            }

            Writer.VerboseFeedBack("Filtering non singed images");
            var removed = images.RemoveAll(m =>
            {
                try
                {
                    m.ReadInfo();
                }
                catch (InvalidDataException)
                {
                    Writer.VerboseWarning($"Invalid image signature for image [{m.OriginName}]");
                    return true;
                }
                catch (Exception e)
                {
                    Writer.VerboseWarning(
                        $"Error occured while trying to read image head for image [{m.OriginName}] [{e.Message}]");
                    return true;
                }

                return false;
            });
            Writer.VerboseFeedBack($"Removed {removed} non signed images");
            Writer.VerboseFeedBack($"Found {images.Count} signed images");
            var imageCollections = new Dictionary<int, List<ImageWrapper>>();
            foreach (var group in images.GroupBy(e => e.Info.DataIdentifier))
            {
                var ims = group.ToList();
                Writer.VerboseFeedBack($"Sorting images of collection {group.Key}");
                ims.Sort((m1, m2) => m1.Info.ImageIndex - m2.Info.ImageIndex);
                imageCollections.Add(group.Key, ims);
            }

            Writer.FeedBack($"Found {imageCollections.Count} data collections");
            if (Context.CollectionNumber != 0)
            {
                imageCollections = imageCollections
                    .Where(group => group.Key == Context.CollectionNumber)
                    .ToDictionary(group => group.Key, group => group.Value);
                if (imageCollections.Count == 0)
                {
                    Writer.Error($"Collection {Context.CollectionString} was not found");
                }
            }

            var failed = false;
            foreach (var (group, collection) in imageCollections)
            {
                try
                {
                    Writer.FeedBack($"Extracting collection {group}");
                    var file = Tyrozyna.ExtractCollection(collection);
                    try
                    {
                        var outDir = Path.Combine(Context.OutDir.FullName, Convert.ToString(group, 16).ToUpper());
                        ZipFile.ExtractToDirectory(file.FullName, outDir, true);
                        Writer.FeedBack($"Extracted collection {group} to output directory {outDir}");
                        file.Delete();
                    }
                    catch
                    {
                        file.Delete();
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Error($"Failed to extract collection {group}");
                    failed = true;
                }
            }

            return failed ? Error.COLLECTION_EXTRACT_FAILED : 0;
        }

        private static int RunHideOptions(IEnumerable<FileSystemInfo> imagePaths, IEnumerable<FileSystemInfo> data)
        {
            Writer.FeedBack("Locating and parsing Images");
            var images = new List<ImageWrapper>();
            foreach (var path in imagePaths)
            {
                foreach (var f in path.GetAllFilesRecursively())
                {
                    try
                    {
                        images.Add(new ImageWrapper(f));
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

            if (images.Count == 0)
            {
                Writer.Error("No supported images were found in the given paths");
                images.ToList().ForEach(Console.WriteLine);
                return Error.NO_IMAGE_FOUND;
            }

            Writer.VerboseFeedBack("Sorting Images");
            images.Sort((a, b) => a.Capacity - b.Capacity);
            Writer.FeedBack($"Found {images.Count} Images");
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
            try
            {
                Writer.VerboseFeedBack($"Creating temporary directory [{tempDir}]");
                var tempDirectory = Directory.CreateDirectory(tempDir);
                Writer.VerboseFeedBack("Copying data to directory");
                foreach (var source in data)
                {
                    var dest = "";
                    try
                    {
                        switch (source)
                        {
                            case FileInfo _:
                            {
                                dest = Path.Combine(tempDirectory.FullName, source.Name);
                                File.Copy(source.FullName, dest);
                                break;
                            }
                            case DirectoryInfo dir:
                                dest = tempDirectory.FullName;
                                dir.CopyTo(tempDirectory);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Writer.VerboseException(e.Message);
                        Writer.Error($"Failed to copy item [{source}] to [{dest}]");
                        return Error.ITEM_COPY_FAIL;
                    }
                }

                try
                {
                    Writer.VerboseFeedBack("Compressing data");
                    var total = 0f;
                    var compressible = tempDirectory.GetAllFilesRecursively().Aggregate(0f, (acc, f) =>
                    {
                        total += f.Length;
                        return acc + (f.OpenRead().IsCompressible() ? f.Length : 0);
                    });
                    var compressionLevel =
                        compressible / total > .7 ? CompressionLevel.Optimal : CompressionLevel.NoCompression;
                    Writer.VerboseFeedBack($"Using compression level " +
                                           (compressionLevel == CompressionLevel.Optimal ? "MAX" : "0"));
                    var archive = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
                    ZipFile.CreateFromDirectory(tempDirectory.FullName, archive.FullName, compressionLevel, false);
                    var pickedImages = new List<ImageWrapper>();
                    var requiredBytes = archive.Length;

                    Writer.VerboseFeedBack("Picking Best Image from collection");
                    var bigImages = images.Where(im => im.Capacity > requiredBytes).ToList();
                    if (bigImages.Count > 0)
                    {
                        var bytes = requiredBytes;
                        var sizes = bigImages.Select(image =>
                            new KeyValuePair<long, ImageWrapper>(image.Capacity - bytes, image)).ToList();
                        var min = sizes.Min(e => e.Key);
                        pickedImages.Add(sizes.First(e => e.Key == min).Value);
                        requiredBytes = 0;
                    }
                    else
                    {
                        foreach (var im in
                            images.ToArray()
                                .Reverse()) // this is a lazy reverse (returns new iterator which reads backwards)
                        {
                            requiredBytes -= im.Capacity;
                            pickedImages.Add(im);
                            if (requiredBytes <= 0)
                            {
                                break;
                            }
                        }
                    }

                    if (requiredBytes <= 0)
                    {
                        Writer.VerboseFeedBack($"selected {pickedImages.Count} Images");
                        foreach (var selected in pickedImages)
                        {
                            Writer.VerboseFeedBack(
                                $"[{selected.Capacity.HumanReadableSize()}] {selected.Origin.FullName} ");
                        }

                        try
                        {
                            Writer.VerboseFeedBack("Starting image processing");
                            var uniqueNames = new HashSet<string>();
                            foreach (var im in pickedImages)
                            {
                                while (uniqueNames.Contains(im.Name))
                                {
                                    im.Name += "_copy";
                                }

                                uniqueNames.Add(im.Name);
                            }

                            var head = new ImageInfo
                            {
                                Fingerprint = ImageInfo.CoriumFingerprint,
                                DataIdentifier = Context.CollectionNumber,
                                TotalImages = images.Count,
                            };
                            Tyrozyna.InsertCancer(archive.OpenRead(), pickedImages, head);
                            Writer.FeedBack($"Done!, generated {pickedImages.Count} images with collection" +
                                            $" key {Context.CollectionString}" +
                                            $" in directory {Context.OutDir.FullName}");
                        }
                        catch (Exception e)
                        {
                            Writer.VerboseException(e.Message);
                            Writer.Error("Failed to process the images");
                            return Error.IMAGE_PROCESS_FAIL;
                        }
                    }
                    else
                    {
                        var capacity = images.Aggregate(0, (acc, im) => acc + im.Capacity);
                        Writer.FeedBack(
                            $"The given data size is {archive.Length.HumanReadableSize()} ({archive.Length} Bytes)");
                        Writer.FeedBack(
                            $"The given images capacity is {capacity.HumanReadableSize()} ({capacity} Bytes) using {Context.Bits} bits and {Context.ChannelCount} channels");
                        Writer.Error(
                            $"images capacity is less than data size, more {requiredBytes.HumanReadableSize()}({requiredBytes} Bytes) is required, try increasing bits option or provide more images");
                        return Error.INSUFFICIENT_IMAGE_SIZE;
                    }
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Error("Failed to compress data");
                    return Error.DATA_COMPRESS_FAIL;
                }
            }
            catch (Exception e)
            {
                Writer.VerboseException(e.Message);
                Writer.Error($"Could not create a temporary directory [{tempDir}]");
                return Error.TEMP_DIR_CREATE_FAIL;
            }

            return 0;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class Error
    {
        // 1 is reserved for arg parsing fail
        public const int TEMP_DIR_CREATE_FAIL = 2;
        public const int DATA_COMPRESS_FAIL = 3;
        public const int INSUFFICIENT_IMAGE_SIZE = 4;
        public const int IMAGE_PROCESS_FAIL = 5;
        public const int ITEM_COPY_FAIL = 6;
        public const int NO_IMAGE_FOUND = 7;
        public const int COLLECTION_EXTRACT_FAILED = 8;
        public const int UNKNOWN = 9;
        public const int OUT_DIR_CREATE_FAIL = 10;
    }
}