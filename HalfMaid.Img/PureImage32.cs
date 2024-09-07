using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HalfMaid.Img.FileFormats;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// A PureImage is just like an Image, and it has nearly the same methods,
	/// but all operations on a PureImage produce a new PureImage:  No data
	/// is modified in place.  This is a more rational API in exchange for a cost
	/// in performance on some (but not all) operations.
	/// </summary>
	[DebuggerDisplay("Image {Width}x{Height}")]
	public readonly struct PureImage32 : IPureImage<Color32>
	{
		#region Core properties and fields

		private readonly Image32 _image;

		/// <summary>
		/// The width of the image, in pixels.
		/// </summary>
		public int Width => _image.Width;

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		public int Height => _image.Height;

		#endregion

		#region Derived properties and static data

		/// <summary>
		/// Obtain direct access to the underlying array of color data.  You should
		/// not change this data if you want to maintain the semantics of the PureImage,
		/// but the array is available so that you can directly read the data.
		/// </summary>
		/// <returns>The underlying array of color data.</returns>
		[Pure]
		public Color32[] Data => _image.Data;

		/// <summary>
		/// The size of this image, represented as a 2D vector.
		/// </summary>
		[Pure]
		public Vector2i Size => _image.Size;

		/// <summary>
		/// A static "empty" image that never has pixel data in it.
		/// </summary>
		public static PureImage32 Empty { get; } = new PureImage32(new Image32(0, 0));

		#endregion

		#region Pixel-plotting

		/// <summary>
		/// Access the pixels using "easy" 2D array-brackets.  This is safe, easy, reliable,
		/// and slower than almost every other way of accessing the pixels, but it's very useful
		/// for simple cases.  Reading a pixel outside the image will return 'Transparent'.
		/// </summary>
		/// <param name="x">The X coordinate of the pixel to read or write.</param>
		/// <param name="y">The Y coordinate of the pixel to read or write.</param>
		/// <returns>The color at that pixel.</returns>
		public Color32 this[int x, int y]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[Pure]
			get => x < 0 || x >= Width || y < 0 || y >= Height ? Color32.Transparent : Data[y * Width + x];
		}

		#endregion

		#region Construction and conversion

		/// <summary>
		/// Construct a new PureImage from an Image.  This simply wraps the
		/// provided Image, which you should not use again after wrapping it.
		/// </summary>
		/// <param name="image">The image to wrap as a PureImage.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(Image32 image)
		{
			_image = image ?? throw new ArgumentNullException(nameof(image));
		}

		/// <summary>
		/// Convert an Image into a PureImage.  This simply wraps the
		/// provided Image, which you should not use again after wrapping it.
		/// </summary>
		/// <param name="image">The Image to convert to a PureImage.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PureImage32(Image32 image)
			=> new PureImage32(image);

		/// <summary>
		/// Convert a PureImage into an Image.  This makes a clone of
		/// the existing PureImage.
		/// </summary>
		/// <param name="pureImage">The PureImage to convert to an Image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Image32(PureImage32 pureImage)
			=> pureImage._image.Clone();

		/// <summary>
		/// Construct an image from the given image file.
		/// </summary>
		/// <param name="filename">The filename to load.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(string filename, ImageFormat imageFormat = default)
			: this(new Image32(filename, imageFormat))
		{
		}

		/// <summary>
		/// Construct an image from the given image file data.
		/// </summary>
		/// <param name="data">The image file data to load.</param>
		/// <param name="filenameIfKnown">The filename of that image file data, if known.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
			: this(new Image32(data, filenameIfKnown, imageFormat))
		{
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 'Transparent'.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(Vector2i size)
			: this(new Image32(size))
		{
		}

		/// <summary>
		/// Construct an image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="fillColor">The color for all of the new pixels in the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(Vector2i size, Color32 fillColor)
			: this(new Image32(size, fillColor))
		{
		}

		/// <summary>
		/// Construct an image of the given size, using the provided 8-bit image data
		/// and color palette to construct it.  This constructor can be used to "promote"
		/// 8-bit images to 32-bit truecolor.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		/// <param name="srcData">The source pixel data, which will be used as indexes
		/// into the provided color palette.</param>
		/// <param name="palette">The color palette, which should be a span of
		/// up to 256 color values.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height, ReadOnlySpan<byte> srcData, ReadOnlySpan<Color32> palette)
			: this(new Image32(width, height, srcData, palette))
		{
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 'Transparent'.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height)
			: this(new Image32(width, height))
		{
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the provided fill color.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		/// <param name="fillColor">The color for all of the new pixels in the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height, Color32 fillColor)
			: this(new Image32(width, height, fillColor))
		{
		}

		/// <summary>
		/// Construct an image around the given color data, WITHOUT copying it.<br />
		/// <br />
		/// WARNING:  This constructor does NOT make a copy of the provided data; it *wraps*
		/// the provided array directly, which must be at least width*height in size (and that
		/// fact will at least be validated so that other methods can assume it).  This is
		/// very fast, but you can also use it to break things if you're not careful.  When
		/// in doubt, this is *not* the method you want.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="data">The data to use for the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height, Color32[] data)
			: this(new Image32(width, height, data))
		{
		}

		/// <summary>
		/// Construct a new image by treating the given raw byte data as a sequence of
		/// RGBA tuples and copying them into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="rawData">The raw color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height, ReadOnlySpan<byte> rawData)
			: this(new Image32(width, height, rawData))
		{
		}

		/// <summary>
		/// Construct a new image by treating the given raw byte data as a sequence of
		/// RGBA tuples and copying them into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="rawData">The raw color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <param name="rawDataLength">The number of bytes in the raw color data array.</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe PureImage32(int width, int height, byte* rawData, int rawDataLength)
			: this(new Image32(width, height, rawData, rawDataLength))
		{
		}

		/// <summary>
		/// Construct a new image from the given a sequence of RGBA tuples, copying them
		/// into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="data">The color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32(int width, int height, ReadOnlySpan<Color32> data)
			: this(new Image32(width, height, data))
		{
		}

		/// <summary>
		/// Construct a new image from the given a sequence of RGBA tuples, copying them
		/// into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="data">The color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <param name="dataLength">The count of Color structs in the provided data
		/// array.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe PureImage32(int width, int height, Color32* data, int dataLength)
			: this(new Image32(width, height, data, dataLength))
		{
		}

		/// <summary>
		/// Extract out a copy of the raw data in this image, as bytes.  This is
		/// slower than pinning the raw data and then casting it to a byte pointer, but
		/// it is considerably safer.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] GetBytes()
			=> _image.GetBytes();

		/// <summary>
		/// Make a perfect duplicate of this image.  This makes a real copy,
		/// not just a duplicate reference.
		/// </summary>
		[Pure]
		IImageBase IImageBase.Clone()
			=> _image.Clone();

		/// <summary>
		/// Make a perfect duplicate of this image.  This makes a real copy,
		/// not just a duplicate reference.
		/// </summary>
		[Pure]
		PureImage32 Clone()
			=> _image.Clone();

		#endregion

		#region Image loading/saving

		/// <summary>
		/// Construct an image from the embedded resource with the given name.  The embedded
		/// resource must be of a file format that this image library is capable of
		/// decoding, like PNG or JPEG.
		/// </summary>
		/// <param name="assembly">The assembly containing the embedded resource.</param>
		/// <param name="name">The name of the embedded image resource to load.  This may use
		/// slashes in the pathname to separate components.</param>
		/// <returns>The newly-loaded image, or PureImage.Empty if no such image exists
		/// or is not a valid image file.</returns>
		[Pure]
		public static PureImage32 FromEmbeddedResource(Assembly assembly, string name)
			=> Image32.FromEmbeddedResource(assembly, name) ?? Empty;

		/// <summary>
		/// Load the given file as a new image, which must be of a supported image
		/// format like PNG or JPEG.
		/// </summary>
		/// <param name="filename">The filename of the image to load.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or PureImage.Empty if it can't be loaded.</returns>
		[Pure]
		public static PureImage32 LoadFile(string filename, ImageFormat imageFormat = default)
			=> Image32.LoadFile(filename, imageFormat) ?? Empty;

		/// <summary>
		/// Load the given chunk of bytes in memory as a new image, which must be
		/// encoded as a supported image file format like PNG or JPEG.
		/// </summary>
		/// <param name="data">The data of the image file to load.</param>
		/// <param name="filenameIfKnown">If the image format is unknown, the filename
		/// can be included to help disambiguate the image data.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or PureImage.Empty if it can't be loaded.</returns>
		[Pure]
		public static PureImage32 LoadFile(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
			=> Image32.LoadFile(data, filenameIfKnown, imageFormat) ?? Empty;

		/// <summary>
		/// Retrieve just the simple metadata from the given image file:
		/// Its color format and its dimensions.
		/// </summary>
		/// <param name="filename">The image file to load metadata for.</param>
		/// <param name="imageFormat">The image format, if known.</param>
		/// <returns>The metadata, or null if the file isn't a readable image format.</returns>
		[Pure]
		public static ImageFileMetadata? LoadFileMetadata(string filename,
			ImageFormat imageFormat = default)
			=> Image32.LoadFileMetadata(filename, imageFormat);

		/// <summary>
		/// Retrieve just the simple metadata from the given image data:
		/// Its color format and its dimensions.
		/// </summary>
		/// <param name="data">The image data.</param>
		/// <param name="filenameIfKnown">If the image format is unknown, the filename
		/// can be included to help disambiguate the image data.</param>
		/// <param name="imageFormat">The image format, if known.</param>
		/// <returns>The metadata, or null if the file isn't a readable image format.</returns>
		[Pure]
		public static ImageFileMetadata? LoadFileMetadata(ReadOnlySpan<byte> data,
			string? filenameIfKnown = null, ImageFormat imageFormat = default)
			=> Image32.LoadFileMetadata(data, filenameIfKnown, imageFormat);

		/// <summary>
		/// Given the first small chunk of a file and possibly its name, attempt to guess
		/// what image file format it is.
		/// </summary>
		/// <param name="bytes">The initial bytes for the file.  You should supply at least
		/// 4096 bytes if the file has at least 4096.</param>
		/// <param name="filenameIfKnown">The name of the file, if known, which may help
		/// break ties if the data is not clear about what kind of file it is.</param>
		/// <returns>The image format for that file, and a rough certainty level
		/// that the file really is that format, from "Yes" to "Probably" to "Maybe" to "No."</returns>
		[Pure]
		public static (ImageFormat Format, ImageCertainty Certainty) GuessFileFormat(ReadOnlySpan<byte> bytes,
			string? filenameIfKnown = null)
			=> Image32.GuessFileFormat(bytes, filenameIfKnown);

		/// <summary>
		/// Read the first small chunk of the given file and attempt to guess what
		/// image file format it is, based on the file's name and its data.
		/// </summary>
		/// <param name="filename">The name of the file to read.</param>
		/// <returns>The image format for that file, and a rough certainty level
		/// that the file really is that format, from "Yes" to "Probably" to "Maybe" to "No."</returns>
		[Pure]
		public static (ImageFormat Format, ImageCertainty Certainty) GuessFileFormat(string filename)
			=> Image32.GuessFileFormat(filename);

		/// <summary>
		/// Save the image to disk as the given file format.
		/// </summary>
		/// <param name="filename">The filename to write.</param>
		/// <param name="format">The image format to produce.</param>
		/// <param name="options">Options specific to this file format, if appropriate.</param>
		[Pure]
		public void SaveFile(string filename, ImageFormat format, IFileSaveOptions? options = null)
			=> _image.SaveFile(filename, format, options);

		/// <summary>
		/// Save the image to an array of bytes as the given file format.  This does not
		/// use maximum compression; it just ensures that the output is an acceptable
		/// implementation of the given file format.
		/// </summary>
		/// <param name="format">The image format to produce.</param>
		/// <param name="options">Options specific to this file format, if appropriate.</param>
		/// <returns>An array of bytes that represents the image in the given file format.</returns>
		[Pure]
		public byte[] SaveFile(ImageFormat format, IFileSaveOptions? options = null)
			=> _image.SaveFile(format, options);

		/// <summary>
		/// Register the given class(es) as being able to load or save
		/// the given image file format.
		/// </summary>
		/// <param name="format">The new file format to register.  Any existing registration
		/// for this file format will be replaced.</param>
		/// <param name="loader">The loader to register, or null to unregister a loader for
		/// this format.</param>
		/// <param name="saver">The saver to register, or null to unregister a saver for
		/// this format.</param>
		public static void RegisterFileFormat(ImageFormat format,
			IImageLoader? loader = null, IImageSaver? saver = null)
			=> Image32.RegisterFileFormat(format, loader, saver);

		/// <summary>
		/// Get the currently-registered image loader for the given image format.
		/// </summary>
		/// <param name="format">The format to retrieve a loader for.</param>
		/// <returns>The loader for that format, or null if there is no such loader.</returns>
		[Pure]
		public static IImageLoader? GetLoader(ImageFormat format)
			=> Image32.GetLoader(format);

		/// <summary>
		/// Get the currently-registered image saver for the given image format.
		/// </summary>
		/// <param name="format">The format to retrieve a saver for.</param>
		/// <returns>The saver for that format, or null if there is no such saver.</returns>
		[Pure]
		public static IImageSaver? GetSaver(ImageFormat format)
			=> Image32.GetSaver(format);

		#endregion

		#region Resizing

		/// <summary>
		/// Perform resizing to fit the given container size using the chosen fitting
		/// mode.  Fast, but can be really, really inaccurate.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <returns>A copy of this image, resized to fit the container.</returns>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		[Pure]
		public PureImage32 ResizeToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit)
			=> Resize(Fit(Size, containerSize, fitMode));

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="imageSize">The new size of the image.</param>
		/// <returns>A copy of this image, resized to fit the container.</returns>
		[Pure]
		public PureImage32 Resize(Vector2i imageSize)
			=> Resize(imageSize.X, imageSize.Y);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="newWidth">The new width of the image.</param>
		/// <param name="newHeight">The new height of the image.</param>
		/// <returns>A copy of this image, resized to fit the container.</returns>
		[Pure]
		public PureImage32 Resize(int newWidth, int newHeight)
		{
			Image32 result = new Image32(newWidth, newHeight);

			int xStep = (int)(((long)Width << 16) / newWidth);
			int yStep = (int)(((long)Height << 16) / newHeight);

			unsafe
			{
				fixed (Color32* destBase = result.Data)
				fixed (Color32* srcBase = Data)
				{
					for (int destY = 0, srcYScaled = 0; destY < newHeight; destY++, srcYScaled += yStep)
					{
						int srcY = srcYScaled >> 16;
						Color32* srcRow = srcBase + srcY * Width;
						Color32* dest = destBase + destY * newWidth;
						Color32* destEnd = dest + newWidth;

						for (int srcXScaled = 0; dest < destEnd; srcXScaled += xStep)
						{
							int srcX = srcXScaled >> 16;
							*dest++ = srcRow[srcX];
						}
					}
				}
			}

			return result;
		}

		#endregion

		#region Resampling

		/// <summary>
		/// Fit the given image to the given container, using one of several
		/// possible fit modes.
		/// </summary>
		/// <param name="imageSize">The image's current size.</param>
		/// <param name="containerSize">The container's size.</param>
		/// <param name="fitMode">How to perform the fit operation.</param>
		/// <returns>The appropriate new size for the image.</returns>
		[Pure]
		public static Vector2i Fit(Vector2i imageSize, Vector2i containerSize, FitMode fitMode)
			=> Image32.Fit(imageSize, containerSize, fitMode);

		/// <summary>
		/// Fit the given image to the given container, using one of several
		/// possible fit modes.
		/// </summary>
		/// <param name="imageSize">The image's current size.</param>
		/// <param name="containerSize">The container's size.</param>
		/// <param name="fitMode">How to perform the fit operation.</param>
		/// <returns>The appropriate new size for the image.</returns>
		[Pure]
		public static Vector2d Fit(Vector2d imageSize, Vector2d containerSize, FitMode fitMode)
			=> Image32.Fit(imageSize, containerSize, fitMode);

		/// <summary>
		/// Perform resampling to fit the given container size using the chosen fitting
		/// mode and resampling mode.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <returns>A copy of the image, resampled.</returns>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		[Pure]
		public PureImage32 ResampleToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit, ResampleMode mode = ResampleMode.BSpline)
			=> Resample(Fit(Size, containerSize, fitMode), mode);

		/// <summary>
		/// Perform resampling using the chosen mode.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="width">The new image width.  If omitted/null, this will be determined
		/// automatically from the given height.</param>
		/// <param name="height">The new image height.  If omitted/null, this will be determined
		/// automatically from the given width.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <returns>A copy of the image, resampled.</returns>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		[Pure]
		public PureImage32 Resample(int? width = null, int? height = null, ResampleMode mode = ResampleMode.BSpline)
			=> Resample(Image32.CalculateResize(Size, width, height), mode);

		/// <summary>
		/// Perform resampling using the chosen mode.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="newSize">The new image size.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <returns>A copy of this image, resampled to the given size.</returns>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		[Pure]
		public PureImage32 Resample(Vector2i newSize, ResampleMode mode = ResampleMode.BSpline)
		{
			Image32 dest = new Image32(newSize);
			ImageResampler.ResampleTo(_image, dest, mode);
			return dest;
		}

		#endregion

		#region Clipping helpers

		/// <summary>
		/// Clip a blit to be within the given bounds of the image.
		/// </summary>
		/// <param name="destImageSize">The size of the destination image we're blitting to.</param>
		/// <param name="srcImageSize">The size of the source image we're blitting from.</param>
		/// <param name="srcX">The source X coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="srcY">The source Y coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="destX">The destination X coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="destY">The destination Y coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="width">The width of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="height">The height of the blit, which will be updated to within bounds of both images.</param>
		/// <returns>True if the blit can proceed, or false if the blit should be aborted due to illegal/unusable values.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ClipBlit(Vector2i destImageSize, Vector2i srcImageSize, ref int srcX, ref int srcY,
			ref int destX, ref int destY, ref int width, ref int height)
			=> Image32.ClipBlit(destImageSize, srcImageSize, ref srcX, ref srcY, ref destX, ref destY, ref width, ref height);

		/// <summary>
		/// Clip the given drawing rectangle to be within the image.
		/// </summary>
		/// <param name="imageSize">The size of the image to clip against.</param>
		/// <param name="x">The left coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="y">The top coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="width">The width of the rectangle, which will be updated to be within the image.</param>
		/// <param name="height">The height of the rectangle, which will be updated to be within the image.</param>
		/// <returns>True if the drawing may proceed, or false if the rectangle is invalid/unusable.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ClipRect(Vector2i imageSize, ref int x, ref int y, ref int width, ref int height)
			=> Image32.ClipRect(imageSize, ref x, ref y, ref width, ref height);

		#endregion

		#region Blitting

		/// <summary>
		/// Extract a subimage from this image.  Really just a blit to a new image.
		/// </summary>
		[Pure]
		public PureImage32 Extract(int x, int y, int width, int height)
			=> _image.Extract(x, y, width, height);

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		[Pure]
		public PureImage32 Crop(int x, int y, int width, int height)
			=> _image.Extract(x, y, width, height);

		/// <summary>
		/// Pad the given image with extra pixels on some or all sides.
		/// Note that the padding for any edge may be negative, which will
		/// cause an edge to be cropped instead of padded.
		/// </summary>
		/// <param name="left">The number of pixels to add to the left edge.</param>
		/// <param name="top">The number of pixels to add to the top edge.</param>
		/// <param name="right">The number of pixels to add to the right edge.</param>
		/// <param name="bottom">The number of pixels to add to the bottom edge.</param>
		/// <param name="fillColor">The padding color (transparent by default).</param>
		[Pure]
		public PureImage32 Pad(int left = 0, int top = 0, int right = 0, int bottom = 0, Color32 fillColor = default)
		{
			Image32 image = new Image32(Width + left + right, Height + top + bottom, fillColor);
			image.Blit(_image, 0, 0, left, top, Width + right, Height + bottom);
			return image;
		}

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle.  This will by default clip
		/// the provided coordinates to perform a safe blit (all pixels outside an image
		/// will be ignored).
		/// </summary>
		/// <param name="srcImage">The source image to copy from.</param>
		/// <param name="srcX">The X coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="srcY">The Y coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="destX">The X coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="destY">The Y coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="width">The width of the rectangle of pixels to copy.</param>
		/// <param name="height">The height of the rectangle of pixels to copy.</param>
		/// <param name="blitFlags">Flags controlling how the blit operation is performed.</param>
		/// <param name="color">The color to use for color blit modes.</param>
		/// <returns>A new image, with part of it replaced by 'srcImage'.</returns>
		/// <remarks>
		/// This Blit() does *not* perform the update in-place, and is typically slower than Image32.Blit()
		/// by a lot.  This is usually not the method you want unless regularity (or piping)
		/// matters more to you than performance.
		/// </remarks>
		[Pure]
		public PureImage32 Blit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height,
			BlitFlags blitFlags = default, Color32 color = default)
		{
			Image32 copy = _image.Clone();
			copy.Blit(srcImage, srcX, srcY, destX, destY, width, height, blitFlags, color);
			return copy;
		}

		#endregion

		#region Pattern blits

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle, repeating the source rectangle
		/// if the destination rectangle is larger than the source.  This will by default clip
		/// the provided destination coordinates to perform a safe blit (all pixels outside an
		/// image will be ignored).  If the source coordinates lie outside the srcImage, this
		/// will throw an exception.
		/// </summary>
		/// <returns>A copy of this image, with the pattern blitted on top of it.</returns>
		[Pure]
		public PureImage32 PatternBlit(Image32 srcImage, Rect srcRect, Rect destRect)
		{
			Image32 copy = _image.Clone();
			copy.PatternBlit(srcImage, srcRect, destRect);
			return copy;
		}

		#endregion

		#region Transformations

		/// <summary>
		/// Vertically flip the image.
		/// </summary>
		/// <returns>A copy of the image, flipped vertically.</returns>
		[Pure]
		public PureImage32 FlipVert()
		{
			Image32 result = new Image32(Width, Height);

			unsafe
			{
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					Color32* srcRow = srcBase;
					Color32* destRow = destBase + Width * (Height - 1);
					for (int y = 0; y < Height; y++)
					{
						Color32* src = srcRow;
						Color32* dest = destRow;
						for (int x = 0; x < Width; x++)
						{
							*dest++ = *src++;
						}
						srcRow += Width;
						destRow -= Width;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Horizontally flip the image.
		/// </summary>
		/// <returns>A copy of the image, flipped horizontally.</returns>
		[Pure]
		public PureImage32 FlipHorz()
		{
			Image32 result = new Image32(Width, Height);

			unsafe
			{
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					Color32* srcRow = srcBase;
					Color32* destRow = destBase;
					for (int y = 0; y < Height; y++)
					{
						Color32* src = srcRow;
						Color32* dest = destRow + Width;
						for (int x = 0; x < Width; x++)
							*--dest = *src++;
						srcRow += Width;
						destRow += Width;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Rotate the image 90 degrees clockwise.
		/// </summary>
		/// <returns>A copy of the image, rotated 90 degrees clockwise.</returns>
		[Pure]
		public PureImage32 Rotate90()
		{
			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Make the result image.
			Image32 result = new Image32(newWidth, newHeight);

			// Copy from the old image to the new image.
			unsafe
			{
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					for (int x = 0; x < oldWidth; x++)
					{
						Color32* src = srcBase + x + oldWidth * (oldHeight - 1);
						Color32* dest = destBase + x * newWidth;
						for (int y = 0; y < oldHeight; y++)
						{
							*dest++ = *src;
							src -= oldWidth;
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Rotate the image 90 degrees counterclockwise.
		/// </summary>
		/// <returns>The image rotated 90 degrees.</returns>
		[Pure]
		public PureImage32 Rotate90CCW()
		{
			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Make the result image.
			Image32 result = new Image32(newWidth, newHeight);

			// Copy from the old image to the new image.
			unsafe
			{
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					for (int x = 0; x < newWidth; x++)
					{
						Color32* src = srcBase + x * oldWidth;
						Color32* dest = destBase + x + newWidth * (newHeight - 1);
						for (int y = 0; y < newHeight; y++)
						{
							*dest = *src++;
							dest -= newWidth;
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Rotate the image 180 degrees.
		/// </summary>
		/// <returns>A copy of the image, rotated 180 degrees.</returns>
		[Pure]
		public PureImage32 Rotate180()
		{
			Image32 result = new Image32(Width, Height);

			unsafe
			{
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					Color32* src = srcBase;
					Color32* dest = destBase + Width * Height;
					Color32* end = srcBase + Width * Height;
					while (src < end)
					{
						*--dest = *src++;
					}
				}
			}

			return result;
		}

		#endregion

		#region Color mixing

		/// <summary>
		/// Mix another image with this by combining their R, G, B, and A values according to the
		/// given opacity level.  The other image must be exactly the same width and height as this image.
		/// </summary>
		/// <param name="other">The other image whose color values should be mixed with this
		/// image's color values.</param>
		/// <param name="amount">How much of the other image to mix in; 0.0 = 100% this image,
		/// 1.0 = 100% the other image.</param>
		/// <returns>A copy of the images mixed together.</returns>
		[Pure]
		public PureImage32 Mix(Image32 other, double amount = 0.5)
		{
			Image32 copy = _image.Clone();
			copy.Mix(other, amount);
			return copy;
		}

		/// <summary>
		/// Multiply every color value in this by the given scalar values.
		/// </summary>
		/// <returns>A copy of this image, with every color value multiplied by the given scalars.</returns>
		public PureImage32 Multiply(float r, float g, float b, float a)
		{
			Image32 copy = _image.Clone();
			copy.Multiply(r, g, b, a);
			return copy;
		}

		/// <summary>
		/// Multiply every color value in this by the given scalar values.
		/// </summary>
		/// <returns>A copy of this image, with every color value multiplied by the given scalars.</returns>
		public PureImage32 Multiply(double r, double g, double b, double a)
		{
			Image32 copy = _image.Clone();
			copy.Multiply(r, g, b, a);
			return copy;
		}

		/// <summary>
		/// Premultiply the alpha value to the red, green, and blue values of every pixel.
		/// </summary>
		/// <returns>A copy of this image, with the alpha values premultiplied.</returns>
		[Pure]
		public PureImage32 PremultiplyAlpha()
		{
			Image32 copy = _image.Clone();
			copy.PremultiplyAlpha();
			return copy;
		}

		#endregion

		#region Color remapping

		/// <summary>
		/// Skim through the image and replace all exact instances of the given color with another.
		/// </summary>
		/// <param name="src">The color to replace.</param>
		/// <param name="dest">Its replacement color.</param>
		/// <returns>A copy of the image, with the given color replaced.</returns>
		[Pure]
		public PureImage32 RemapColor(Color32 src, Color32 dest)
		{
			Image32 copy = _image.Clone();
			copy.RemapColor(src, dest);
			return copy;
		}

		/// <summary>
		/// Skim through the image and replace many colors at once via a dictionary.
		/// This is typically much slower than many calls to Remap() above, unless the
		/// replacement table is large relative to the image size.
		/// </summary>
		/// <param name="dictionary">The dictionary that describes all replacement values.
		/// If a color does not exist in the dictionary, it will not be changed.</param>
		/// <returns>The image, with the given colors remapped.</returns>
		[Pure]
		public PureImage32 RemapColor(Dictionary<Color32, Color32> dictionary)
		{
			Image32 copy = _image.Clone();
			copy.RemapColor(dictionary);
			return copy;
		}

		/// <summary>
		/// Remap each color by passing it through the given matrix transform.
		/// </summary>
		/// <param name="matrix">The matrix to multiply each color vector by.</param>
		[Pure]
		public PureImage32 RemapColor(Matrix3 matrix)
		{
			Image32 copy = _image.Clone();
			copy.RemapColor(matrix);
			return copy;
		}

		/// <summary>
		/// Transform this image into a silhouette by keeping its alpha values unchanged
		/// but replacing its red/green/blue values with the given values from the silhouette color.
		/// This even affects pixels with an alpha of zero.
		/// </summary>
		/// <param name="color">The silhouette color.</param>
		/// <returns>The new silhouette image.</returns>
		[Pure]
		public PureImage32 Silhouette(Color32 color)
		{
			Image32 copy = _image.Clone();
			copy.Silhouette(color);
			return copy;
		}

		/// <summary>
		/// Transform this image into a silhouette by keeping its alpha values unchanged
		/// but replacing its red/green/blue values with the given values from the silhouette
		/// color, premultiplied against its alpha values.
		/// </summary>
		/// <param name="color">The silhouette color.</param>
		/// <returns>The new silhouette image.</returns>
		[Pure]
		public PureImage32 SilhouettePM(Color32 color)
		{
			Image32 copy = _image.Clone();
			copy.SilhouettePM(color);
			return copy;
		}

		/// <summary>
		/// Apply uniform gamma to all three color values (i.e., convert each color to [0, 1] and then
		/// raise it to the given power).
		/// </summary>
		/// <param name="gamma">The gamma exponent to apply.  0 to 1 results in brighter
		/// images, and 1 to infinity results in darker images.</param>
		[Pure]
		public PureImage32 Gamma(double gamma)
		{
			Image32 copy = _image.Clone();
			copy.Gamma(gamma);
			return copy;
		}

		/// <summary>
		/// Apply separate gamma amounts to all three color values (i.e., convert each color to
		/// [0, 1] and then raise it to the given power).  This is slightly slower than applying
		/// a uniform gamma to all three color values.
		/// </summary>
		/// <param name="rAmount">The amount of gamma adjustment to apply to the red channel.
		/// &gt;1 is brighter, &lt;1 is darker.</param>
		/// <param name="gAmount">The amount of gamma adjustment to apply to the green channel.</param>
		/// <param name="bAmount">The amount of gamma adjustment to apply to the blue channel.</param>
		/// <returns>A copy of the image with the given gamma applied.</returns>
		[Pure]
		public PureImage32 Gamma(double rAmount, double gAmount, double bAmount)
		{
			Image32 copy = _image.Clone();
			copy.Gamma(rAmount, gAmount, bAmount);
			return copy;
		}

		/// <summary>
		/// Convert the image to grayscale.
		/// </summary>
		/// <param name="useRelativeBrightness">Whether to factor in the relative
		/// brightness of each color channel, or to treat each channel identically.  In
		/// relative brightness mode, you may specify the brightness values to use for each
		/// channel independently, but the sum of r+g+b must always be less than or equal to 1.0.</param>
		/// <param name="r">The relative brightness for the red channel to use, on a scale
		/// of 0 to 1.</param>
		/// <param name="g">The relative brightness for the green channel to use, on a scale
		/// of 0 to 1.</param>
		/// <param name="b">The relative brightness for the blue channel to use, on a scale
		/// of 0 to 1.</param>
		/// <returns>A copy of the image, converted to grayscale.</returns>
		[Pure]
		public PureImage32 Grayscale(bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			Image32 copy = _image.Clone();
			copy.Grayscale(useRelativeBrightness, r, g, b);
			return copy;
		}

		/// <summary>
		/// Convert the image to 8-bit grayscale.
		/// </summary>
		/// <param name="useRelativeBrightness">Whether to factor in the relative
		/// brightness of each color channel, or to treat each channel identically.</param>
		/// <param name="r">The relative red brightness factor to use when performing the conversion.
		/// By deafult, this is 0.299.</param>
		/// <param name="g">The relative green brightness factor to use when performing the conversion.
		/// By deafult, this is 0.587.</param>
		/// <param name="b">The relative blue brightness factor to use when performing the conversion.
		/// By deafult, this is 0.114.</param>
		/// <returns>A new Image8 that represents this image as an 8-bit grayscale image.</returns>
		[Pure]
		public Image8 Grayscale8(bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
			=> _image.Grayscale8(useRelativeBrightness, r, g, b);

		/// <summary>
		/// Remove some or all of the color saturation from an image.
		/// </summary>
		/// <param name="amount">The amount of saturation to keep on a range of 0 to 1,
		/// where 0 results in a grayscale image, and 1 results in no change at all.</param>
		/// <param name="useRelativeBrightness">Whether to factor in the relative
		/// brightness of each color channel, or to treat each channel identically.  In
		/// relative brightness mode, you may specify the brightness values to use for each
		/// channel independently, but the sum of r+g+b must always be less than or equal to 1.0.</param>
		/// <param name="r">The relative brightness for the red channel to use, on a scale
		/// of 0 to 1.</param>
		/// <param name="g">The relative brightness for the green channel to use, on a scale
		/// of 0 to 1.</param>
		/// <param name="b">The relative brightness for the blue channel to use, on a scale
		/// of 0 to 1.</param>
		/// <returns>A copy of the image, with some of the saturation removed.</returns>
		[Pure]
		public PureImage32 Desaturate(double amount,
			bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			Image32 copy = _image.Clone();
			copy.Desaturate(amount, useRelativeBrightness, r, g, b);
			return copy;
		}

		/// <summary>
		/// Remap the image to a sepia-tone version of itself by forcing
		/// every color's position in YIQ-space.
		/// </summary>
		/// <param name="amount">How saturated the sepia is.  0.0 = grayscale, 1.0 = orange, -1.0 = blue.</param>
		/// <returns>A copy of this image, as sepia tones.</returns>
		[Pure]
		public PureImage32 Sepia(double amount = 0.125)
		{
			Image32 copy = _image.Clone();
			copy.Sepia(amount);
			return copy;
		}

		/// <summary>
		/// Invert the red, green, and blue values in the image (i.e., replace each R with 255-R,
		/// and so on for the other channels).  Alpha will be left unchanged.
		/// </summary>
		/// <returns>A copy of this image, inverted.</returns>
		[Pure]
		public PureImage32 Invert()
		{
			Image32 copy = _image.Clone();
			copy.Invert();
			return copy;
		}

		/// <summary>
		/// Invert one channel in the image (i.e., replace each R with 255-R).
		/// </summary>
		/// <returns>A copy of this image, inverted.</returns>
		[Pure]
		public PureImage32 Invert(ColorChannel channel)
		{
			Image32 copy = _image.Clone();
			copy.Invert(channel);
			return copy;
		}

		/// <summary>
		/// Adjust the hue, saturation, and lightness of an image.  This converts each pixel
		/// in the image to HSL, adds the given deltas, and then converts it back to RGB.
		/// </summary>
		/// <param name="deltaHue">The amount of change to the hue value, from -360.0 to +360.0
		/// degrees.</param>
		/// <param name="deltaSaturation">The amount of change to the saturation value, from -1.0
		/// (grayscale) to +1.0 (maximum saturation).</param>
		/// <param name="deltaLightness">The amount of change to the lightness value, from -1.0 (black)
		/// to +1.0 (white).</param>
		/// <returns>A copy of this image, with the given color adjustments applied.</returns>
		[Pure]
		public PureImage32 HueSaturationLightness(double deltaHue, double deltaSaturation, double deltaLightness)
		{
			Image32 copy = _image.Clone();
			copy.HueSaturationLightness(deltaHue, deltaSaturation, deltaLightness);
			return copy;
		}

		/// <summary>
		/// Adjust the hue, saturation, and brightness of an image.  This converts each pixel
		/// in the image to HSB, adds the given deltas, and then converts it back to RGB.
		/// </summary>
		/// <param name="deltaHue">The amount of change to the hue value, from -360.0 to +360.0
		/// degrees.</param>
		/// <param name="deltaSaturation">The amount of change to the saturation value, from -1.0
		/// (grayscale) to +1.0 (maximum saturation).</param>
		/// <param name="deltaBrightness">The amount of change to the brightness value, from -1.0 (black)
		/// to +1.0 (white).</param>
		/// <returns>A copy of this image, with the given color adjustments applied.</returns>
		[Pure]
		public PureImage32 HueSaturationBrightness(double deltaHue, double deltaSaturation, double deltaBrightness)
		{
			Image32 copy = _image.Clone();
			copy.HueSaturationBrightness(deltaHue, deltaSaturation, deltaBrightness);
			return copy;
		}

		/// <summary>
		/// Adjust the brightness and contrast of the image (R, G, and B channels).
		/// </summary>
		/// <param name="brightness">The brightness adjustment, on a scale
		/// of -1.0 = all black, 0.0 = no change, and +1.0 = all white.</param>
		/// <param name="contrast">The contrast adjustment on a scale of -1.0 = featureless gray,
		/// 0.0 = no change, and +1.0 = maximum contrast (pure black and pure white).</param>
		/// <returns>A copy of this image, with the color adjustments applied.</returns>
		[Pure]
		public PureImage32 BrightnessContrast(double brightness, double contrast)
		{
			Image32 copy = _image.Clone();
			copy.BrightnessContrast(brightness, contrast);
			return copy;
		}

		/// <summary>
		/// Adjust the brightness range of the image to the given values.  This "stretches"
		/// the existing values of each channel to match the provided new range.  If passed
		/// 0 and 256, no changes will be made to the image.  The provided parameters may
		/// be outside the normal range of 0 and 256:  Brightness adjustments will be clamped
		/// to the allowed endpoints.
		/// </summary>
		/// <param name="newMin">The new minimum value.</param>
		/// <param name="newRange">The new range, from that minimum.</param>
		/// <returns>A copy of this image, with the color adjustments applied.</returns>
		[Pure]
		public PureImage32 AdjustRange(double newMin, double newRange)
		{
			Image32 copy = _image.Clone();
			copy.AdjustRange(newMin, newRange);
			return copy;
		}

		/// <summary>
		/// Adjust the brightness range of the image to the given values.  This "stretches"
		/// the existing values of each channel to match the provided new range.  If passed
		/// 0 and 256, no changes will be made to the image.  The provided parameters may
		/// be outside the normal range of 0 and 256:  Brightness adjustments will be clamped
		/// to the allowed endpoints.
		/// </summary>
		/// <param name="redNewMin">The new minimum value for the red channel.</param>
		/// <param name="redRange">The new range, from that minimum, for the red channel.</param>
		/// <param name="greenNewMin">The new minimum value for the green channel.</param>
		/// <param name="greenRange">The new range, from that minimum, for the green channel.</param>
		/// <param name="blueNewMin">The new minimum value for the blue channel.</param>
		/// <param name="blueRange">The new range, from that minimum, for the blue channel.</param>
		/// <param name="alphaNewMin">The new minimum value for the alpha channel.</param>
		/// <param name="alphaRange">The new range, from that minimum, for the alpha channel.</param>
		/// <returns>A copy of this image, with the color adjustments applied.</returns>
		[Pure]
		public PureImage32 AdjustRange(double redNewMin, double redRange,
			double greenNewMin, double greenRange,
			double blueNewMin, double blueRange,
			double alphaNewMin, double alphaRange)
		{
			Image32 copy = _image.Clone();
			copy.AdjustRange(redNewMin, redRange, greenNewMin, greenRange,
				blueNewMin, blueRange, alphaNewMin, alphaRange);
			return copy;
		}

		/// <summary>
		/// Perform a fast remapping of the colors in the image via the given four
		/// lookup tables, one table per channel.
		/// </summary>
		/// <param name="redLookup">A lookup table for the red channel, which must have at least 256 entries.</param>
		/// <param name="greenLookup">A lookup table for the green channel, which must have at least 256 entries.</param>
		/// <param name="blueLookup">A lookup table for the blue channel, which must have at least 256 entries.</param>
		/// <param name="alphaLookup">A lookup table for the alpha channel, which must have at least 256 entries.</param>
		/// <returns>A copy of this image, with the color adjustments applied.</returns>
		[Pure]
		public PureImage32 RemapValues(ReadOnlySpan<byte> redLookup, ReadOnlySpan<byte> greenLookup,
			ReadOnlySpan<byte> blueLookup, ReadOnlySpan<byte> alphaLookup)
		{
			Image32 copy = _image.Clone();
			copy.RemapValues(redLookup, greenLookup, blueLookup, alphaLookup);
			return copy;
		}

		/// <summary>
		/// Adjust the color temperature of the image.  This uses
		/// Tanner Helland's color-temperature technique, which he describes at
		/// https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html .
		/// </summary>
		/// <param name="temperature">The desired color temperature in Kelvin.  6600 effectively
		/// leaves the image as-is.  Values from 1000 to 40000 are likely to work reasonably
		/// well; values outside that range will be clamped to that range.</param>
		/// <returns>A copy of this image, with the color adjustments applied.</returns>
		public PureImage32 ColorTemperature(double temperature)
		{
			Image32 copy = _image.Clone();
			copy.ColorTemperature(temperature);
			return copy;
		}

		#endregion

		#region Channel extraction/combining

		/// <summary>
		/// Extract out the given RGBA channel from this image as a new 8-bit image.
		/// </summary>
		/// <param name="channel">The channel to extract.</param>
		/// <returns>The extracted channel.</returns>
		/// <exception cref="ArgumentException">Thrown if the given channel is unknown.</exception>
		[Pure]
		public Image8 ExtractChannel(ColorChannel channel)
			=> _image.ExtractChannel(channel);

		/// <summary>
		/// Combine separate 8-bit RGBA channel images into a single 32-bit RGBA image.
		/// </summary>
		/// <param name="red">The red channel.  If null, all zeros will be used.</param>
		/// <param name="green">The green channel.  If null, all zeros will be used.</param>
		/// <param name="blue">The blue channel.  If null, all zeros will be used.</param>
		/// <param name="alpha">The alpha channel.  If null, all 255 values will be used.</param>
		/// <returns>The resulting image.</returns>
		/// <exception cref="ArgumentException">Thrown if the provided image channels are
		/// not all of the same dimensions.</exception>
		/// <exception cref="ArgumentNullException">Thrown if all four channel images are null.</exception>
		[Pure]
		public static PureImage32 CombineChannels(Image8? red = null, Image8? green = null, Image8? blue = null,
			Image8? alpha = null)
			=> Image32.CombineChannels(red, green, blue, alpha);

		/// <summary>
		/// Swap color channels in the image with other color channels in the same image.
		/// </summary>
		/// <param name="redChannel">The current channel to use for the new red channel.</param>
		/// <param name="greenChannel">The current channel to use for the new green channel.</param>
		/// <param name="blueChannel">The current channel to use for the new blue channel.</param>
		/// <param name="alphaChannel">The current channel to use for the new alpha channel.</param>
		/// <returns>A copy of this image, with the channels rearranged.</returns>
		[Pure]
		public PureImage32 SwapChannels(ColorChannel redChannel,
			ColorChannel greenChannel, ColorChannel blueChannel,
			ColorChannel alphaChannel = ColorChannel.Alpha)
		{
			Image32 copy = _image.Clone();
			copy.SwapChannels(redChannel, greenChannel, blueChannel, alphaChannel);
			return copy;
		}

		#endregion

		#region Componentwise operators

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been added together.
		/// </summary>
		/// <param name="a">The first image to add.</param>
		/// <param name="b">The second image to add.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been added together.</returns>
		[Pure]
		public static PureImage32 operator +(PureImage32 a, PureImage32 b)
			=> a._image + b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been subtracted.
		/// </summary>
		/// <param name="a">The first image.</param>
		/// <param name="b">The second image to subtract.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been subtracted.</returns>
		[Pure]
		public static PureImage32 operator -(PureImage32 a, PureImage32 b)
			=> a._image - b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been multiplied together.
		/// </summary>
		/// <param name="a">The first image to multiply.</param>
		/// <param name="b">The second image to multiply.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been multiplied.</returns>
		[Pure]
		public static PureImage32 operator *(PureImage32 a, PureImage32 b)
			=> a._image * b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static PureImage32 operator *(PureImage32 image, int scalar)
			=> image._image * scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static PureImage32 operator *(PureImage32 image, double scalar)
			=> image._image * scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static PureImage32 operator /(PureImage32 image, int scalar)
			=> image._image / scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static PureImage32 operator /(PureImage32 image, double scalar)
			=> image._image / scalar;

		/// <summary>
		/// Create a new image of the same size as the given image, where each
		/// pixel's R, G, and B components have been inverted, replacing each value V
		/// with 255 - V.
		/// </summary>
		/// <param name="image">The image to invert.</param>
		/// <returns>A new image where all components have been inverted.</returns>
		[Pure]
		public static PureImage32 operator ~(PureImage32 image)
			=> ~image._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been inverted, replacing each value V
		/// with (256 - V) % 256.
		/// </summary>
		/// <param name="image">The image to negate.</param>
		/// <returns>A new image where all components have been inverted.</returns>
		[Pure]
		public static PureImage32 operator -(PureImage32 image)
			=> -image._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-or'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-or'ed together.</returns>
		[Pure]
		public static PureImage32 operator |(PureImage32 a, PureImage32 b)
			=> a._image | b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-and'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-and'ed together.</returns>
		[Pure]
		public static PureImage32 operator &(PureImage32 a, PureImage32 b)
			=> a._image & b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-xor'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-xor'ed together.</returns>
		[Pure]
		public static PureImage32 operator ^(PureImage32 a, PureImage32 b)
			=> a._image ^ b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted left by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted left.</returns>
		[Pure]
		public static PureImage32 operator <<(PureImage32 image, int amount)
			=> image._image << amount;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted logically right by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted logically right.</returns>
		[Pure]
		public static PureImage32 operator >>(PureImage32 image, int amount)
			=> image._image >> amount;

		#endregion

		#region Quantization

		/// <summary>
		/// Generate a quantized palette of this image with at most the given number
		/// of colors in it.  This uses Heckbert's median-cut algorithm.
		/// </summary>
		/// <param name="numColors">The maximum number of colors to emit in the
		/// quantized palette.</param>
		/// <param name="useOriginalColors">Whether to use only the original colors
		/// or to choose other nearby colors for a better palette.</param>
		/// <param name="includeAlpha">Whether to include alpha in the calculations,
		/// or whether to ignore it (if ignored, all colors in the palette will have
		/// an alpha of 255).</param>
		/// <returns>The palette.</returns>
		[Pure]
		public Color32[] Quantize(int numColors,
			bool useOriginalColors = false, bool includeAlpha = false)
			=> _image.Quantize(numColors, useOriginalColors, includeAlpha);

		/// <summary>
		/// Generate a histogram of all colors in the image.  The return data will not
		/// be ordered by color value, but by frequency, since "sort by color" doesn't
		/// make much sense for RGBA tuples.
		/// </summary>
		/// <param name="includeAlpha">Whether to include the alpha when computing the
		/// histogram, or whether to ignore alpha.  If alpha is not included, all returned
		/// colors will be opaque (alpha = 255).</param>
		/// <returns>A histogram representing each color of the image and the number of
		/// times it occurs, sorted in descending order of frequency.</returns>
		[Pure]
		public (Color32, int)[] Histogram(bool includeAlpha = false)
			=> _image.Histogram(includeAlpha);

		/// <summary>
		/// Convert this image to a paletted 8-bit image with the given palette using
		/// the given dither algorithm.
		/// </summary>
		/// <param name="palette">The palette to use.</param>
		/// <param name="ditherMode">The dithering technique to apply.  By default, no
		/// dithering is applied, and every color is remapped to its nearest neighbor.</param>
		/// <param name="useVisualWeighting">Whether to use linear-space color distances
		/// or visually-weighted color distances.  Visually-weighted color distances may
		/// produce more accurate conversions, at a performance cost.</param>
		/// <returns>The same image, converted to a paletted 8-bit image.</returns>
		public Image8 ToImage8(ReadOnlySpan<Color32> palette, DitherMode ditherMode = default,
			bool useVisualWeighting = false)
			=> _image.ToImage8(palette, ditherMode, useVisualWeighting);

		/// <summary>
		/// Convert this image to a paletted 8-bit image with the given number of colors using
		/// the given dither algorithm.
		/// </summary>
		/// <param name="numColors">The number of colors to use in the palette.
		/// The default is 256.</param>
		/// <param name="useOriginalColors">Whether to use only the original colors
		/// or to choose other nearby colors for a better palette.</param>
		/// <param name="includeAlpha">Whether to include alpha in the calculations,
		/// or whether to ignore it (if ignored, all colors in the palette will have
		/// an alpha of 255).</param>
		/// <param name="ditherMode">The dithering technique to apply.  By default, no
		/// dithering is applied, and every color is remapped to its nearest neighbor.</param>
		/// <param name="useVisualWeighting">Whether to use linear-space color distances
		/// or visually-weighted color distances.  Visually-weighted color distances may
		/// produce more accurate conversions, at a performance cost.</param>
		/// <returns>The same image, converted to a paletted 8-bit image.</returns>
		public Image8 ToImage8(int numColors = 256, DitherMode ditherMode = default,
			bool useOriginalColors = false, bool includeAlpha = false,
			bool useVisualWeighting = false)
			=> _image.ToImage8(numColors, ditherMode,
				useOriginalColors, includeAlpha, useVisualWeighting);

		/// <summary>
		/// Convert an Image32 to an Image24.
		/// </summary>
		/// <returns>A copy of this image.</returns>
		public Image24 ToImage24()
			=> _image.ToImage24();

		/// <summary>
		/// Converting an Image32 to an Image32 is a no-op, but it is allowed, and does
		/// nothing more than simply Clone() the image.
		/// </summary>
		/// <returns>A copy of this image.</returns>
		public Image32 ToImage32()
			=> _image.ToImage32();

		#endregion

		#region Filling

		/// <summary>
		/// Create a new image of the same dimensions that consists only of the given color.
		/// </summary>
		/// <returns>A new image of the same size that consists only of the given color.</returns>
		[Pure]
		public PureImage32 Fill(Color32 color)
			=> new PureImage32(Size, color);

		#endregion

		#region Rectangle Filling (solid)

		/// <summary>
		/// Fill the given rectangular area of the image with the given color.
		/// </summary>
		/// <param name="rect">The rectangular area of the image to fill.</param>
		/// <param name="drawColor">The color to fill with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping step if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		/// <returns>A copy of the image that has had the given rectangle filled with color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage32 FillRect(Rect rect, Color32 drawColor, BlitFlags blitFlags = BlitFlags.Copy)
			=> FillRect(rect.X, rect.Y, rect.Width, rect.Height, drawColor, blitFlags);

		/// <summary>
		/// Fill the given rectangular area of the image with the given color.
		/// </summary>
		/// <param name="x">The leftmost coordinate of the rectangle to fill.</param>
		/// <param name="y">The topmost coordinate of the rectangle to fill.</param>
		/// <param name="width">The width of the rectangle to fill.</param>
		/// <param name="height">The height of the rectangle to fill.</param>
		/// <param name="drawColor">The color to fill with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping step if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		/// <returns>A copy of the image that has had the given rectangle filled with color.</returns>
		[Pure]
		public PureImage32 FillRect(int x, int y, int width, int height, Color32 drawColor,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.FillRect(x, y, width, height, drawColor, blitFlags);
			return copy;
		}

		#endregion

		#region Rectangle Filling (gradient)

		/// <summary>
		/// Fill the given rectangle with a smooth color gradient defined by the
		/// four corners of the fill.
		/// </summary>
		/// <param name="rect">The rectangular area of the image to fill.</param>
		/// <param name="topLeft">The color at the top-left corner of the rectangle.</param>
		/// <param name="topRight">The color at the top-right corner of the rectangle.</param>
		/// <param name="bottomLeft">The color at the bottom-left corner of the rectangle.</param>
		/// <param name="bottomRight">The color at the bottom-right corner of the rectangle.</param>
		/// <param name="blitFlags">Which mode to use to draw the gradient:  This method
		/// works in Copy, Alpha, or PMAlpha modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is
		/// fully contained within the image.</param>
		/// <returns>A copy of the image, with a gradient rectangle drawn on it.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage32 FillGradientRect(Rect rect, Color32 topLeft, Color32 topRight, Color32 bottomLeft, Color32 bottomRight,
			BlitFlags blitFlags = BlitFlags.Copy)
			=> FillGradientRect(rect.X, rect.Y, rect.Width, rect.Height,
				topLeft, topRight, bottomLeft, bottomRight, blitFlags);

		/// <summary>
		/// Fill the given rectangle with a smooth color gradient defined by the
		/// four corners of the fill.
		/// </summary>
		/// <param name="x">The leftmost coordinate of the rectangle to fill.</param>
		/// <param name="y">The topmost coordinate of the rectangle to fill.</param>
		/// <param name="width">The width of the rectangle to fill.</param>
		/// <param name="height">The height of the rectangle to fill.</param>
		/// <param name="topLeft">The color at the top-left corner of the rectangle.</param>
		/// <param name="topRight">The color at the top-right corner of the rectangle.</param>
		/// <param name="bottomLeft">The color at the bottom-left corner of the rectangle.</param>
		/// <param name="bottomRight">The color at the bottom-right corner of the rectangle.</param>
		/// <param name="blitFlags">Which mode to use to draw the gradient:  This method
		/// works in Copy, Alpha, or PMAlpha modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is
		/// fully contained within the image.</param>
		/// <returns>A copy of the image, with a gradient rectangle drawn on it.</returns>
		public PureImage32 FillGradientRect(int x, int y, int width, int height,
			Color32 topLeft, Color32 topRight, Color32 bottomLeft, Color32 bottomRight,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.FillGradientRect(x, y, width, height, topLeft, topRight, bottomLeft, bottomRight, blitFlags);
			return copy;
		}

		#endregion

		#region Rectangle Drawing

		/// <summary>
		/// Draw a rectangle, with the given thickness.  The outer coordinates of the rectangle
		/// are provided, and the thickness goes *inward*.  The rectangle will be drawn
		/// inside the given coordinates (i.e., this is just like FillRect with a hole in the
		/// middle; unlike some other rectangle-drawing routines, this does *not* draw outside
		/// the given rectangle).
		/// </summary>
		/// <param name="rect">The area to draw rectangle on.</param>
		/// <param name="color">The color to draw with.</param>
		/// <param name="thickness">The thickness of the rectangle to draw, in pixels.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping steps if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		[Pure]
		public PureImage32 DrawRect(Rect rect, Color32 color, int thickness = 1, BlitFlags blitFlags = BlitFlags.Copy)
			=> DrawRect(rect.X, rect.Y, rect.Width, rect.Height, color, thickness, blitFlags);

		/// <summary>
		/// Draw a rectangle, with the given thickness.  The outer coordinates of the rectangle
		/// are provided, and the thickness goes *inward*.  The rectangle will be drawn
		/// inside the given coordinates (i.e., this is just like FillRect with a hole in the
		/// middle; unlike some other rectangle-drawing routines, this does *not* draw outside
		/// the given rectangle).
		/// </summary>
		/// <param name="x">The leftmost coordinate of the rectangle to draw.</param>
		/// <param name="y">The topmost coordinate of the rectangle to draw.</param>
		/// <param name="width">The width of the rectangle to draw.</param>
		/// <param name="height">The height of the rectangle to draw.</param>
		/// <param name="color">The color to draw with.</param>
		/// <param name="thickness">The thickness of the rectangle to draw, in pixels.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping steps if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		[Pure]
		public PureImage32 DrawRect(int x, int y, int width, int height, Color32 color, int thickness = 1,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.DrawRect(x, y, width, height, color, thickness, blitFlags);
			return copy;
		}

		#endregion

		#region Line drawing

		/// <summary>
		/// An implementation of Bresenham's line-drawing routine, with Cohen-Sutherland
		/// clipping to the image (unless you pass BlitFlags.FastUnsafe).
		/// </summary>
		/// <param name="p1">The starting point to draw from.</param>
		/// <param name="p2">The ending point to draw to.</param>
		/// <param name="color">The color to draw with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping steps if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		/// <returns>A copy of the image, with the line drawn on it.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage32 DrawLine(Vector2i p1, Vector2i p2, Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
			=> DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, blitFlags);

		/// <summary>
		/// An implementation of Bresenham's line-drawing routine, with Cohen-Sutherland
		/// clipping to the image (unless you pass BlitFlags.FastUnsafe).
		/// </summary>
		/// <param name="x1">The starting X coordinate to draw from.</param>
		/// <param name="y1">The starting Y coordinate to draw from.</param>
		/// <param name="x2">The ending X coordinate to draw to.</param>
		/// <param name="y2">The ending Y coordinate to draw to.</param>
		/// <param name="color">The color to draw with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping steps if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		/// <returns>A copy of the image, with the line drawn on it.</returns>
		[Pure]
		public PureImage32 DrawLine(int x1, int y1, int x2, int y2, Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.DrawLine(x1, y1, x2, y2, color, blitFlags);
			return copy;
		}

		#endregion

		#region "Thick" line drawing

		/// <summary>
		/// Draw a "thick" line, which is just a rotated rectangle projected on
		/// top of the given endpoints.
		/// </summary>
		/// <param name="x1">The starting X coordinate of the line.</param>
		/// <param name="y1">The starting Y coordinate of the line.</param>
		/// <param name="x2">The ending X coordinate of the line.</param>
		/// <param name="y2">The ending Y coordinate of the line.</param>
		/// <param name="thickness">The thickness of the line, in pixels.  This can be fractional.</param>
		/// <param name="color">The color of the line.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage32 DrawThickLine(int x1, int y1, int x2, int y2, double thickness,
			Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
			=> DrawThickLine(new Vector2d(x1 + 0.5, y1 + 0.5), new Vector2d(x2 + 0.5, y2 + 0.5),
				thickness, color, blitFlags);

		/// <summary>
		/// Draw a "thick" line, which is just a rotated rectangle projected on
		/// top of the given endpoints.
		/// </summary>
		/// <param name="start">The start point of the line.</param>
		/// <param name="end">The end point of the line.</param>
		/// <param name="thickness">The thickness of the line, in pixels.  This can be fractional.</param>
		/// <param name="color">The color of the line.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with a thick line drawn on it.</returns>
		[Pure]
		public PureImage32 DrawThickLine(Vector2i start, Vector2i end, double thickness,
			Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.DrawThickLine(start, end, thickness, color, blitFlags);
			return copy;
		}

		/// <summary>
		/// Draw a "thick" line, which is just a rotated rectangle projected on
		/// top of the given endpoints.
		/// </summary>
		/// <param name="start">The start point of the line.</param>
		/// <param name="end">The end point of the line.</param>
		/// <param name="thickness">The thickness of the line, in pixels.  This can be fractional.</param>
		/// <param name="color">The color of the line.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with a thick line drawn on it.</returns>
		[Pure]
		public PureImage32 DrawThickLine(Vector2d start, Vector2d end, double thickness,
			Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.DrawThickLine(start, end, thickness, color, blitFlags);
			return copy;
		}

		#endregion

		#region Polygon filling

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(IEnumerable<Vector2> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2[] hardenedPoints = points is Vector2[] array ? array : points.ToArray();
			return FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(IEnumerable<Vector2d> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2d[] hardenedPoints = points is Vector2d[] array ? array : points.ToArray();
			return FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(IEnumerable<Vector2i> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2d[] hardenedPoints = points.Select(p => (Vector2d)p).ToArray();
			return FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(ReadOnlySpan<Vector2> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.FillPolygon(points, color, blitFlags);
			return copy;
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(ReadOnlySpan<Vector2d> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.FillPolygon(points, color, blitFlags);
			return copy;
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage32 FillPolygon(ReadOnlySpan<Vector2i> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.FillPolygon(points, color, blitFlags);
			return copy;
		}

		#endregion

		#region Curve drawing

		/// <summary>
		/// Draw a cubic Bézier spline.
		/// </summary>
		/// <param name="p1">The start point.</param>
		/// <param name="c1">The control point for the start point.</param>
		/// <param name="c2">The control point for the end point.</param>
		/// <param name="p2">The end point.</param>
		/// <param name="color">The color to draw the curve in.</param>
		/// <param name="steps">How many steps (line segments) to use to approximate the curve.
		/// By default, this is derived from the distances between the points.</param>
		/// <param name="blitFlags">Flags to control how the line is drawn.</param>
		/// <returns>A copy of the image with the cubic Bézier drawn on it.</returns>
		[Pure]
		public PureImage32 DrawBezier(Vector2 p1, Vector2 c1, Vector2 c2, Vector2 p2,
			Color32 color, int steps = 0, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image32 copy = _image.Clone();
			copy.DrawBezier(p1, c1, c2, p2, color, steps, blitFlags);
			return copy;
		}

		#endregion

		#region Convolutions

		/// <summary>
		/// Apply a 3x3 emboss convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where emboss has been applied.</returns>
		public PureImage32 Emboss(double strength = 1.0)
			=> _image.Emboss(strength);

		/// <summary>
		/// Apply a 3x3 sharpening convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where sharpening has been applied.</returns>
		public PureImage32 Sharpen(double strength = 1.0)
			=> _image.Sharpen(strength);

		/// <summary>
		/// Apply a 3x3 edge-detection convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <param name="includeDiagonals">Whether to use the version of the kernel that includes diagonals.</param>
		/// <returns>A new image where edge-detection has been applied.</returns>
		public PureImage32 EdgeDetect(double strength = 1.0, bool includeDiagonals = false)
			=> _image.EdgeDetect(strength, includeDiagonals);

		/// <summary>
		/// Apply a simple 3x3 box-blur convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where box blur has been applied.</returns>
		public PureImage32 BoxBlur(double strength = 1.0)
			=> _image.BoxBlur(strength);

		/// <summary>
		/// Apply an approximate 3x3 Gaussian-blur convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where an approximate 3x3 Gaussian blur has been applied.</returns>
		public PureImage32 RoundBlur(double strength = 1.0)
			=> _image.RoundBlur(strength);

		/// <summary>
		/// Apply a 3x3 convolution kernel to the given image, generating a new
		/// image and returning it.  Any values that lie outside the range of [0, 255]
		/// will be clamped to their acceptable endpoints.  At the edges of the
		/// image, only a partial kernel will be applied, and the kernel's divisor will
		/// be adjusted accordingly.  The alpha values will not be affected.
		/// </summary>
		/// <param name="kernel">The 3x3 kernel to apply, in row-major order.</param>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.  Note that this value simply
		/// describes how to multiply the given kernel against the identity kernel,
		/// so values outside the range of 0.0 to 1.0 are well-defined.</param>
		/// <param name="divideBySum">Whether to divide by the sum of the kernel
		/// values applied, or to use the kernel values as-is.</param>
		/// <returns>A copy of the image, with the convolution kernel applied.</returns>
		public PureImage32 Convolve3x3(double[] kernel, double strength = 1.0, bool divideBySum = false)
			=> _image.Convolve3x3(kernel, strength, divideBySum);

		/// <summary>
		/// Apply a Nx1 convolution kernel to the given image, generating a new
		/// image and returning it.  Any values that lie outside the range of [0, 255]
		/// will be clamped to their acceptable endpoints.  At the edges of the
		/// image, only a partial kernel will be applied, and the kernel's divisor will
		/// be adjusted accordingly.  The alpha values will not be affected.
		/// </summary>
		/// <param name="kernel">The N-unit 1-dimensional kernel to apply.  This must
		/// have an odd number of elements.  The center element will be aligned over
		/// each target pixel, while elements before it will be applied to the left
		/// of that pixel, and elements after it will be applied to the right of that
		/// pixel.</param>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.  Note that this value simply
		/// describes how to multiply the given kernel against the identity kernel,
		/// so values outside the range of 0.0 to 1.0 are well-defined.</param>
		/// <param name="divideBySum">Whether to divide by the sum of the kernel
		/// values applied, or to use the kernel values as-is.</param>
		/// <returns>A copy of the image, with the convolution kernel applied.</returns>
		[Pure]
		public PureImage32 ConvolveHorz(ReadOnlySpan<double> kernel, double strength = 1.0, bool divideBySum = false)
			=> _image.ConvolveHorz(kernel, strength, divideBySum);

		/// <summary>
		/// Apply a 1xN convolution kernel to the given image, generating a new
		/// image and returning it.  Any values that lie outside the range of [0, 255]
		/// will be clamped to their acceptable endpoints.  At the edges of the
		/// image, only a partial kernel will be applied, and the kernel's divisor will
		/// be adjusted accordingly.  The alpha values will not be affected.
		/// </summary>
		/// <param name="kernel">The N-unit 1-dimensional kernel to apply.  This must
		/// have an odd number of elements.  The center element will be aligned over
		/// each target pixel, while elements before it will be applied above
		/// that pixel, and elements after it will be applied below that pixel.</param>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.  Note that this value simply
		/// describes how to multiply the given kernel against the identity kernel,
		/// so values outside the range of 0.0 to 1.0 are well-defined.</param>
		/// <param name="divideBySum">Whether to divide by the sum of the kernel
		/// values applied, or to use the kernel values as-is.</param>
		/// <returns>A copy of the image, with the convolution kernel applied.</returns>
		[Pure]
		public PureImage32 ConvolveVert(ReadOnlySpan<double> kernel, double strength = 1.0, bool divideBySum = false)
			=> _image.ConvolveVert(kernel, strength, divideBySum);

		/// <summary>
		/// Apply an arbitrary MxN convolution kernel to the given image, generating a new
		/// image and returning it.  Any values that lie outside the range of [0, 255]
		/// will be clamped to their acceptable endpoints.  At the edges of the
		/// image, only a partial kernel will be applied, and the kernel's divisor will
		/// be adjusted accordingly.  The alpha values will not be affected.
		/// </summary>
		/// <param name="kernel">The MxN kernel to apply.</param>
		/// <param name="kernelWidth">The width of the convolution kernel (the number of
		/// horizontal values).  This must be an odd number >= 1.</param>
		/// <param name="kernelHeight">The height of the convolution kernel (the number of
		/// vertical values).  This must be an odd number >= 1.</param>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.  Note that this value simply
		/// describes how to multiply the given kernel against the identity kernel,
		/// so values outside the range of 0.0 to 1.0 are well-defined.</param>
		/// <param name="divideBySum">Whether to divide by the sum of the kernel
		/// values applied, or to use the kernel values as-is.</param>
		/// <returns>A copy of the image, with the convolution kernel applied.</returns>
		[Pure]
		public PureImage32 Convolve(ReadOnlySpan<double> kernel, int kernelWidth, int kernelHeight,
			double strength = 1.0, bool divideBySum = false)
			=> _image.Convolve(kernel, kernelWidth, kernelHeight, strength, divideBySum);

		/// <summary>
		/// Apply a full Gaussian blur to the image.
		/// </summary>
		/// <param name="radius">The radius of the Gaussian blur, in pixels.</param>
		/// <param name="sigma">The standard deviation of the Gaussian distribution.
		/// If omitted, it will be determined from the radius.  This must not be zero.</param>
		/// <param name="strength">How strongly to apply the blur; 1.0 will apply the blur fully,
		/// while 0.0 will leave the original image unchanged.</param>
		/// <returns>A copy of the image, with the Gaussian blur applied.</returns>
		[Pure]
		public PureImage32 GaussianBlur(double radius, double? sigma = null, double strength = 1.0)
			=> _image.GaussianBlur(radius, sigma, strength);

		#endregion

		#region Content transparency testing

		/// <summary>
		/// Given a rectangle that contains some content, shrink the rectangle so that
		/// it does not contain any right-side columns that are transparent.
		/// </summary>
		/// <param name="rect">The starting rectangle.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>The smallest width that surrounds actual non-transparent content
		/// within the given rectangle, which may be the original width.</returns>
		[Pure]
		public int MeasureContentWidth(Rect rect, byte cutoff = 0)
			=> _image.MeasureContentWidth(rect, cutoff);

		/// <summary>
		/// Given a rectangle that contains some content, shrink the rectangle so that
		/// it does not contain any bottom rows that are transparent.
		/// </summary>
		/// <param name="rect">The starting rectangle.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>The smallest height that surrounds actual non-transparent content
		/// within that rectangle, which may be the original height.</returns>
		[Pure]
		public int MeasureContentHeight(Rect rect, byte cutoff = 0)
			=> _image.MeasureContentHeight(rect, cutoff);

		/// <summary>
		/// Determine if the given rectangle is entirely transparent.  Pixels outside
		/// the image will be treated as transparent, and a zero-width or zero-height
		/// rectangle will as well.
		/// </summary>
		/// <param name="rect">The rectangle of pixels to test.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>True if the row is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsRectTransparent(Rect rect, byte cutoff = 0)
			=> _image.IsRectTransparent(rect, cutoff);

		/// <summary>
		/// Determine if the row of pixels starting at (x, y) and of the given width
		/// is entirely transparent (color.A &lt;= cutoff).  Pixels outside the image will be
		/// treated as transparent, and a zero-width row will as well.
		/// </summary>
		/// <param name="x">The starting X offset of the row.</param>
		/// <param name="y">The vertical offset of the row.</param>
		/// <param name="width">The row's width in pixels.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>True if the row is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsRowTransparent(int x, int y, int width, byte cutoff = 0)
			=> _image.IsRowTransparent(x, y, width, cutoff);

		/// <summary>
		/// Determine if the column of pixels starting at (x, y) and of the given height
		/// is entirely transparent (color.A &lt;= cutoff).  Pixels outside the image will be
		/// treated as transparent, and a zero-height column will as well.
		/// </summary>
		/// <param name="x">The horizontal offset of the column.</param>
		/// <param name="y">The starting Y offset of the column.</param>
		/// <param name="height">The column's height in pixels.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>True if the column is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsColumnTransparent(int x, int y, int height, byte cutoff = 0)
			=> _image.IsColumnTransparent(x, y, height, cutoff);

		#endregion

		#region Equality and hash codes

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel identical.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if the other object is an identical image to this image, false otherwise.</returns>
		[Pure]
		public override bool Equals(object? obj)
			=> obj is PureImage32 other && Equals(other)
				|| obj is Image32 other2 && _image.Equals(other2);

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel
		/// identical.  This runs in O(n) worst-case time, but that's only hit if the
		/// two images have identical dimensions:  For images with unequal dimensions,
		/// this always runs in O(1) time.
		/// </summary>
		/// <param name="other">The other image to compare against.</param>
		/// <returns>True if the other image is an identical image to this image, false otherwise.</returns>
		[Pure]
		public bool Equals(PureImage32 other)
			=> _image.Equals(other._image);

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel
		/// identical.  This runs in O(n) worst-case time, but that's only hit if the
		/// two images have identical dimensions:  For images with unequal dimensions,
		/// this always runs in O(1) time.
		/// </summary>
		/// <param name="other">The other image to compare against.</param>
		/// <returns>True if the other image is an identical image to this image, false otherwise.</returns>
		[Pure]
		public bool Equals(Image32 other)
			=> _image.Equals(other);

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>True if the images are identical, false otherwise.</returns>
		[Pure]
		public static bool operator ==(PureImage32 a, PureImage32 b)
			=> a.Equals(b);

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>False if the images are identical, true otherwise.</returns>
		[Pure]
		public static bool operator !=(PureImage32 a, PureImage32 b)
			=> !a.Equals(b);

		/// <summary>
		/// Calculate a hash code representing the pixels of this image.  This runs
		/// in O(width*height) time and does not cache its result, so don't invoke this
		/// unless you need it.
		/// </summary>
		[Pure]
		public override int GetHashCode()
			=> _image.GetHashCode();

		#endregion
	}
}
