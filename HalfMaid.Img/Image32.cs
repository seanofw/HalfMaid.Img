using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenTK.Mathematics;
using HalfMaid.Img.FileFormats;

namespace HalfMaid.Img
{
	/// <summary>
	/// An image abstraction:  A rectangular grid of colors, in 32-bit (with 8-bit alpha)
	/// mode, with methods to query and manipulate it.  Importantly, the image is always stored
	/// in an ordinary managed array of pixel data, so that it can be directly accessed and
	/// manipulated by the program.
	/// </summary>
	/// <remarks>
	/// Note that because the data is always stored in managed memory, all built-in image
	/// operations provided by this class are therefore performed on the CPU, not the GPU.  They
	/// are unrolled and optimized and written with unsafe and pointers so that they'll run as
	/// fast as possible on the CPU, but this class is written with *direct access* to the
	/// underlying data as a key design constraint:  Unlike many image/bitmap systems, you
	/// always have direct access to the underlying bytes of the image if you want it, which
	/// makes it ideal for many use cases other image systems don't support.
	/// 
	/// So this class is intended to be as capable as possible, and to offer many useful
	/// drawing/transformation/mutation operations directly, while still being completely
	/// managed code that runs on an ordinary CPU.
	/// </remarks>
	[DebuggerDisplay("Image {Width}x{Height}")]
	public class Image32 : IImage<Color32>, IEquatable<Image32>
	{
		#region Core properties and fields

		/// <summary>
		/// The actual data of this image, which is always guaranteed to be
		/// a one-dimensional contiguous array of 32-bit RGBA color tuples, in order of
		/// top-to-bottom, left-to-right, with no gaps or padding.  (i.e., the length
		/// of the data will always equal Width*Height.)  This is publicly exposed so
		/// that it can be read or written directly, or even pinned and manipulated
		/// using pointers.
		/// </summary>
		public Color32[] Data { get; private set; }

		/// <summary>
		/// The width of the image, in pixels.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		public int Height { get; private set; }

		#endregion

		#region Derived properties and static data

		/// <summary>
		/// The size of this image, represented as a 2D vector.
		/// </summary>
		[Pure]
		public Vector2i Size => new Vector2i(Width, Height);

		/// <summary>
		/// Access to the pure API for this image, which is (mostly) the
		/// same as the regular API, but all operations on the image result
		/// in a new image instance.
		/// </summary>
		[Pure]
		public PureImage32 Pure => new PureImage32(this);

		/// <summary>
		/// A static "empty" image that never has pixel data in it.
		/// </summary>
		public static Image32 Empty { get; } = new Image32(0, 0);

		#endregion

		#region Pixel-plotting

		/// <summary>
		/// Access the pixels using "easy" 2D array-brackets.  This is safe, easy, reliable,
		/// and slower than almost every other way of accessing the pixels, but it's very useful
		/// for simple cases.  Reading a pixel outside the image will return 'Transparent,'
		/// and writing a pixel outside the image is a no-op.
		/// </summary>
		/// <param name="x">The X coordinate of the pixel to read or write.</param>
		/// <param name="y">The Y coordinate of the pixel to read or write.</param>
		/// <returns>The color at that pixel.</returns>
		public Color32 this[int x, int y]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[Pure]
			get => x < 0 || x >= Width || y < 0 || y >= Height ? Color32.Transparent : Data[y * Width + x];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (x < 0 || x >= Width || y < 0 || y >= Height)
					return;
				Data[y * Width + x] = value;
			}
		}

		#endregion

		#region Construction and conversion

		/// <summary>
		/// Construct an image from the given image file.
		/// </summary>
		/// <param name="filename">The filename to load.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(string filename, ImageFormat imageFormat = default)
		{
			Image32? image = LoadFile(filename, imageFormat);
			if (image == null)
				throw new ArgumentException($"'{filename}' is not readable as a known image format.");

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

		/// <summary>
		/// Construct an image from the given image file data.
		/// </summary>
		/// <param name="data">The image file data to load.</param>
		/// <param name="filenameIfKnown">The filename of that image file data, if known.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
		{
			Image32? image = LoadFile(data, filenameIfKnown, imageFormat);
			if (image == null)
				throw new ArgumentException($"The given data is not readable as a known image format.");

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 'Transparent'.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(Vector2i size)
		{
			Width = size.X;
			Height = size.Y;
			Data = new Color32[size.X * size.Y];
		}

		/// <summary>
		/// Construct an image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="fillColor">The color for all of the new pixels in the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(Vector2i size, Color32 fillColor)
		{
			Width = size.X;
			Height = size.Y;
			Data = new Color32[size.X * size.Y];
			Fill(fillColor);
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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Image32(int width, int height, ReadOnlySpan<byte> srcData, ReadOnlySpan<Color32> palette)
		{
			int size = width * height;

			if (srcData.Length < size)
				throw new ArgumentException("Source data ({srcData.Length} bytes) is too small for image ({Width}x{Height} pixels).");

			Width = width;
			Height = height;
			Data = new Color32[size];

			unsafe
			{
				fixed (byte* srcBase = srcData)
				fixed (Color32* destBase = Data)
				{
					int count = size;
					byte* src = srcBase;
					Color32* dest = destBase;
					while (count >= 8)
					{
						dest[0] = palette[src[0]];
						dest[1] = palette[src[1]];
						dest[2] = palette[src[2]];
						dest[3] = palette[src[3]];
						dest[4] = palette[src[4]];
						dest[5] = palette[src[5]];
						dest[6] = palette[src[6]];
						dest[7] = palette[src[7]];
						dest += 8;
						src += 8;
						count -= 8;
					}
					if ((count & 4) != 0)
					{
						dest[0] = palette[src[0]];
						dest[1] = palette[src[1]];
						dest[2] = palette[src[2]];
						dest[3] = palette[src[3]];
						dest += 4;
						src += 4;
					}
					if ((count & 2) != 0)
					{
						dest[0] = palette[src[0]];
						dest[1] = palette[src[1]];
						dest += 2;
						src += 2;
					}
					if ((count & 1) != 0)
						dest[0] = palette[src[0]];
				}
			}
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 'Transparent'.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(int width, int height)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the provided fill color.
		/// </summary>
		/// <param name="width">The width of the new image, in pixels.</param>
		/// <param name="height">The height of the new image, in pixels.</param>
		/// <param name="fillColor">The color for all of the new pixels in the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32(int width, int height, Color32 fillColor)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];
			Fill(fillColor);
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
		public Image32(int width, int height, Color32[] data)
		{
			if (data.Length < width * height)
				throw new ArgumentException("Cannot construct a new image using an undersized color array;"
					+ $" array is {data.Length} values, but width {width} x height {height} requires {width * height} values.");
			Data = data;
			Width = width;
			Height = height;
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
		public Image32(int width, int height, ReadOnlySpan<byte> rawData)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];

			Overwrite(rawData);
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
		public unsafe Image32(int width, int height, byte* rawData, int rawDataLength)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];

			Overwrite(rawData, rawDataLength);
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
		public Image32(int width, int height, ReadOnlySpan<Color32> data)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];

			Overwrite(data);
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
		public unsafe Image32(int width, int height, Color32* data, int dataLength)
		{
			Width = width;
			Height = height;
			Data = new Color32[width * height];

			Overwrite(data, dataLength);
		}

		/// <summary>
		/// Extract out a copy of the raw data in this image, as bytes.  This is
		/// slower than pinning the raw data and then casting it to a byte pointer, but
		/// it is considerably safer.
		/// </summary>
		[Pure]
		public byte[] GetBytes()
		{
			unsafe
			{
				byte[] dest = new byte[Width * Height * 4];
				fixed (byte* destBase = dest)
				fixed (Color32* srcBase = Data)
				{
					Buffer.MemoryCopy(srcBase, destBase, Width * Height * 4, Width * Height * 4);
				}
				return dest;
			}
		}

		/// <summary>
		/// Replace this entire image instance with a copy of the given other image's data and size.
		/// </summary>
		/// <param name="other">The other image to replace.</param>
		/// <remarks>
		/// This is possibly the most impure thing that can be done to an image, as it destroys
		/// everything from the source image in place. Use with caution. 😬
		/// </remarks>
		public void Replace(Image32 other)
		{
			Width = other.Width;
			Height = other.Height;
			Data = new Color32[Width * Height];
			Overwrite(other.Data);
		}

		/// <summary>
		/// Overwrite all of the pixels in this image with the given raw pixel data,
		/// represented as a sequence of RGBA tuples, which must be at least as large
		/// as this image.
		/// </summary>
		/// <param name="rawData">The raw data to overwrite this image with.</param>
		/// <exception cref="ArgumentException">Thrown if the provided source data isn't large enough.</exception>
		public void Overwrite(ReadOnlySpan<byte> rawData)
		{
			unsafe
			{
				fixed (byte* rawDataBase = rawData)
				{
					Overwrite(rawDataBase, rawData.Length);
				}
			}
		}

		/// <summary>
		/// Overwrite all of the pixels in this image with the given pixel data,
		/// which must be at least as large as this image.
		/// </summary>
		/// <param name="data">The data to overwrite this image with.</param>
		/// <exception cref="ArgumentException">Thrown if the provided source data isn't large enough.</exception>
		public void Overwrite(ReadOnlySpan<Color32> data)
		{
			if (data.Length < Data.Length)
				throw new ArgumentException($"Not enough data for RGBA image of size {Width}x{Height} (only {data.Length} pixels provided).");

			unsafe
			{
				fixed (Color32* dataBase = data)
				{
					Overwrite(dataBase, data.Length);
				}
			}
		}

		/// <summary>
		/// Overwrite all of the pixels in this image with the given pixel data,
		/// which must be at least as large as this image.
		/// </summary>
		/// <param name="data">The data to overwrite this image with.</param>
		/// <param name="dataLength">The count of Color structs in the provided data
		/// array.</param>
		/// <exception cref="ArgumentException">Thrown if the provided source data isn't large enough.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Overwrite(Color32* data, int dataLength)
			=> Overwrite((byte*)data, dataLength * 4);

		/// <summary>
		/// Overwrite all of the pixels in this image with the given raw pixel data,
		/// represented as a sequence of RGBA tuples, which must be at least as large
		/// as this image.
		/// </summary>
		/// <param name="rawData">The raw data to overwrite this image with.</param>
		/// <param name="rawDataLength">The number of bytes in the raw data.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public unsafe void Overwrite(byte* rawData, int rawDataLength)
		{
			if (rawDataLength < Width * Height * 4)
				throw new ArgumentException($"Not enough data in source array for an image of size {Width}x{Height}.");
			if (rawDataLength == 0)
				return;
			if (rawData == null)
				throw new ArgumentNullException(nameof(rawData));

			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* dest = destBase;
					Color32* src = (Color32*)rawData;
					int count = Width * Height;

					while (count >= 8)
					{
						dest[0] = src[0];
						dest[1] = src[1];
						dest[2] = src[2];
						dest[3] = src[3];
						dest[4] = src[4];
						dest[5] = src[5];
						dest[6] = src[6];
						dest[7] = src[7];
						dest += 8;
						src += 8;
						count -= 8;
					}
					if ((count & 4) != 0)
					{
						dest[0] = src[0];
						dest[1] = src[1];
						dest[2] = src[2];
						dest[3] = src[3];
						dest += 4;
						src += 4;
					}
					if ((count & 2) != 0)
					{
						dest[0] = src[0];
						dest[1] = src[1];
						dest += 2;
						src += 2;
					}
					if ((count & 1) != 0)
						dest[0] = src[0];
				}
			}
		}

		/// <summary>
		/// Make a perfect duplicate of this image and return it.
		/// </summary>
		/// <returns>The newly-cloned image.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public Image32 Clone()
			=> new Image32(Width, Height, Data.AsSpan());

		/// <summary>
		/// Make a perfect duplicate of this image and return it.
		/// </summary>
		/// <returns>The newly-cloned image.</returns>
		[Pure]
		IImage IImage.Clone()
			=> new Image32(Width, Height, Data.AsSpan());

		#endregion

		#region Image loading/saving

		/// <summary>
		/// The registered image file-format loaders.  This is considered immutable; if you need
		/// to modify it, make a copy, and use Interlocked.Exchange() to swap in your copy.
		/// </summary>
		private static IReadOnlyDictionary<ImageFormat, IImageLoader> _loaders
			= new Dictionary<ImageFormat, IImageLoader>(new Dictionary<ImageFormat, IImageLoader>
			{
				{ ImageFormat.Bmp, new FileFormats.Bmp.BmpLoader() },
				{ ImageFormat.Jpeg, new FileFormats.Jpeg.JpegLoader() },
				{ ImageFormat.Png, new FileFormats.Png.PngLoader() },
				{ ImageFormat.Gif, new FileFormats.Gif.GifLoader() },
				{ ImageFormat.Targa, new FileFormats.Targa.TargaLoader() },
			});

		/// <summary>
		/// The registered image file-format savers.  This is considered immutable; if you need
		/// to modify it, make a copy, and use Interlocked.Exchange() to swap in your copy.
		/// </summary>
		private static IReadOnlyDictionary<ImageFormat, IImageSaver> _savers
			= new Dictionary<ImageFormat, IImageSaver>(new Dictionary<ImageFormat, IImageSaver>
			{
				{ ImageFormat.Bmp, new FileFormats.Bmp.BmpSaver() },
				{ ImageFormat.Jpeg, new FileFormats.Jpeg.JpegSaver() },
				{ ImageFormat.Png, new FileFormats.Png.PngSaver() },
				{ ImageFormat.Gif, new FileFormats.Gif.GifSaver() },
				{ ImageFormat.Targa, new FileFormats.Targa.TargaSaver() },
			});

		/// <summary>
		/// Construct an image from the embedded resource with the given name.  The embedded
		/// resource must be of a file format that this image library is capable of
		/// decoding, like PNG or JPEG.
		/// </summary>
		/// <param name="assembly">The assembly containing the embedded resource.</param>
		/// <param name="name">The name of the embedded image resource to load.  This may use
		/// slashes in the pathname to separate components.</param>
		/// <returns>The newly-loaded image, or null if no such image exists or is not
		/// a valid image file.</returns>
		[Pure]
		public static Image32? FromEmbeddedResource(Assembly assembly, string name)
		{
			byte[] bytes;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (Stream? stream = assembly.GetManifestResourceStream(name.Replace('/', '.').Replace('\\', '.')))
				{
					if (stream == null)
						return null;
					stream.CopyTo(memoryStream);
				}
				bytes = memoryStream.ToArray();
			}

			return LoadFile(bytes, name);
		}

		/// <summary>
		/// Load the given file as a new image, which must be of a supported image
		/// format like PNG or JPEG.
		/// </summary>
		/// <param name="filename">The filename of the image to load.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or null if it can't be loaded.</returns>
		[Pure]
		public static Image32? LoadFile(string filename, ImageFormat imageFormat = default)
		{
			byte[] bytes;
			try
			{
				bytes = File.ReadAllBytes(filename);
			}
			catch
			{
				return null;
			}

			return LoadFile(bytes, filename, imageFormat);
		}

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
		/// <returns>The new image, or null if it can't be decoded.</returns>
		[Pure]
		public static Image32? LoadFile(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
		{
			// If we weren't told what format the data is, then attempt to guess.
			if (imageFormat == default)
			{
				(ImageFormat guessedFormat, ImageCertainty imageCertainty) = GuessFileFormat(data, filenameIfKnown);
				if (imageCertainty < ImageCertainty.Maybe)
					return null;
				imageFormat = guessedFormat;
			}

			// Look up the loader that knows how to decode this data.
			if (!_loaders.TryGetValue(imageFormat, out IImageLoader? loader))
				return null;

			// Attempt to load the image in something passably close to its native color format.
			ImageLoadResult? loadResult = loader.LoadImage(data);
			if (loadResult == null)
				return null;

			// Turn the result into an Image.
			if (loadResult.Image is Image32 image32)
				return image32;
			else if (loadResult.Image is Image8 image8)
				return image8.ToImage32();
			else
				return null;
		}

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
		{
			byte[]? data = ReadFirstFileChunk(filename, 16384);
			if (data == null)
				return null;

			return LoadFileMetadata(data, filename, imageFormat);
		}

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
		{
			// If we weren't told what format the data is, then attempt to guess.
			if (imageFormat == default)
			{
				(ImageFormat guessedFormat, ImageCertainty imageCertainty) = GuessFileFormat(data, filenameIfKnown);
				if (imageCertainty < ImageCertainty.Maybe)
					return null;
				imageFormat = guessedFormat;
			}

			// Look up the loader that knows how to decode this data.
			if (!_loaders.TryGetValue(imageFormat, out IImageLoader? loader))
				return null;

			return loader.GetMetadata(data);
		}

		/// <summary>
		/// Read a small chunk of the given file, quickly.
		/// </summary>
		/// <param name="filename">The name of the file to read.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The first 'count' bytes of the file, or fewer than that if
		/// the file is smaller than 'count', or null if the file can't be read.</returns>
		[Pure]
		private static byte[]? ReadFirstFileChunk(string filename, int count)
		{
			try
			{
				using FileStream file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

				byte[] buffer = new byte[count];
				int numRead = file.Read(buffer, 0, count);

				return numRead >= count ? buffer
					: buffer.AsSpan().Slice(0, numRead).ToArray();
			}
			catch
			{
				return null;
			}
		}

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
		{
			ImageFormat dataFormat = ImageFormat.None;
			ImageCertainty dataCertainty = ImageCertainty.No;

			ImageFormat nameFormat = ImageFormat.None;
			ImageCertainty nameCertainty = ImageCertainty.No;

			// Test against all of the image loaders.
			foreach (IImageLoader loader in _loaders.Values)
			{
				ImageCertainty dataMatch = loader.DoesDataMatch(bytes);
				ImageCertainty nameMatch = !string.IsNullOrEmpty(filenameIfKnown)
					? loader.DoesNameMatch(filenameIfKnown!)
					: ImageCertainty.No;

				if (dataMatch > dataCertainty)
				{
					// If we get a "Yes" on the data, it wins, no matter what the
					// name is, and we can skip testing anything else.
					if (dataMatch == ImageCertainty.Yes)
						return (loader.Format, ImageCertainty.Yes);

					dataCertainty = dataMatch;
					dataFormat = loader.Format;
				}

				if (nameMatch > nameCertainty)
				{
					nameCertainty = nameMatch;
					nameFormat = loader.Format;
				}
			}

			// We've tested them all, and none were a solid "yes" on the data, so now
			// figure out which one is the closest match.

			// If the data is a solid match, it doesn't matter what the name
			// claims the data is.
			if (dataCertainty >= ImageCertainty.Probably)
				return (dataFormat, dataCertainty);

			// The data is a "maybe" at best, so let's see if there's a solid match
			// on the same name.
			if (dataCertainty == ImageCertainty.Maybe
				&& nameCertainty >= ImageCertainty.Probably
				&& dataFormat == nameFormat)
				return (dataFormat, nameCertainty);

			// Whichever is stronger, the data or the name, has to win at this
			// point, since they're effectively both "maybe" or "no."
			if (dataCertainty == ImageCertainty.Maybe)
				return (dataFormat, dataCertainty);
			if (nameCertainty == ImageCertainty.Maybe)
				return (nameFormat, nameCertainty);

			// Didn't find it.
			return (default, ImageCertainty.No);
		}

		/// <summary>
		/// Read the first small chunk of the given file and attempt to guess what
		/// image file format it is, based on the file's name and its data.
		/// </summary>
		/// <param name="filename">The name of the file to read.</param>
		/// <returns>The image format for that file, and a rough certainty level
		/// that the file really is that format, from "Yes" to "Probably" to "Maybe" to "No."</returns>
		[Pure]
		public static (ImageFormat Format, ImageCertainty Certainty) GuessFileFormat(string filename)
		{
			byte[]? chunk = ReadFirstFileChunk(filename, 4096);
			if (chunk == null)
				return (default, ImageCertainty.No);

			return GuessFileFormat(chunk, filename);
		}

		/// <summary>
		/// Save the image to disk as the given file format.
		/// </summary>
		/// <param name="filename">The filename to write.</param>
		/// <param name="format">The image format to produce.</param>
		/// <param name="options">Options specific to this file format, if appropriate.</param>
		[Pure]
		public void SaveFile(string filename, ImageFormat format, IFileSaveOptions? options = null)
		{
			byte[] bytes = SaveFile(format, options);
			File.WriteAllBytes(filename, bytes);
		}

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
		{
			if (!_savers.TryGetValue(format, out IImageSaver? saver))
				throw new ArgumentException($"Unknown image format '{format}'.");

			return saver.SaveImage(this, null, options);
		}

		/// <summary>
		/// Register the given class(es) as being able to load or save the given image file
		/// format.  This is an expensive operation, but it generally is not performed very
		/// often, typically only at application startup.
		/// </summary>
		/// <param name="format">The new file format to register.  Any existing registration
		/// for this file format will be replaced.</param>
		/// <param name="loader">The loader to register, or null to unregister a loader for
		/// this format.</param>
		/// <param name="saver">The saver to register, or null to unregister a saver for
		/// this format.</param>
		public static void RegisterFileFormat(ImageFormat format,
			IImageLoader? loader = null, IImageSaver? saver = null)
			=> RegisterFileFormats(new[] { (format, loader, saver) });

		/// <summary>
		/// Register or unregister multiple file formats at once.  This is an expensive
		/// operation, but it generally is not performed very often, typically only at
		/// application startup.  This bulk form is much faster than performing repeated
		/// calls to RegisterFileFormat().
		/// </summary>
		/// <param name="formats">The formats to update.  Each existing loader and saver
		/// for that format will be replaced with the new, given loader and saver (with null
		/// meaning 'none').</param>
		public static void RegisterFileFormats(IEnumerable<
			(ImageFormat Format, IImageLoader? Loader, IImageSaver? Saver)> formats)
		{
			Dictionary<ImageFormat, IImageLoader> newLoaders = _loaders.ToDictionary(p => p.Key, p => p.Value);
			Dictionary<ImageFormat, IImageSaver> newSavers = _savers.ToDictionary(p => p.Key, p => p.Value);

			foreach ((ImageFormat format, IImageLoader? loader, IImageSaver? saver) in formats)
			{
				if (loader != null)
					newLoaders[format] = loader;
				else
					newLoaders.Remove(format);

				if (saver != null)
					newSavers[format] = saver;
				else
					newSavers.Remove(format);
			}

			Interlocked.Exchange(ref _loaders, newLoaders);
			Interlocked.Exchange(ref _savers, newSavers);
		}

		/// <summary>
		/// Get the list of all known image formats that can be loaded.
		/// </summary>
		/// <returns>An array containing all known loader image formats.  This array
		/// is dynamically created for the caller, and is not used internally.</returns>
		[Pure]
		public static ImageFormat[] GetRegisteredLoaderFormats()
			=> _loaders.Keys.ToArray();

		/// <summary>
		/// Get the list of all known image formats that can be saved.
		/// </summary>
		/// <returns>An array containing all known saver image formats.  This array
		/// is dynamically created for the caller, and is not used internally.</returns>
		[Pure]
		public static ImageFormat[] GetRegisteredSaverFormats()
			=> _savers.Keys.ToArray();

		/// <summary>
		/// Get the currently-registered image loader for the given image format.
		/// </summary>
		/// <param name="format">The format to retrieve a loader for.</param>
		/// <returns>The loader for that format, or null if there is no such loader.</returns>
		[Pure]
		public static IImageLoader? GetLoader(ImageFormat format)
			=> _loaders.TryGetValue(format, out IImageLoader? loader) ? loader : null;

		/// <summary>
		/// Get the currently-registered image saver for the given image format.
		/// </summary>
		/// <param name="format">The format to retrieve a saver for.</param>
		/// <returns>The saver for that format, or null if there is no such saver.</returns>
		[Pure]
		public static IImageSaver? GetSaver(ImageFormat format)
			=> _savers.TryGetValue(format, out IImageSaver? saver) ? saver : null;

		#endregion

		#region Resizing

		/// <summary>
		/// Perform resizing to fit the given container size using the chosen fitting
		/// mode, in-place.  Fast, but can be really, really inaccurate.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		public void ResizeToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit)
			=> Resize(Fit(Size, containerSize, fitMode));

		/// <summary>
		/// Resize the image using nearest-neighbor sampling, in-place.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="imageSize">The new size of the image.</param>
		public void Resize(Vector2i imageSize)
			=> Resize(imageSize.X, imageSize.Y);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling, in-place.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="newWidth">The new width of the image.</param>
		/// <param name="newHeight">The new height of the image.</param>
		public void Resize(int newWidth, int newHeight)
		{
			PureImage32 result = Pure.Resize(newWidth, newHeight);
			Data = result.Data;
			Width = result.Width;
			Height = result.Height;
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
			=> (Vector2i)(Fit((Vector2d)imageSize, (Vector2d)containerSize, fitMode)
				+ new Vector2d(0.5, 0.5));

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
		{
			switch (fitMode)
			{
				default:
					return imageSize;

				case FitMode.Stretch:
					return containerSize;

				case FitMode.Fit:
					double imageWidth = Math.Max(imageSize.X, 0);
					double imageHeight = Math.Max(imageSize.Y, 0);
					double containerWidth = Math.Max(containerSize.X, 0);
					double containerHeight = Math.Max(containerSize.Y, 0);
					double ratioImage = imageHeight != 0 ? imageWidth / imageHeight : 0;
					double ratioContainer = containerHeight != 0 ? containerWidth / containerHeight : 0;
					if (ratioImage <= 0)
						return new Vector2d(0, 0);
					return ratioContainer > ratioImage
						? new Vector2d(imageWidth * containerHeight / imageHeight, containerHeight)
						: new Vector2d(containerWidth, imageHeight * containerWidth / imageWidth);

				case FitMode.Fill:
					imageWidth = Math.Max(imageSize.X, 0);
					imageHeight = Math.Max(imageSize.Y, 0);
					containerWidth = Math.Max(containerSize.X, 0);
					containerHeight = Math.Max(containerSize.Y, 0);
					ratioImage = imageHeight != 0 ? imageWidth / imageHeight : 0;
					ratioContainer = containerHeight != 0 ? containerWidth / containerHeight : 0;
					if (ratioImage <= 0)
						return new Vector2d(0, 0);
					return ratioContainer < ratioImage
						? new Vector2d(imageWidth * containerHeight / imageHeight, containerHeight)
						: new Vector2d(containerWidth, imageHeight * containerWidth / imageWidth);
			}
		}

		/// <summary>
		/// Perform resampling to fit the given container size using the chosen fitting
		/// mode and resampling mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		public void ResampleToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit, ResampleMode mode = ResampleMode.BSpline)
			=> Resample(Fit(Size, containerSize, fitMode), mode);

		/// <summary>
		/// Perform resampling using the chosen mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="width">The new image width.  If omitted/null, this will be determined
		/// automatically from the given height.</param>
		/// <param name="height">The new image height.  If omitted/null, this will be determined
		/// automatically from the given width.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		public void Resample(int? width = null, int? height = null, ResampleMode mode = ResampleMode.BSpline)
			=> Resample(CalculateResampleSize(width, height), mode);

		/// <summary>
		/// Calculate the new dimensions of the resampled image, given that one or both
		/// of the new dimensions may have been omitted.
		/// </summary>
		/// <param name="userWidth">The new desired width, which may be omitted.</param>
		/// <param name="userHeight">The new desired height, which may be omitted.</param>
		/// <returns>The fully-calculated dimensions of the new image.</returns>
		[Pure]
		internal Vector2i CalculateResampleSize(int? userWidth, int? userHeight)
		{
			int width, height;
			if (userWidth.HasValue)
			{
				width = userWidth.Value;
				if (userHeight.HasValue)
					height = userHeight.Value;
				else
				{
					double ooAspectRatio = (double)Height / Width;
					height = (int)(width * ooAspectRatio + 0.5);
				}
			}
			else if (userHeight.HasValue)
			{
				height = userHeight.Value;
				double aspectRatio = (double)Width / Height;
				width = (int)(height * aspectRatio + 0.5);
			}
			else
			{
				width = Width;
				height = Height;
			}

			return new Vector2i(width, height);
		}

		/// <summary>
		/// Perform resampling using the chosen mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="newSize">The new image size.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		public void Resample(Vector2i newSize, ResampleMode mode = ResampleMode.BSpline)
		{
			if (newSize == Size)
				return;

			if (newSize.X <= 0 || newSize.Y <= 0)
				throw new ArgumentException("Size of new image must be greater than zero.");

			Image32 dest = new Image32(newSize);

			ImageResampler.ResampleTo(this, dest, mode);

			Width = dest.Width;
			Height = dest.Height;
			Data = dest.Data;
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
		public static bool ClipBlit(Vector2i destImageSize, Vector2i srcImageSize, ref int srcX, ref int srcY,
			ref int destX, ref int destY, ref int width, ref int height)
		{
			if (width <= 0 || height <= 0
				|| srcX >= srcImageSize.X || srcY >= srcImageSize.Y
				|| destX >= destImageSize.X || destY >= destImageSize.Y)
				return false;

			if (srcX < 0)
			{
				width += srcX;
				destX -= srcX;
				srcX = 0;
			}
			if (srcY < 0)
			{
				height += srcY;
				destY -= srcY;
				srcY = 0;
			}
			if (destX < 0)
			{
				width += destX;
				srcX -= destX;
				destX = 0;
			}
			if (destY < 0)
			{
				height += destY;
				srcY -= destY;
				destY = 0;
			}

			if (width > srcImageSize.X - srcX)
				width = srcImageSize.X - srcX;
			if (height > srcImageSize.Y - srcY)
				height = srcImageSize.Y - srcY;
			if (width > destImageSize.X - destX)
				width = destImageSize.X - destX;
			if (height > destImageSize.Y - destY)
				height = destImageSize.Y - destY;

			return width > 0 && height > 0;
		}

		/// <summary>
		/// Clip the given drawing rectangle to be within the image.
		/// </summary>
		/// <param name="imageSize">The size of the destination image we're drawing on.</param>
		/// <param name="x">The left coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="y">The top coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="width">The width of the rectangle, which will be updated to be within the image.</param>
		/// <param name="height">The height of the rectangle, which will be updated to be within the image.</param>
		/// <returns>True if the drawing may proceed, or false if the rectangle is invalid/unusable.</returns>
		public static bool ClipRect(Vector2i imageSize, ref int x, ref int y, ref int width, ref int height)
		{
			if (imageSize.X <= 0 || height <= 0 || x >= imageSize.X || y >= imageSize.Y)
				return false;

			if (x < 0)
			{
				width += x;
				x = 0;
			}
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (width > imageSize.X - x)
				width = imageSize.X - x;
			if (height > imageSize.Y - y)
				height = imageSize.Y - y;

			return width > 0 && height > 0;
		}

		#endregion

		#region Blitting

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		[Pure]
		public Image32 Extract(int x, int y, int width, int height)
		{
			Image32 image = new Image32(width, height);
			image.Blit(this, x, y, 0, 0, width, height);
			return image;
		}

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		public void Crop(int x, int y, int width, int height)
		{
			Image32 image = new Image32(width, height);
			image.Blit(this, x, y, 0, 0, width, height);

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

		/// <summary>
		/// Pad the given image with extra pixels on some or all sides, in-place.
		/// Note that the padding for any edge may be negative, which will
		/// cause an edge to be cropped instead of padded.
		/// </summary>
		/// <param name="left">The number of pixels to add to the left edge.</param>
		/// <param name="top">The number of pixels to add to the top edge.</param>
		/// <param name="right">The number of pixels to add to the right edge.</param>
		/// <param name="bottom">The number of pixels to add to the bottom edge.</param>
		/// <param name="fillColor">The padding color (transparent by default).</param>
		public void Pad(int left = 0, int top = 0, int right = 0, int bottom = 0, Color32 fillColor = default)
		{
			PureImage32 image = Pure.Pad(left, top, right, bottom, fillColor);
			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle, but copy all colors as black,
		/// of the given alpha (factoring in any existing alpha).  This will by default clip
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
		/// <param name="alpha">The alpha to apply to the resulting pixels; this can be used to make
		/// the resulting shadow less opaque than the original pixels are.</param>
		public void ShadowBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, int alpha)
		{
			if (!ClipBlit(Size, srcImage.Size, ref srcX, ref srcY, ref destX, ref destY, ref width, ref height))
				return;

			FastUnsafeShadowBlit(srcImage, srcX, srcY, destX, destY, width, height, alpha);
		}

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle, in-place.  This will by default clip
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
		/// <param name="blitFlags">Flags controlling how the copy is performed.</param>
		public void Blit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height,
			BlitFlags blitFlags = default)
		{
			if ((blitFlags & BlitFlags.FastUnsafe) == 0)
			{
				if (!ClipBlit(Size, srcImage.Size, ref srcX, ref srcY, ref destX, ref destY, ref width, ref height))
					return;
			}

			switch (blitFlags & (BlitFlags.ModeMask | BlitFlags.FlipHorz))
			{
				case BlitFlags.Copy:
					FastUnsafeBlit(srcImage, srcX, srcY, destX, destY, width, height,
						(blitFlags & BlitFlags.FlipVert) != 0);
					break;
				case BlitFlags.Transparent:
					FastUnsafeTransparentBlit(srcImage, srcX, srcY, destX, destY, width, height,
						(blitFlags & BlitFlags.FlipVert) != 0);
					break;
				case BlitFlags.Alpha:
					FastUnsafeAlphaBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.PMAlpha:
					FastUnsafePMAlphaBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.Add:
					FastUnsafeAddBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.Multiply:
					FastUnsafeMultiplyBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;

				case BlitFlags.Copy | BlitFlags.FlipHorz:
					FastUnsafeBlitFlipHorz(srcImage, srcX, srcY, destX, destY, width, height,
						(blitFlags & BlitFlags.FlipVert) != 0);
					break;
				case BlitFlags.Transparent | BlitFlags.FlipHorz:
					FastUnsafeTransparentBlitFlipHorz(srcImage, srcX, srcY, destX, destY, width, height,
						(blitFlags & BlitFlags.FlipVert) != 0);
					break;

				default:
					throw new InvalidOperationException($"Unsupported blit mode: {blitFlags & BlitFlags.ModeMask}");
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  This doesn't apply alpha;
		/// it just slams new data over top of old.  Make sure all values are within range; this is as
		/// unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
			[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					if (flipVert)
					{
						src += srcImage.Width * (height - 1);
						srcSkip = -width - srcImage.Width;
					}

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						while (count >= 8)
						{
							ulong u0 = ((ulong*)src)[0];
							ulong u1 = ((ulong*)src)[1];
							ulong u2 = ((ulong*)src)[2];
							ulong u3 = ((ulong*)src)[3];
							((ulong*)dest)[0] = u0;
							((ulong*)dest)[1] = u1;
							((ulong*)dest)[2] = u2;
							((ulong*)dest)[3] = u3;
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest[0] = src[0];
								dest[1] = src[1];
								dest[2] = src[2];
								dest[3] = src[3];
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								dest[0] = src[0];
								dest[1] = src[1];
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								*dest++ = *src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  This skips
		/// any Color32.Transparent pixels.  Make sure all values are within range; this is as
		/// unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeTransparentBlit(Image32 srcImage,
			int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					if (flipVert)
					{
						src += srcImage.Width * (height - 1);
						srcSkip = -width - srcImage.Width;
					}

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						Color32 c;
						while (count >= 8)
						{
							if ((c = src[0]).A != 0) dest[0] = c;
							if ((c = src[1]).A != 0) dest[1] = c;
							if ((c = src[2]).A != 0) dest[2] = c;
							if ((c = src[3]).A != 0) dest[3] = c;
							if ((c = src[4]).A != 0) dest[4] = c;
							if ((c = src[5]).A != 0) dest[5] = c;
							if ((c = src[6]).A != 0) dest[6] = c;
							if ((c = src[7]).A != 0) dest[7] = c;
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								if ((c = src[0]).A != 0) dest[0] = c;
								if ((c = src[1]).A != 0) dest[1] = c;
								if ((c = src[2]).A != 0) dest[2] = c;
								if ((c = src[3]).A != 0) dest[3] = c;
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								if ((c = src[0]).A != 0) dest[0] = c;
								if ((c = src[1]).A != 0) dest[1] = c;
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								if ((c = src[0]).A != 0) dest[0] = c;
								src++;
								dest++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					if (!flipVert)
					{
						Debug.Assert(dest >= destBase && dest - destX <= destLimit);
						Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
					}
					else
					{
						Debug.Assert(dest >= destBase && dest - destX <= destLimit);
						Debug.Assert(src + srcImage.Width >= srcBase && src + srcImage.Width <= srcLimit);
					}
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, applying
		/// alpha from the source, and treating the dest as opaque.  Make sure all values are
		/// within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeAlphaBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							if (s.A == 255)
							{
								// No need to apply alpha math; this pixel is opaque.
								*dest++ = s;
							}
							else if (s.A != 0)
							{
								// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
								// we do the math for real, but exclusively using integers (for performance).
								uint a = s.A, ia = (uint)(255 - s.A);
								uint r = s.R * a + d.R * ia + 127;
								uint g = s.G * a + d.G * ia + 127;
								uint b = s.B * a + d.B * ia + 127;
								r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
								g = (g + 1 + (g >> 8)) >> 8;
								b = (b + 1 + (b >> 8)) >> 8;
								*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
							}
							else dest++;
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, applying
		/// alpha from the (premultiplied!) source, and treating the dest as opaque.  Make sure all values are
		/// within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafePMAlphaBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							if (s.A == 255)
							{
								// No need to apply alpha math; this pixel is opaque.
								*dest++ = s;
							}
							else if (s.A != 0)
							{
								// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
								// we do the math for real, but exclusively using integers (for performance).
								// This uses premultiplied source colors for even more performance.
								uint ia = (uint)(255 - s.A);
								uint r = d.R * ia + 127;
								uint g = d.G * ia + 127;
								uint b = d.B * ia + 127;
								r = s.R + ((r + 1 + (r >> 8)) >> 8);
								g = s.G + ((g + 1 + (g >> 8)) >> 8);
								b = s.B + ((b + 1 + (b >> 8)) >> 8);
								*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
							}
							else dest++;
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, applying
		/// alpha from the source, treating the dest as opaque, and treating all source
		/// colors as black.  Make sure all values are within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeShadowBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY,
			int width, int height, int alpha)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							uint a = s.A * (uint)alpha;
							a = (a + 1 + (a >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
							if (a == 255)
							{
								// No need to apply alpha math; this pixel is opaque.
								*dest++ = Color32.Black;
							}
							else if (a != 0)
							{
								// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
								// we do the math for real, but exclusively using integers (for performance).
								uint ia = (255 - a);
								uint r = d.R * ia + 127;
								uint g = d.G * ia + 127;
								uint b = d.B * ia + 127;
								uint da = d.A;
								r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
								g = (g + 1 + (g >> 8)) >> 8;
								b = (b + 1 + (b >> 8)) >> 8;
								da = Math.Min(da + a, 255);
								*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)da);
							}
							else dest++;
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  This doesn't apply alpha;
		/// it just slams new data over top of old.  Make sure all values are within range; this is as
		/// unsafe as it sounds.  This flips the pixels horizontally while copying.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeBlitFlipHorz(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX + width;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width + width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					if (flipVert)
					{
						src += srcImage.Width * (height - 1);
						srcSkip = -width - srcImage.Width;
					}

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						while (count >= 8)
						{
							dest -= 8;
							dest[7] = src[0];
							dest[6] = src[1];
							dest[5] = src[2];
							dest[4] = src[3];
							dest[3] = src[4];
							dest[2] = src[5];
							dest[1] = src[6];
							dest[0] = src[7];
							count -= 8;
							src += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest -= 4;
								dest[3] = src[0];
								dest[2] = src[1];
								dest[1] = src[2];
								dest[0] = src[3];
								src += 4;
							}
							if ((count & 2) != 0)
							{
								dest -= 2;
								dest[1] = src[0];
								dest[0] = src[1];
								src += 2;
							}
							if ((count & 1) != 0)
							{
								*dest-- = *src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					if (!flipVert)
					{
						Debug.Assert(dest >= destBase && dest - destX <= destLimit);
						Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
					}
					else
					{
						Debug.Assert(dest >= destBase && dest - destX <= destLimit);
						Debug.Assert(src + srcImage.Width >= srcBase && src + srcImage.Width <= srcLimit);
					}
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  This skips
		/// any Color32.Transparent pixels.  Make sure all values are within range; this is as
		/// unsafe as it sounds.  This flips the pixels horizontally while copying.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeTransparentBlitFlipHorz(Image32 srcImage,
			int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX + width;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width + width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					if (flipVert)
					{
						src += srcImage.Width * (height - 1);
						srcSkip = -width - srcImage.Width;
					}

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						Color32 c;
						while (count >= 8)
						{
							dest -= 8;
							if ((c = src[0]).A != 0) dest[7] = c;
							if ((c = src[1]).A != 0) dest[6] = c;
							if ((c = src[2]).A != 0) dest[5] = c;
							if ((c = src[3]).A != 0) dest[4] = c;
							if ((c = src[4]).A != 0) dest[3] = c;
							if ((c = src[5]).A != 0) dest[2] = c;
							if ((c = src[6]).A != 0) dest[1] = c;
							if ((c = src[7]).A != 0) dest[0] = c;
							count -= 8;
							src += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest -= 4;
								if ((c = src[0]).A != 0) dest[3] = c;
								if ((c = src[1]).A != 0) dest[2] = c;
								if ((c = src[2]).A != 0) dest[1] = c;
								if ((c = src[3]).A != 0) dest[0] = c;
								src += 4;
							}
							if ((count & 2) != 0)
							{
								dest -= 2;
								if ((c = src[0]).A != 0) dest[1] = c;
								if ((c = src[1]).A != 0) dest[0] = c;
								src += 2;
							}
							if ((count & 1) != 0)
							{
								dest--;
								if ((c = src[0]).A != 0) dest[0] = c;
								src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					if (!flipVert)
					{
						Debug.Assert(dest >= destBase && dest - destX <= destLimit);
						Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
					}
					else
					{
						Debug.Assert(dest >= destBase && dest <= destLimit);
						Debug.Assert(src + srcImage.Width >= srcBase && src + srcImage.Width <= srcLimit);
					}
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, adding
		/// all color values in the source and destination together (i.e., result.r = src.r + dest.r).
		/// Make sure all values are within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeAddBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							byte r = (byte)Math.Min(s.R + d.R, 255);
							byte g = (byte)Math.Min(s.G + d.G, 255);
							byte b = (byte)Math.Min(s.B + d.B, 255);
							byte a = (byte)Math.Min(s.A + d.A, 255);
							*dest++ = new Color32(r, g, b, a);
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, subtracting
		/// all color values in the source and destination together (i.e., result.r = src.r - dest.r).
		/// Make sure all values are within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeRSubBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							byte r = (byte)Math.Max(s.R - d.R, 0);
							byte g = (byte)Math.Max(s.G - d.G, 0);
							byte b = (byte)Math.Max(s.B - d.B, 0);
							byte a = (byte)Math.Max(s.A - d.A, 0);
							*dest++ = new Color32(r, g, b, a);
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, subtracting
		/// all color values in the source and destination together (i.e., result.r = dest.r - src.r).
		/// Make sure all values are within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeSubBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							byte r = (byte)Math.Max(d.R - s.R, 0);
							byte g = (byte)Math.Max(d.G - s.G, 0);
							byte b = (byte)Math.Max(d.B - s.B, 0);
							byte a = (byte)Math.Max(d.A - s.A, 0);
							*dest++ = new Color32(r, g, b, a);
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle, multiplying
		/// all color values in the source and destination together (i.e., result.r = src.r * dest.r / 255).
		/// Make sure all values are within range; this is as unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeMultiplyBlit(Image32 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* src = srcBase + srcImage.Width * srcY + srcX;
					Color32* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					Color32* destLimit = destBase + Width * Height;
					Color32* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							Color32 s = *src++, d = *dest;
							uint r = (uint)(s.R * d.R);
							uint g = (uint)(s.G * d.G);
							uint b = (uint)(s.B * d.B);
							uint a = (uint)(s.A * d.A);
							r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
							g = (g + 1 + (g >> 8)) >> 8;
							b = (b + 1 + (b >> 8)) >> 8;
							a = (a + 1 + (a >> 8)) >> 8;
							*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
						} while (--count != 0);
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);

					Debug.Assert(dest >= destBase && dest - destX <= destLimit);
					Debug.Assert(src >= srcBase && src - srcX <= srcLimit);
				}
			}
		}

#endregion

		#region Pattern blits

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle in-place, repeating the source rectangle
		/// if the destination rectangle is larger than the source.  This will by default clip
		/// the provided destination coordinates to perform a safe blit (all pixels outside an
		/// image will be ignored).  If the source coordinates lie outside the srcImage, this
		/// will throw an exception.
		/// </summary>
		public void PatternBlit(Image32 srcImage, Rect srcRect, Rect destRect)
		{
			if (srcRect.X < 0 || srcRect.Y < 0 || srcRect.Width < 1 || srcRect.Height < 1
				|| srcRect.X > srcImage.Width - srcRect.Width
				|| srcRect.Y > srcImage.Height - srcRect.Height)
				throw new ArgumentException($"Illegal source rectangle for PatternBlit: Rect is {srcRect}, but image is {srcImage.Size}.");

			int offsetX = 0, offsetY = 0;
			int destX = destRect.X, destY = destRect.Y, width = destRect.Width, height = destRect.Height;
			if (!ClipRect(Size, ref destX, ref destY, ref width, ref height))
				return;

			if (destX > destRect.X)
				offsetX = (destX - destRect.X) % srcRect.Width;
			if (destY > destRect.Y)
				offsetY = (destY - destRect.Y) % srcRect.Height;

			FastUnsafePatternBlit(srcImage, srcRect, new Vector2i(offsetX, offsetY),
				new Rect(destX, destY, width, height));
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  This doesn't apply alpha;
		/// it just slams new data over top of old.  Make sure all values are within range; this is as
		/// unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafePatternBlit(Image32 srcImage, Rect srcRect, Vector2i offset, Rect destRect)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = srcImage.Data)
				{
					Color32* dest = destBase + Width * destRect.Y + destRect.X;
					int destSkip = Width - destRect.Width;
					int srcX = srcRect.X;
					int srcY = srcRect.Y;
					int offsetY = offset.Y;
					int height = destRect.Height;
					int srcWidth = srcImage.Width;

					do
					{
						int offsetX = offset.X;
						int count = destRect.Width;
						Color32* src = srcBase + (srcY + offsetY) * srcWidth;
						do
						{
							*dest++ = src[srcX + offsetX];
							if (++offsetX >= srcRect.Width)
								offsetX = 0;
						} while (--count != 0);
						dest += destSkip;
						if (++offsetY >= srcRect.Height)
							offsetY = 0;
					} while (--height != 0);
				}
			}
		}

		#endregion

		#region Transformations

		/// <summary>
		/// Vertically flip the image, in-place.
		/// </summary>
		public void FlipVert()
		{
			unsafe
			{
				fixed (Color32* dataBase = Data)
				{
					Color32* topRow = dataBase;
					Color32* bottomRow = dataBase + Width * (Height - 1);
					for (int y = 0; y < Height / 2; y++)
					{
						for (int x = 0; x < Width; x++)
						{
							Color32 temp = *topRow;
							*topRow = *bottomRow;
							*bottomRow = temp;
							topRow++;
							bottomRow++;
						}
						bottomRow -= Width * 2;
					}
				}
			}
		}

		/// <summary>
		/// Horizontally flip the image, in-place.
		/// </summary>
		public void FlipHorz()
		{
			unsafe
			{
				fixed (Color32* dataBase = Data)
				{
					Color32* ptr = dataBase;
					for (int y = 0; y < Height; y++)
					{
						Color32* start = ptr;
						Color32* end = ptr + Width;
						for (int x = 0; x < Width / 2; x++)
						{
							end--;
							Color32 temp = *start;
							*start = *end;
							*end = temp;
							start++;
						}
						ptr += Width;
					}
				}
			}
		}

		/// <summary>
		/// Rotate the image 90 degrees clockwise, in-place.
		/// </summary>
		public void Rotate90()
		{
			PureImage32 rotated = Pure.Rotate90();
			Data = rotated.Data;
			Width = rotated.Width;
			Height = rotated.Height;
		}

		/// <summary>
		/// Rotate the image 90 degrees counterclockwise, in-place.
		/// </summary>
		public void Rotate90CCW()
		{
			PureImage32 rotated = Pure.Rotate90CCW();
			Data = rotated.Data;
			Width = rotated.Width;
			Height = rotated.Height;
		}

		/// <summary>
		/// Rotate the image 180 degrees, in-place.
		/// </summary>
		public void Rotate180()
		{
			unsafe
			{
				fixed (Color32* dataBase = Data)
				{
					Color32* ptr = dataBase;
					Color32* endPtr = ptr + Width * Height - 1;
					while (ptr < endPtr)
					{
						ptr++;
						Color32 temp = *ptr;
						*ptr = *endPtr;
						*endPtr = temp;
						endPtr--;
					}
				}
			}
		}

		#endregion

		#region Color mixing

		/// <summary>
		/// Mix another image with this by combining their R, G, B, and A values according to the
		/// given opacity level, in-place.  The other image must be exactly the same width and
		/// height as this image.
		/// </summary>
		/// <param name="other">The other image whose color values should be mixed with this
		/// image's color values.</param>
		/// <param name="amount">How much of the other image to mix in; 0.0 = 100% this image,
		/// 1.0 = 100% the other image.</param>
		public void Mix(Image32 other, double amount = 0.5)
		{
			if (other.Width != Width || other.Height != Height)
				throw new ArgumentException("Dimensions of other image are not the same as dimensions of this image.");
			if (Width <= 0 || Height <= 0)
				return;

			int sa = (int)(amount * 65536 + 0.5f);
			int da = 65536 - sa;

			unsafe
			{
				fixed (Color32* destBase = Data)
				fixed (Color32* srcBase = other.Data)
				{
					Color32* src = srcBase;
					Color32* dest = destBase;

					int count = Width * Height;
					do
					{
						Color32 s = *src++, d = *dest;
						uint r = (uint)(s.R * sa + d.R * da) >> 16;
						uint g = (uint)(s.G * sa + d.G * da) >> 16;
						uint b = (uint)(s.B * sa + d.B * da) >> 16;
						uint a = (uint)(s.A * sa + d.A * da) >> 16;
						*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
					} while (--count != 0);
				}
			}
		}

		/// <summary>
		/// Multiply every color value in this by the given scalar values.
		/// </summary>
		public void Multiply(float r, float g, float b, float a)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* dest = destBase;

					int count = Width * Height;
					do
					{
						Color32 d = *dest;
						*dest++ = new Color32((int)(d.R * r + 0.5f), (int)(d.G * g + 0.5f), (int)(d.B * b + 0.5f), (int)(d.A * a + 0.5f));
					} while (--count != 0);
				}
			}
		}

		/// <summary>
		/// Multiply every color value in this by the given scalar values.
		/// </summary>
		public void Multiply(double r, double g, double b, double a)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* dest = destBase;

					int count = Width * Height;
					do
					{
						Color32 d = *dest;
						*dest++ = new Color32((int)(d.R * r + 0.5), (int)(d.G * g + 0.5), (int)(d.B * b + 0.5), (int)(d.A * a + 0.5));
					} while (--count != 0);
				}
			}
		}

		/// <summary>
		/// Premultiply the alpha value to the red, green, and blue values of every pixel, in-place.
		/// </summary>
		public void PremultiplyAlpha()
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					byte* dest = (byte*)destBase;
					int count = Width * Height;

					byte* ptr = (byte*)destBase;
					byte* end = ptr + count * 4;

					for (; ptr < end; ptr += 4)
					{
						uint a = ptr[3];

						uint r = ptr[0];
						r *= a;
						r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
						ptr[0] = (byte)r;

						uint g = ptr[1];
						g *= a;
						g = (g + 1 + (g >> 8)) >> 8;
						ptr[1] = (byte)g;

						uint b = ptr[2];
						b *= a;
						b = (b + 1 + (b >> 8)) >> 8;
						ptr[2] = (byte)b;
					}
				}
			}
		}

		#endregion

		#region Color remapping

		/// <summary>
		/// Skim through the image and replace all exact instances of the given color with another.
		/// </summary>
		/// <param name="src">The color to replace.</param>
		/// <param name="dest">Its replacement color.</param>
		public void RemapColor(Color32 src, Color32 dest)
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end - 8)
					{
						if (ptr[0] == src) ptr[0] = dest;
						if (ptr[1] == src) ptr[1] = dest;
						if (ptr[2] == src) ptr[2] = dest;
						if (ptr[3] == src) ptr[3] = dest;
						if (ptr[4] == src) ptr[4] = dest;
						if (ptr[5] == src) ptr[5] = dest;
						if (ptr[6] == src) ptr[6] = dest;
						if (ptr[7] == src) ptr[7] = dest;
						ptr += 8;
					}

					int count = (int)(end - ptr);
					if (count != 0)
					{
						if ((count & 4) != 0)
						{
							if (ptr[0] == src) ptr[0] = dest;
							if (ptr[1] == src) ptr[1] = dest;
							if (ptr[2] == src) ptr[2] = dest;
							if (ptr[3] == src) ptr[3] = dest;
							ptr += 4;
						}
						if ((count & 2) != 0)
						{
							if (ptr[0] == src) ptr[0] = dest;
							if (ptr[1] == src) ptr[1] = dest;
							ptr += 2;
						}
						if ((count & 1) != 0)
						{
							if (ptr[0] == src) ptr[0] = dest;
						}
					}
				}
			}
		}

		/// <summary>
		/// Skim through the image and replace many colors at once via a dictionary.
		/// This is typically much slower than many calls to Remap() above, unless the
		/// replacement table is large relative to the image size.
		/// </summary>
		/// <param name="dictionary">The dictionary that describes all replacement values.
		/// If a color does not exist in the dictionary, it will not be changed.</param>
		public void RemapColor(Dictionary<Color32, Color32> dictionary)
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end - 8)
					{
						if (dictionary.TryGetValue(ptr[0], out Color32 replacement))
							ptr[0] = replacement;
						if (dictionary.TryGetValue(ptr[1], out replacement))
							ptr[1] = replacement;
						if (dictionary.TryGetValue(ptr[2], out replacement))
							ptr[2] = replacement;
						if (dictionary.TryGetValue(ptr[3], out replacement))
							ptr[3] = replacement;
						if (dictionary.TryGetValue(ptr[4], out replacement))
							ptr[4] = replacement;
						if (dictionary.TryGetValue(ptr[5], out replacement))
							ptr[5] = replacement;
						if (dictionary.TryGetValue(ptr[6], out replacement))
							ptr[6] = replacement;
						if (dictionary.TryGetValue(ptr[7], out replacement))
							ptr[7] = replacement;
						ptr += 8;
					}

					int count = (int)(end - ptr);
					if (count != 0)
					{
						if ((count & 4) != 0)
						{
							if (dictionary.TryGetValue(ptr[0], out Color32 replacement))
								ptr[0] = replacement;
							if (dictionary.TryGetValue(ptr[1], out replacement))
								ptr[1] = replacement;
							if (dictionary.TryGetValue(ptr[2], out replacement))
								ptr[2] = replacement;
							if (dictionary.TryGetValue(ptr[3], out replacement))
								ptr[3] = replacement;
							ptr += 4;
						}
						if ((count & 2) != 0)
						{
							if (dictionary.TryGetValue(ptr[0], out Color32 replacement))
								ptr[0] = replacement;
							if (dictionary.TryGetValue(ptr[1], out replacement))
								ptr[1] = replacement;
							ptr += 2;
						}
						if ((count & 1) != 0)
						{
							if (dictionary.TryGetValue(ptr[0], out Color32 replacement))
								ptr[0] = replacement;
						}
					}
				}
			}
		}

		/// <summary>
		/// Remap each color by passing it through the given matrix transform.
		/// </summary>
		/// <param name="matrix">The matrix to multiply each color vector by.</param>
		public void RemapColor(Matrix3 matrix)
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end)
					{
						Color32 src = *ptr;
						Vector3 v = new Vector3(src.R * (1 / 255f), src.G * (1 / 255f), src.B * (1 / 255f));
						v = matrix * v;
						Color32 result = new Color32((int)(v.X * 255 + 0.5f), (int)(v.Y * 255 + 0.5f), (int)(v.Z * 255 + 0.5f), src.A);
						*ptr++ = result;
					}
				}
			}
		}

		/// <summary>
		/// Apply uniform gamma to all three color values (i.e., convert each color to [0, 1] and then
		/// raise it to the given power).
		/// </summary>
		/// <param name="gamma">The gamma exponent to apply.  0 to 1 results in brighter
		/// images, and 1 to infinity results in darker images.</param>
		public void Gamma(double gamma)
		{
			Span<byte> remapTable = stackalloc byte[256];
			for (int i = 0; i < 256; i++)
			{
				int raised = (int)(Math.Pow(i / 255.0f, gamma) * 255.0f + 0.5f);
				remapTable[i] = (byte)Math.Max(Math.Min(raised, 255), 0);
			}

			unsafe
			{
				fixed (Color32* imageBase = Data)
				fixed (byte* remap = remapTable)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end)
					{
						*ptr = new Color32(remap[ptr->R], remap[ptr->G], remap[ptr->B], ptr->A);
						ptr++;
					}
				}
			}
		}

		/// <summary>
		/// Apply separate gamma to all three color values (i.e., convert each color to [0, 1] and then
		/// raise it to the given power).  This is slightly slower than applying
		/// a uniform gamma to all three color values.
		/// </summary>
		/// <param name="rAmount">The amount of gamma adjustment to apply to the red channel.
		/// &gt;1 is brighter, &lt;1 is darker.</param>
		/// <param name="gAmount">The amount of gamma adjustment to apply to the green channel.</param>
		/// <param name="bAmount">The amount of gamma adjustment to apply to the blue channel.</param>
		public void Gamma(double rAmount, double gAmount, double bAmount)
		{
			Span<byte> remapTable = stackalloc byte[768];
			for (int i = 0; i < 256; i++)
			{
				int raised = (int)(Math.Pow(i / 255.0, rAmount) * 255.0 + 0.5);
				remapTable[i] = (byte)Math.Max(Math.Min(raised, 255), 0);
				raised = (int)(Math.Pow(i / 255.0, gAmount) * 255.0 + 0.5);
				remapTable[i + 256] = (byte)Math.Max(Math.Min(raised, 255), 0);
				raised = (int)(Math.Pow(i / 255.0, bAmount) * 255.0 + 0.5);
				remapTable[i + 512] = (byte)Math.Max(Math.Min(raised, 255), 0);
			}

			unsafe
			{
				fixed (Color32* imageBase = Data)
				fixed (byte* remap = remapTable)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end)
					{
						*ptr = new Color32(remap[ptr->R], remap[ptr->G + 256], remap[ptr->B + 512], ptr->A);
						ptr++;
					}
				}
			}
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
		public void Grayscale(bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					if (useRelativeBrightness)
					{
						uint rc = (uint)(65536 * r + 0.5);
						uint gc = (uint)(65536 * g + 0.5);
						uint bc = (uint)(65536 * b + 0.5);
						if (rc + gc + bc > 65536)
							throw new ArgumentException("Sum of r+g+b brightness values must be less than or equal to 1.0.");

						while (ptr < end)
						{
							Color32 src = *ptr;
							uint y = ((rc * src.R + gc * src.G + bc * src.B) + 32768) >> 16;
							Color32 result = new Color32((byte)y, (byte)y, (byte)y, src.A);
							*ptr++ = result;
						}
					}
					else
					{
						while (ptr < end)
						{
							Color32 src = *ptr;
							// Multiply/shift here is faster than divide-by-three.
							uint y = (((uint)src.R + (uint)src.G + (uint)src.B) * 683) >> 11;
							Color32 result = new Color32((byte)y, (byte)y, (byte)y, src.A);
							*ptr++ = result;
						}
					}
				}
			}
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
		{
			Image8 dest = new Image8(Size, Palettes.Grayscale256);

			unsafe
			{
				fixed (Color32* imageBase = Data)
				fixed (byte* destBase = dest.Data)
				{
					Color32* srcPtr = imageBase;
					Color32* end = imageBase + Width * Height;
					byte* destPtr = destBase;

					if (useRelativeBrightness)
					{
						uint rc = (uint)(65536 * r + 0.5);
						uint gc = (uint)(65536 * g + 0.5);
						uint bc = (uint)(65536 * b + 0.5);
						if (rc + gc + bc > 65536)
							throw new ArgumentException("Sum of r+g+b brightness values must be less than or equal to 1.0.");

						while (srcPtr < end)
						{
							Color32 src = *srcPtr++;
							uint y = ((rc * src.R + gc * src.G + bc * src.B) + 32768) >> 16;
							*destPtr++ = (byte)y;
						}
					}
					else
					{
						while (srcPtr < end)
						{
							Color32 src = *srcPtr++;
							// Multiply/shift here is faster than divide-by-three.
							uint y = (((uint)src.R + (uint)src.G + (uint)src.B) * 341) >> 10;
							*destPtr += (byte)y;
						}
					}
				}
			}

			return dest;
		}

		/// <summary>
		/// Remove some or all of the color saturation from an image, in-place.
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
		/// <remarks>This is implemented as the same math as the Grayscale() method, but
		/// only mixing in *some* of the resulting grayscale color.  Fast, and fairly simple.</remarks>
		public void Desaturate(double amount,
			bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			int v = (int)(Math.Max(Math.Min(amount * 256 + 0.5, 256), 0));
			int iv = 256 - v;

			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					if (useRelativeBrightness)
					{
						uint rc = (uint)(65536 * r + 0.5);
						uint gc = (uint)(65536 * g + 0.5);
						uint bc = (uint)(65536 * b + 0.5);
						if (rc + gc + bc > 65536)
							throw new ArgumentException("Sum of r+g+b brightness values must be less than or equal to 1.0.");

						while (ptr < end)
						{
							Color32 src = *ptr;
							uint y = ((rc * src.R + gc * src.G + bc * src.B) + 32768) >> 16;
							Color32 result = new Color32(
								(byte)((src.R * v + y * iv) >> 8),
								(byte)((src.G * v + y * iv) >> 8),
								(byte)((src.B * v + y * iv) >> 8),
								src.A);
							*ptr++ = result;
						}
					}
					else
					{
						while (ptr < end)
						{
							Color32 src = *ptr;
							// Multiply/shift here is faster than divide-by-three.
							uint y = (((uint)src.R + (uint)src.G + (uint)src.B) * 683) >> 11;
							Color32 result = new Color32(
								(byte)((src.R * v + y * iv) >> 8),
								(byte)((src.G * v + y * iv) >> 8),
								(byte)((src.B * v + y * iv) >> 8),
								src.A);
							*ptr++ = result;
						}
					}
				}
			}
		}

		/// <summary>
		/// Remap the image to a sepia-tone version of itself by forcing
		/// every color's position in YIQ-space.
		/// </summary>
		/// <param name="amount">How saturated the sepia is.  0.0 = grayscale, 1.0 = orange, -1.0 = blue.</param>
		public void Sepia(double amount = 0.125)
		{
			amount = Math.Min(Math.Max(amount, -1.0), +1.0);
			int newI = (int)(amount * 255 + 0.5);

			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end)
					{
						// Note that the Y values here match the "Color32.ApparentBrightness"
						// constants:  You get brightness weighting here whether you want it
						// or not.
						Color32 src = *ptr;
						float y = 0.299f  * src.R + 0.587f  * src.G + 0.114f  * src.B;
						float i = 0.5959f * src.R - 0.2746f * src.G - 0.3213f * src.B;
						float q = 0.2115f * src.R - 0.5227f * src.G + 0.3112f * src.B;
						i = newI;
						q = 0;
						float r = 1f * y + 0.956f * i + 0.619f * q;
						float g = 1f * y - 0.272f * i - 0.647f * q;
						float b = 1f * y - 1.106f * i + 1.703f * q;
						Color32 result = new Color32((int)(r + 0.5f), (int)(g + 0.5f), (int)(b + 0.5f), src.A);
						*ptr++ = result;
					}
				}
			}
		}

		/// <summary>
		/// Invert the red, green, and blue values in the image (i.e., replace each R with 255-R,
		/// and so on for the other channels).  Alpha will be left unchanged.
		/// </summary>
		public void Invert()
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					while (ptr < end)
					{
						Color32 src = *ptr;
						*ptr++ = new Color32((byte)~src.R, (byte)~src.G, (byte)~src.B, src.A);
					}
				}
			}
		}

		/// <summary>
		/// Invert one channel in the image (i.e., replace each R with 255-R).
		/// </summary>
		public void Invert(ColorChannel channel)
		{
			unsafe
			{
				fixed (Color32* imageBase = Data)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Width * Height;

					switch (channel)
					{
						case ColorChannel.Red:
							while (ptr < end)
							{
								Color32 src = *ptr;
								*ptr++ = new Color32((byte)~src.R, src.G, src.B, src.A);
							}
							break;
					}
				}
			}
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
		public void HueSaturationLightness(double deltaHue, double deltaSaturation, double deltaLightness)
		{
			float dh = (float)deltaHue;
			float ds = (float)deltaSaturation;
			float dl = (float)deltaLightness;

			for (int i = 0; i < Data.Length; i++)
			{
				Color32 c = Data[i];
				(float hue, float sat, float lit) = c.ToHsl();
				hue += dh;
				if (ds > 0)
					sat = sat * (1 - ds) + ds;
				else if (ds < 0)
					sat = sat * (1 + ds);
				if (dl > 0)
					lit = lit * (1 - dl) + dl;
				else if (dl < 0)
					lit = lit * (1 + dl);
				Color32 c2 = Color32.FromHsl(hue, sat, lit);
				Data[i] = c2;
			}
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
		public void HueSaturationBrightness(double deltaHue, double deltaSaturation, double deltaBrightness)
		{
			float dh = (float)deltaHue;
			float ds = (float)deltaSaturation;
			float db = (float)deltaBrightness;

			for (int i = 0; i < Data.Length; i++)
			{
				Color32 c = Data[i];
				(float hue, float sat, float brt) = c.ToHsb();
				hue += dh;
				if (ds > 0)
					sat = sat * (1 - ds) + ds;
				else if (ds < 0)
					sat = sat * (1 + ds);
				if (db > 0)
					brt = brt * (1 - db) + db;
				else if (db < 0)
					brt = brt * (1 + db);
				Color32 c2 = Color32.FromHsb(hue, sat, brt);
				Data[i] = c2;
			}
		}

		#endregion

		#region Channel extraction/combining

		/// <summary>
		/// Extract out the given RGBA channel from this image as a new 8-bit image.
		/// </summary>
		/// <param name="channel">The channel to extract.</param>
		/// <returns>The extracted channel.</returns>
		/// <exception cref="ArgumentException">Thrown if the given channel is unknown.</exception>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		[Pure]
		public Image8 ExtractChannel(ColorChannel channel)
		{
			Image8 result = new Image8(Size, Palettes.Grayscale256);

			unsafe
			{
				fixed (byte* destBase = result.Data)
				fixed (Color32* srcBase = Data)
				{
					byte* dest = destBase;
					Color32* src = srcBase;
					int count = Width * Height;
					int eight = count >> 3;
					int rest = count & 7;

					switch (channel)
					{
						case ColorChannel.Red:
							while (eight-- != 0)
							{
								dest[0] = src[0].R;
								dest[1] = src[1].R;
								dest[2] = src[2].R;
								dest[3] = src[3].R;
								dest[4] = src[4].R;
								dest[5] = src[5].R;
								dest[6] = src[6].R;
								dest[7] = src[7].R;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = (src++)->R;
							break;

						case ColorChannel.Green:
							while (eight-- != 0)
							{
								dest[0] = src[0].G;
								dest[1] = src[1].G;
								dest[2] = src[2].G;
								dest[3] = src[3].G;
								dest[4] = src[4].G;
								dest[5] = src[5].G;
								dest[6] = src[6].G;
								dest[7] = src[7].G;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = (src++)->G;
							break;

						case ColorChannel.Blue:
							while (eight-- != 0)
							{
								dest[0] = src[0].B;
								dest[1] = src[1].B;
								dest[2] = src[2].B;
								dest[3] = src[3].B;
								dest[4] = src[4].B;
								dest[5] = src[5].B;
								dest[6] = src[6].B;
								dest[7] = src[7].B;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = (src++)->B;
							break;

						case ColorChannel.Alpha:
							while (eight-- != 0)
							{
								dest[0] = src[0].A;
								dest[1] = src[1].A;
								dest[2] = src[2].A;
								dest[3] = src[3].A;
								dest[4] = src[4].A;
								dest[5] = src[5].A;
								dest[6] = src[6].A;
								dest[7] = src[7].A;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = (src++)->A;
							break;

						default:
							throw new ArgumentException(nameof(channel));
					}
				}
			}

			return result;
		}

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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		[Pure]
		public static Image32 CombineChannels(Image8? red = null, Image8? green = null, Image8? blue = null,
			Image8? alpha = null)
		{
			Image8? firstNonNull = red ?? green ?? blue ?? alpha;
			if (firstNonNull == null)
				throw new ArgumentNullException("All image arguments cannot be null.");

			Vector2i size = firstNonNull.Size;
			if ((red != null && red.Size != size)
				|| (green != null && green.Size != size)
				|| (blue != null && blue.Size != size)
				|| (alpha != null && alpha.Size != size))
				throw new ArgumentException("All channels must have the same dimensions.");

			byte[] defaultRow = null!;
			if (red == null || blue == null || green == null)
				defaultRow = new byte[size.X];

			byte[] defaultAlphaRow = null!;
			if (alpha == null)
			{
				defaultAlphaRow = new byte[size.X];
				for (int i = 0; i < size.X; i++)
					defaultAlphaRow[i] = 255;
			}

			Image32 result = new Image32(size);
			unsafe
			{
				fixed (Color32* destBase = result.Data)
				fixed (byte* redBase = red?.Data)
				fixed (byte* greenBase = green?.Data)
				fixed (byte* blueBase = blue?.Data)
				fixed (byte* alphaBase = alpha?.Data)
				fixed (byte* defaultBase = defaultRow)
				fixed (byte* defaultAlphaBase = defaultAlphaRow)
				{
					Color32* dest = destBase;

					for (int row = 0; row < size.Y; row++)
					{
						byte* r = (red != null ? redBase : defaultBase);
						byte* g = (green != null ? greenBase : defaultBase);
						byte* b = (blue != null ? blueBase : defaultBase);
						byte* a = (alpha != null ? alphaBase : defaultAlphaBase);

						int eight = size.X >> 3;
						int rest = size.X & 7;
						while (eight-- != 0)
						{
							dest[0] = new Color32(r[0], g[0], b[0], a[0]);
							dest[1] = new Color32(r[1], g[1], b[1], a[1]);
							dest[2] = new Color32(r[2], g[2], b[2], a[2]);
							dest[3] = new Color32(r[3], g[3], b[3], a[3]);
							dest[4] = new Color32(r[4], g[4], b[4], a[4]);
							dest[5] = new Color32(r[5], g[5], b[5], a[5]);
							dest[6] = new Color32(r[6], g[6], b[6], a[6]);
							dest[7] = new Color32(r[7], g[7], b[7], a[7]);

							dest += 8;
							r += 8;
							g += 8;
							b += 8;
							a += 8;
						}
						while (rest-- != 0)
							*dest++ = new Color32(*r++, *g++, *b++, *a++);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Swap color channels in the image with other color channels in the same image.
		/// </summary>
		/// <param name="redChannel">The current channel to use for the new red channel.</param>
		/// <param name="greenChannel">The current channel to use for the new green channel.</param>
		/// <param name="blueChannel">The current channel to use for the new blue channel.</param>
		/// <param name="alphaChannel">The current channel to use for the new alpha channel.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public void SwapChannels(ColorChannel redChannel,
			ColorChannel greenChannel, ColorChannel blueChannel,
			ColorChannel alphaChannel = ColorChannel.Alpha)
		{
			if ((int)redChannel < 1 || (int)redChannel > 4
				|| (int)greenChannel < 1 || (int)greenChannel > 4
				|| (int)blueChannel < 1 || (int)blueChannel > 4
				|| (int)alphaChannel < 1 || (int)alphaChannel > 4)
				throw new ArgumentException("SwapChannels() must be passed non-None arguments for each channel.");

			unsafe
			{
				int r = (int)(redChannel - 1);
				int g = (int)(greenChannel - 1);
				int b = (int)(blueChannel - 1);
				int a = (int)(alphaChannel - 1);

				byte* tmp = stackalloc byte[16];

				int count = Width * Height;

				fixed (Color32* destBase = Data)
				{
					byte* dest = (byte *)destBase;

					int four = count >> 2;
					int rest = count & 3;
					while (four-- != 0)
					{
						((uint*)tmp)[0] = ((uint*)dest)[0];
						((uint*)tmp)[1] = ((uint*)dest)[1];
						((uint*)tmp)[2] = ((uint*)dest)[2];
						((uint*)tmp)[3] = ((uint*)dest)[3];

						dest[ 0] = tmp[     r];
						dest[ 1] = tmp[     g];
						dest[ 2] = tmp[     b];
						dest[ 3] = tmp[     a];

						dest[ 4] = tmp[ 4 + r];
						dest[ 5] = tmp[ 4 + g];
						dest[ 6] = tmp[ 4 + b];
						dest[ 7] = tmp[ 4 + a];

						dest[ 8] = tmp[ 8 + r];
						dest[ 9] = tmp[ 8 + g];
						dest[10] = tmp[ 8 + b];
						dest[11] = tmp[ 8 + a];

						dest[12] = tmp[12 + r];
						dest[13] = tmp[12 + g];
						dest[14] = tmp[12 + b];
						dest[15] = tmp[12 + a];

						dest += 16;
					}
					while (rest-- != 0)
					{
						((uint*)tmp)[0] = ((uint*)dest)[0];
						dest[0] = tmp[r];
						dest[1] = tmp[g];
						dest[2] = tmp[b];
						dest[3] = tmp[a];
						dest += 4;
					}
				}
			}
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
		public static Image32 operator +(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot add images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot add images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ + *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been subtracted.
		/// </summary>
		/// <param name="a">The first image.</param>
		/// <param name="b">The second image to subtract.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been subtracted.</returns>
		[Pure]
		public static Image32 operator -(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot subtract images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot subtract images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ - *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been multiplied together.
		/// </summary>
		/// <param name="a">The first image to multiply.</param>
		/// <param name="b">The second image to multiply.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been multiplied.</returns>
		[Pure]
		public static Image32 operator *(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot multiply images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot multiply images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ * *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static Image32 operator *(Image32 image, int scalar)
		{
			Image32 result = new Image32(image.Size);

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ * scalar;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been multiplied by a scalar.
		/// </summary>
		/// <param name="image">The image to multiply.</param>
		/// <param name="scalar">The scalar to multiply each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static Image32 operator *(Image32 image, double scalar)
		{
			Image32 result = new Image32(image.Size);

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ * scalar;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static Image32 operator /(Image32 image, int scalar)
		{
			Image32 result = new Image32(image.Size);

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ / scalar;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been divided by a scalar.
		/// </summary>
		/// <param name="image">The image to divide.</param>
		/// <param name="scalar">The scalar to divide each value by.</param>
		/// <returns>A new image where all components have been scaled.</returns>
		[Pure]
		public static Image32 operator /(Image32 image, double scalar)
		{
			Image32 result = new Image32(image.Size);

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ / scalar;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given image, where each
		/// pixel's R, G, and B components have been inverted, replacing each value V
		/// with 255 - V.
		/// </summary>
		/// <param name="image">The image to invert.</param>
		/// <returns>A new image where all components have been inverted.</returns>
		[Pure]
		public static Image32 operator ~(Image32 image)
		{
			image = image.Clone();
			image.Invert();
			return image;
		}

		/// <summary>
		/// Create a new image of the same size as the given image, where each
		/// pixel's R, G, and B components have been inverted, replacing each value V
		/// with (256 - V) % 256.
		/// </summary>
		/// <param name="image">The image to negate.</param>
		/// <returns>A new image where all components have been inverted.</returns>
		[Pure]
		public static Image32 operator -(Image32 image)
		{
			Image32 result = new Image32(image.Size);

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = -*imagePtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-or'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-or'ed together.</returns>
		[Pure]
		public static Image32 operator |(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ | *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-and'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-and'ed together.</returns>
		[Pure]
		public static Image32 operator &(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ & *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, B, and A components have been bitwise-xor'ed together.
		/// </summary>
		/// <param name="a">The first image to combine.</param>
		/// <param name="b">The second image to combine.  This must have the same
		/// width and height as the first image.</param>
		/// <returns>A new image where all components have been bitwise-xor'ed together.</returns>
		[Pure]
		public static Image32 operator ^(Image32 a, Image32 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image32 result = new Image32(a.Size);

			unsafe
			{
				fixed (Color32* aBase = a.Data)
				fixed (Color32* bBase = b.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					Color32* aPtr = aBase;
					Color32* bPtr = bBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *aPtr++ ^ *bPtr++;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted left by the given number of bits.
		/// </summary>
		/// <param name="image">The image to shift.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted left.</returns>
		[Pure]
		public static Image32 operator <<(Image32 image, int amount)
		{
			if (amount < 0)
				return image >> -amount;
			else if (amount == 0)
				return image.Clone();

			Image32 result = new Image32(image.Size);
			if (amount >= 8)
				return result;

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ << amount;
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted logically right by the given number of bits.
		/// </summary>
		/// <param name="image">The image to shift.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted logically right.</returns>
		[Pure]
		public static Image32 operator >>(Image32 image, int amount)
		{
			if (amount < 0)
				return image << -amount;
			else if (amount == 0)
				return image.Clone();

			Image32 result = new Image32(image.Size);
			if (amount >= 8)
				return result;

			unsafe
			{
				fixed (Color32* imageBase = image.Data)
				fixed (Color32* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					Color32* imagePtr = imageBase;
					Color32* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = *imagePtr++ >> amount;
				}
			}

			return result;
		}

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
		{
			(Color32 Color, int Count)[] histogram = Histogram(includeAlpha);
			Color32[] palette = MedianCutQuantizer.Quantize(histogram, numColors, useOriginalColors, includeAlpha);
			return palette;
		}

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
		public (Color32 Color, int Count)[] Histogram(bool includeAlpha = false)
		{
			// First, generate a copy of the data, sorted by color code.
			uint[] sortedCopy = new uint[Width * Height];

			if (includeAlpha)
			{
				for (int i = 0, end = Width * Height; i < end; i++)
				{
					Color32 c = Data[i];
					sortedCopy[i] = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | (c.B));
				}
			}
			else
			{
				for (int i = 0, end = Width * Height; i < end; i++)
				{
					Color32 c = Data[i];
					sortedCopy[i] = (uint)((255 << 24) | (c.R << 16) | (c.G << 8) | (c.B));
				}
			}

			Array.Sort(sortedCopy);

			// Now scan the sorted data for repeated spans of the same color.
			// The rule for this will vary depending on whether we're including
			// or ignoring alpha.
			List<(Color32 Color, int Count)> result = new List<(Color32 Color, int Count)>();

			uint currentColor = sortedCopy[0];
			int count = 1;

			for (int i = 1; i < sortedCopy.Length; i++)
			{
				if (sortedCopy[i] == currentColor)
					count++;
				else
				{
					Color32 color = new Color32(
						(byte)((currentColor >> 16) & 0xFF),
						(byte)((currentColor >>  8) & 0xFF),
						(byte)((currentColor      ) & 0xFF),
						(byte)((currentColor >> 24) & 0xFF)
					);
					result.Add((color, count));

					currentColor = sortedCopy[i];
					count = 1;
				}
			}
			{
				Color32 color = new Color32(
					(byte)((currentColor >> 16) & 0xFF),
					(byte)((currentColor >>  8) & 0xFF),
					(byte)((currentColor      ) & 0xFF),
					(byte)((currentColor >> 24) & 0xFF)
				);
				result.Add((color, count));
			}

			// For numerical stability, we sort the result by count and then by
			// color code, just so the output is consistent for a consistent input.
			result.Sort((a, b) => a.Count != b.Count ? b.Count - a.Count
				: a.Color.A != b.Color.A ? a.Color.A - b.Color.A
				: a.Color.R != b.Color.R ? a.Color.R - b.Color.R
				: a.Color.G != b.Color.G ? a.Color.G - b.Color.G
				: a.Color.B != b.Color.B ? a.Color.B - b.Color.B
				: 0);

			return result.ToArray();
		}

		/// <summary>
		/// Create a ditherer that can convert truecolor RGBA images to 8-bit images.
		/// </summary>
		/// <param name="palette">The palette to use.</param>
		/// <param name="ditherMode">The dithering technique to apply.  By default, no
		/// dithering is applied, and every color is remapped to its nearest neighbor.</param>
		/// <param name="useVisualWeighting">Whether to use linear-space color distances
		/// or visually-weighted color distances.  Visually-weighted color distances may
		/// produce more accurate conversions, at a performance cost.</param>
		/// <returns>An IDitherer instance that can perform dithering from truecolor to
		/// paletted 8-bit.</returns>
		public static IDitherer GetDitherer(DitherMode ditherMode, ReadOnlySpan<Color32> palette,
			bool useVisualWeighting = false)
		{
			IDitherer ditherer = ditherMode switch
			{
				DitherMode.Nearest => new Dithering.NearestNeighborDitherer(),
				DitherMode.Ordered8x8 => new Dithering.Ordered8x8Ditherer(),
				DitherMode.Ordered4x4 => new Dithering.Ordered4x4Ditherer(),
				DitherMode.Ordered2x2 => new Dithering.Ordered2x2Ditherer(),
				DitherMode.FloydSteinberg => new Dithering.FloydSteinbergDitherer(),
				DitherMode.Atkinson => new Dithering.AtkinsonDitherer(),
				DitherMode.Stucki => new Dithering.StuckiDitherer(),
				DitherMode.Burkes => new Dithering.BurkesDitherer(),
				DitherMode.Jarvis => new Dithering.JarvisDitherer(),
				_ => new Dithering.NearestNeighborDitherer(),
			};

			ditherer.Setup(palette, useVisualWeighting);

			return ditherer;
		}

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
			=> GetDitherer(ditherMode, palette, useVisualWeighting).Dither(this);

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
		{
			Color32[] palette = Quantize(numColors, useOriginalColors, includeAlpha);
			return ToImage8(palette.AsSpan(), ditherMode, useVisualWeighting);
		}

		/// <summary>
		/// Converting an Image to an Image is a no-op, but it is allowed, and does
		/// nothing more than simply Clone() the image.
		/// </summary>
		/// <returns>A copy of this image.</returns>
		Image32 IImage.ToImage32()
			=> Clone();

		#endregion

		#region Filling

		/// <summary>
		/// Fill the given image as fast as possible with the given color.
		/// </summary>
		public void Fill(Color32 color)
		{
			int count = Data.Length;
			if (count <= 0)
				return;

			unsafe
			{
				fixed (Color32* bufferStart = Data)
				{
					Color32* dest = bufferStart;

					// Unroll and do 8 at a time, when possible.
					while (count >= 8)
					{
						dest[0] = color;
						dest[1] = color;
						dest[2] = color;
						dest[3] = color;
						dest[4] = color;
						dest[5] = color;
						dest[6] = color;
						dest[7] = color;
						dest += 8;
						count -= 8;
					}

					// Do whatever's left over.
					while (count-- > 0)
						*dest++ = color;
				}
			}
		}

		#endregion

		#region Rectangle Filling (solid)

		/// <summary>
		/// Fill the given rectangular area of the image with the given color, in place.
		/// </summary>
		/// <param name="rect">The rectangular area of the image to fill.</param>
		/// <param name="drawColor">The color to fill with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.  You can also apply the
		/// FastUnsafe flag to skip the clipping step if you are certain that the given
		/// rectangle is fully contained within the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FillRect(Rect rect, Color32 drawColor, BlitFlags blitFlags = BlitFlags.Copy)
			=> FillRect(rect.X, rect.Y, rect.Width, rect.Height, drawColor, blitFlags);

		/// <summary>
		/// Fill the given rectangular area of the image with the given color, in place.
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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public void FillRect(int x, int y, int width, int height, Color32 drawColor,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			if ((blitFlags & BlitFlags.FastUnsafe) == 0
				&& !ClipRect(Size, ref x, ref y, ref width, ref height))
				return;

			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* dest = destBase + Width * y + x;
					int nextLine = Width - width;

					BlitFlags drawMode = 0;
					switch (blitFlags & BlitFlags.ModeMask)
					{
						default:
						case BlitFlags.Copy:
							drawMode = BlitFlags.Copy;
							break;

						case BlitFlags.Transparent:
							if (drawColor.A == 0)
								return;
							drawMode = BlitFlags.Copy;
							break;

						case BlitFlags.Alpha:
							if (drawColor.A == 0)
								return;
							drawMode = drawColor.A == 255 ? BlitFlags.Copy : BlitFlags.Alpha;
							break;

						case BlitFlags.PMAlpha:
							if (drawColor.A == 0)
								return;
							drawMode = drawColor.A == 255 ? BlitFlags.Copy : BlitFlags.PMAlpha;
							break;
					}

					switch (drawMode)
					{
						case BlitFlags.Copy:
							while (height-- != 0)
							{
								int count = width;
								while (count-- != 0)
									*dest++ = drawColor;
								dest += nextLine;
							}
							break;

						case BlitFlags.Alpha:
							while (height-- != 0)
							{
								int count = width;
								while (count-- != 0)
								{
									Color32 d = *dest;
									uint a = drawColor.A, ia = (uint)(255 - drawColor.A);
									uint r = drawColor.R * a + d.R * ia + 127;
									uint g = drawColor.G * a + d.G * ia + 127;
									uint b = drawColor.B * a + d.B * ia + 127;
									r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
									g = (g + 1 + (g >> 8)) >> 8;
									b = (b + 1 + (b >> 8)) >> 8;
									*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
								}
								dest += nextLine;
							}
							break;

						case BlitFlags.PMAlpha:
							while (height-- != 0)
							{
								int count = width;
								while (count-- != 0)
								{
									Color32 d = *dest;
									uint ia = (uint)(255 - drawColor.A);
									uint r = drawColor.R + d.R * ia + 127;
									uint g = drawColor.G + d.G * ia + 127;
									uint b = drawColor.B + d.B * ia + 127;
									r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
									g = (g + 1 + (g >> 8)) >> 8;
									b = (b + 1 + (b >> 8)) >> 8;
									*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
								}
								dest += nextLine;
							}
							break;
					}
				}
			}
		}

		#endregion

		#region Rectangle Filling (gradient)

		/// <summary>
		/// These values are used to manage the rendering of a single span of a
		/// gradient being drawn on an image.
		/// </summary>
		private struct GradientValues
		{
			// The starting X,Y coordinate of the current span.
			public int x;
			public int y;

			// The length of the current span.
			public int width;

			// The starting RGBA values for the current span, shifted left by 16 bits.
			public uint r1;
			public uint g1;
			public uint b1;
			public uint a1;

			// The ending RGBA values for the current span, shifted left by 16 bits.
			public uint r2;
			public uint g2;
			public uint b2;
			public uint a2;

			// The deltas to adjust the start RGBA values by for each successive span.
			public int rd1;
			public int gd1;
			public int bd1;
			public int ad1;

			// The deltas to adjust the end RGBA values by for each successive span.
			public int rd2;
			public int gd2;
			public int bd2;
			public int ad2;
		}

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FillGradientRect(Rect rect, Color32 topLeft, Color32 topRight, Color32 bottomLeft, Color32 bottomRight,
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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public void FillGradientRect(int x, int y, int width, int height,
			Color32 topLeft, Color32 topRight, Color32 bottomLeft, Color32 bottomRight,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			if ((blitFlags & BlitFlags.FastUnsafe) == 0
				&& !ClipRect(Size, ref x, ref y, ref width, ref height))
				return;

			int ooCount = 65536 / (height - 1);

			GradientValues v = default;

			v.x = x;
			v.y = y;
			v.width = width;

			v.r1 = (uint)topLeft.R << 16;
			v.g1 = (uint)topLeft.G << 16;
			v.b1 = (uint)topLeft.B << 16;
			v.a1 = (uint)topLeft.A << 16;
			v.r2 = (uint)topRight.R << 16;
			v.g2 = (uint)topRight.G << 16;
			v.b2 = (uint)topRight.B << 16;
			v.a2 = (uint)topRight.A << 16;

			v.rd1 = (bottomLeft.R - topLeft.R) * ooCount;
			v.gd1 = (bottomLeft.G - topLeft.G) * ooCount;
			v.bd1 = (bottomLeft.B - topLeft.B) * ooCount;
			v.ad1 = (bottomLeft.A - topLeft.A) * ooCount;
			v.rd2 = (bottomRight.R - topRight.R) * ooCount;
			v.gd2 = (bottomRight.G - topRight.G) * ooCount;
			v.bd2 = (bottomRight.B - topRight.B) * ooCount;
			v.ad2 = (bottomRight.A - topRight.A) * ooCount;

			BlitFlags drawMode = blitFlags & BlitFlags.ModeMask;

			while (height-- != 0)
			{
				FillGradientSpanFast(v, drawMode);

				v.r1 = (uint)((int)v.r1 + v.rd1);
				v.g1 = (uint)((int)v.g1 + v.gd1);
				v.b1 = (uint)((int)v.b1 + v.bd1);
				v.a1 = (uint)((int)v.a1 + v.ad1);
				v.r2 = (uint)((int)v.r2 + v.rd2);
				v.g2 = (uint)((int)v.g2 + v.gd2);
				v.b2 = (uint)((int)v.b2 + v.bd2);
				v.a2 = (uint)((int)v.a2 + v.ad2);

				v.y++;
			}
		}

		/// <summary>
		/// Fill one horizontal span of a gradient rectangle.
		/// </summary>
		/// <param name="v">The gradient color values at the start and end of this span,
		/// with the start coordinate of the span, and its length.  This data is brought
		/// in by reference.</param>
		/// <param name="blitFlags">Which mode to use to draw the gradient:  This method
		/// works in Copy, Alpha, or PMAlpha modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping step if you are certain that the given rectangle is
		/// fully contained within the image.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FillGradientSpanFast(in GradientValues v, BlitFlags blitFlags = BlitFlags.Copy)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* row = destBase + Width * v.y;

					int ooCount = 65536 / (v.width - 1);

					uint r = v.r1;
					uint g = v.g1;
					uint b = v.b1;
					uint a = v.a1;

					int rd = (int)((((long)v.r2 - (long)v.r1) * ooCount) >> 16);
					int gd = (int)((((long)v.g2 - (long)v.g1) * ooCount) >> 16);
					int bd = (int)((((long)v.b2 - (long)v.b1) * ooCount) >> 16);
					int ad = (int)((((long)v.a2 - (long)v.a1) * ooCount) >> 16);

					Color32* dest = row + v.x;

					switch (blitFlags)
					{
						case BlitFlags.Copy:
							int count = v.width;
							while (count-- != 0)
							{
								*dest++ = new Color32(
									(byte)((r + 32768) >> 16),
									(byte)((g + 32768) >> 16),
									(byte)((b + 32768) >> 16),
									(byte)((a + 32768) >> 16));
								r = (uint)((int)r + rd);
								g = (uint)((int)g + gd);
								b = (uint)((int)b + bd);
								a = (uint)((int)a + ad);
							}
							break;

						case BlitFlags.Alpha:
							count = v.width;
							while (count-- != 0)
							{
								Color32 d = *dest;
								uint ca = ((a + 32768) >> 16);
								uint ax = ca, iax = (uint)(255 - ca);
								uint rx = ((r + 32768) >> 16) * ax + d.R * iax + 127;
								uint gx = ((g + 32768) >> 16) * ax + d.G * iax + 127;
								uint bx = ((b + 32768) >> 16) * ax + d.B * iax + 127;
								rx = (rx + 1 + (rx >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
								gx = (gx + 1 + (gx >> 8)) >> 8;
								bx = (bx + 1 + (bx >> 8)) >> 8;
								*dest++ = new Color32((byte)rx, (byte)gx, (byte)bx, (byte)255);
								r = (uint)((int)r + rd);
								g = (uint)((int)g + gd);
								b = (uint)((int)b + bd);
								a = (uint)((int)a + ad);
							}
							break;

						case BlitFlags.PMAlpha:
							count = v.width;
							while (count-- != 0)
							{
								Color32 d = *dest;
								uint ca = ((a + 32768) >> 16);
								uint iax = (uint)(255 - ca);
								uint rx = ((r + 32768) >> 16) + d.R * iax + 127;
								uint gx = ((g + 32768) >> 16) + d.G * iax + 127;
								uint bx = ((b + 32768) >> 16) + d.B * iax + 127;
								rx = (rx + 1 + (rx >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
								gx = (gx + 1 + (gx >> 8)) >> 8;
								bx = (bx + 1 + (bx >> 8)) >> 8;
								*dest++ = new Color32((byte)rx, (byte)gx, (byte)bx, (byte)255);
								r = (uint)((int)r + rd);
								g = (uint)((int)g + gd);
								b = (uint)((int)b + bd);
								a = (uint)((int)a + ad);
							}
							break;
					}
				}
			}
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
		public void DrawRect(Rect rect, Color32 color, int thickness = 1, BlitFlags blitFlags = BlitFlags.Copy)
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
		public void DrawRect(int x, int y, int width, int height, Color32 color, int thickness = 1,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			if (thickness >= width / 2 || thickness >= height / 2)
			{
				FillRect(x, y, width, height, color, blitFlags);
				return;
			}

			// Top edge.
			FillRect(x, y, width, thickness, color, blitFlags);

			// Bottom edge.
			FillRect(x, y + height - thickness, width, thickness, color, blitFlags);

			// Left edge.
			int edgeY = y + thickness;
			int edgeHeight = height - thickness * 2;
			FillRect(x, edgeY, thickness, edgeHeight, color, blitFlags);

			// Right edge.
			FillRect(x + width - thickness, edgeY, thickness, edgeHeight, color, blitFlags);
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawLine(Vector2i p1, Vector2i p2, Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
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
		public void DrawLine(int x1, int y1, int x2, int y2, Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			if ((blitFlags & BlitFlags.FastUnsafe) != 0)
			{
				DrawLineFastUnsafe(x1, y1, x2, y2, color, blitFlags);
				return;
			}

			OutCode code1 = ComputeOutCode(x1, y1);
			OutCode code2 = ComputeOutCode(x2, y2);

			while (true)
			{
				if ((code1 | code2) == 0)
				{
					DrawLineFastUnsafe(x1, y1, x2, y2, color, blitFlags);
					return;
				}
				else if ((code1 & code2) != 0)
				{
					return;
				}
				else
				{
					OutCode outCode = code1 != 0 ? code1 : code2;

					int x = 0, y = 0;
					if ((outCode & OutCode.Bottom) != 0)
					{
						x = x1 + (int)((long)(x2 - x1) * (Height - 1 - y1) / (y2 - y1));
						y = Height - 1;
					}
					else if ((outCode & OutCode.Top) != 0)
					{
						x = x1 + (int)((long)(x2 - x1) * -y1 / (y2 - y1));
						y = 0;
					}
					else if ((outCode & OutCode.Right) != 0)
					{
						y = y1 + (int)((long)(y2 - y1) * (Width - 1 - x1) / (x2 - x1));
						x = Width - 1;
					}
					else if ((outCode & OutCode.Left) != 0)
					{
						y = y1 + (int)((long)(y2 - y1) * -x1 / (x2 - x1));
						x = 0;
					}

					if (outCode == code1)
					{
						x1 = x;
						y1 = y;
						code1 = ComputeOutCode(x1, y1);
						blitFlags &= ~BlitFlags.SkipStart;
					}
					else
					{
						x2 = x;
						y2 = y;
						code2 = ComputeOutCode(x2, y2);
					}
				}
			}
		}

		/// <summary>
		/// OutCode flags for clipping.
		/// </summary>
		[Flags]
		private enum OutCode
		{
			Inside = 0,

			Left = 1 << 0,
			Right = 1 << 1,
			Top = 1 << 2,
			Bottom = 1 << 3,
		}

		/// <summary>
		/// Calculate outcodes for Cohen-Sutherland clipping against the image boundaries.
		/// </summary>
		/// <param name="x">The X coordinate to calculate an OutCode for.</param>
		/// <param name="y">The Y coordinate to calculate an OutCode for.</param>
		/// <returns>The combined OutCode for the given coordinate.</returns>
		[Pure]
		private OutCode ComputeOutCode(int x, int y)
		{
			OutCode code = OutCode.Inside;

			if (x < 0)
				code |= OutCode.Left;
			else if (x >= Width)
				code |= OutCode.Right;

			if (y < 0)
				code |= OutCode.Top;
			else if (y >= Height)
				code |= OutCode.Bottom;

			return code;
		}

		/// <summary>
		/// An implementation of Bresenham's line-drawing routine.  This is very
		/// fast, but it performs no clipping.
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
		private void DrawLineFastUnsafe(int x1, int y1, int x2, int y2, Color32 color, BlitFlags blitFlags)
		{
			int dx = Math.Abs(x2 - x1);
			int sx = x1 < x2 ? 1 : -1;
			int dy = -Math.Abs(y2 - y1);
			int sy = y1 < y2 ? Width : -Width;
			int index = x1 + y1 * Width;
			int end = x2 + y2 * Width;

			int err = dx + dy;

			if ((blitFlags & BlitFlags.ModeMask) != BlitFlags.Copy)
			{
				if (color.A == 0)
					return;
				if (color.A == 255 || (blitFlags & BlitFlags.ModeMask) == BlitFlags.Transparent)
					blitFlags = blitFlags & ~BlitFlags.ModeMask | BlitFlags.Copy;
			}

			if ((blitFlags & BlitFlags.SkipStart) != 0)
			{
				if (index == end)
					return;

				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; index += sx; }
				if (e2 <= dx) { err += dx; index += sy; }
			}

			unsafe
			{
				switch (blitFlags & BlitFlags.ModeMask)
				{
					case BlitFlags.Copy:
						while (true)
						{
							Data[index] = color;

							if (index == end)
								break;

							int e2 = 2 * err;
							if (e2 >= dy) { err += dy; index += sx; }
							if (e2 <= dx) { err += dx; index += sy; }
						}
						break;

					case BlitFlags.Alpha:
						while (true)
						{
							Color32 d = Data[index];
							uint a = color.A, ia = (uint)(255 - color.A);
							uint r = color.R * a + d.R * ia + 127;
							uint g = color.G * a + d.G * ia + 127;
							uint b = color.B * a + d.B * ia + 127;
							r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
							g = (g + 1 + (g >> 8)) >> 8;
							b = (b + 1 + (b >> 8)) >> 8;
							Data[index] = new Color32((byte)r, (byte)g, (byte)b, (byte)255);

							if (index == end)
								break;

							int e2 = 2 * err;
							if (e2 >= dy) { err += dy; index += sx; }
							if (e2 <= dx) { err += dx; index += sy; }
						}
						break;

					case BlitFlags.PMAlpha:
						while (true)
						{
							Color32 d = Data[index];
							uint ia = (uint)(255 - color.A);
							uint r = d.R * ia + 127;
							uint g = d.G * ia + 127;
							uint b = d.B * ia + 127;
							r = color.R + ((r + 1 + (r >> 8)) >> 8);
							g = color.G + ((g + 1 + (g >> 8)) >> 8);
							b = color.B + ((b + 1 + (b >> 8)) >> 8);
							Data[index] = new Color32((byte)r, (byte)g, (byte)b, (byte)255);

							if (index == end)
								break;

							int e2 = 2 * err;
							if (e2 >= dy) { err += dy; index += sx; }
							if (e2 <= dx) { err += dx; index += sy; }
						}
						break;
				}
			}
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
		public void DrawThickLine(int x1, int y1, int x2, int y2, double thickness,
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawThickLine(Vector2i start, Vector2i end, double thickness,
			Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
			=> DrawThickLine(new Vector2d(start.X + 0.5, start.Y + 0.5), new Vector2d(end.X + 0.5, end.Y + 0.5),
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
		public void DrawThickLine(Vector2d start, Vector2d end, double thickness,
			Color32 color, BlitFlags blitFlags = BlitFlags.Copy)
		{
			Span<Vector2d> points = stackalloc Vector2d[4];

			Vector2d unit = end - start;
			unit.Normalize();

			Vector2d perpUnit = new Vector2d(-unit.Y, unit.X);
			double halfThickness = thickness * 0.5;
			Vector2d offset = perpUnit * halfThickness;

			points[0] = start + offset;
			points[1] = end + offset;
			points[2] = end - offset;
			points[3] = start - offset;

			FillPolygon(points, color, blitFlags);
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
		public void FillPolygon(IEnumerable<Vector2> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2d[] hardenedPoints = points.Select(p => (Vector2d)p).ToArray();
			FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		public void FillPolygon(IEnumerable<Vector2d> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2d[] hardenedPoints = points is Vector2d[] array ? array : points.ToArray();
			FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		public void FillPolygon(IEnumerable<Vector2i> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			Vector2d[] hardenedPoints = points.Select(p => (Vector2d)p).ToArray();
			FillPolygon(hardenedPoints.AsSpan(), color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			// Make a space to promote the points up to doubles.
			Span<Vector2d> doublePoints = points.Length < 64
				? stackalloc Vector2d[points.Length]
				: new Vector2d[points.Length];

			// Copy them over, promoting them.
			for (int i = points.Length - 1; i >= 0; i--)
				doublePoints[i] = points[i];

			// Use the result.
			FillPolygon(doublePoints, color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2i> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			// Make a space to promote the points up to doubles.
			Span<Vector2d> doublePoints = points.Length < 64
				? stackalloc Vector2d[points.Length]
				: new Vector2d[points.Length];

			// Copy them over, promoting them.
			for (int i = points.Length - 1; i >= 0; i--)
				doublePoints[i] = points[i];

			// Use the result.
			FillPolygon(doublePoints, color, blitFlags);
		}

		/// <summary>
		/// Fill a polygon using the odd/even technique.  The polygon will be
		/// clipped to the image if it exceeds the image's boundary.
		/// </summary>
		/// <param name="points">The points describing the polygon.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2d> points, Color32 color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			BlitFlags drawMode = 0;
			switch (blitFlags & BlitFlags.ModeMask)
			{
				default:
				case BlitFlags.Copy:
					drawMode = BlitFlags.Copy;
					break;

				case BlitFlags.Transparent:
					if (color.A == 0)
						return;
					drawMode = BlitFlags.Copy;
					break;

				case BlitFlags.Alpha:
					if (color.A == 0)
						return;
					drawMode = color.A == 255 ? BlitFlags.Copy : BlitFlags.Alpha;
					break;

				case BlitFlags.PMAlpha:
					if (color.A == 0)
						return;
					drawMode = color.A == 255 ? BlitFlags.Copy : BlitFlags.PMAlpha;
					break;
			}

			int numXes;
			Span<double> xes = points.Length < 128
				? stackalloc double[points.Length + 2]
				: new double[points.Length + 2];

			// Find the polygon's vertical extent, and clamp it to the image boundary.
			double minY = double.PositiveInfinity;
			double maxY = double.NegativeInfinity;
			foreach (Vector2 point in points)
			{
				minY = Math.Min(minY, point.Y);
				maxY = Math.Max(maxY, point.Y);
			}

			// Clamp to the extent of the image.
			int iMinY = (int)Math.Min(Math.Max(minY, 0), Height + 1);
			int iMaxY = (int)(Math.Min(Math.Max(minY, 0), Height + 1) + 1f);

			// Loop through the rows of the image.
			for (int y = iMinY; y < iMaxY; y++)
			{
				// Build a list of nodes on this row.
				double dy = y + 0.5;
				numXes = 0;
				Vector2d prev = points[points.Length - 1];
				for (int i = 0; i < points.Length; i++)
				{
					Vector2d current = points[i];
					if (current.Y < dy && dy <= prev.Y || prev.Y < dy && dy <= current.Y)
					{
						xes[numXes++] = current.X
							+ (dy - current.Y) / (prev.Y - current.Y) * (prev.X - current.X);
					}
					prev = current;
				}
				Span<double> usedXes = xes.Slice(0, numXes);

				// Sort the X coordinates in ascending order.
#if NET5_0_OR_GREATER
				usedXes.Sort();
#else
				ArraySortHelper<double, double>.IntrospectiveSort(usedXes, usedXes, Comparer<double>.Default);
#endif

				// Fill the pixels between edge pairs.
				FillSpansFast(y, usedXes, color, drawMode);
			}
		}

		/// <summary>
		/// Given the coordinates of a set of spans to draw, all with the same Y coordinate,
		/// draw them as fast as possible.  The X coordinates of the spans will be clipped
		/// to the image.
		/// </summary>
		/// <param name="y">The Y coordinate of the spans.</param>
		/// <param name="points">The points that define the spans, in pairs of fractional
		/// start/end X coordinates for each span.</param>
		/// <param name="color">The color to fill the polygon with.</param>
		/// <param name="drawMode">Which mode to use to draw the color:  This method
		/// works in Copy, Transparent, Alpha, or PMAlpha modes.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FillSpansFast(int y, ReadOnlySpan<double> points, Color32 color,
			BlitFlags drawMode = BlitFlags.Copy)
		{
			unsafe
			{
				fixed (Color32* destBase = Data)
				{
					Color32* row = destBase + Width * y;

					switch (drawMode)
					{
						case BlitFlags.Copy:
							for (int i = 0; i < points.Length; i += 2)
							{
								if (points[i] >= Width)
									break;
								if (points[i + 1] <= 0)
									continue;
								int start = Math.Max((int)(points[i] + 0.5), 0);
								int end = Math.Min((int)(points[i + 1] + 0.5) + 1, Width - 1);
								int count = end - start;
								Color32* dest = row + start;
								while (count >= 8)
								{
									dest[0] = color;
									dest[1] = color;
									dest[2] = color;
									dest[3] = color;
									dest[4] = color;
									dest[5] = color;
									dest[6] = color;
									dest[7] = color;
									dest += 8;
									count -= 8;
								}
								if ((count & 4) != 0)
								{
									dest[0] = color;
									dest[1] = color;
									dest[2] = color;
									dest[3] = color;
									dest += 4;
								}
								if ((count & 2) != 0)
								{
									dest[0] = color;
									dest[1] = color;
									dest += 2;
								}
								if ((count & 1) != 0)
									dest[0] = color;
							}
							break;

						case BlitFlags.Alpha:
							for (int i = 0; i < points.Length; i += 2)
							{
								if (points[i] >= Width)
									break;
								if (points[i + 1] <= 0)
									continue;
								int start = Math.Max((int)(points[i] + 0.5), 0);
								int end = Math.Min((int)(points[i + 1] + 0.5) + 1, Width - 1);
								int count = end - start;
								Color32* dest = row + start;
								while (count > 0)
								{
									Color32 d = *dest;
									uint a = color.A, ia = (uint)(255 - color.A);
									uint r = color.R * a + d.R * ia + 127;
									uint g = color.G * a + d.G * ia + 127;
									uint b = color.B * a + d.B * ia + 127;
									r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
									g = (g + 1 + (g >> 8)) >> 8;
									b = (b + 1 + (b >> 8)) >> 8;
									*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
									count--;
								}
							}
							break;

						case BlitFlags.PMAlpha:
							for (int i = 0; i < points.Length; i += 2)
							{
								if (points[i] >= Width)
									break;
								if (points[i + 1] <= 0)
									continue;
								int start = Math.Max((int)(points[i] + 0.5), 0);
								int end = Math.Min((int)(points[i + 1] + 0.5) + 1, Width - 1);
								int count = end - start;
								Color32* dest = row + start;
								while (count > 0)
								{
									Color32 d = *dest;
									uint ia = (uint)(255 - color.A);
									uint r = color.R + d.R * ia + 127;
									uint g = color.G + d.G * ia + 127;
									uint b = color.B + d.B * ia + 127;
									r = (r + 1 + (r >> 8)) >> 8;    // Divide by 255, faster than "r /= 255"
									g = (g + 1 + (g >> 8)) >> 8;
									b = (b + 1 + (b >> 8)) >> 8;
									*dest++ = new Color32((byte)r, (byte)g, (byte)b, (byte)255);
									count--;
								}
							}
							break;
					}
				}
			}
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
		public void DrawBezier(Vector2 p1, Vector2 c1, Vector2 c2, Vector2 p2,
			Color32 color, int steps = 0, BlitFlags blitFlags = BlitFlags.Copy)
		{
			if (steps <= 0)
			{
				double d1 = (c1 - p1).Length;
				double d2 = (c2 - c1).Length;
				double d3 = (p2 - c2).Length;
				steps = Math.Max((int)((d1 + d2 + d3) * 0.25f), 20);
			}

			double ooSteps = 1.0 / steps;

			int lastX = (int)(p1.X + 0.5f), lastY = (int)(p1.Y + 0.5f);

			// Plot the first point.
			if ((blitFlags & BlitFlags.SkipStart) == 0)
				DrawLine(lastX, lastY, lastX, lastY, color, blitFlags);

#if NETCOREAPP
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
			[Pure]
			static Vector2 CalcBezier(Vector2 start, Vector2 c1, Vector2 c2, Vector2 end, double t)
			{
				double it = 1 - t;
				double it2 = it * it;
				double t2 = (double)t * t;

				double a = it * it2;
				double b = 3 * it2 * t;
				double c = 3 * it * t2;
				double d = t2 * t;

				double x = a * start.X + b * c1.X + c * c2.X + d * end.X;
				double y = a * start.Y + b * c1.Y + c * c2.Y + d * end.Y;

				return new Vector2((float)x, (float)y);
			}

			// Draw successive line segments, skipping the start of each line segment.
			blitFlags |= BlitFlags.SkipStart;
			for (int i = 1; i <= steps; i++)
			{
				Vector2 p = CalcBezier(p1, c1, c2, p2, i * ooSteps);

				int ix = (int)(p.X + 0.5f), iy = (int)(p.Y + 0.5f);

				if (ix != lastX || iy != lastY)
					DrawLine(lastX, lastY, ix, iy, color, blitFlags);

				lastX = ix;
				lastY = iy;
			}
		}

		#endregion

		#region Convolutions

		/// <summary>
		/// Apply a 3x3 emboss convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where emboss has been applied.</returns>
		public Image32 Emboss(double strength = 1.0)
			=> Convolve3x3(_embossKernel, strength);

		private readonly double[] _embossKernel = new double[]
		{
			-2, -1, 0,
			-1,  1, 1,
			 0,  1, 2
		};

		/// <summary>
		/// Apply a 3x3 sharpening convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where sharpening has been applied.</returns>
		public Image32 Sharpen(double strength = 1.0)
			=> Convolve3x3(_sharpenKernel, strength);

		private readonly double[] _sharpenKernel = new double[]
		{
			 0, -1,  0,
			-1,  5, -1,
			 0, -1,  0
		};

		/// <summary>
		/// Apply a 3x3 edge-detection convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <param name="includeDiagonals">Whether to use the version of the kernel that includes diagonals.</param>
		/// <returns>A new image where edge-detection has been applied.</returns>
		public Image32 EdgeDetect(double strength = 1.0, bool includeDiagonals = false)
			=> Convolve3x3(includeDiagonals ? _edgeDetectionDiagonalKernel : _edgeDetectionKernel, strength);

		private readonly double[] _edgeDetectionKernel = new double[]
		{
			 0, -1,  0,
			-1,  4, -1,
			 0, -1,  0
		};

		private readonly double[] _edgeDetectionDiagonalKernel = new double[]
		{
			-1, -1, -1,
			-1,  8, -1,
			-1, -1, -1
		};

		/// <summary>
		/// Apply a simple 3x3 box-blur convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where box blur has been applied.</returns>
		public Image32 BoxBlur(double strength = 1.0)
			=> Convolve3x3(_boxBlurKernel, strength, true);

		private readonly double[] _boxBlurKernel = new double[]
		{
			1, 1, 1,
			1, 1, 1,
			1, 1, 1,
		};

		/// <summary>
		/// Apply an approximate 3x3 Gaussian-blur convolution kernel.
		/// </summary>
		/// <param name="strength">How strong to apply the kernel, where 0.0 is not
		/// at all, and 1.0 is the kernel as given.</param>
		/// <returns>A new image where an approximate 3x3 Gaussian blur has been applied.</returns>
		public Image32 ApproximateGaussianBlurFast(double strength = 1.0)
			=> Convolve3x3(_gaussianBlurKernel, strength, true);

		private readonly double[] _gaussianBlurKernel = new double[]
		{
			1, 2, 1,
			2, 4, 2,
			1, 2, 1,
		};

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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Image32 Convolve3x3(double[] kernel, double strength = 1.0, bool divideBySum = false)
		{
			if (kernel == null || kernel.Length != 9)
				throw new ArgumentException("The provided 3x3 convolution kernel must contain exactly 9 values.");

			Span<double> scaledKernel = stackalloc double[9];

			scaledKernel[0] = kernel[0] * strength;
			scaledKernel[1] = kernel[1] * strength;
			scaledKernel[2] = kernel[2] * strength;
			scaledKernel[3] = kernel[3] * strength;
			scaledKernel[4] = kernel[4] * strength + (1.0 - strength);
			scaledKernel[5] = kernel[5] * strength;
			scaledKernel[6] = kernel[6] * strength;
			scaledKernel[7] = kernel[7] * strength;
			scaledKernel[8] = kernel[8] * strength;

			Image32 result = new Image32(Size);

#if NETCOREAPP
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
			static byte Clamp(double result, double scale)
				=> (byte)Math.Max(Math.Min((int)(result * scale + 0.5), 255), 0);

			unsafe
			{
				fixed (double* k = scaledKernel)
				fixed (Color32* destBase = result.Data)
				fixed (Color32* srcBase = Data)
				{
					byte* dest = (byte*)destBase;
					byte* src = (byte*)srcBase;

					double d;
					double m;

					int w = Width * 4;

					int y = 0;

					// Special case: Single-row image.
					if (Height == 1)
					{
						// Super-special case of a 1x1 image.
						if (Width == 1)
						{
							m = divideBySum ? 1.0 / ((d = k[4]) != 0 ? d : 1.0) : 1.0;
							dest[0] = Clamp(src[0] * k[4], m); // Red
							dest[1] = Clamp(src[1] * k[4], m); // Green
							dest[2] = Clamp(src[2] * k[4], m); // Blue
							dest[3] = src[3]; // Alpha
							return result;
						}

						// First pixel in the row.
						m = divideBySum ? 1.0 / ((d = k[4] + k[5]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(src[+0] * k[4] + src[+4] * k[5], m); // Red
						dest[1] = Clamp(src[+1] * k[4] + src[+5] * k[5], m); // Green
						dest[2] = Clamp(src[+2] * k[4] + src[+6] * k[5], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;

						// Middle of the row.
						m = divideBySum ? 1.0 / ((d = k[3] + k[4] + k[5]) != 0 ? d : 1.0) : 1.0;
						for (int x = 1; x < Width - 1; x++)
						{
							dest[0] = Clamp(src[-4] * k[3] + src[+0] * k[4] + src[+4] * k[5], m); // Red
							dest[1] = Clamp(src[-3] * k[3] + src[+1] * k[4] + src[+5] * k[5], m); // Green
							dest[2] = Clamp(src[-2] * k[3] + src[+2] * k[4] + src[+6] * k[5], m); // Blue
							dest[3] = src[+3]; // Alpha
							dest += 4;
							src += 4;
						}

						// Last pixel in the row.
						m = divideBySum ? 1.0 / ((d = k[3] + k[4]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(src[-4] * k[3] + src[+0] * k[4], m); // Red
						dest[1] = Clamp(src[-3] * k[3] + src[+1] * k[4], m); // Green
						dest[2] = Clamp(src[-2] * k[3] + src[+2] * k[4], m); // Blue
						dest[3] = src[+3]; // Alpha

						return result;
					}
					else if (Width == 1)
					{
						// Single-column image.

						// First pixel in the column.
						m = divideBySum ? 1.0 / ((d = k[4] + k[7]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(src[+0] * k[4] + src[+w+0] * k[7], m); // Red
						dest[1] = Clamp(src[+1] * k[4] + src[+w+1] * k[7], m); // Green
						dest[2] = Clamp(src[+2] * k[4] + src[+w+2] * k[7], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;

						// Middle of the column.
						m = divideBySum ? 1.0 / ((d = k[1] + k[4] + k[7]) != 0 ? d : 1.0) : 1.0;
						for (y = 1; y < Height - 1; y++)
						{
							dest[0] = Clamp(src[-w+0] * k[1] + src[+0] * k[4] + src[+w+0] * k[7], m); // Red
							dest[1] = Clamp(src[-w+1] * k[1] + src[+1] * k[4] + src[+w+1] * k[7], m); // Green
							dest[2] = Clamp(src[-w+2] * k[1] + src[+2] * k[4] + src[+w+2] * k[7], m); // Blue
							dest[3] = src[+3]; // Alpha
							dest += 4;
							src += 4;
						}

						// Last pixel in the column.
						m = divideBySum ? 1.0 / ((d = k[1] + k[4]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(src[-w+0] * k[1] + src[+0] * k[4], m); // Red
						dest[1] = Clamp(src[-w+1] * k[1] + src[+1] * k[4], m); // Green
						dest[2] = Clamp(src[-w+2] * k[1] + src[+2] * k[4], m); // Blue
						dest[3] = src[+3]; // Alpha

						return result;
					}

					// Normal case of a 2x2 image or larger.

					// First row.
					{
						// First pixel in the first row.
						m = divideBySum ? 1.0 / ((d = k[4] + k[5] + k[7] + k[8]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(  src[  +0] * k[4] + src[  +4] * k[5]
						                + src[+w+0] * k[7] + src[+w+4] * k[8], m); // Red
						dest[1] = Clamp(  src[  +1] * k[4] + src[  +5] * k[5]
						                + src[+w+1] * k[7] + src[+w+5] * k[8], m); // Green
						dest[2] = Clamp(  src[  +2] * k[4] + src[  +6] * k[5]
						                + src[+w+2] * k[7] + src[+w+6] * k[8], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;

						// Middle of the first row.
						m = divideBySum ? 1.0 / ((d = k[3] + k[4] + k[5] + k[6] + k[7] + k[8]) != 0 ? d : 1.0) : 1.0;
						for (int x = 1; x < Width - 1; x++)
						{
							dest[0] = Clamp(  src[  -4] * k[3] + src[  +0] * k[4] + src[  +4] * k[5]
							                + src[+w-4] * k[6] + src[+w+0] * k[7] + src[+w+4] * k[8], m); // Red
							dest[1] = Clamp(  src[  -3] * k[3] + src[  +1] * k[4] + src[  +5] * k[5]
							                + src[+w-3] * k[6] + src[+w+1] * k[7] + src[+w+5] * k[8], m); // Green
							dest[2] = Clamp(  src[  -2] * k[3] + src[  +2] * k[4] + src[  +6] * k[5]
							                + src[+w-2] * k[6] + src[+w+2] * k[7] + src[+w+6] * k[8], m); // Blue
							dest[3] = src[+3]; // Alpha
							dest += 4;
							src += 4;
						}

						// Last pixel of the first row.
						m = divideBySum ? 1.0 / ((d = k[3] + k[4] + k[6] + k[7]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(  src[  -4] * k[3] + src[  +0] * k[4]
						                + src[+w-4] * k[6] + src[+w+0] * k[7], m); // Red
						dest[1] = Clamp(  src[  -3] * k[3] + src[  +1] * k[4]
						                + src[+w-3] * k[6] + src[+w+1] * k[7], m); // Green
						dest[2] = Clamp(  src[  -2] * k[3] + src[  +2] * k[4]
						                + src[+w-2] * k[6] + src[+w+2] * k[7], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;
					}

					// Every row in between.
					double ms = divideBySum ? 1.0 / ((d = k[1] + k[2] + k[4] + k[5] + k[7] + k[8]) != 0 ? d : 1.0) : 1.0;
					double mm = divideBySum ? 1.0 / ((d = k[0] + k[1] + k[2] + k[3] + k[4] + k[5] + k[6] + k[7] + k[8]) != 0 ? d : 1.0) : 1.0;
					double me = divideBySum ? 1.0 / ((d = k[0] + k[1] + k[3] + k[4] + k[6] + k[7]) != 0 ? d : 1.0) : 1.0;
					for (y = 1; y < Height - 1; y++)
					{
						// First pixel in the row.
						dest[0] = Clamp(  src[-w+0] * k[1] + src[-w+4] * k[2]
						                + src[  +0] * k[4] + src[  +4] * k[5]
						                + src[+w+0] * k[7] + src[+w+4] * k[8], ms); // Red
						dest[1] = Clamp(  src[-w+1] * k[1] + src[-w+5] * k[2]
						                + src[  +1] * k[4] + src[  +5] * k[5]
						                + src[+w+1] * k[7] + src[+w+5] * k[8], ms); // Green
						dest[2] = Clamp(  src[-w+2] * k[1] + src[-w+6] * k[2]
						                + src[  +2] * k[4] + src[  +6] * k[5]
						                + src[+w+2] * k[7] + src[+w+6] * k[8], ms); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;

						for (int x = 1; x < Width - 1; x++)
						{
							// General case.  This is the case that runs the most and doesn't have
							// any boundary conditions.  It's also the case that does the most math,
							// as it has to apply the entire convolution kernel.
							dest[0] = Clamp(  src[-w-4] * k[0] + src[-w+0] * k[1] + src[-w+4] * k[2]
							                + src[  -4] * k[3] + src[  +0] * k[4] + src[  +4] * k[5]
							                + src[+w-4] * k[6] + src[+w+0] * k[7] + src[+w+4] * k[8], mm); // Red
							dest[1] = Clamp(  src[-w-3] * k[0] + src[-w+1] * k[1] + src[-w+5] * k[2]
							                + src[  -3] * k[3] + src[  +1] * k[4] + src[  +5] * k[5]
							                + src[+w-3] * k[6] + src[+w+1] * k[7] + src[+w+5] * k[8], mm); // Green
							dest[2] = Clamp(  src[-w-2] * k[0] + src[-w+2] * k[1] + src[-w+6] * k[2]
							                + src[  -2] * k[3] + src[  +2] * k[4] + src[  +6] * k[5]
							                + src[+w-2] * k[6] + src[+w+2] * k[7] + src[+w+6] * k[8], mm); // Blue
							dest[3] = src[+3]; // Alpha
							dest += 4;
							src += 4;
						}

						// Last pixel in the row.
						dest[0] = Clamp(  src[-w-4] * k[0] + src[-w+0] * k[1]
						                + src[  -4] * k[3] + src[  +0] * k[4]
						                + src[+w-4] * k[6] + src[+w+0] * k[7], me); // Red
						dest[1] = Clamp(  src[-w-3] * k[0] + src[-w+1] * k[1]
						                + src[  -3] * k[3] + src[  +1] * k[4]
						                + src[+w-3] * k[6] + src[+w+1] * k[7], me); // Green
						dest[2] = Clamp(  src[-w-2] * k[0] + src[-w+2] * k[1]
						                + src[  -2] * k[3] + src[  +2] * k[4]
						                + src[+w-2] * k[6] + src[+w+2] * k[7], me); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;
					}

					// Last row.
					{
						// First pixel in the last row.
						m = divideBySum ? 1.0 / ((d = k[1] + k[2] + k[4] + k[5]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(  src[-w+0] * k[1] + src[-w+4] * k[2]
						                + src[  +0] * k[4] + src[  +4] * k[5], m); // Red
						dest[1] = Clamp(  src[-w+1] * k[1] + src[-w+5] * k[2]
						                + src[  +1] * k[4] + src[  +5] * k[5], m); // Green
						dest[2] = Clamp(  src[-w+2] * k[1] + src[-w+6] * k[2]
						                + src[  +2] * k[4] + src[  +6] * k[5], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;

						// Middle of the last row.
						m = divideBySum ? 1.0 / ((d = k[0] + k[1] + k[2] + k[3] + k[4] + k[5]) != 0 ? d : 1.0) : 1.0;
						for (int x = 1; x < Width - 1; x++)
						{
							dest[0] = Clamp(  src[-w-4] * k[0] + src[-w+0] * k[1] + src[-w+4] * k[2]
							                + src[  -4] * k[3] + src[  +0] * k[4] + src[  +4] * k[5], m); // Red
							dest[1] = Clamp(  src[-w-3] * k[0] + src[-w+1] * k[1] + src[-w+5] * k[2]
							                + src[  -3] * k[3] + src[  +1] * k[4] + src[  +5] * k[5], m); // Green
							dest[2] = Clamp(  src[-w-2] * k[0] + src[-w+2] * k[1] + src[-w+6] * k[2]
							                + src[  -2] * k[3] + src[  +2] * k[4] + src[  +6] * k[5], m); // Blue
							dest[3] = src[+3]; // Alpha
							dest += 4;
							src += 4;
						}

						// Last pixel in the last row.
						m = divideBySum ? 1.0 / ((d = k[0] + k[1] + k[3] + k[4]) != 0 ? d : 1.0) : 1.0;
						dest[0] = Clamp(  src[-w-4] * k[0] + src[-w+0] * k[1]
						                + src[  -4] * k[3] + src[  +0] * k[4], m); // Red
						dest[1] = Clamp(  src[-w-3] * k[0] + src[-w+1] * k[1]
						                + src[  -3] * k[3] + src[  +1] * k[4], m); // Green
						dest[2] = Clamp(  src[-w-2] * k[0] + src[-w+2] * k[1]
						                + src[  -2] * k[3] + src[  +2] * k[4], m); // Blue
						dest[3] = src[+3]; // Alpha
						dest += 4;
						src += 4;
					}
				}
			}

			return result;
		}

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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Image32 ConvolveHorz(ReadOnlySpan<double> kernel, double strength = 1.0, bool divideBySum = false)
			=> Convolve(kernel, kernel.Length, 1, strength, divideBySum);

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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Image32 ConvolveVert(ReadOnlySpan<double> kernel, double strength = 1.0, bool divideBySum = false)
			=> Convolve(kernel, 1, kernel.Length, strength, divideBySum);

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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Image32 Convolve(ReadOnlySpan<double> kernel, int kernelWidth, int kernelHeight,
			double strength = 1.0, bool divideBySum = false)
		{
			if (kernelWidth < 1 || (kernelWidth & 1) != 1 || kernelWidth >= 32768)
				throw new ArgumentException("Convolution kernel width must be an odd number >= 1 and <= 32767.");
			if (kernelHeight < 1 || (kernelHeight & 1) != 1 || kernelHeight >= 32768)
				throw new ArgumentException("Convolution kernel height must be an odd number >= 1 and <= 32767.");

			int kernelSize = kernelWidth * kernelHeight;
			if (kernel.Length < kernelSize)
				throw new ArgumentException($"Convolution kernel must have at least {kernelSize} elements if it is {kernelWidth}x{kernelHeight}.");

			int halfWidth = kernelWidth / 2;
			int halfHeight = kernelHeight / 2;
			int kernelCenter = halfHeight * kernelWidth + halfWidth;

			Span<double> scaledKernel = kernelSize < 256
				? stackalloc double[kernelSize]
				: new double[kernelSize];

			for (int i = 0; i < kernelSize; i++)
				scaledKernel[i] = kernel[i] * strength;

			scaledKernel[kernelCenter] = kernel[kernelCenter] + (1.0 - strength);

			Image32 result = new Image32(Width, Height);

			unsafe
			{
				fixed (double* kernelBase = scaledKernel)
				fixed (Color32* srcBase = Data)
				fixed (Color32* destBase = result.Data)
				{
					Color32* src = srcBase;
					Color32* dest = destBase;
					for (int y = 0; y < Height; y++)
					{
						for (int x = 0; x < Width; x++)
						{
							int sy = Math.Max(y - halfWidth, 0), ey = Math.Min(y + halfWidth, Height - 1);
							int sx = Math.Max(x - halfHeight, 0), ex = Math.Min(x + halfHeight, Width - 1);

							int cols = ex - sx + 1;
							int rows = ey - sy + 1;
							int koffset = (sy - y) * kernelWidth + (sx - x);
							Color32* localSrc = src - kernelCenter + (sx - x);
							double* localKernel = kernelBase + (sx - x);

							double r = 0, g = 0, b = 0, a = 0;

							for (int dy = 0; dy < rows; dy++)
							{
								for (int dx = 0; dx < cols; dx++)
								{
									double k = localKernel[dx];
									Color32 c = localSrc[dx];
									r += c.R * k;
									g += c.G * k;
									b += c.B * k;
									a += c.A * k;
								}
								localKernel += kernelWidth;
								localSrc += Width;
							}

							if (divideBySum)
							{
								double ooSum = 1.0 / (rows * cols);
								r *= ooSum;
								g *= ooSum;
								b *= ooSum;
								a *= ooSum;
							}

							*dest++ = new Color32(
								(int)(r + 0.5),
								(int)(g + 0.5),
								(int)(b + 0.5),
								(int)(a + 0.5)
							);
						}
					}
				}

				return result;
			}
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
		/// any opacity at all will be considered non-transparent.</param>
		/// <returns>The smallest width that surrounds actual non-transparent content
		/// within the given rectangle, which may be the original width.</returns>
		[Pure]
		public int MeasureContentWidth(Rect rect, byte cutoff = 0)
		{
			for (int width = rect.Width; width > 0; width--)
			{
				if (!IsColumnTransparent(rect.X + width - 1, rect.Y, rect.Height, cutoff))
					return width;
			}
			return 0;
		}

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
		{
			for (int height = rect.Height; height > 0; height--)
			{
				if (!IsRowTransparent(rect.X, rect.Y + height - 1, rect.Width, cutoff))
					return height;
			}
			return 0;
		}

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
		{
			int x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height;
			if (!ClipRect(Size, ref x, ref y, ref width, ref height))
				return true;

			unsafe
			{
				fixed (Color32* basePtr = Data)
				{
					Color32* src = basePtr + y * Width + x;
					int leftover = Width - width;
					for (int i = 0; i < height; i++)
					{
						for (int j = 0; j < width; j++)
						{
							if (src->A > cutoff)
								return false;
							src++;
						}
						src += leftover;
					}
				}
			}

			return true;
		}

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
		{
			if (y < 0 || y >= Height)
				return true;
			if (x < 0)
			{
				width += x;
				x = 0;
			}
			if (x + width > Width)
				width = Width - x;
			if (width <= 0)
				return true;

			unsafe
			{
				fixed (Color32* basePtr = Data)
				{
					Color32* src = basePtr + y * Width + x;
					for (int i = 0; i < width; i++)
					{
						if (src->A > cutoff)
							return false;
						src++;
					}
				}
			}

			return true;
		}

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
		{
			if (x < 0 || x >= Width)
				return true;
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (y + height > Height)
				height = Height - y;
			if (height <= 0)
				return true;

			unsafe
			{
				fixed (Color32* basePtr = Data)
				{
					Color32* src = basePtr + y * Width + x;
					for (int i = 0; i < height; i++)
					{
						if (src->A > cutoff)
							return false;
						src += Width;
					}
				}
			}

			return true;
		}

		#endregion

		#region Color subset testing

		/// <summary>
		/// Determine if this is a grayscale image.  For it to be grayscale, every pixel
		/// entry must have R = G = B.
		/// </summary>
		/// <returns>True if this is a grayscale image.</returns>
		public bool IsGrayscale()
		{
			foreach (Color32 color in Data)
			{
				if (color.R != color.G || color.R != color.B)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if this is a single-channel image, and if so, which color channel.
		/// This ignores the alpha component of each pixel.
		/// </summary>
		/// <returns>The single color channel used by this image (Red, Green, or Blue);
		/// or None if this image uses more than one color channel.</returns>
		public ColorChannel IsSingleChannel()
		{
			byte r = 0, g = 0, b = 0;

			foreach (Color32 color in Data)
			{
				r |= color.R;
				g |= color.G;
				b |= color.B;
			}

			if ((g | b) == 0 && r != 0)
				return ColorChannel.Red;
			if ((r | b) == 0 && g != 0)
				return ColorChannel.Green;
			if ((r | g) == 0 && b != 0)
				return ColorChannel.Blue;

			return ColorChannel.None;
		}

		#endregion

		#region Equality and hash codes

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel identical.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if the other object is an identical image to this image, false otherwise.</returns>
		[Pure]
		public override bool Equals(object? obj)
			=> obj is Image32 other && Equals(other)
				|| obj is PureImage32 other2 && other2.Equals(this);

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel
		/// identical.  This runs in O(n) worst-case time, but that's only hit if the
		/// two images have identical dimensions:  For images with unequal dimensions,
		/// this always runs in O(1) time.
		/// </summary>
		/// <param name="other">The other image to compare against.</param>
		/// <returns>True if the other image is an identical image to this image, false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public bool Equals(PureImage32 other)
			=> other.Equals(this);

		/// <summary>
		/// Compare this image against another to determine if they're pixel-for-pixel
		/// identical.  This runs in O(n) worst-case time, but that's only hit if the
		/// two images have identical dimensions:  For images with unequal dimensions,
		/// this always runs in O(1) time.
		/// </summary>
		/// <param name="other">The other image to compare against.</param>
		/// <returns>True if the other image is an identical image to this image, false otherwise.</returns>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		[Pure]
		public unsafe bool Equals(Image32? other)
		{
			if (ReferenceEquals(this, other))
				return true;
			if (ReferenceEquals(other, null))
				return false;

			if (Width != other.Width
				|| Height != other.Height
				|| Data.Length != other.Data.Length)
				return false;

			fixed (Color32* dataBase = Data)
			fixed (Color32* otherDataBase = other.Data)
			{
				uint* data = (uint*)dataBase;
				uint* otherData = (uint*)otherDataBase;
				int count = Data.Length;
				while (count >= 8)
				{
					if (data[0] != otherData[0]) return false;
					if (data[1] != otherData[1]) return false;
					if (data[2] != otherData[2]) return false;
					if (data[3] != otherData[3]) return false;
					if (data[4] != otherData[4]) return false;
					if (data[5] != otherData[5]) return false;
					if (data[6] != otherData[6]) return false;
					if (data[7] != otherData[7]) return false;
					data += 8;
					otherData += 8;
					count -= 8;
				}
				if ((count & 4) != 0)
				{
					if (data[0] != otherData[0]) return false;
					if (data[1] != otherData[1]) return false;
					if (data[2] != otherData[2]) return false;
					if (data[3] != otherData[3]) return false;
					data += 4;
					otherData += 4;
				}
				if ((count & 2) != 0)
				{
					if (data[0] != otherData[0]) return false;
					if (data[1] != otherData[1]) return false;
					data += 2;
					otherData += 2;
				}
				if ((count & 1) != 0)
				{
					if (data[0] != otherData[0]) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>True if the images are identical, false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static bool operator ==(Image32? a, Image32? b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);

		/// <summary>
		/// Compare two images for equality.  This will perform a pixel-for-pixel test
		/// if necessary; it is not just a reference-equality test.
		/// </summary>
		/// <param name="a">The first image to compare.</param>
		/// <param name="b">The second image to compare.</param>
		/// <returns>False if the images are identical, true otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static bool operator !=(Image32? a, Image32? b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		/// <summary>
		/// Calculate a hash code representing the pixels of this image.  This runs
		/// in O(width*height) time and does not cache its result, so don't invoke this
		/// unless you need it.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		[Pure]
		public unsafe override int GetHashCode()
		{
			fixed (Color32* dataBase = Data)
			{
				uint hashCode = 0;

				uint* data = (uint*)dataBase;
				int count = Data.Length;
				while (count-- != 0)
				{
					hashCode = unchecked(hashCode * 65599 + *data++);
				}

				return unchecked((int)hashCode);
			}
		}

		#endregion
	}
}
