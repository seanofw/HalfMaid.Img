# HalfMaid.Img

Copyright &copy; 2019-2024 by Sean Werkema

Released under the [MIT Open-Source License](LICENSE.txt).  Share and enjoy 😁

-----------------------------------------------------------------------------

## Introduction

_Another_ image library?  _Why???_

The HalfMaid image library is designed to be a fast, flexible tool for working with images in .NET and C#.  It's designed as a library, not a framework:  It does what you ask it to do, and it does its best to get out of your way the rest of the time.  Most image libraries want you to work _their_ way:  This library intentionally does its best _not_ to care.

There are a few core design principles in this library:

1. **Pixel data is always stored in plain-jane user-accessible arrays.** \
    \
    Most imaging libraries — for speed — store your image's data hidden away behind at least two or three layers of abstraction.  You don't know what the internal format is, and you're not supposed to care, and if you want access to raw pixel data, special, expensive operations are required.  We take the opposite view here:  There is a literal `Data[]` array of pixels available at all times, and if you want to pin that memory and manipulate the bytes directly, nothing's stopping you.  They're _your_ images, and they're _your_ data.

2. **No storage or layout guesswork.** \
    \
    Okay, this is a lot like the point above, but it really means that if you've got RGB pixels, they're always stored as three bytes, in the order of _red_, _green_, and then _blue_.  Not BGR0, not BGAR, not every other row of pixels inverted, not ridiculous orders or data arrangements that haven't made sense since 1986.  There are no padding bytes, there are no extra bytes in each line, and the lines aren't backwards or upside-down.  Everythiong is left-to-right and top-to-bottom — simple, predictable layouts.  All of the excuses for why image data is arranged any other way than the _most obvious layouts_ need to join fanny packs, slap bracelets, and the Macarena in the dustbin of history.

3. **Batteries included.** \
    \
    In most imaging libraries, flipping an image horizontally requires an astonishing amount of code:  Make a graphics surface, calculate a transformation matrix, allocate storage, render the image transformed, cleanup, and on and on.  This is insanity.  It should be — and here it _is_ — no harder than calling `FlipHorz()`.  Want to draw on your images?  `DrawLine()`, `FillRect()`, `DrawBezier()`, it's all built-in.  Need to convert between image formats?  Look no farther than `ToImage32()` or `Quantize()` and `Dither()`.  Trying to load or save an image?  Call `Image32.Load()` or `myImage.Save()`.  Yes, it's less flexible than a giant rendering pipeline — but it covers 99% of the cases you'll ever run into, you can do it in one line of code.

4. **As fast as possible, without violating rule #1.** \
    \
    You shouldn't feel like you need to switch tools to get something done.  So all of those image operations?  They're all nice and fast.  Unrolled loops.  Strength reduction.  Tail cases.   Yes, your GPU could probably do all of this faster — at the added cost of shipping the image data back and forth.  We're doing all the work on the CPU as fast as we can so that our built-in operations don't get in the way of your custom image hackery.

5. **Direct support for common file formats.** \
    \
    Out-of-the-box, with nothing else installed, you can read and write JPEG, PNG, GIF, Targa, and BMP, and there's a reader for Aseprite files too.  (You can even work with _animated_ GIF images, if you want to!)  For JPEG, we embed a copy of LibJpegTurbo, which is as turbo as its name implies, but for all the rest, we include _fully-managed code_ C# implementations.  Install one Nuget package and you're done.

6. **Portable, reusable everywhere.** \
    \
    Using .NET 8?  It'll be like butter.  .NET Core 3.1?  No problem.  Still on .NET Framework 4.x?  We got you covered.  Developing on MacOS, testing on Windows, deploying on Linux?  Yes, yes, and yes.  You shouldn't have to give up on good tools just because you're working in older code.

7. **Painfully obvious — but documented anyway.** \
    \
    It should be as clear as possible what every method and parameter and does, with no naming fluff or extra parameters to get something done.  `Image32`?  Shocker:  It's an image where each pixel is 32 bits!  (And in the obvious order of R, G, B, and A.)  It's `image.Rotate90()`, not `graphicsContext.ApplyConstrainedAffineProjection(image, CommonMatrixes.PiOverTwo)`.  But just in case something's not immediately obvious?  We have `///` Intellisense documentation on _everything_.  Press `.` and never have to guess how to use it.

8. **Compatible with your environment.** \
    \
    Trying to work with other external image systems too?  There's a `HalfMaid.Img.OpenGL` add-on that provides nice easy C#-friendly `Texture` objects.  The `HalfMaid.Img.Gdi` add-on will give you Windows `Bitmap` and `Clipboard` support.  You're only one Nuget package away from full support for your favorite graphics environment.  And, of course, since the image data is just bytes in memory in a predictable layout, it's not hard to roll your own if you ever need to.

9. **Managed and safe — everywhere that's reasonable.** \
    \
    You code in .NET because you don't like segfaults and heap-stomping and buffer overruns.  Safe code is good code, so wherever possible (given rule #4), we stick to safe, managed code throughout.  C++ might go faster, but C# is safer, and there's a lot to be said for code that won't unpredictably explode.

10. **Open source.** \
    \
    Everything's MIT Open-Source Licensed*, so you can use it anywhere: public, private, free, open, closed, educational, community, commercial, or otherwise.  Licensing shouldn't hold you back from using good tools everywhere you go. \
    \
    (*Note: [LibJPEG Turbo](https://github.com/libjpeg-turbo/libjpeg-turbo) uses the BSD 3-clause license, not the MIT license, but for nearly every real-world use case, they're effectively the same.)

All that sound good?  Then keep reading :-)

-----------------------------------------------------------------------------

## Installation

Nuget is your friend.

* **HalfMaid.Img** - The core library, with common image-manipulation operations, image file I/O, and lots, lots more.  The core image data structures are `Image32` and `Image8` and `Color32`.
* **HalfMaid.Img.OpenGL** - This adds `Texture32` and `Texture8`, which do exactly what you think they do.
* **HalfMaid.Img.Gdi** - This adds various Windows-flavored extension methods on `Image32` and `Image8` and `Bitmap` too, as well as the `ClipboardImage` class.

This library has an external dependency on the **OpenTK.Mathematics** Nuget package.  Which, honestly, you should be using anyway because it's awesome, much nicer than System.Numerics for all of your vector/matrix needs.  For old platforms like .NET Framework 4.x, we _embed a full port_ of **OpenTK.Mathematics** and various newer `System` classes so that you can use `HalfMaid.Img` exactly the same way — no compromises, no tweaks.

## Classes

These are the classes you're going to work with a lot in this library:

### Core classes

* **Color32**.  This is a `struct`, with four bytes named `R`, `G`, `B`, and `A`.  It's 32 bits long. \
    \
    It also has dozens of operators and common methods so you can work with its data easily, static properties for common colors like `Fuschia` and `Olive`, parsing and stringification methods for every common format, and even conversions to and from other common color spaces like HSL.

* **Color24**.  This is a `struct` like `Color32`, but it's just `R`, `G`, and `B`.  It's 24 bits long.  It also has dozens of operators and methods and static properties and converters.

* **Image32**.  This is a `class`, and it has an `int Width`, an `int Height`, and a `Color32[] Data`.  The pixel data is arranged top-to-bottom, left-to-right. \
    \
    It also has dozens and dozens of methods for manipulating its pixels, and it has sensible operator overloads, and it has `Load()` and `Save()` methods for getting data in and out of it.  You can `Blit()` and `Resample()` and `Draw()` and `Crop()` and so many more, all as methods right on the class itself.

* **Image8**.  This is a `class`, and it has an `int Width`, an `int Height`, a `byte[] Data`, and a `Color32[] Palette`.  The pixel data is arranged top-to-bottom, left-to-right. \
    \
    Just like `Image32`, this has dozens and dozens of methods for manipulating its pixels, and sensible operator overloads, and `Load()` and `Save()` and all the rest.

### Rational API

Are you working in F#?  Or do you just really like immutable data structures?  We've got you covered too!

* **PureImage32**.  This is like `Image32`, and it has nearly the same operations (and it uses `Image32` under the hood to do the heavy lifting!), but every method in it is declared `[Pure]` and has rational, immutable semantics:  Image + image = new image.  Seriously.  Even when a method doesn't really make a lot of sense to be implemented in a pure, rational form, it's implemented that way:  `myPureImage.DrawLine()` really will give you a copy of the image with a new line drawn on it.

* **PureImage8**.  Like `Image8`, but the pure, immutable version.

### Other classes

There are *lots* more classes in this library.  Here are a few others that might be worth knowing about:

* **IImage**.  This is an interface shared by both `Image32` and `Image8` so that you can have a single shape to describe both of them.  It doesn't have a *lot* of methods on it, but it includes enough that you can do the basics in a uniform way.

* **IImageLoader**/**IImageSaver**.  These interfaces describe classes that know how to load or save image files in a specific file format.

* **Rect**.  This is a `struct`, and it has an `int X`, an `int Y`, an `int Width`, and an `int Height`.  OpenTK.Mathematics gives us a `Box2i`, but that's often a less convenient structure than `Rect` for common drawing operations, so we include an integer-valued `struct Rect`.  It includes several dozen methods and operators for working with it easily and interoperating with `Box2i` and `Vector2i`.

* **Palettes**.  Just like `Color32/24` include common color names, the `Palettes` class includes static instances of common 8-bit palettes so you don't have to roll your own.

* **Zlib**.  .NET has included support for Deflate-based data compression for a long time, but support for Zlib-style headers and checksums was only added in .NET 6.  This class provides a compatibility layer so that Zlib-style data compression works all the way back into the .NET Framework 4.x era, _without_ needing an external implementation.

-----------------------------------------------------------------------------

## Image operations

This library has _lots_ of common image operations are built in.  Here's a table of the supported operations, with short summaries for each.  (This table is a _summary_:  For full details on method parameters, return values, and exceptions, you should refer to the extensive API reference documentation / Intellisense documentation.)

Note that constructors are documented separately below:  Each class includes several constructors, but the constructors differ substantially between classes because of the differences in their underlying data formats and semantics.

### Properties

| Name  | Availability | Summary |
| ----- |------------- | ------- |
| `Size` property | 32, 8, P32, P8 | The size of the image, represented as a 2D vector. |
| `Pure` property | 32, 8 | Wrap this image in a `Pure*` struct. |
| static `Empty` property | 32, 8, P32, P8 | A static image of size (0, 0). |

### Whole-image operations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `GetBytes()` | 32, 8, P32, P8 | Clone a copy of this image data, as a raw byte array. |
| `Replace()` | 32, 8 | Overwrite this image with an exact copy of another image. |
| `ReplacePalette()` | 8 | Replace this image's palette with a copy of the given palette data. |
| `Overwrite()` | 32, 8 | Overwrite this image's data with a copy of the provided data. |
| `Overwrite()` | 32, 8 | Overwrite this image's data with a copy of the provided data. |
| `Clone()` | 32, 8, P32, P8 | Make a perfect duplicate of this image. |
| `ToImage32()` | 32, 8, P32, P8 | Convert this to a 32-bit RGBA image. |
| `ToImage8()` | 32, P32 | Convert this to an 8-bit paletted image, using the given palette and dithering algorithm. |

### Loading and saving

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `SaveFile()` | 32, 8, P32, P8 | Save an image to a file, or to a byte array. |
| static `LoadFile()` | 32, 8, P32, P8 | Load an image from a file, an embedded resource, or a byte array. |
| static `LoadFileMetadata()` | 32, 8, P32, P8 | Load just the color format and width/height (fast). |
| static `GuessFileFormat()` | 32, 8, P32, P8 | Best-guess analysis for the image file format, based on name _and_ data. |
| static `RegisterFileFormat()` | 32, 8, P32, P8 | Include support for an additional image file format. |
| static `RegisterFileFormats()` | 32 | Include support for _many_ additional image file formats. |
| static `GetRegisteredLoaderFormats()` | 32 | Get the list of all image formats that can be loaded. |
| static `GetRegisteredSaverFormats()` | 32 | Get the list of all image formats that can be saved. |
| static `GetLoader()` | 32, 8, P32, P8 | Get the registered loader class for a specific image format. |
| static `GetSaver()` | 32, 8, P32, P8 | Get the registered saver class for a specific image format. |

### Resizing and resampling

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Resize()` | 32, 8, P32, P8 | Resize an image fast using nearest-neighbor sampling. |
| `ResizeToFit()` | 32, 8, P32, P8 | Resize to fit inside the given container, maintaining aspect ratio. |
| `Resample()` | 32, P32 | Resize an image using a resampling filter like bilinear or B-spline or Lanczos. |
| `ResampleToFit()` | 32, P32 | Resample to fit inside the given container, maintaining aspect ratio. |
| static `Fit()` | 32, 8, P32, P8 | Calculate best-fit inside a given container, maintaining aspect ratio. |

### Blitting and cropping

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| static `ClipBlit()` | 32, 8, P32, P8 | Clip a blit operation to be within the given bounds of the image. |
| static `ClipRect()` | 32, 8, P32, P8 | Clip the given drawing rectangle to be within the image. |
| `Crop()` | 32, 8, P32, P8 | Crop this image to the given rectangle (in place). |
| `Extract()` | 32, 8, P32, P8 | Crop this image to the given rectangle (non-destructive). |
| `Pad()` | 32, 8, P32, P8 | Pad the boundary of the image with extra pixels of a given color. |
| `Blit()` | 32, 8, P32, P8 | Copy a rectangle from source to destination, in one of several modes. (See [blit flags](#flags-and-options) below.) |
| `PatternBlit()` | 32, 8, P32, P8 | Copy from source to destination, repeating source to fill destination. |
| `ShadowBlit()` | 32 | Copy from source to destination, but copy all colors as black (with the same alpha). |

### Orientation transformations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `FlipVert()` | 32, 8, P32, P8 | Flip the image vertically. |
| `FlipHorz()` | 32, 8, P32, P8 | Flip the image horizontally. |
| `Rotate90()` | 32, 8, P32, P8 | Rotate the image 90 degrees clockwise. |
| `Rotate90CCW()` | 32, 8, P32, P8 | Rotate the image 90 degrees counterclockwise. |
| `Rotate180()` | 32, 8, P32, P8 | Rotate the image 180 degrees. |

### Color transformations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Mix()` | 32, 8, P32, P8 | Blend this image with another, using the given percentage to describe how much of each. |
| `Multiply()` | 32, P32 | Multiply every pixel component in the image by a scalar value. |
| `MultiplyPalette()` | 8, P8 | Multiply every color component in the palette by a scalar value. |
| `PremultiplyAlpha()` | 32, 8, P32, P8 | Premultiply the alpha component to the red, green, and blue channels. |
| `RemapColor()` | 32, P32 | Replace exact instances of the given color(s) with another. |
| `RemapColor()` | 8, P8 | Replace exact instances of the given pixel value(s) with another. |
| `RemapColor(matrix)` | 32, P32 | Treat each pixel as a vector, and apply a matrix transform to it. |
| `RemapPalette()` | 8, P8 | Replace the given palette entry(s) with another. |
| `RemapPalette(matrix)` | 8, P8 | Treat each palette entry as a vector, and apply a matrix transform to it. |
| `ExtractChannel()` | 32, 8, P32, P8 | Extract out the given color channel as a new `Image8`. |
| static `CombineChannels()` | 32, P32 | Combine separate `Image8`s representing R, G, B, and A as a new `Image32`. |
| `SwapChannels()` | 32, 8, P32, P8 | Exchange the R, G, B or A color channels with each other. |

### Color filters & effects

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Gamma()` | 32, 8, P32, P8 | Apply gamma to the image (or palette). |
| `Grayscale()` | 32, 8, P32, P8 | Convert the image (or palette) to grayscale, optionally using weighting. |
| `Grayscale8()` | 32, P32 | Convert the image to grayscale, optionally using weighting, and return an `Image8`. |
| `Desaturate()` | 32, 8, P32, P8 | Partially desaturate the image (or palette) toward grayscale. |
| `Sepia()` | 32, 8, P32, P8 | Remap the image to a sepia-toned image. |
| `HueSaturationLightness()` | 32, 8, P32, P8 | Adjust hue, saturation, and lightness of the image (or palette). |
| `HueSaturationBrightness()` | 32, 8, P32, P8 | Adjust hue, saturation, and brightness of the image (or palette). |
| `ToGrayscale256()` | 32, 8, P32, P8 | Convert the image to use the standard `Grayscale256` palette. |
| `Invert()` | 8, P8 | Replace each pixel value `v` with `255-v`. |
| `Invert()` | 32, P32 | Replace one or more of the red, green, and blue channels `v` with `255-v`. |
| `InvertPalette()` | 8, P8 | In the palette, replace one or more of the red, green, and blue channels `v` with `255-v`. |

### Color analysis and dithering

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Quantize()` | 32, P32 | Use a Heckbert median-cut to calculate an ideal palette. |
| `Histogram()` | 32, P32 | Generate a count each color used in the image. |
| static `GetDitherer()` | 32 | Retrieve an implementation of a specific dithering algorithm, by enum. |
| `ToImage8()` | 32, P32 | Convert this to an 8-bit paletted image, using the given palette and dithering algorithm. |

### Drawing operations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `this[x, y]` | 32, 8, P32*, P8* | Read or write the pixel at the given coordinate. |
| `DrawRect()` | 32, 8, P32, P8 | Draw a rectangular border with the given color. |
| `DrawLine()` | 32, 8, P32, P8 | Draw a line in the given color. |
| `DrawThickLine()` | 32, 8, P32, P8 | Draw a "thick" line (filled rectangular polygon) in the given color. |
| `DrawBezier()` | 32, 8, P32, P8 | Draw a Bézier curve in the given color. |
| `Fill()` | 32, 8, P32, P8 | Fill the entire image quickly with the given color. |
| `FillRect()` | 32, 8, P32, P8 | Fill a rectangular area with the given color. |
| `FillGradientRect()` | 32, P32 | Fill a rectangular area with the given color gradient. |
| `FillPolygon()` | 32, 8, P32, P8 | Fill an arbitrary polygon with the given color. |

### Image convolutions / Effects

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Emboss()` | 32, P32 | Apply a 3x3 emboss convolution kernel. |
| `Sharpen()` | 32, P32 | Apply a 3x3 sharpen convolution kernel. |
| `EdgeDetect()` | 32, P32 | Apply a 3x3 sharpen edge-detection kernel. |
| `BoxBlur()` | 32, P32 | Apply a 3x3 box-blur convolution kernel. |
| `ApproximateGaussianBlurFast()` | 32, P32 | Apply a 3x3 approximate Gaussian blur. |
| `Convolve3x3()` | 32, P32 | Apply a custom 3x3 convolution kernel (highly optimized case). |
| `ConvolveHorz()` | 32, P32 | Apply a custom Nx1 convolution kernel. |
| `ConvolveVert()` | 32, P32 | Apply a custom 1xN convolution kernel. |
| `Convolve()` | 32, P32 | Apply an arbitrary custom MxN convolution kernel. |

### Pixel transparency tests

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `MeasureContentWidth()` | 32, 8, P32, P8 | Measure width by searching for nontransparent pixels. |
| `MeasureContentHeight()` | 32, 8, P32, P8 | Measure height by searching for nontransparent pixels. |
| `IsRectTransparent()` | 32, 8, P32, P8 | Determine whether the given area is transparent. |
| `IsRowTransparent()` | 32, 8, P32, P8 | Determine whether the row of pixels is transparent. |
| `IsColumnTransparent()` | 32, 8, P32, P8 | Determine whether the column of pixels is transparent. |

### Color tests

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `IsGrayscale()` | 32, 8, P32, P8 | Determine if every color's R = G = B. |
| `IsGrayscale256()` | 8, P8 | Determine if this uses the `Grayscale256` palette. |
| `IsSingleChannel()` | 32, 8, P32, P8 | Determine if this is a single-channel image, and if so, which color channel. |

### Operators

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `operator +()` | 32, 8, P32, P8 | Add two images, componentwise per pixel. |
| `operator -()` | 32, 8, P32, P8 | Subtract two images, componentwise per pixel. |
| `operator *()` | 32, 8, P32, P8 | Multiply two images, componentwise per pixel. |
| `operator *(scalar)` | 32, 8, P32, P8 | Multiply this image by a scalar, componentwise per pixel. |
| `operator /(scalar)` | 32, 8, P32, P8 | Divide this image by a scalar, componentwise per pixel. |
| `operator `&#124;`()` | 32, 8, P32, P8 | Bitwise-or of two images, componentwise per pixel. |
| `operator &()` | 32, 8, P32, P8 | Bitwise-and of two images, componentwise per pixel. |
| `operator ^()` | 32, 8, P32, P8 | Bitwise-exclusive-or of two images, componentwise per pixel. |
| `operator <<(c)` | 32, 8, P32, P8 | Bitwise left shift, componentwise per pixel, by the given bit count `c`. |
| `operator >>(c)` | 32, 8, P32, P8 | Bitwise right shift, componentwise per pixel, by the given bit count `c`. |
| unary `operator ~()` | 32, 8, P32, P8 | Same as calling `Invert()`, replacing each `v` with `255-v`. |
| unary `operator -()` | 32, 8, P32, P8 | Replacing each color component `v` with `(256-v) % 256`. |
| `operator ==()` | 32, 8, P32, P8 | Deep equality test: True if size, pixels, and palette entries are identical. |
| `operator !=()` | 32, 8, P32, P8 | Deep equality test: False if size, pixels, and palette entries are identical. |

The equality and hash-code methods are not strictly operators, but they're closely related, so they're documented here as well.  Note that `operator==()` and `operator!=()` are implemented using `Equals()`:

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Equals()` | 32, 8, P32, P8 | Deep equality test: True if size, pixels, and palette entries are identical. |
| `GetHashCode()` | 32, 8, P32, P8 | Calculate a hash code for this image data (including pixels and palette). |

All image types implement `IEquatable<T>`, not just `Equals(object)`.

-----------------------------------------------------------------------------

## Image construction

There are *lots* of ways to create an image object, including creating both empty images, and copying from raw data sources and other images.  While the constructors are intended to be similar across each major image type, they necessarily differ due to differences in their underlying data, so they're each documented here explicitly.

| Name | Description |
| ---- | ----------- |
| `Image32(int, int)` | Construct an image of the given size (filled Transparent). |
| `Image32(int, int, Color32)` | Construct an image of the given size, filling with the given color. |
| `Image32(Vector2i)` | Construct an empty image of the given size (filled Transparent). |
| `Image32(Vector2i, Color32)` | Construct an empty image of the given size, filling with the given color. |
| `Image32(int, int, ReadOnlySpan<byte>, ReadOnlySpan<Color32>)` | Construct an image of the given size, by copying the given 8-bit data and palette. |
| `Image32(int, int, Color32[])` | Construct an image around the given color data, WITHOUT copying it. |
| `Image32(int, int, ReadOnlySpan<byte>)` | Construct an image by copying the raw bytes as color data. |
| `Image32(int, int, byte*, int)` | Construct an image by copying the raw bytes as color data. |
| `Image32(int, int, ReadOnlySpan<Color32>)` | Construct an image by copying the given color data. |
| `Image32(int, int, Color32*, int)` | Construct an image by copying the given color data. |
| `Image32(string, ImageFormat)` | Construct an image by loading it from a file, by name. |
| `Image32(ReadOnlySpan<byte>, string, ImageFormat)` | Construct an image by decoding preloaded file data. |

`PureImage32` includes all of the above, as well as `PureImage32(Image32)`, which wraps the given `Image32` instance in a `PureImage32` struct.

`PureImage32` also provides casting operators to/from `Image32`:  `Image32` --&gt; `PureImage32` is implicit, and `PureImage32` --&gt; `Image32` is explicit.

| Name | Description |
| ---- | ----------- |
| `Image8(int, int, IEnumerable<Color32>)` | Construct an empty image of the given size (filled with zeros). |
| `Image8(int, int, ReadOnlySpan<Color32>)` | Construct an empty image of the given size (filled with zeros). |
| `Image8(int, int, byte, IEnumerable<Color32>)` | Construct an image of the given size, filling with the given color. |
| `Image8(int, int, byte, ReadOnlySpan<Color32>)` | Construct an image of the given size, filling with the given color. |
| `Image8(Vector2i, IEnumerable<Color32>)` | Construct an empty image of the given size (filled with zeros). |
| `Image8(Vector2i, ReadOnlySpan<Color32>)` | Construct an empty image of the given size (filled with zeros). |
| `Image8(Vector2i, byte, IEnumerable<Color32>)` | Construct an image of the given size, filling with the given color. |
| `Image8(Vector2i, byte, ReadOnlySpan<Color32>)` | Construct an image of the given size, filling with the given color. |
| `Image8(int, int, ReadOnlySpan<byte>, IEnumerable<Color32>)` | Construct an image of the given size, by copying the given 8-bit data and palette. |
| `Image8(int, int, ReadOnlySpan<byte>, ReadOnlySpan<Color32>)` | Construct an image of the given size, by copying the given 8-bit data and palette. |
| `Image8(int, int, byte[], Color32[])` | Construct an image around the given color data, WITHOUT copying it. |
| `Image8(int, int, byte*, int, Color32*, int)` | Construct an image by copying the raw bytes as color data. |
| `Image8(string, ImageFormat)` | Construct an image by loading it from a file, by name. |
| `Image8(ReadOnlySpan<byte>, string, ImageFormat)` | Construct an image by decoding preloaded file data. |

`PureImage8` includes all of the above, as well as `PureImage8(Image8)`, which wraps the given `Image8` instance in a `PureImage8` struct.

`PureImage8` also provides casting operators to/from `Image8`:  `Image8` --&gt; `PureImage8` is implicit, and `PureImage8` --&gt; `Image8` is explicit.

-----------------------------------------------------------------------------

## Color32

The `Color32` and `Color24` structs include many properties and methods, and as the image classes build on top of these, it is worth summarizing them here in their own section for reference.  (For more complete details, refer to the API documentation / Intellisense documentation.)  Note that unlike many color implementations, these are _immutable_ data structures:  If you want to change a value, you replace the whole color object.  (Of course, you can bypass this rule using `unsafe` and pointers, but this rule exists to help keep your code sane.)

Note that the complete set of W3C/CSS named colors is available, both for parsing and stringification, as well as first-class static properties on both classes, so that you can write `Color32.Red` or `Color32.Parse("red")` and it does what you think it should.  The complete list of color names and values is included in the appendix.

### Color properties and fields

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| static `ApparentRedBrightness` | 32, 24 | `0.299`, per ITU-R Recommendation BT.601. |
| static `ApparentGreenBrightness` | 32, 24 | `0.587`, per ITU-R Recommendation BT.601. |
| static `ApparentBlueBrightness` | 32, 24 | `0.114`, per ITU-R Recommendation BT.601. |
| `R` | 32, 24 | Red component, from 0 to 255. |
| `G` | 32, 24 | Green component, from 0 to 255. |
| `B` | 32, 24 | Blue component, from 0 to 255. |
| `A` | 32 | Alpha component, from 0 (transparent) to 255 (opaque). |
| `Rf` | 32, 24 | Red component, from 0.0f to 1.0f. |
| `Gf` | 32, 24 | Green component, from 0.0f to 1.0f. |
| `Bf` | 32, 24 | Blue component, from 0.0f to 1.0f. |
| `Af` | 32 | Alpha component, from 0.0f (transparent) to 1.0f (opaque). |
| `Rd` | 32, 24 | Red component, from 0.0 to 1.0. |
| `Gd` | 32, 24 | Green component, from 0.0 to 1.0. |
| `Bd` | 32, 24 | Blue component, from 0.0 to 1.0. |
| `Ad` | 32 | Alpha component, from 0.0 (transparent) to 1.0 (opaque). |
| `this[int]` | 32, 24 | Retrieve a color value by position, in order of R, G, B, A. |
| `this[ColorChannel]` | 32, 24 | Retrieve a color value by channel name. |

### Color construction

The `Color24` constructors are exactly the same as those below, minus the last parameter (alpha).

* `Color32(float, float, float, [float])` - Construct a color from three or four floating-point values (each 0.0f to 1.0f).
* `Color32(double, double, double, [double])` - Construct a color from three or four floating-point values (each 0.0 to 1.0).
* `Color32(int, int, int, [int])` - Construct a color from three or four integer values (each 0 to 255).
* `Color32(byte, byte, byte, [byte])` - Construct a color from three or four integer values (each 0 to 255).

### Color operators

Both structs implement `IEquatable<T>`.  `Color24` can also be implicitly cast to `Color32` (with an alpha of 255).  `Color32` can be explicitly cast to `Color24` (dropping the alpha channel).

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `operator +()` | 32, 24 | Componentwise addition on each color channel. |
| `operator -()` | 32, 24 | Componentwise subtraction on each color channel. |
| `operator *()` | 32, 24 | Multiply each color channel by a scalar. |
| `operator /()` | 32, 24 | Divide each color channel by a scalar. |
| `operator |()` | 32, 24 | Componentwise bitwise-or of each color channel. |
| `operator &()` | 32, 24 | Componentwise bitwise-and of each color channel. |
| `operator ^()` | 32, 24 | Componentwise bitwise-exclusive-or of each color channel. |
| `operator >>()` | 32, 24 | Bit-shift each color channel right by a count. |
| `operator <<()` | 32, 24 | Bit-shift each color channel left by a count. |
| unary `operator -()` | 32, 24 | "Invert" the color, replacing each channel `v` with `(256-v) % 256`. |
| unary `operator ~()` | 32, 24 | "Invert" the color, replacing each channel `v` with `255-v`. |
| `operator ==()` | 32, 24 | Compare this color to another for equality. |
| `operator !=()` | 32, 24 | Compare this color to another for equality. |

### Operator-like methods

These methods aren't strictly operators, but they are often used in operator-like ways.

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Equals()` | 32, 24 | Compare this color to another for equality. |
| `GetHashCode()` | 32, 24 | Generate a hash code from the color components. |
| `Deconstruct()` | 32, 24 | Support C# deconstruction syntax |
| `ToFloats()` | 32, 24 | Convert this color to a tuple of 3 or 4 floats. |
| `ToDoubles()` | 32, 24 | Convert this color to a tuple of 3 or 4 doubles. |
| `ToInts()` | 32, 24 | Convert this color to a tuple of 3 or 4 ints. |

### Color transformations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Gamma()` | 32, 24 | Apply gamma correction to each of the R, G, and B channels. |
| `Mix()` | 32, 24 | Combine two colors, from 0% to 100% of each. |
| `Merge()` | 32, 24 | Combine two colors at exactly 50% of each. |
| `Opaque()` | 32 | The same color, but with the alpha channel at 255. |
| `ZeroR()` | 32, 24 | The same color, but with the red channel at 0. |
| `ZeroG()` | 32, 24 | The same color, but with the green channel at 0. |
| `ZeroB()` | 32, 24 | The same color, but with the blue channel at 0. |
| `ZeroR()` | 32, 24 | The same color, but with the red channel at 0. |
| `MaxR()` | 32, 24 | The same color, but with the red channel at 255. |
| `MaxG()` | 32, 24 | The same color, but with the green channel at 255. |
| `MaxB()` | 32, 24 | The same color, but with the blue channel at 255. |
| `MaxA()` | 32 | The same color, but with the alpha channel at 255. |
| `Premultiply()` | 32 | Premultiply the alpha against the other channels. |
| `Unpremultiply()` | 32 | Un-premultiply the alpha (approximately). |
| static `Premultiply()` | 32 | Premultiply the given value against the others. |
| static `Unpremultiply()` | 32 | Un-premultiply the given values (approximately). |

### Color-space transformations

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `ToHsv()` | 32, 24 | Convert this color to a hue/saturation/brightness (HSB/HSV) form. |
| `ToHsb()` | 32, 24 | Convert this color to a hue/saturation/brightness (HSB/HSV) form. |
| `ToHsl()` | 32, 24 | Convert this color to a hue/saturation/lightness (HSL) form. |
| static `FromHsv()` | 32, 24 | Construct a color from a hue/saturation/brightness (HSB/HSV) form. |
| static `FromHsb()` | 32, 24 | Construct a color from a hue/saturation/brightness (HSB/HSV) form. |
| static `FromHsl()` | 32, 24 | Construct a color from a hue/saturation/lightness (HSL) form. |

### Color strings and parsing

| Name | Availability | Summary |
| ---- | ------------ | ------- |
| `Hex6` | 32 24 | Convert to a six-char hex string: `#RRGGBB`. |
| `Hex8` | 32 | Convert to an eight-char hex string: `#RRGGBBAA`*. |
| `ToString()` | 32 24 | Convert to a string in the most "natural" possible format. |
| `ToRgbString()` | 32 24 | Convert to a string of the form `rgb(R, G, B, [A])`*. |
| `ToVectorString()` | 32 24 | Convert to a string of the form `(R, G, B, [A])`. |
| `ToHexString()` | 32 24 | Convert to a string of the form `#RRGGBB` or `#RRGGBBAA`. |
| `Parse()` | 32 24 | Parse a color**. |
| `TryParse()` | 32 24 | Try to parse a color, and return status**. |
| `TryParseHexColor()` | 32 24 | Try to parse in one of `#RGB`, `#RGBA`, `#RRGGBB`, or `#RRGGBBAA` formats. |

*Note that Microsoft WPF uses `#AARRGGBB`, but the W3C CSS standard uses `#RRGGBBAA`.  This library follows the W3C.  `ToStringRgb()` will prefer to emit `rgb()` and not `rgba()` form if `A` is 255, and per the CSS spec, will emit `A` as a floating-point value from 0.0 to 1.0.

**Supported string formats for colors include `#RGB`, `#RGBA`, `#RRGGBB`, `#RRGGBBAA`, `rgb(R, G, B)`, `rgba(R, G, B, A)`, and the standard web color names.

-----------------------------------------------------------------------------

## Aseprite support

This library includes a special loader for Aseprite image files.  Aseprite files can contain both layers and animation, and the Aseprite loader supports both directly.  Note that this does not include an Aseprite compositor, for combining layers together for display.

Aseprite files are handled specially because in most use cases involving Aseprite files, you want full access to the layer/animation data, not just a static image, so that you can render the animations and/or layers as needed.  (Aseprite is frequently used for creating video game artwork.)

To load an Aseprite image, first get its bytes from wherever you're storing them, and then decode it with the `AsepriteImage` constructor:

```cs
byte[] myFile = File.ReadAllBytes("something.aseprite");
AsepriteImage image = AsepriteImage(myFile);
```

The image includes first-class properties for `Width` and `Height`, among others, but importantly, it contains a sequence of `Frames` representing the entire animation:

```cs
public IReadOnlyList<AsepriteFrame> Frames { get; }
```

Each frame contains a palette, some basic animation properties, and a layer stack:

```cs
public ushort Duration { get; }               // Frame length, in milliseconds.
public Color32[] Palette { get; }             // The palette for this frame.
public AsepriteGroupLayer LayerTree { get; }  // The tree of layers.
public List<AsepriteLayer> Layers { get; }    // All layers for this frame, in order.
public List<AsepriteCel> Cels { get; }        // The raw cel data for this frame.
public List<AsepriteTag> Tags { get; }        // Any tags attached to this frame.
```

The `Layers` property describes the stacking order for the associated `Cels`.  Each `Layer` may be either an `AsepriteGroupLayer` (a container of other layers) or an `AsepriteImageLayer` (a reference to a set of cels).

Each `Cel` actually contains image data.  It has rectangular rendering coordinates, an opacity to be multiplied against the included pixels, and either an `Image8` or an `Image32` attached to it:

```cs
public short X { get; }
public short Y { get; }
public short Width { get; }
public short Height { get; }

public byte Opacity { get; }

public Image8? Image8 { get; }
public Image32? Image32 { get; }

public Image8? CanvasImage8 { get; }
public Image32? CanvasImage32 { get; }
```

Because Aseprite cels may have a nonzero origin, the `CanvasImage*` versions of each `Image*` property describe only those pixels visible within the Aseprite image's virtual "canvas."  The `Image*` properties describe the stored data in the file, while the `CanvasImage*` properties only include the pixels that are actually seen.

So to access the image in an 8-bit Aseprite file that only has one layer and one frame, you would use something like this:

```cs
byte[] myFile = File.ReadAllBytes("something.aseprite");
AsepriteImage asepriteImage = AsepriteImage(myFile);
AsepriteImageFrame frame = asepriteImage.Frames[0];
AsepriteImageLayer layer = (AsepriteImageLayer)frame.Layers[0];
Image8 image = layer.Cels[0].CanvasImage8;
```

A proper implementation would include error checks, as there is no guarantee that the file has a specific number of frames or layers, or that the first layer is necessarily an image layer.

-----------------------------------------------------------------------------

## Flags and options

### Blit modes

`Blit()` supports several modes, in the `BlitFlags` enum:

* `Copy` - Copy all pixels, exactly as given.
* `Transparent` - Copy or write pixels with simple transparency.
* `Alpha` - Copy or write pixels, applying alpha ('over' mode).
* `PMAlpha` - Copy or write pixels, applying alpha, with R, G, and B treated as premultiplied ('over' mode).
* `Multiply` - Multiply each source pixel value by each destination pixel value.
* `Add` - Add each source pixel value and each destination pixel value.
* `Sub` - Subtract the source from the destination.
* `RSub` - Subtract the destination from the source ("reverse subtract").
* `FlipHorz` - Flip the source horizontally when blitting (supported by Copy, Transparent, Alpha, and PMAlpha modes).
* `FlipVert` - Flip the source vertically when blitting (supported by Copy, Transparent, Alpha, and PMAlpha modes).
* `FastUnsafe` - Don't calculate crop boundaries for a guaranteed-safe blit:  Assume all coordinates are safe and blit as fast as possible.  This is DANGEROUS and can stomp outside the image data if you're not careful, but it's the fastest way to shove pixels around.

### Dithering algorithms

There are several algorithms available for dithering from truecolor RGBA to paletted images, each with its own benefits and drawbacks.  The included algorithms are (from the `DitherMode` enum):

* `Nearest` - Nearest-neighbor dither.  Poor quality, but very fast.
* `Ordered8x8` - An 8x8 ordered dither.
* `Ordered4x4` - A 4x4 ordered dither.
* `Ordered2x2` - A 2x2 ordered dither.
* `FloydSteinberg` - Floyd-Steinberg error-diffused dithering.
* `Atkinson` - Bill Atkinson's error-diffused dithering.
* `Stucki` - Stucki error-diffused dithering.
* `Burkes` - Burkes error-diffused dithering.
* `Jarvis` - Jarvis error-diffused dithering.

### Resampling algorithms

The `Resample()` methods take a parameter to describe which resampling algorithm to use.  Several algorithms are available (from the `ResampleMode` enum):

* `Box` - Nearest-neighbor or "box" sampling.
* `Triangle` - Bilinear filtering or "triangle" sampling.
* `Hermite` - Filtering using the Hermite curve.
* `Bell` - The Bell function (3rd-order, or quadratic B-spline).
* `BSpline` - Fit and sample a 4th-order (cubic) B-spline.
* `Mitchell` - The two-parameter cubic function proposed by Mitchell &amp; Netravali (see SIGGRAPH 88).
* `Lanczos3` - 3-lobed sinusoidal Lanczos approximation.
* `Lanczos5` - 5-lobed sinusoidal Lanczos approximation.
* `Lanczos7` - 7-lobed sinusoidal Lanczos approximation.
* `Lanczos9` - 9-lobed sinusoidal Lanczos approximation.
* `Lanczos11` - 11-lobed sinusoidal Lanczos approximation.

You may combine these with flags that describe how to resample at the edges of the image:

* `TopBack` - At the top edge, sample going back down into the image.
* `TopWrap` - At the top edge, wrap and sample from the bottom of the image.
* `BottomBack` - At the bottom edge, sample going back down into the image.
* `BottomWrap` - At the bottom edge, wrap and sample from the bottom of the image.
* `LeftBack` - At the left edge, sample going rightward back into the image.
* `LeftWrap` - At the left edge, wrap and sample from the right side of the image.
* `RightBack` - At the right edge, sample going leftward back into the image.
* `RightWrap` - At the right edge, wrap and sample from the left side of the image.
* `VertBack` - Combination of `TopBack` and `BottomBack`
* `VertWrap` - Combination of `TopWrap` and `BottomWrap`
* `HorzBack` - Combination of `LeftBack` and `RightBack`
* `HorzWrap` - Combination of `LeftWrap` and `RightWrap`
* `Back` - Combination of all `Back` flags.
* `Wrap` - Combination of all `Wrap` flags.

The `Back` flags generally cause the resampler to reverse direction at the edge and continue sampling backwards into the image, while the `Wrap` flags generally cause the sampler to wrap to pixels on the opposite edge.

-----------------------------------------------------------------------------

## Palettes

Several palettes are included out-of-the-box for use by 8-bit images.  All palette entries have an alpha value of 255.

### Grayscale palettes

Each palette with fewer than 256 gray values has two variants, "A" and "B".  "A" palettes
repeat bits, while "B" palettes use trailing zero bits.

* `BlackAndWhite` - A two-color palette, with 0 = black, and 1 = white.
* `Grayscale4A` and `Grayscale4B` - A four-color palette, with 0 = black and 3 = white.
* `Grayscale8A` and `Grayscale8B` - An eight-color palette, with 0 = black and 7 = white.
* `Grayscale16A` and `Grayscale16B` - A sixteen-color palette, with 0 = black and 15 = white.
* `Grayscale32A` and `Grayscale32B` - A thirty-two-color palette, with 0 = black and 31 = white.
* `Grayscale64A` and `Grayscale64B` - A sixty-four-color palette, with 0 = black and 63 = white.
* `Grayscale128A` and `Grayscale128B` - A one-hundred-twenty-eight-color palette, with 0 = black and 127 = white.
* `Grayscale256` - A 256-color grayscale palette, with 0 = black and 255 = white.

### Legacy and classic palettes

* `Cga16` - The classic CGA 16-color palette, with brown.
* `Cga16Alt` - The CGA 16-color palette, with dark yellow, not brown.
* `Ega64` - The full EGA 64-color palette.
* `Windows16` - The classic Windows 16-color palette.
* `Commodore64_16` - A Commodore 64 16-color palette, per the C64 wiki.
* `NES64` - A full NES 64-color palette (with duplicate blacks).
* `NES54` - A NES 54-color palette (with no duplicates).
* `WebBasic16` - Like the classic CGA 16-color palette, but just different enough to be different.
* `Web216` - The web-safe 216-color palette, in common order of blue-green-red.

-----------------------------------------------------------------------------

## Appendix: Color names

Each of these colors is available for parsing, stringification, and as a first-class static property on each of `Color32` and `Color24`, with the notable exception of `Transparent`, which is only available for `Color32`.  These match the standard W3C CSS color names.

When converting colors to a string, the colors will use the casing below; when parsing colors from a string, the parsing will be case-insensitive.  (Note that when converting to a string, the duplicate gray/grey forms will prefer the spelling with `a`, not `e`.)

| Name | Value |
| ---- | ----- |
| `Transparent` | (0, 0, 0, 0) |
| `AntiqueWhite` | (250, 235, 215) |
| `Aqua` | (0, 255, 255) |
| `Aquamarine` | (127, 255, 212) |
| `Azure` | (240, 255, 255) |
| `Beige` | (245, 245, 220) |
| `Bisque` | (255, 228, 196) |
| `Black` | (0, 0, 0) |
| `BlanchedAlmond` | (255, 235, 205) |
| `Blue` | (0, 0, 255) |
| `BlueViolet` | (138, 43, 226) |
| `Brown` | (165, 42, 42) |
| `Burlywood` | (222, 184, 135) |
| `CadetBlue` | (95, 158, 160) |
| `Chartreuse` | (127, 255, 0) |
| `Chocolate` | (210, 105, 30) |
| `Coral` | (255, 127, 80) |
| `CornflowerBlue` | (100, 149, 237) |
| `Cornsilk` | (255, 248, 220) |
| `Crimson` | (220, 20, 60) |
| `Cyan` | (0, 255, 255) |
| `DarkBlue` | (0, 0, 139) |
| `DarkCyan` | (0, 139, 139) |
| `DarkGoldenrod` | (184, 134, 11) |
| `DarkGray` | (169, 169, 169) |
| `DarkGreen` | (0, 100, 0) |
| `DarkGrey` | (169, 169, 169) |
| `DarkKhaki` | (189, 183, 107) |
| `DarkMagenta` | (139, 0, 139) |
| `DarkOliveGreen` | (85, 107, 47) |
| `DarkOrange` | (255, 140, 0) |
| `DarkOrchid` | (153, 50, 204) |
| `DarkRed` | (139, 0, 0) |
| `DarkSalmon` | (233, 150, 122) |
| `DarkSeaGreen` | (143, 188, 143) |
| `DarkSlateBlue` | (72, 61, 139) |
| `DarkSlateGray` | (47, 79, 79) |
| `DarkSlateGrey` | (47, 79, 79) |
| `DarkTurquoise` | (0, 206, 209) |
| `DarkViolet` | (148, 0, 211) |
| `DeepPink` | (255, 20, 147) |
| `DeepSkyBlue` | (0, 191, 255) |
| `DimGray` | (105, 105, 105) |
| `DimGrey` | (105, 105, 105) |
| `DodgerBlue` | (30, 144, 255) |
| `FireBrick` | (178, 34, 34) |
| `FloralWhite` | (255, 250, 240) |
| `ForestGreen` | (34, 139, 34) |
| `Fuchsia` | (255, 0, 255) |
| `Gainsboro` | (220, 220, 220) |
| `GhostWhite` | (248, 248, 255) |
| `Gold` | (255, 215, 0) |
| `Goldenrod` | (218, 165, 32) |
| `Gray` | (128, 128, 128) |
| `Green` | (0, 128, 0) |
| `GreenYellow` | (173, 255, 47) |
| `Grey` | (128, 128, 128) |
| `Honeydew` | (240, 255, 240) |
| `HotPink` | (255, 105, 180) |
| `IndianRed` | (205, 92, 92) |
| `Indigo` | (75, 0, 130) |
| `Ivory` | (255, 255, 240) |
| `Khaki` | (240, 230, 140) |
| `Lavender` | (230, 230, 250) |
| `LavenderBlush` | (255, 240, 245) |
| `LawnGreen` | (124, 252, 0) |
| `LemonChiffon` | (255, 250, 205) |
| `LightBlue` | (173, 216, 230) |
| `LightCoral` | (240, 128, 128) |
| `LightCyan` | (224, 255, 255) |
| `LightGoldenrodYellow` | (250, 250, 210) |
| `LightGray` | (211, 211, 211) |
| `LightGreen` | (144, 238, 144) |
| `LightGrey` | (211, 211, 211) |
| `LightPink` | (255, 182, 193) |
| `LightSalmon` | (255, 160, 122) |
| `LightSeaGreen` | (32, 178, 170) |
| `LightSkyBlue` | (135, 206, 250) |
| `LightSlateGray` | (119, 136, 153) |
| `LightSlateGrey` | (119, 136, 153) |
| `LightSteelBlue` | (176, 196, 222) |
| `LightYellow` | (255, 255, 224) |
| `Lime` | (0, 255, 0) |
| `LimeGreen` | (50, 205, 50) |
| `Linen` | (250, 240, 230) |
| `Magenta` | (255, 0, 255) |
| `Maroon` | (128, 0, 0) |
| `MediumAquamarine` | (102, 205, 170) |
| `MediumBlue` | (0, 0, 205) |
| `MediumOrchid` | (186, 85, 211) |
| `MediumPurple` | (147, 112, 219) |
| `MediumSeaGreen` | (60, 179, 113) |
| `MediumSlateBlue` | (123, 104, 238) |
| `MediumSpringGreen` | (0, 250, 154) |
| `MediumTurquoise` | (72, 209, 204) |
| `MediumVioletRed` | (199, 21, 133) |
| `MidnightBlue` | (25, 25, 112) |
| `MintCream` | (245, 255, 250) |
| `MistyRose` | (255, 228, 225) |
| `Moccasin` | (255, 228, 181) |
| `NavajoWhite` | (255, 222, 173) |
| `Navy` | (0, 0, 128) |
| `OldLace` | (253, 245, 230) |
| `Olive` | (128, 128, 0) |
| `OliveDrab` | (107, 142, 35) |
| `Orange` | (255, 165, 0) |
| `Orangered` | (255, 69, 0) |
| `Orchid` | (218, 112, 214) |
| `PaleGoldenrod` | (238, 232, 170) |
| `PaleGreen` | (152, 251, 152) |
| `PaleTurquoise` | (175, 238, 238) |
| `PaleVioletRed` | (219, 112, 147) |
| `PapayaWhip` | (255, 239, 213) |
| `Peach` | (255, 192, 128) |
| `PeachPuff` | (255, 218, 185) |
| `Peru` | (205, 133, 63) |
| `Pink` | (255, 192, 203) |
| `Plum` | (221, 160, 221) |
| `PowderBlue` | (176, 224, 230) |
| `Purple` | (128, 0, 128) |
| `RebeccaPurple` | (102, 51, 153) |
| `Red` | (255, 0, 0) |
| `RosyBrown` | (188, 143, 143) |
| `RoyalBlue` | (65, 105, 225) |
| `SaddleBrown` | (139, 69, 19) |
| `Salmon` | (250, 128, 114) |
| `SandyBrown` | (244, 164, 96) |
| `SeaGreen` | (46, 139, 87) |
| `Seashell` | (255, 245, 238) |
| `Sienna` | (160, 82, 45) |
| `Silver` | (192, 192, 192) |
| `SkyBlue` | (135, 206, 235) |
| `SlateBlue` | (106, 90, 205) |
| `SlateGray` | (112, 128, 144) |
| `Snow` | (255, 250, 250) |
| `SpringGreen` | (0, 255, 127) |
| `SteelBlue` | (70, 130, 180) |
| `Tan` | (210, 180, 140) |
| `Tea` | (0, 128, 128) |
| `Thistle` | (216, 191, 216) |
| `Tomato` | (255, 99, 71) |
| `Turquoise` | (64, 224, 208) |
| `Violet` | (238, 130, 238) |
| `Wheat` | (245, 222, 179) |
| `White` | (255, 255, 255) |
| `WhiteSmoke` | (245, 245, 245) |
| `Yellow` | (255, 255, 0) |
| `YellowGreen` | (154, 205, 50) |

-----------------------------------------------------------------------------

## Credits, and support and contact info

Have questions?  Found a bug?  Want a feature?  Submit it as an issue on Github:  https://www.github.com/seanofw/HalfMaid.Img

Credits:

* Most of this was written by Sean Werkema in 2019-2024, although some parts were based on an older C++ image library he wrote circa 2003.
* The GIF LZW decoder/encoder classes are (_very_) loosely based on old C code published by Steve Rimmer in his book _Supercharged Bitmap Graphics_ in 1992.  The rest of the GIF file support is a clean-room implementation based on the published spec.
* LibJpegTurbo is used for JPEG decoding, because JPEGs are a beast.
* All other file-format decoders (BMP, Targa, PNG, Aseprite) are original clean-room implementations based on their published file format specs.
