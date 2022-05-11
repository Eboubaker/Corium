﻿# Corium

Corium is an image [steganography](https://en.wikipedia.org/wiki/Steganography) utility which can hide files inside
images.

## Installation

download self-contained specific platform executable or multi-platform dotnet executable
from [releases](https://github.com/Eboubaker/Corium/releases).

## Usage

```
Usage:
  Corium [options] [command]

Options:
  -v, --verbose   Turn on verbose mode which will show more debug information.. [default: False]
  -s, --silent    Turn on silent mode (no messages will appear on the console, if return code was 0 then operation was
                  successful).. [default: False]
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hide     Hide files inside images
  extract  Extract hidden files from previously processed images
```

### Hide command

```
hide
  Hide files inside images

Usage:
  Corium [options] hide

Options:
  -i, --images <images> (REQUIRED)  The path(s) to the image(s) or directory(s) containing images that is to be used by
                                    Corium for hiding files.
  -d, --data <data> (REQUIRED)      The path to the file(s) or directory(s) that is to be hidden inside the images.
  -o, --output <output>             The name or the path of the output directory(processed image collections will be
                                    dropped in this output directory). [default: processed]
  -a, --alpha                       Use alpha channels in the output images (increases image capacity). [default: False]
  -b, --bits <bits>                 Set how many bits to be used (1-8) from every pixel channel default 3. [default: 3]
  -c, --collection <collection>     Set the collection number of the output images. [default: 1A2805D4]
  -n, --no-sub-directory            by default processed images will be placed in a folder whose name is the collection
                                    name of the imagesenabling this option will disable this feature and all images
                                    will be put in the same output directory. [default: False]
  -v, --verbose                     Turn on verbose mode which will show more debug information.. [default: False]
  -s, --silent                      Turn on silent mode (no messages will appear on the console, if return code was 0
                                    then operation was successful).. [default: False]
  -?, -h, --help                    Show help and usage information

```

### Extract command

```
extract
  Extract hidden files from previously processed images

Usage:
  Corium [options] extract

Options:
  -i, --images <images> (REQUIRED)  The path(s) to the image(s) or directory(s) containing images that is to be used by
                                    Corium to extract files.
  -c, --collection <collection>     Set the target collection number to be extracted from the input images if the
                                    images contains more than one collection,
                                    this will cause Corium to only extract from the specified collection if it was found
                                    if no collection number was specified corium will extract all found collections
                                    [default: <Auto-Generated>]
  -o, --output <output>             The name or the path of the output directory(extracted files will be dropped in
                                    this directory). [default: extracted]
  -b, --bits <bits>                 Set how many bits to be used (1-8) from every pixel channel default 3, (must be
                                    same as when data was hidden inside images otherwise extraction will fail).
                                    [default: 3]
  -a, --alpha                       (DEFAULT None) Use alpha channels in the input images (set to true if images were
                                    used by corium with alpha option set to true when hiding files). [default: False]
  -ncd, --no-collection-directory   by default extracted files will be placed in a folder whose name is the collection
                                    name of the imagesenabling this option will disable this feature and all collection
                                    files will be put in the same output directory. [default: False]
  -v, --verbose                     Turn on verbose mode which will show more debug information.. [default: False]
  -s, --silent                      Turn on silent mode (no messages will appear on the console, if return code was 0
                                    then operation was successful).. [default: False]
  -?, -h, --help                    Show help and usage information

```

## Example

The source code of this project is hidden inside the
image [source-code.jpg](https://raw.githubusercontent.com/Eboubaker/Corium/main/source-code.jpg).
![source-code](https://raw.githubusercontent.com/Eboubaker/Corium/main/source-code.jpg)

Download this image and extract the files with corium.

```
wget -O source-code.jpg https://raw.githubusercontent.com/Eboubaker/Corium/main/source-code.jpg
corium --verbose extract --images source-code.jpg
```