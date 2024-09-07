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
	/// A PureImage8 is just like an Image8, and it has nearly the same methods,
	/// but all operations on a PureImage8 produce a new PureImage8:  No data
	/// is modified in place.  This is a more rational API in exchange for a cost
	/// in performance on some (but not all) operations.
	/// </summary>
	[DebuggerDisplay("Image8 {Width}x{Height}")]
	public readonly struct PureImage8 : IPureImage<byte>
	{
		#region Core properties and fields

		private readonly Image8 _image;

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
		/// Obtain direct access to the underlying array of pixel data.  You should
		/// not change this data if you want to maintain the semantics of the PureImage8,
		/// but the array is available so that you can directly read the data.
		/// </summary>
		/// <returns>The underlying array of pixel data.</returns>
		[Pure]
		public byte[] Data => _image.Data;

		/// <summary>
		/// Obtain direct access to the underlying array of palette data.  You should
		/// not change this data if you want to maintain the semantics of the PureImage8,
		/// but the array is available so that you can directly read the data.
		/// </summary>
		/// <returns>The underlying array of color data.</returns>
		[Pure]
		public Color32[] Palette => _image.Palette;

		/// <summary>
		/// The size of this image, represented as a 2D vector.
		/// </summary>
		[Pure]
		public Vector2i Size => _image.Size;

		/// <summary>
		/// A static "empty" image that never has pixel data in it.
		/// </summary>
		public static PureImage8 Empty { get; } = new PureImage8(new Image8(0, 0));

		#endregion

		#region Pixel-plotting

		/// <summary>
		/// Access the pixels using "easy" 2D array-brackets.  This is safe, easy, reliable,
		/// and slower than almost every other way of accessing the pixels, but it's very useful
		/// for simple cases.  Reading a pixel outside the image will return 0.
		/// </summary>
		/// <param name="x">The X coordinate of the pixel to read or write.</param>
		/// <param name="y">The Y coordinate of the pixel to read or write.</param>
		/// <returns>The color at that pixel.</returns>
		public byte this[int x, int y]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[Pure]
			get => x < 0 || x >= Width || y < 0 || y >= Height ? (byte)0 : Data[y * Width + x];
		}

		#endregion

		#region Construction and conversion

		/// <summary>
		/// Construct a new PureImage8 from an Image8.  This simply wraps the
		/// provided Image8, which you should not use again after wrapping it.
		/// </summary>
		/// <param name="image">The image to wrap as a PureImage8.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(Image8 image)
		{
			_image = image ?? throw new ArgumentNullException(nameof(image));
		}

		/// <summary>
		/// Convert an Image8 into a PureImage8.  This simply wraps the
		/// provided Image8, which you should not use again after wrapping it.
		/// </summary>
		/// <param name="image">The Image8 to convert to a PureImage8.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PureImage8(Image8 image)
			=> new PureImage8(image);

		/// <summary>
		/// Convert a PureImage8 into an Image8.  This makes a clone of
		/// the existing PureImage8.
		/// </summary>
		/// <param name="pureImage">The PureImage8 to convert to an Image8.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Image8(PureImage8 pureImage)
			=> pureImage._image.Clone();

		/// <summary>
		/// Construct an image from the given image file.
		/// </summary>
		/// <param name="filename">The filename to load.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(string filename, ImageFormat imageFormat = default)
			: this(new Image8(filename, imageFormat))
		{
		}

		/// <summary>
		/// Construct an image from the given image file data.
		/// </summary>
		/// <param name="data">The image file data to load.</param>
		/// <param name="filenameIfKnown">The filename of that image file data, if known.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
			: this(new Image8(data, filenameIfKnown, imageFormat))
		{
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(Vector2i size)
			: this(new Image8(size))
		{
		}

		/// <summary>
		/// Construct an image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="fillColor">The color for all of the new pixels in the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(Vector2i size, byte fillColor)
			: this(new Image8(size, fillColor))
		{
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(int width, int height)
			: this(new Image8(width, height))
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
		public PureImage8(int width, int height, byte fillColor)
			: this(new Image8(width, height, fillColor))
		{
		}

		/// <summary>
		/// Construct a pure image around the given color data, WITHOUT copying it.<br />
		/// <br />
		/// WARNING:  This constructor does NOT make a copy of the provided data; it *wraps*
		/// the provided arrays directly, which must be at least width*height in size (and that
		/// fact will at least be validated so that other methods can assume it).  This is
		/// very fast, but you can also use it to break things if you're not careful.  When
		/// in doubt, this is *not* the method you want.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="data">The data to use for the image.</param>
		/// <param name="palette">The palette to use for the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PureImage8(int width, int height, byte[] data, Color32[] palette)
			: this(new Image8(width, height, data, palette))
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
		public PureImage8(int width, int height, ReadOnlySpan<byte> rawData)
			: this(new Image8(width, height, rawData))
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
		public unsafe PureImage8(int width, int height, byte* rawData, int rawDataLength)
			: this(new Image8(width, height, rawData, rawDataLength))
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
		public PureImage8 Clone()
			=> _image.Clone();

		/// <summary>
		/// Convert this image to a 24-bit RGBA image.
		/// </summary>
		[Pure]
		public Image24 ToImage24()
			=> _image.ToImage24();

		/// <summary>
		/// Convert this image to a 32-bit RGBA image.
		/// </summary>
		[Pure]
		public Image32 ToImage32()
			=> _image.ToImage32();

		#endregion

		#region Image8 loading/saving

		/// <summary>
		/// Construct an image from the embedded resource with the given name.  The embedded
		/// resource must be of a file format that this image library is capable of
		/// decoding, like PNG or JPEG.
		/// </summary>
		/// <param name="assembly">The assembly containing the embedded resource.</param>
		/// <param name="name">The name of the embedded image resource to load.  This may use
		/// slashes in the pathname to separate components.</param>
		/// <returns>The newly-loaded image, or PureImage8.Empty if no such image exists
		/// or is not a valid image file.</returns>
		[Pure]
		public static PureImage8 FromEmbeddedResource(Assembly assembly, string name)
			=> Image8.FromEmbeddedResource(assembly, name) ?? Empty;

		/// <summary>
		/// Load the given file as a new image, which must be of a supported image
		/// format like PNG or JPEG.
		/// </summary>
		/// <param name="filename">The filename of the image to load.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or PureImage8.Empty if it can't be loaded.</returns>
		[Pure]
		public static PureImage8 LoadFile(string filename, ImageFormat imageFormat = default)
			=> Image8.LoadFile(filename, imageFormat) ?? Empty;

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
		/// <returns>The new image, or PureImage8.Empty if it can't be loaded.</returns>
		[Pure]
		public static PureImage8 LoadFile(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
			=> Image8.LoadFile(data, filenameIfKnown, imageFormat) ?? Empty;

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
		public PureImage8 ResizeToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit)
			=> Resize(Fit(Size, containerSize, fitMode));

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="imageSize">The new size of the image.</param>
		/// <returns>A copy of this image, resized to fit the container.</returns>
		[Pure]
		public PureImage8 Resize(Vector2i imageSize)
			=> Resize(imageSize.X, imageSize.Y);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="newWidth">The new width of the image.</param>
		/// <param name="newHeight">The new height of the image.</param>
		/// <returns>A copy of this image, resized to fit the container.</returns>
		[Pure]
		public PureImage8 Resize(int newWidth, int newHeight)
		{
			Image8 result = new Image8(newWidth, newHeight);

			int xStep = (int)(((long)Width << 16) / newWidth);
			int yStep = (int)(((long)Height << 16) / newHeight);

			unsafe
			{
				fixed (byte* destBase = result.Data)
				fixed (byte* srcBase = Data)
				{
					for (int destY = 0, srcYScaled = 0; destY < newHeight; destY++, srcYScaled += yStep)
					{
						int srcY = srcYScaled >> 16;
						byte* srcRow = srcBase + srcY * Width;
						byte* dest = destBase + destY * newWidth;
						byte* destEnd = dest + newWidth;

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
		/// <param name="imageSize">The size of the image to clip to.</param>
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
		public PureImage8 Extract(int x, int y, int width, int height)
			=> _image.Extract(x, y, width, height);

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		[Pure]
		public PureImage8 Crop(int x, int y, int width, int height)
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
		public PureImage8 Pad(int left = 0, int top = 0, int right = 0, int bottom = 0, byte fillColor = default)
		{
			Image8 image = new Image8(Width + left + right, Height + top + bottom, fillColor);
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
		/// <param name="blitFlags">Flags that control how the blit operation is performed.</param>
		/// <returns>A new image, with part of it replaced by 'srcImage'.</returns>
		/// <remarks>
		/// This Blit() does *not* perform the update in-place, and is typically slower than Image8.Blit()
		/// by a lot.  This is usually not the method you want unless regularity (or piping)
		/// matters more to you than performance.
		/// </remarks>
		[Pure]
		public PureImage8 Blit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height,
			BlitFlags blitFlags = default)
		{
			Image8 copy = _image.Clone();
			copy.Blit(srcImage, srcX, srcY, destX, destY, width, height, blitFlags);
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
		public PureImage8 PatternBlit(Image8 srcImage, Rect srcRect, Rect destRect)
		{
			Image8 copy = _image.Clone();
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
		public PureImage8 FlipVert()
		{
			Image8 result = new Image8(Width, Height);

			unsafe
			{
				fixed (byte* srcBase = Data)
				fixed (byte* destBase = result.Data)
				{
					byte* srcRow = srcBase;
					byte* destRow = destBase + Width * (Height - 1);
					for (int y = 0; y < Height; y++)
					{
						byte* src = srcRow;
						byte* dest = destRow;
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
		public PureImage8 FlipHorz()
		{
			Image8 result = new Image8(Width, Height);

			unsafe
			{
				fixed (byte* srcBase = Data)
				fixed (byte* destBase = result.Data)
				{
					byte* srcRow = srcBase;
					byte* destRow = destBase;
					for (int y = 0; y < Height; y++)
					{
						byte* src = srcRow;
						byte* dest = destRow + Width;
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
		public PureImage8 Rotate90()
		{
			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Make the result image.
			Image8 result = new Image8(newWidth, newHeight);

			// Copy from the old image to the new image.
			unsafe
			{
				fixed (byte* srcBase = Data)
				fixed (byte* destBase = result.Data)
				{
					for (int x = 0; x < oldWidth; x++)
					{
						byte* src = srcBase + x + oldWidth * (oldHeight - 1);
						byte* dest = destBase + x * newWidth;
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
		public PureImage8 Rotate90CCW()
		{
			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Make the result image.
			Image8 result = new Image8(newWidth, newHeight);

			// Copy from the old image to the new image.
			unsafe
			{
				fixed (byte* srcBase = Data)
				fixed (byte* destBase = result.Data)
				{
					for (int x = 0; x < newWidth; x++)
					{
						byte* src = srcBase + x * oldWidth;
						byte* dest = destBase + x + newWidth * (newHeight - 1);
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
		public PureImage8 Rotate180()
		{
			Image8 result = new Image8(Width, Height);

			unsafe
			{
				fixed (byte* srcBase = Data)
				fixed (byte* destBase = result.Data)
				{
					byte* src = srcBase;
					byte* dest = destBase + Width * Height;
					byte* end = srcBase + Width * Height;
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
		/// Multiply every color value in the palette by the given scalar values.
		/// </summary>
		/// <returns>A copy of this image, with every color in the palette
		/// multiplied by the given scalars.</returns>
		[Pure]
		public PureImage8 MultiplyPalette(float r, float g, float b, float a)
		{
			Image8 copy = _image.Clone();
			copy.MultiplyPalette(r, g, b, a);
			return copy;
		}

		/// <summary>
		/// Multiply every color value in the palette by the given scalar values.
		/// </summary>
		/// <returns>A copy of this image, with every color value multiplied by the given scalars.</returns>
		[Pure]
		public PureImage8 MultiplyPalette(double r, double g, double b, double a)
		{
			Image8 copy = _image.Clone();
			copy.MultiplyPalette(r, g, b, a);
			return copy;
		}

		/// <summary>
		/// Premultiply the alpha value to the red, green, and blue values of every
		/// color in the palette.
		/// </summary>
		/// <returns>A copy of this image, with the alpha values premultiplied.</returns>
		[Pure]
		public PureImage8 PremultiplyAlpha()
		{
			Image8 copy = _image.Clone();
			copy.PremultiplyAlpha();
			return copy;
		}

		#endregion

		#region Color remapping

		/// <summary>
		/// Skim through the image and replace all exact instances of the given value with another.
		/// </summary>
		/// <param name="src">The pixel value to replace.</param>
		/// <param name="dest">Its replacement pixel value.</param>
		/// <returns>A copy of the image, with the given pixel value replaced.</returns>
		[Pure]
		public PureImage8 RemapColor(byte src, byte dest)
		{
			Image8 copy = _image.Clone();
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
		public PureImage8 RemapColor(Dictionary<byte, byte> dictionary)
		{
			Image8 copy = _image.Clone();
			copy.RemapColor(dictionary);
			return copy;
		}

		/// <summary>
		/// Skim through the palette and replace all exact instances of the given color with another.
		/// </summary>
		/// <param name="src">The color to replace.</param>
		/// <param name="dest">Its replacement color.</param>
		/// <returns>A copy of the image, with the given color replaced.</returns>
		[Pure]
		public PureImage8 RemapPalette(Color32 src, Color32 dest)
		{
			Image8 copy = _image.Clone();
			copy.RemapPalette(src, dest);
			return copy;
		}

		/// <summary>
		/// Skim through the palette and replace many colors at once via a dictionary.
		/// This is typically much slower than many calls to Remap() above, unless the
		/// replacement table is large relative to the palette size.
		/// </summary>
		/// <param name="dictionary">The dictionary that describes all replacement values.
		/// If a color does not exist in the dictionary, it will not be changed.</param>
		/// <returns>The image, with the given colors remapped.</returns>
		[Pure]
		public PureImage8 RemapPalette(Dictionary<Color32, Color32> dictionary)
		{
			Image8 copy = _image.Clone();
			copy.RemapPalette(dictionary);
			return copy;
		}

		/// <summary>
		/// Remap each color by passing it through the given matrix transform.
		/// </summary>
		/// <param name="matrix">The matrix to multiply each color vector by.</param>
		[Pure]
		public PureImage8 RemapPalette(Matrix3 matrix)
		{
			Image8 copy = _image.Clone();
			copy.RemapPalette(matrix);
			return copy;
		}

		/// <summary>
		/// Apply uniform gamma to all three color values (i.e., convert each color to [0, 1] and then
		/// raise it to the given power).
		/// </summary>
		/// <param name="gamma">The gamma exponent to apply.  0 to 1 results in brighter
		/// images, and 1 to infinity results in darker images.</param>
		[Pure]
		public PureImage8 Gamma(double gamma)
		{
			Image8 copy = _image.Clone();
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
		public PureImage8 Gamma(double rAmount, double gAmount, double bAmount)
		{
			Image8 copy = _image.Clone();
			copy.Gamma(rAmount, gAmount, bAmount);
			return copy;
		}

		/// <summary>
		/// Convert the palette to grayscale.
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
		public PureImage8 Grayscale(bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			Image8 copy = _image.Clone();
			copy.Grayscale(useRelativeBrightness, r, g, b);
			return copy;
		}

		/// <summary>
		/// Remove some or all of the color saturation from the palette.
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
		public PureImage8 Desaturate(double amount,
			bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			Image8 copy = _image.Clone();
			copy.Desaturate(amount, useRelativeBrightness, r, g, b);
			return copy;
		}

		/// <summary>
		/// Remap the palette to a sepia-tone version of itself by forcing
		/// every color's position in YIQ-space.
		/// </summary>
		/// <param name="amount">How saturated the sepia is.  0.0 = grayscale, 1.0 = orange, -1.0 = blue.</param>
		/// <returns>A copy of this image, as sepia tones.</returns>
		[Pure]
		public PureImage8 Sepia(double amount = 0.125)
		{
			Image8 copy = _image.Clone();
			copy.Sepia(amount);
			return copy;
		}

		/// <summary>
		/// Invert the red, green, and blue values in the palette (i.e., replace each R with 255-R,
		/// and so on for the other channels).  Alpha will be left unchanged.
		/// </summary>
		/// <returns>A copy of this image, inverted.</returns>
		[Pure]
		public PureImage8 InvertPalette()
		{
			Image8 copy = _image.Clone();
			copy.InvertPalette();
			return copy;
		}

		/// <summary>
		/// Invert one channel in the image (i.e., replace each R with 255-R).
		/// </summary>
		/// <returns>A copy of this image, inverted.</returns>
		[Pure]
		public PureImage8 InvertPalette(ColorChannel channel)
		{
			Image8 copy = _image.Clone();
			copy.InvertPalette(channel);
			return copy;
		}

		/// <summary>
		/// Adjust the hue, saturation, and lightness of the palette.  This converts each color
		/// in the palette to HSL, adds the given deltas, and then converts it back to RGB.
		/// </summary>
		/// <param name="deltaHue">The amount of change to the hue value, from -360.0 to +360.0
		/// degrees.</param>
		/// <param name="deltaSaturation">The amount of change to the saturation value, from -1.0
		/// (grayscale) to +1.0 (maximum saturation).</param>
		/// <param name="deltaLightness">The amount of change to the lightness value, from -1.0 (black)
		/// to +1.0 (white).</param>
		/// <returns>A copy of this image, with the given color adjustments applied.</returns>
		[Pure]
		public PureImage8 HueSaturationLightness(double deltaHue, double deltaSaturation, double deltaLightness)
		{
			Image8 copy = _image.Clone();
			copy.HueSaturationLightness(deltaHue, deltaSaturation, deltaLightness);
			return copy;
		}

		/// <summary>
		/// Adjust the hue, saturation, and brightness of the palettte.  This converts each color
		/// in the palette to HSB, adds the given deltas, and then converts it back to RGB.
		/// </summary>
		/// <param name="deltaHue">The amount of change to the hue value, from -360.0 to +360.0
		/// degrees.</param>
		/// <param name="deltaSaturation">The amount of change to the saturation value, from -1.0
		/// (grayscale) to +1.0 (maximum saturation).</param>
		/// <param name="deltaBrightness">The amount of change to the brightness value, from -1.0 (black)
		/// to +1.0 (white).</param>
		/// <returns>A copy of this image, with the given color adjustments applied.</returns>
		[Pure]
		public PureImage8 HueSaturationBrightness(double deltaHue, double deltaSaturation, double deltaBrightness)
		{
			Image8 copy = _image.Clone();
			copy.HueSaturationBrightness(deltaHue, deltaSaturation, deltaBrightness);
			return copy;
		}

		#endregion

		#region Channel extraction/combining

		/// <summary>
		/// Extract out the given RGBA channel from this image as a new 8-bit image,
		/// mapping each pixel value through its palette entry.
		/// </summary>
		/// <param name="channel">The channel to extract.</param>
		/// <returns>The extracted channel.</returns>
		/// <exception cref="ArgumentException">Thrown if the given channel is unknown.</exception>
		[Pure]
		public Image8 ExtractChannel(ColorChannel channel)
			=> _image.ExtractChannel(channel);

		/// <summary>
		/// Swap color channels in the palette with other color channels in the same palette.
		/// </summary>
		/// <param name="redChannel">The current channel to use for the new red channel.</param>
		/// <param name="greenChannel">The current channel to use for the new green channel.</param>
		/// <param name="blueChannel">The current channel to use for the new blue channel.</param>
		/// <param name="alphaChannel">The current channel to use for the new alpha channel.</param>
		/// <returns>A copy of this image, with the channels rearranged.</returns>
		[Pure]
		public PureImage8 SwapChannels(ColorChannel redChannel,
			ColorChannel greenChannel, ColorChannel blueChannel,
			ColorChannel alphaChannel = ColorChannel.Alpha)
		{
			Image8 copy = _image.Clone();
			copy.SwapChannels(redChannel, greenChannel, blueChannel, alphaChannel);
			return copy;
		}

		#endregion

		#region Componentwise operators

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been added together.
		/// </summary>
		/// <param name="a">The first image to add.</param>
		/// <param name="b">The second image to add.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been added together.</returns>
		[Pure]
		public static PureImage8 operator +(PureImage8 a, PureImage8 b)
			=> a._image + b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been subtracted.
		/// </summary>
		/// <param name="a">The first image.</param>
		/// <param name="b">The second image to subtract.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been subtracted.</returns>
		[Pure]
		public static PureImage8 operator -(PureImage8 a, PureImage8 b)
			=> a._image - b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been multiplied together.
		/// </summary>
		/// <param name="a">The first image to multiply.</param>
		/// <param name="b">The second image to multiply.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been multiplied.</returns>
		[Pure]
		public static PureImage8 operator *(PureImage8 a, PureImage8 b)
			=> a._image * b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all values have been scaled.</returns>
		[Pure]
		public static PureImage8 operator *(PureImage8 image, int scalar)
			=> image._image * scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all values have been scaled.</returns>
		[Pure]
		public static PureImage8 operator *(PureImage8 image, double scalar)
			=> image._image * scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all values have been scaled.</returns>
		[Pure]
		public static PureImage8 operator /(PureImage8 image, int scalar)
			=> image._image / scalar;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all values have been scaled.</returns>
		[Pure]
		public static PureImage8 operator /(PureImage8 image, double scalar)
			=> image._image / scalar;

		/// <summary>
		/// Create a new image of the same size as the given image, where each
		/// pixel value has been inverted, replacing each value V
		/// with 255 - V.
		/// </summary>
		/// <param name="image">The image to invert.</param>
		/// <returns>A new image where all values have been inverted.</returns>
		[Pure]
		public static PureImage8 operator ~(PureImage8 image)
			=> ~image._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been inverted, replacing each value V
		/// with (256 - V) % 256.
		/// </summary>
		/// <param name="image">The image to negate.</param>
		/// <returns>A new image where all components have been subtracted.</returns>
		[Pure]
		public static PureImage8 operator -(PureImage8 image)
			=> -image._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has been bitwise-or'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been bitwise-or'ed together.</returns>
		[Pure]
		public static PureImage8 operator |(PureImage8 a, PureImage8 b)
			=> a._image | b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has have been bitwise-and'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been bitwise-and'ed together.</returns>
		[Pure]
		public static PureImage8 operator &(PureImage8 a, PureImage8 b)
			=> a._image & b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has components have been bitwise-xor'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all values have been bitwise-xor'ed together.</returns>
		[Pure]
		public static PureImage8 operator ^(PureImage8 a, PureImage8 b)
			=> a._image ^ b._image;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has have been shifted left by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all values have been shifted left.</returns>
		[Pure]
		public static PureImage8 operator <<(PureImage8 image, int amount)
			=> image._image << amount;

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel value has have been shifted logically right by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all values have been shifted logically right.</returns>
		[Pure]
		public static PureImage8 operator >>(PureImage8 image, int amount)
			=> image._image >> amount;

		#endregion

		#region Filling

		/// <summary>
		/// Create a new image of the same dimensions that consists only of the given color.
		/// </summary>
		/// <returns>A new image of the same size that consists only of the given color.</returns>
		[Pure]
		public PureImage8 Fill(byte color)
			=> new PureImage8(Size, color);

		#endregion

		#region Rectangle Filling (solid)

		/// <summary>
		/// Fill the given rectangular area of the image with the given color.
		/// </summary>
		/// <param name="rect">The rectangular area of the image to fill.</param>
		/// <param name="drawColor">The color to fill with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		/// <returns>A copy of the image that has had the given rectangle filled with color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage8 FillRect(Rect rect, byte drawColor, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		/// <returns>A copy of the image that has had the given rectangle filled with color.</returns>
		[Pure]
		public PureImage8 FillRect(int x, int y, int width, int height, byte drawColor,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
			copy.FillRect(x, y, width, height, drawColor, blitFlags);
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
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		[Pure]
		public PureImage8 DrawRect(Rect rect, byte color, int thickness = 1, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		[Pure]
		public PureImage8 DrawRect(int x, int y, int width, int height, byte color, int thickness = 1,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		/// <returns>A copy of the image, with the line drawn on it.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage8 DrawLine(Vector2i p1, Vector2i p2, byte color, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is fully
		/// contained within the image.</param>
		/// <returns>A copy of the image, with the line drawn on it.</returns>
		[Pure]
		public PureImage8 DrawLine(int x1, int y1, int x2, int y2, byte color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public PureImage8 DrawThickLine(int x1, int y1, int x2, int y2, double thickness,
			byte color, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with a thick line drawn on it.</returns>
		[Pure]
		public PureImage8 DrawThickLine(Vector2i start, Vector2i end, double thickness,
			byte color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with a thick line drawn on it.</returns>
		[Pure]
		public PureImage8 DrawThickLine(Vector2d start, Vector2d end, double thickness,
			byte color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(IEnumerable<Vector2> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(IEnumerable<Vector2d> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(IEnumerable<Vector2i> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(ReadOnlySpan<Vector2> points, byte color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(ReadOnlySpan<Vector2d> points, byte color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		/// works in Copy and Transparent modes.</param>
		/// <returns>A copy of the image, with the given polygon drawn on top of it.</returns>
		[Pure]
		public PureImage8 FillPolygon(ReadOnlySpan<Vector2i> points, byte color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
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
		public PureImage8 DrawBezier(Vector2 p1, Vector2 c1, Vector2 c2, Vector2 p2,
			byte color, int steps = 0, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Image8 copy = _image.Clone();
			copy.DrawBezier(p1, c1, c2, p2, color, steps, blitFlags);
			return copy;
		}

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
		/// any nonzero value at all will be considered non-transparent.</param>
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
		/// any nonzero value at all will be considered non-transparent.</param>
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
		/// any nonzero value at all will be considered non-transparent.</param>
		/// <returns>True if the row is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsRectTransparent(Rect rect, byte cutoff = 0)
			=> _image.IsRectTransparent(rect, cutoff);

		/// <summary>
		/// Determine if the row of pixels starting at (x, y) and of the given width
		/// is entirely transparent (color &lt;= cutoff).  Pixels outside the image will be
		/// treated as transparent, and a zero-width row will as well.
		/// </summary>
		/// <param name="x">The starting X offset of the row.</param>
		/// <param name="y">The vertical offset of the row.</param>
		/// <param name="width">The row's width in pixels.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any nonzero value at all will be considered non-transparent.</param>
		/// <returns>True if the row is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsRowTransparent(int x, int y, int width, byte cutoff = 0)
			=> _image.IsRowTransparent(x, y, width, cutoff);

		/// <summary>
		/// Determine if the column of pixels starting at (x, y) and of the given height
		/// is entirely transparent (color &lt;= cutoff).  Pixels outside the image will be
		/// treated as transparent, and a zero-height column will as well.
		/// </summary>
		/// <param name="x">The horizontal offset of the column.</param>
		/// <param name="y">The starting Y offset of the column.</param>
		/// <param name="height">The column's height in pixels.</param>
		/// <param name="cutoff">The transparency cutoff; values greater than this will
		/// be considered non-transparent, while values of this or less will be
		/// considered transparent.  This defaults to 0, meaning that any pixel with
		/// any nonzero value at all will be considered non-transparent.</param>
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
			=> obj is PureImage8 other && Equals(other)
				|| obj is Image8 other2 && _image.Equals(other2);

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel
		/// identical.  This runs in O(n) worst-case time, but that's only hit if the
		/// two images have identical dimensions:  For images with unequal dimensions,
		/// this always runs in O(1) time.
		/// </summary>
		/// <param name="other">The other image to compare against.</param>
		/// <returns>True if the other image is an identical image to this image, false otherwise.</returns>
		[Pure]
		public bool Equals(PureImage8 other)
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
		public bool Equals(Image8 other)
			=> _image.Equals(other);

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>True if the images are identical, false otherwise.</returns>
		[Pure]
		public static bool operator ==(PureImage8 a, PureImage8 b)
			=> a.Equals(b);

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>False if the images are identical, true otherwise.</returns>
		[Pure]
		public static bool operator !=(PureImage8 a, PureImage8 b)
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
