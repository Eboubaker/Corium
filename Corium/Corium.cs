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
                                              "which allows to hide files inside images");
            var hideCommand = new Command("hide", "Hide files inside images");
            var extractCommand = new Command("extract", "Extract hidden files from previously processed images");

            rootCommand.AddCommand(hideCommand);
            rootCommand.AddCommand(extractCommand);

            CoriumOptions.AddCommandsOptions(rootCommand, hideCommand, extractCommand);

            // ReSharper disable once ConvertToLocalFunction
            Func<IEnumerable<FileSystemInfo>, IEnumerable<FileSystemInfo>, DirectoryInfo, string, int, bool, bool, bool,
                bool, int> hideProxy = (images, data, output,
                collection, bits, alpha,
                verbose, silent, noSubDirectory) =>
            {
                RegisterGlobalOptions(verbose, alpha, bits, output, collection, noSubDirectory);
                return RunHideOptions(images, data);
            };

            // ReSharper disable once ConvertToLocalFunction
            Func<FileSystemInfo[], DirectoryInfo, string, int, bool, bool, bool, bool, int> extractProxy = (
                images, output, collection,
                bits, alpha, verbose, silent, no_collection_directory) =>
            {
                RegisterGlobalOptions(verbose, alpha, bits, output, collection, no_collection_directory);
                return RunExtractOptions(images);
            };

            hideCommand.Handler = CommandHandler.Create(hideProxy);
            extractCommand.Handler = CommandHandler.Create(extractProxy);

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

        private static void RegisterGlobalOptions(bool verbose, bool alpha, int bits, DirectoryInfo output,
            string collection, bool noCollectionDirectory)
        {
            Context.Bits = bits;
            Context.Verbose = verbose;
            Context.Alpha = alpha;
            Context.ChannelCount = Convert.ToByte(alpha) + 3;
            Context.CollectionNumber = string.IsNullOrEmpty(collection) ? 0 : Convert.ToInt32(collection, 16);
            Context.CollectionString = collection.ToUpper();
            Context.OutDir = output;
            Context.NoCollectionFolder = noCollectionDirectory;
        }

        private static int RunHideOptions(IEnumerable<FileSystemInfo> imagePaths, IEnumerable<FileSystemInfo> data)
        {
            if (!Context.NoCollectionFolder)
                Context.OutDir = Context.OutDir.CreateSubdirectory(Context.CollectionString);
            Writer.VerboseFeedBack("Verbose mode is active");

            if (!Context.OutDir.Exists)
            {
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
            }

            Writer.FeedBack("Locating and parsing Images");
            var images = new List<ImageWrapper>();
            foreach (var path in imagePaths)
            {
                foreach (var f in path.GetAllFilesRecursively())
                {
                    try
                    {
                        if (f.IsRecognisedImageFile())
                            images.Add(new ImageWrapper(f));
                        else
                            throw new OutOfMemoryException(
                                $"skipped image: unrecognized/invalid image format: {f.FullName}");
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
                Writer.FeedBack("Copying data to temporary directory...");
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
                    Writer.FeedBack("Compressing data");
                    var total = 0f;
                    var compressible = tempDirectory.GetAllFilesRecursively().Aggregate(0f, (acc, f) =>
                    {
                        total += f.Length;
                        return acc + (f.OpenRead().IsCompressible() ? f.Length : 0);
                    });
                    var compressionLevel =
                        compressible / total > .7 ? CompressionLevel.Optimal : CompressionLevel.NoCompression;
                    Writer.VerboseFeedBack($"Using compression level " +
                                           (compressionLevel == CompressionLevel.Optimal ? "Optimal" : "Zero"));
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
                        Writer.FeedBack($"selected {pickedImages.Count} Images");
                        var sum = 0;
                        foreach (var selected in pickedImages)
                        {
                            sum += selected.Capacity;
                            Writer.VerboseFeedBack(
                                $"[{selected.Capacity.HumanReadableSize()}] image {selected.OriginFile.FullName} ");
                        }

                        Writer.FeedBack(
                            $"Selected images capacity is {sum.HumanReadableSize()} data size is {archive.Length.HumanReadableSize()}");
                        try
                        {
                            Writer.FeedBack("Starting image processing");
                            var uniqueNames = new HashSet<string>();
                            foreach (var im in pickedImages)
                            {
                                var counter = 2;
                                while (uniqueNames.Contains(im.Name) ||
                                       File.Exists(Path.Combine(Context.OutDir.FullName,
                                           im.FileName(Context.OutExtension))))
                                {
                                    im.Name = im.OriginName + "_" + counter;
                                    counter++;
                                }

                                uniqueNames.Add(im.Name);
                            }

                            var head = new ImageInfo
                            {
                                Fingerprint = ImageInfo.CoriumFingerprint,
                                DataIdentifier = Context.CollectionNumber,
                                TotalImages = pickedImages.Count
                            };
                            ImageCollection.WriteToStream(archive.OpenRead(), pickedImages, head);
                            Writer.FeedBack($"Generated {pickedImages.Count} image" +
                                            (pickedImages.Count > 1 ? "s" : "") +
                                            " with collection" +
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

        private static int RunExtractOptions(FileSystemInfo[] searchPaths)
        {
            Writer.FeedBack("Locating and parsing Images");
            var images = new List<ImageWrapper>();
            foreach (var path in searchPaths)
            foreach (var f in path.GetAllFilesRecursively())
                try
                {
                    images.Add(new ImageWrapper(f));
                }
                catch (IndexOutOfRangeException e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Warning($"Image is too small [{f}]");
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

            if (images.Count == 0)
            {
                Writer.Error("No supported images were found in the given paths");
                searchPaths.ToList().ForEach(Console.WriteLine);
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
                    Writer.VerboseWarning($"Invalid image signature for image [{m.OriginFile.FullName}]");
                    return true;
                }
                catch (Exception e)
                {
                    Writer.VerboseWarning(
                        $"Error occured while trying to read image head for image [{m.OriginFile.FullName}] [{e.Message}]");
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

            if (imageCollections.Count == 0)
            {
                Writer.Error(removed > 0
                    ? "no signed image found, make sure --bits and --alpha options are correct"
                    : "no valid image was found in specified paths");
                return Error.NO_COLLECTION_FOUND;
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
                    return Error.COLLECTION_NOT_FOUND;
                }
            }

            if (Context.CollectionNumber == 0 && imageCollections.Count > 1)
                Writer.Suggestion(
                    "more than one collection detected, use --collection option to only extract a specific collection");

            var failed = false;
            foreach (var (collectionNumber, collection) in imageCollections)
            {
                var collectionHash = Convert.ToString(collectionNumber, 16).ToUpper();
                try
                {
                    Writer.FeedBack($"Extracting collection {collectionHash}");
                    var tempZip = ImageCollection.ExtractCollection(collection);
                    try
                    {
                        var outPath = Path.Combine(Context.OutDir.FullName, collectionHash);
                        if (Directory.Exists(outPath))
                        {
                            Writer.Error($"output directory already exists: {outPath}");
                            throw new Exception("output directory already exists");
                        }

                        try
                        {
                            Writer.VerboseFeedBack($"creating output directory {outPath}");
                            Directory.CreateDirectory(outPath);
                        }
                        catch (IOException e)
                        {
                            Writer.Error($"could not create output directory: {outPath} caused by: {e.Message}");
                        }

                        Writer.FeedBack("Decompressing data");
                        ZipFile.ExtractToDirectory(tempZip.FullName, outPath, true);
                        Writer.FeedBack($"Extracted collection {collectionHash} to output directory {outPath}");
                        tempZip.Delete();
                    }
                    catch
                    {
                        tempZip.Delete();
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Writer.VerboseException(e.Message);
                    Writer.Error($"Failed to extract collection {collectionHash}\nuse verbose logging for details");
                    failed = true;
                }
            }

            return failed ? Error.COLLECTION_EXTRACT_FAILED : 0;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Error
    {
        // 1 is reserved for arg parsing fail
        // ReSharper disable once UnusedMember.Global
        public const int ARG_PARSE_FAIL = 1;
        public const int TEMP_DIR_CREATE_FAIL = 2;
        public const int DATA_COMPRESS_FAIL = 3;
        public const int INSUFFICIENT_IMAGE_SIZE = 4;
        public const int IMAGE_PROCESS_FAIL = 5;
        public const int ITEM_COPY_FAIL = 6;
        public const int NO_IMAGE_FOUND = 7;
        public const int COLLECTION_EXTRACT_FAILED = 8; // one or all collection extraction failed
        public const int UNKNOWN = 9;
        public const int OUT_DIR_CREATE_FAIL = 10;
        public const int NO_COLLECTION_FOUND = 11;
        public const int COLLECTION_NOT_FOUND = 12;
    }
}