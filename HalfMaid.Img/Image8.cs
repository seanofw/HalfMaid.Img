using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HalfMaid.Img.FileFormats;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// An image abstraction:  A rectangular grid of colors, each color represented
	/// as a single byte, with methods to manipulate it.  This includes a color table
	/// (a palette) that can be used to translate each byte into an RGBA color; however,
	/// most operations on this class do not interact with the color table.
	/// </summary>
	[DebuggerDisplay("Image8 {Width}x{Height}")]
	public class Image8 : IImage<byte>
	{
		#region Core properties and fields

		/// <summary>
		/// The actual data of this image, which is always guaranteed to be
		/// a one-dimensional contiguous array of 8-bit values, in order of
		/// top-to-bottom, left-to-right, with no gaps or padding.  (i.e., the length
		/// of the data will always equal Width*Height.)  This is publicly exposed so
		/// that it can be read or written directly, or even pinned and manipulated
		/// using pointers.
		/// </summary>
		public byte[] Data { get; private set; }

		/// <summary>
		/// The palette (color table), which you can manipulate directly.
		/// </summary>
		public Color32[] Palette { get; private set; }

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
		public PureImage8 Pure => new PureImage8(this);

		/// <summary>
		/// A static "empty" image that never has pixel data in it.
		/// </summary>
		public static Image8 Empty { get; } = new Image8(0, 0);

		#endregion

		#region Pixel-plotting

		/// <summary>
		/// Access the pixels using "easy" 2D array-brackets.  This is safe, easy, reliable,
		/// and slower than almost every other way of accessing the pixels, but it's very useful
		/// for simple cases.  Reading a pixel outside the image will return 0,
		/// and writing a pixel outside the image is a no-op.
		/// </summary>
		/// <param name="x">The X coordinate of the pixel to read or write.</param>
		/// <param name="y">The Y coordinate of the pixel to read or write.</param>
		/// <returns>The color at that pixel.</returns>
		public byte this[int x, int y]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => x < 0 || x >= Width || y < 0 || y >= Height ? (byte)0 : Data[y * Width + x];

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
		public Image8(string filename, ImageFormat imageFormat = default)
		{
			Image8? image = LoadFile(filename, imageFormat);
			if (image == null)
				throw new ArgumentException($"'{filename}' is not readable as a known image format.");

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
			Palette = image.Palette;
		}

		/// <summary>
		/// Construct an image from the given image file data.
		/// </summary>
		/// <param name="data">The image file data to load.</param>
		/// <param name="filenameIfKnown">The filename of that image file data, if known.</param>
		/// <param name="imageFormat">The file format of that file, if known.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
			ImageFormat imageFormat = default)
		{
			Image8? image = LoadFile(data, filenameIfKnown, imageFormat);
			if (image == null)
				throw new ArgumentException($"The given data is not readable as a known image format.");

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
			Palette = image.Palette;
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="palette">The initial palette for the image.  If not provided,
		/// the palette will be 256 colors, all zeros (transparent black).  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(Vector2i size, IEnumerable<Color32>? palette = null)
		{
			Width = size.X;
			Height = size.Y;
			Data = new byte[size.X * size.Y];
			Palette = palette?.Take(256).ToArray() ?? new Color32[256];
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="palette">The initial palette for the image.  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(Vector2i size, ReadOnlySpan<Color32> palette)
		{
			Width = size.X;
			Height = size.Y;
			Data = new byte[size.X * size.Y];
			Palette = palette.Slice(0, Math.Min(palette.Length, 256)).ToArray();
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="fillColor">The initial color for all pixels of the image.</param>
		/// <param name="palette">The initial palette for the image.  If not provided,
		/// the palette will be 256 colors, all zeros (transparent black).  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(Vector2i size, byte fillColor, IEnumerable<Color32>? palette = null)
		{
			Width = size.X;
			Height = size.Y;
			Data = new byte[size.X * size.Y];
			Palette = palette?.Take(256).ToArray() ?? new Color32[256];
			Fill(fillColor);
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="size">The dimensions of the image, in pixels.</param>
		/// <param name="fillColor">The initial color for all pixels of the image.</param>
		/// <param name="palette">The initial palette for the image.  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(Vector2i size, byte fillColor, ReadOnlySpan<Color32> palette)
		{
			Width = size.X;
			Height = size.Y;
			Data = new byte[size.X * size.Y];
			Palette = palette.Slice(0, Math.Min(palette.Length, 256)).ToArray();
			Fill(fillColor);
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="palette">The initial palette for the image.  If not provided,
		/// the palette will be 256 colors, all zeros (transparent black).  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(int width, int height, IEnumerable<Color32>? palette = null)
		{
			Width = width;
			Height = height;
			Data = new byte[width * height];
			Palette = palette?.Take(256).ToArray() ?? new Color32[256];
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to 0.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="palette">The initial palette for the image.  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(int width, int height, ReadOnlySpan<Color32> palette)
		{
			Width = width;
			Height = height;
			Data = new byte[width * height];
			Palette = palette.Slice(0, Math.Min(palette.Length, 256)).ToArray();
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="fillColor">The initial color for all pixels of the image.</param>
		/// <param name="palette">The initial palette for the image.  If not provided,
		/// the palette will be 256 colors, all zeros (transparent black).  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(int width, int height, byte fillColor, IEnumerable<Color32>? palette = null)
		{
			Width = width;
			Height = height;
			Data = new byte[width * height];
			Palette = palette?.Take(256).ToArray() ?? new Color32[256];
			Fill(fillColor);
		}

		/// <summary>
		/// Construct an empty image of the given size.  The pixels will initially
		/// be set to the given fill color.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="fillColor">The initial color for all pixels of the image.</param>
		/// <param name="palette">The initial palette for the image.  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(int width, int height, byte fillColor, ReadOnlySpan<Color32> palette)
		{
			Width = width;
			Height = height;
			Data = new byte[width * height];
			Palette = palette.Slice(0, Math.Min(palette.Length, 256)).ToArray();
			Fill(fillColor);
		}

		/// <summary>
		/// Construct an image around the given color data, WITHOUT copying it.<br />
		/// <br />
		/// WARNING:  This constructor does NOT make a copy of the provided data; it *wraps*
		/// the provided arrays directly; notably, the byte array must be at least width*height
		/// in size (and that fact will at least be validated so that other methods can assume
		/// it).  This is very fast, but you can also use it to break things if you're not
		/// careful.  When in doubt, this is *not* the method you want.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="data">The data to use for the image.</param>
		/// <param name="palette">The palette to use for the image.  This must have between
		/// 0 and 256 entries (inclusive).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image8(int width, int height, byte[] data, Color32[] palette)
		{
			if (data.Length < width * height)
				throw new ArgumentException("Cannot construct a new image using an undersized color array;"
					+ $" array is {data.Length} values, but width {width} x height {height} requires {width * height} values.");
			if (palette.Length > 256)
				throw new ArgumentOutOfRangeException(nameof(palette));

			Data = data;
			Width = width;
			Height = height;
			Palette = palette;
		}

		/// <summary>
		/// Construct a new image by copying the raw byte data into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="rawData">The raw color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <param name="palette">The initial palette for the image.  If not provided,
		/// the palette will be 256 colors, all zeros (transparent black).  If larger
		/// than 256 colors, the first 256 colors will be taken.</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		public Image8(int width, int height, ReadOnlySpan<byte> rawData, IEnumerable<Color32>? palette = null)
		{
			Width = width;
			Height = height;
			Palette = palette?.Take(256).ToArray() ?? new Color32[256];

			int size = width * height;
			Data = new byte[size];

			unsafe
			{
				fixed (byte* data = rawData)
				{
					Overwrite(data, rawData.Length);
				}
			}
		}

		/// <summary>
		/// Construct a new image by copying the raw byte data into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="rawData">The raw color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <param name="palette">The initial palette for the image.  If larger than 256
		/// colors, the first 256 colors will be taken.</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		public Image8(int width, int height, ReadOnlySpan<byte> rawData, ReadOnlySpan<Color32> palette)
		{
			Width = width;
			Height = height;
			Palette = palette.Slice(0, Math.Min(palette.Length, 256)).ToArray();

			int size = width * height;
			Data = new byte[size];

			unsafe
			{
				fixed (byte* data = rawData)
				{
					Overwrite(data, rawData.Length);
				}
			}
		}

		/// <summary>
		/// Construct a new image by copying the raw byte data into the new image's memory.
		/// </summary>
		/// <param name="width">The width of the new image.</param>
		/// <param name="height">The height of the new image.</param>
		/// <param name="rawData">The raw color data to copy into the image.  This must
		/// be large enough for the given image.</param>
		/// <param name="rawDataLength">The number of bytes in the source data array.</param>
		/// <param name="palette">The start of the color palette for the image.</param>
		/// <param name="paletteCount">The number of entries in the color palette, which
		/// must be from 0 to 256 (inclusive).</param>
		/// <exception cref="ArgumentException">Thrown if rawData is too short for the image dimensions.</exception>
		public unsafe Image8(int width, int height, byte *rawData, int rawDataLength, Color32* palette = null, int paletteCount = 0)
		{
			Width = width;
			Height = height;

			int size = width * height;
			Data = new byte[size];

			Overwrite(rawData, rawDataLength);

			if (palette != null)
			{
				Palette = null!;
				ReplacePalette(palette, paletteCount);
			}
			else
			{
				if (paletteCount < 0 || paletteCount > 256)
					throw new ArgumentOutOfRangeException(nameof(paletteCount));
				Palette = new Color32[paletteCount];
			}
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
				byte[] dest = new byte[Width * Height];
				fixed (byte* destBase = dest)
				fixed (byte* srcBase = Data)
				{
					Buffer.MemoryCopy(srcBase, destBase, Width * Height, Width * Height);
				}
				return dest;
			}
		}

		/// <summary>
		/// Replace the current palette for the image with a new palette, copying
		/// the provided colors as a new color array.
		/// </summary>
		/// <param name="palette">The palette to copy as the new palette.</param>
		public void ReplacePalette(IEnumerable<Color32> palette)
			=> Palette = palette.ToArray();

		/// <summary>
		/// Replace the current palette for the image with a new palette, copying
		/// the provided colors as a new color array.
		/// </summary>
		/// <param name="palette">The palette to copy as the new palette.</param>
		public void ReplacePalette(ReadOnlySpan<Color32> palette)
			=> Palette = palette.ToArray();

		/// <summary>
		/// Replace the current palette for the image with a new palette, copying
		/// the provided colors as a new color array.
		/// </summary>
		/// <param name="palette">The start of the palette to copy as the new palette.</param>
		/// <param name="paletteCount">The number of entries in the color palette, which
		/// must be from 0 to 256 (inclusive).</param>
		public unsafe void ReplacePalette(Color32* palette, int paletteCount)
		{
			if (paletteCount < 0 || paletteCount > 256)
				throw new ArgumentOutOfRangeException(nameof(paletteCount));

			Palette = new Color32[paletteCount];
			if (paletteCount == 0)
				return;

			unsafe
			{
				fixed (Color32* destBase = Palette)
				{
					Color32* dest = destBase;
					Color32* src = palette;
					for (int i = 0; i < paletteCount; i++)
						*dest++ = *src++;
				}
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
		public void Replace(Image8 other)
		{
			Width = other.Width;
			Height = other.Height;
			Data = new byte[Width * Height];
			Overwrite(other.Data);
		}

		/// <summary>
		/// Overwrite all of the pixels in this image with the given raw pixel data,
		/// represented as a sequence of bytes, which must be at least as large
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
		/// Overwrite all of the pixels in this image with the given raw pixel data,
		/// represented as a sequence of bytes, which must be at least as large
		/// as this image.
		/// </summary>
		/// <param name="rawData">The raw data to overwrite this image with.</param>
		/// <param name="rawDataLength">The number of bytes in the raw data.</param>
		/// <exception cref="ArgumentException">Thrown if the provided source data isn't large enough.</exception>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public unsafe void Overwrite(byte *rawData, int rawDataLength)
		{
			if (rawData == null)
				throw new ArgumentNullException(nameof(rawData));
			if (rawDataLength < Width * Height)
				throw new ArgumentException($"Not enough data in source array for an image of size {Width}x{Height}.");

			unsafe
			{
				fixed (byte* destBase = Data)
				{
					byte* dest = destBase;
					byte* src = rawData;
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
		/// Promote this 8-bit paletted image to a 32-bit truecolor RGBA image.
		/// </summary>
		/// <returns>A new 32-bit truecolor RGBA image that contains the same pixels.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image32 ToImage32()
			=> new Image32(Width, Height, Data, Palette);

		/// <summary>
		/// Make a perfect duplicate of this image and return it.
		/// </summary>
		/// <returns>The newly-cloned image.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public Image8 Clone()
			=> new Image8(Width, Height, Data.AsSpan(), Palette.AsSpan());

		/// <summary>
		/// Make a perfect duplicate of this image and return it.
		/// </summary>
		/// <returns>The newly-cloned image.</returns>
		[Pure]
		IImage IImage.Clone()
			=> new Image8(Width, Height, Data.AsSpan(), Palette.AsSpan());

		#endregion

		#region Loading and saving

		/// <summary>
		/// Construct an image from the embedded resource with the given name.  The embedded
		/// resource must be of a file format that this image library is capable of
		/// decoding, like PNG or GIF.
		/// </summary>
		/// <param name="assembly">The assembly containing the embedded resource.</param>
		/// <param name="name">The name of the embedded image resource to load.  This may use
		/// slashes in the pathname to separate components.</param>
		/// <returns>The newly-loaded image, or null if no such image exists or is not
		/// a valid image file.</returns>
		public static Image8? FromEmbeddedResource(Assembly assembly, string name)
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
		/// format like PNG or GIF.
		/// </summary>
		/// <param name="filename">The filename of the image to load.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or null if it can't be loaded.</returns>
		public static Image8? LoadFile(string filename, ImageFormat imageFormat = default)
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
		/// encoded as a supported image file format like PNG or GIF.
		/// </summary>
		/// <param name="data">The data of the image file to load.</param>
		/// <param name="filenameIfKnown">If the image format is unknown, the filename
		/// can be included to help disambiguate the image data.</param>
		/// <param name="imageFormat">The format of the image file, if known;
		/// if None, the format will be determined automatically from the filename
		/// and the data.</param>
		/// <returns>The new image, or null if it can't be decoded.</returns>
		public static Image8? LoadFile(ReadOnlySpan<byte> data, string? filenameIfKnown = null,
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
			IImageLoader? loader = Image32.GetLoader(imageFormat);
			if (loader == null)
				return null;

			// Attempt to load the image in something passably close to its native color format.
			ImageLoadResult? loadResult = loader.LoadImage(data);
			if (loadResult == null)
				return null;

			// Turn the result into an Image.
			if (loadResult.Image is Image8 image8)
				return image8;
			else if (loadResult.Image != null)
				throw new InvalidDataException("Supplied image data uses more than 256 colors and cannot be directly represented as an Image8.");
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
			IImageSaver? saver = Image32.GetSaver(format);
			if (saver == null)
				throw new ArgumentException($"Unknown image format '{format}'.");

			return saver.SaveImage(this, null, options);
		}

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

		#region Resizing/resampling

		/// <summary>
		/// Perform resizing to fit the given container size using the chosen fitting
		/// mode.  Fast, but can be really, really inaccurate.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		public void ResizeToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit)
			=> Resize(Fit(Size, containerSize, fitMode));

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="imageSize">The new size of the image.</param>
		public void Resize(Vector2i imageSize)
			=> Resize(imageSize.X, imageSize.Y);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="newWidth">The new width of the image.</param>
		/// <param name="newHeight">The new height of the image.</param>
		public void Resize(int newWidth, int newHeight)
		{
			byte[] newData = new byte[newWidth * newHeight];

			int xStep = (int)(((long)Width << 16) / newWidth);
			int yStep = (int)(((long)Height << 16) / newHeight);

			unsafe
			{
				fixed (byte* destBase = newData)
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

			Width = newWidth;
			Height = newHeight;
			Data = newData;
		}

		/// <summary>
		/// Fit the given image to the given container, using one of several
		/// possible fit modes.
		/// </summary>
		/// <param name="imageSize">The image's current size.</param>
		/// <param name="containerSize">The container's size.</param>
		/// <param name="fitMode">How to perform the fit operation.</param>
		/// <returns>The appropriate new size for the image.</returns>
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
		public Image8 Extract(int x, int y, int width, int height)
		{
			Image8 image = new Image8(width, height);
			image.Blit(this, x, y, 0, 0, width, height);
			return image;
		}

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		public void Crop(int x, int y, int width, int height)
		{
			Image8 image = new Image8(width, height);
			image.Blit(this, x, y, 0, 0, width, height);

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

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
		public void Pad(int left = 0, int top = 0, int right = 0, int bottom = 0, byte fillColor = default)
		{
			Image8 image = new Image8(Width + left + right, Height + top + bottom, fillColor);
			image.Blit(this, 0, 0, left, top, Width + right, Height + bottom);

			Width = image.Width;
			Height = image.Height;
			Data = image.Data;
		}

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle.  This will by default clip
		/// the provided coordinates to perform a safe blit (all pixels outside an image
		/// will be ignored).
		/// </summary>
		public void Blit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height,
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

				case BlitFlags.Add:
					FastUnsafeAddBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.Multiply:
					FastUnsafeMultiplyBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.Sub:
					FastUnsafeSubBlit(srcImage, srcX, srcY, destX, destY, width, height);
					break;
				case BlitFlags.RSub:
					FastUnsafeRSubBlit(srcImage, srcX, srcY, destX, destY, width, height);
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
		private void FastUnsafeBlit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					byte* destLimit = destBase + Width * Height;
					byte* srcLimit = srcBase + srcImage.Width * srcImage.Height;

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
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
							dest[3] = src[3];
							dest[4] = src[4];
							dest[5] = src[5];
							dest[6] = src[6];
							dest[7] = src[7];
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
		/// any pixels that are color 0.  Make sure all values are within range; this is as
		/// unsafe as it sounds.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeTransparentBlit(Image8 srcImage,
			int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					byte* destLimit = destBase + Width * Height;
					byte* srcLimit = srcBase + srcImage.Width * srcImage.Height;

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
						byte c;
						while (count >= 8)
						{
							if ((c = src[0]) != 0) dest[0] = c;
							if ((c = src[1]) != 0) dest[1] = c;
							if ((c = src[2]) != 0) dest[2] = c;
							if ((c = src[3]) != 0) dest[3] = c;
							if ((c = src[4]) != 0) dest[4] = c;
							if ((c = src[5]) != 0) dest[5] = c;
							if ((c = src[6]) != 0) dest[6] = c;
							if ((c = src[7]) != 0) dest[7] = c;
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								if ((c = src[0]) != 0) dest[0] = c;
								if ((c = src[1]) != 0) dest[1] = c;
								if ((c = src[2]) != 0) dest[2] = c;
								if ((c = src[3]) != 0) dest[3] = c;
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								if ((c = src[0]) != 0) dest[0] = c;
								if ((c = src[1]) != 0) dest[1] = c;
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								if ((c = src[0]) != 0) dest[0] = c;
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
		/// Fast, unsafe add of src image rectangle values to dest image rectangle values.
		/// Make sure all coordinates are within range; this is as unsafe as it sounds.
		/// </summary>
		private void FastUnsafeAddBlit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;

					do
					{
						int count = width;
						while (count >= 8)
						{
							dest[0] = (byte)Math.Min(src[0] + dest[0], 255);
							dest[1] = (byte)Math.Min(src[1] + dest[1], 255);
							dest[2] = (byte)Math.Min(src[2] + dest[2], 255);
							dest[3] = (byte)Math.Min(src[3] + dest[3], 255);
							dest[4] = (byte)Math.Min(src[4] + dest[4], 255);
							dest[5] = (byte)Math.Min(src[5] + dest[5], 255);
							dest[6] = (byte)Math.Min(src[6] + dest[6], 255);
							dest[7] = (byte)Math.Min(src[7] + dest[7], 255);
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest[0] = (byte)Math.Min(src[0] + dest[0], 255);
								dest[1] = (byte)Math.Min(src[1] + dest[1], 255);
								dest[2] = (byte)Math.Min(src[2] + dest[2], 255);
								dest[3] = (byte)Math.Min(src[3] + dest[3], 255);
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								dest[0] = (byte)Math.Min(src[0] + dest[0], 255);
								dest[1] = (byte)Math.Min(src[1] + dest[1], 255);
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								dest[0] = (byte)Math.Min(src[0] + dest[0], 255);
								dest++;
								src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);
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
		private void FastUnsafeMultiplyBlit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;
					byte* destLimit = destBase + Width * Height;
					byte* srcLimit = srcBase + srcImage.Width * srcImage.Height;

					do
					{
						Debug.Assert(dest >= destBase && dest + width <= destLimit);
						Debug.Assert(src >= srcBase && src + width <= srcLimit);

						int count = width;
						do
						{
							byte s = *src++, d = *dest;
							uint v = (uint)(s * d);
							v = (v + 1 + (v >> 8)) >> 8;    // Divide by 255, faster than "v /= 255"
							*dest++ = (byte)v;
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
		/// Fast, unsafe subtract of src image rectangle values to dest image rectangle values.
		/// Make sure all coordinates are within range; this is as unsafe as it sounds.
		/// </summary>
		private void FastUnsafeSubBlit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;

					do
					{
						int count = width;
						while (count >= 8)
						{
							dest[0] = (byte)Math.Max(dest[0] - src[0], 0);
							dest[1] = (byte)Math.Max(dest[1] - src[1], 0);
							dest[2] = (byte)Math.Max(dest[2] - src[2], 0);
							dest[3] = (byte)Math.Max(dest[3] - src[3], 0);
							dest[4] = (byte)Math.Max(dest[4] - src[4], 0);
							dest[5] = (byte)Math.Max(dest[5] - src[5], 0);
							dest[6] = (byte)Math.Max(dest[6] - src[6], 0);
							dest[7] = (byte)Math.Max(dest[7] - src[7], 0);
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest[0] = (byte)Math.Max(dest[0] - src[0], 0);
								dest[1] = (byte)Math.Max(dest[1] - src[1], 0);
								dest[2] = (byte)Math.Max(dest[2] - src[2], 0);
								dest[3] = (byte)Math.Max(dest[3] - src[3], 0);
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								dest[0] = (byte)Math.Min(dest[0] - src[0], 0);
								dest[1] = (byte)Math.Min(dest[1] - src[1], 0);
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								dest[0] = (byte)Math.Min(dest[0] - src[0], 0);
								dest++;
								src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe subtract of src image rectangle values to dest image rectangle values.
		/// Make sure all coordinates are within range; this is as unsafe as it sounds.
		/// </summary>
		private void FastUnsafeRSubBlit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width - width;

					do
					{
						int count = width;
						while (count >= 8)
						{
							dest[0] = (byte)Math.Max(src[0] - dest[0], 0);
							dest[1] = (byte)Math.Max(src[1] - dest[1], 0);
							dest[2] = (byte)Math.Max(src[2] - dest[2], 0);
							dest[3] = (byte)Math.Max(src[3] - dest[3], 0);
							dest[4] = (byte)Math.Max(src[4] - dest[4], 0);
							dest[5] = (byte)Math.Max(src[5] - dest[5], 0);
							dest[6] = (byte)Math.Max(src[6] - dest[6], 0);
							dest[7] = (byte)Math.Max(src[7] - dest[7], 0);
							count -= 8;
							src += 8;
							dest += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest[0] = (byte)Math.Max(src[0] - dest[0], 0);
								dest[1] = (byte)Math.Max(src[1] - dest[1], 0);
								dest[2] = (byte)Math.Max(src[2] - dest[2], 0);
								dest[3] = (byte)Math.Max(src[3] - dest[3], 0);
								src += 4;
								dest += 4;
							}
							if ((count & 2) != 0)
							{
								dest[0] = (byte)Math.Min(src[0] - dest[0], 0);
								dest[1] = (byte)Math.Min(src[1] - dest[1], 0);
								src += 2;
								dest += 2;
							}
							if ((count & 1) != 0)
							{
								dest[0] = (byte)Math.Min(src[0] - dest[0], 0);
								dest++;
								src++;
							}
						}
						src += srcSkip;
						dest += destSkip;
					} while (--height != 0);
				}
			}
		}

		/// <summary>
		/// Fast, unsafe copy from src image rectangle to dest image rectangle.  Make sure all
		/// values are within range; this is as unsafe as it sounds.  This flips the pixels
		/// horizontally while copying.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FastUnsafeBlitFlipHorz(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX + width;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width + width;
					byte* destLimit = destBase + Width * Height;
					byte* srcLimit = srcBase + srcImage.Width * srcImage.Height;

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
		/// any Color.Transparent pixels.  Make sure all values are within range; this is as
		/// unsafe as it sounds.  This flips the pixels horizontally while copying.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public void FastUnsafeTransparentBlitFlipHorz(Image8 srcImage,
			int srcX, int srcY, int destX, int destY, int width, int height, bool flipVert)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* src = srcBase + srcImage.Width * srcY + srcX;
					byte* dest = destBase + Width * destY + destX + width;
					int srcSkip = srcImage.Width - width;
					int destSkip = Width + width;
					byte* destLimit = destBase + Width * Height;
					byte* srcLimit = srcBase + srcImage.Width * srcImage.Height;

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
						byte c;
						while (count >= 8)
						{
							dest -= 8;
							if ((c = src[0]) != 0) dest[7] = c;
							if ((c = src[1]) != 0) dest[6] = c;
							if ((c = src[2]) != 0) dest[5] = c;
							if ((c = src[3]) != 0) dest[4] = c;
							if ((c = src[4]) != 0) dest[3] = c;
							if ((c = src[5]) != 0) dest[2] = c;
							if ((c = src[6]) != 0) dest[1] = c;
							if ((c = src[7]) != 0) dest[0] = c;
							count -= 8;
							src += 8;
						}
						if (count != 0)
						{
							if ((count & 4) != 0)
							{
								dest -= 4;
								if ((c = src[0]) != 0) dest[3] = c;
								if ((c = src[1]) != 0) dest[2] = c;
								if ((c = src[2]) != 0) dest[1] = c;
								if ((c = src[3]) != 0) dest[0] = c;
								src += 4;
							}
							if ((count & 2) != 0)
							{
								dest -= 2;
								if ((c = src[0]) != 0) dest[1] = c;
								if ((c = src[1]) != 0) dest[0] = c;
								src += 2;
							}
							if ((count & 1) != 0)
							{
								dest--;
								if ((c = src[0]) != 0) dest[0] = c;
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

		#endregion

		#region Pattern blits

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle, repeating the source rectangle
		/// if the destination rectangle is larger than the source.  This will by default clip
		/// the provided destination coordinates to perform a safe blit (all pixels outside an
		/// image will be ignored).  If the source coordinates lie outside the srcImage, this
		/// will throw an exception.
		/// </summary>
		public void PatternBlit(Image8 srcImage, Rect srcRect, Rect destRect)
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
		private void FastUnsafePatternBlit(Image8 srcImage, Rect srcRect, Vector2i offset, Rect destRect)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				fixed (byte* srcBase = srcImage.Data)
				{
					byte* dest = destBase + Width * destRect.Y + destRect.X;
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
						byte* src = srcBase + (srcY + offsetY) * srcWidth;
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
				fixed (byte* dataBase = Data)
				{
					byte* topRow = dataBase;
					byte* bottomRow = dataBase + Width * (Height - 1);
					for (int y = 0; y < Height / 2; y++)
					{
						for (int x = 0; x < Width; x++)
						{
							byte temp = *topRow;
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
				fixed (byte* dataBase = Data)
				{
					byte* ptr = dataBase;
					for (int y = 0; y < Height; y++)
					{
						byte* start = ptr;
						byte* end = ptr + Width;
						for (int x = 0; x < Width / 2; x++)
						{
							end--;
							byte temp = *start;
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
		/// Rotate the image in place 90 degrees clockwise.
		/// </summary>
		public void Rotate90CW()
		{
			// Copy everything off to the side.
			byte[] oldData = new byte[Data.Length];
			Array.Copy(Data, oldData, Data.Length);

			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Copy from the old data to the new data (i.e., the original data buffer).
			for (int y = 0; y < oldHeight; y++)
			{
				for (int x = 0; x < oldWidth; x++)
				{
					byte b = oldData[(y * Width) + x];
					Data[(Height - y) + (x * Width)] = b;
				}
			}

			// Update the width and height.
			Width = newWidth;
			Height = newHeight;
		}

		/// <summary>
		/// Rotate the image in place 90 degrees counterclockwise.
		/// </summary>
		public void Rotate90CCW()
		{
			// Copy everything off to the side.
			byte[] oldData = new byte[Data.Length];
			Array.Copy(Data, oldData, Data.Length);

			// The new dimensions.
			int oldWidth = Width, oldHeight = Height;
			int newWidth = Height, newHeight = Width;

			// Copy from the old data to the new data (i.e., the original data buffer).
			for (int y = 0; y < oldHeight; y++)
			{
				for (int x = 0; x < oldWidth; x++)
				{
					byte b = oldData[(Height - y) + (x * Width)];
					Data[(y * Width) + x] = b;
				}
			}

			// Update the width and height.
			Width = newWidth;
			Height = newHeight;
		}

		/// <summary>
		/// Rotate the image in place 180 degrees.
		/// </summary>
		public void Rotate180()
		{
			unsafe
			{
				fixed (byte* dataBase = Data)
				{
					byte* ptr = dataBase;
					byte* endPtr = ptr + Width * Height - 1;
					while (ptr < endPtr)
					{
						ptr++;
						byte temp = *ptr;
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
		/// Multiply every color value in the palette by the given scalar values.
		/// </summary>
		public void MultiplyPalette(float r, float g, float b, float a)
		{
			unsafe
			{
				fixed (Color32* destBase = Palette)
				{
					Color32* dest = destBase;

					int count = Palette.Length;
					do
					{
						Color32 d = *dest;
						*dest++ = new Color32((int)(d.R * r + 0.5f), (int)(d.G * g + 0.5f), (int)(d.B * b + 0.5f), (int)(d.A * a + 0.5f));
					} while (--count != 0);
				}
			}
		}

		/// <summary>
		/// Multiply every color value in the palette by the given scalar values.
		/// </summary>
		public void MultiplyPalette(double r, double g, double b, double a)
		{
			unsafe
			{
				fixed (Color32* destBase = Palette)
				{
					Color32* dest = destBase;

					int count = Palette.Length;
					do
					{
						Color32 d = *dest;
						*dest++ = new Color32((int)(d.R * r + 0.5), (int)(d.G * g + 0.5), (int)(d.B * b + 0.5), (int)(d.A * a + 0.5));
					} while (--count != 0);
				}
			}
		}

		/// <summary>
		/// Premultiply the alpha value to the red, green, and blue values of every
		/// color in the palette.
		/// </summary>
		public void PremultiplyAlpha()
		{
			unsafe
			{
				fixed (Color32* destBase = Palette)
				{
					byte* dest = (byte*)destBase;
					int count = Palette.Length;

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
		/// Skim through the image and replace all exact instances of the given value with another.
		/// </summary>
		/// <param name="src">The value to replace.</param>
		/// <param name="dest">Its replacement value.</param>
		public void RemapColor(byte src, byte dest)
		{
			unsafe
			{
				fixed (byte* imageBase = Data)
				{
					byte* ptr = imageBase;
					byte* end = imageBase + Width * Height;

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
		/// Skim through the image and replace many values at once via a dictionary.
		/// This is typically much slower than many calls to Remap() above, unless the
		/// replacement table is large relative to the image size.
		/// </summary>
		/// <param name="dictionary">The dictionary that describes all replacement values.
		/// If a value does not exist in the dictionary, it will not be changed.</param>
		public void RemapColor(Dictionary<byte, byte> dictionary)
		{
			unsafe
			{
				fixed (byte* imageBase = Data)
				{
					byte* ptr = imageBase;
					byte* end = imageBase + Width * Height;

					while (ptr < end - 8)
					{
						if (dictionary.TryGetValue(ptr[0], out byte replacement))
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
							if (dictionary.TryGetValue(ptr[0], out byte replacement))
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
							if (dictionary.TryGetValue(ptr[0], out byte replacement))
								ptr[0] = replacement;
							if (dictionary.TryGetValue(ptr[1], out replacement))
								ptr[1] = replacement;
							ptr += 2;
						}
						if ((count & 1) != 0)
						{
							if (dictionary.TryGetValue(ptr[0], out byte replacement))
								ptr[0] = replacement;
						}
					}
				}
			}
		}

		/// <summary>
		/// Invert each value in the image (i.e., replace each X with 255-X).
		/// </summary>
		public void Invert()
		{
			unsafe
			{
				fixed (byte* imageBase = Data)
				{
					byte* ptr = imageBase;
					byte* end = imageBase + Width * Height;

					while (ptr < end)
					{
						*ptr = (byte)~*ptr;
					}
				}
			}
		}

		/// <summary>
		/// Skim through the palette and replace all exact instances of the given color with another.
		/// </summary>
		/// <param name="src">The color to replace.</param>
		/// <param name="dest">Its replacement color.</param>
		public void RemapPalette(Color32 src, Color32 dest)
		{
			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
		public void RemapPalette(Dictionary<Color32, Color32> dictionary)
		{
			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
		public void RemapPalette(Matrix3 matrix)
		{
			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
				fixed (Color32* imageBase = Palette)
				fixed (byte* remap = remapTable)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
				fixed (Color32* imageBase = Palette)
				fixed (byte* remap = remapTable)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

					while (ptr < end)
					{
						*ptr = new Color32(remap[ptr->R], remap[ptr->G + 256], remap[ptr->B + 512], ptr->A);
						ptr++;
					}
				}
			}
		}

		/// <summary>
		/// Convert the palette to grayscale (in place).
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
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
		/// Remove some or all of the color saturation from a palette, in-place.
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
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
		/// Remap the palette to a sepia-tone version of itself by forcing
		/// every color's position in YIQ-space.
		/// </summary>
		/// <param name="amount">How saturated the sepia is.  0.0 = grayscale, 1.0 = orange, -1.0 = blue.</param>
		public void Sepia(double amount = 0.125)
		{
			amount = Math.Min(Math.Max(amount, -1.0), +1.0);
			int newI = (int)(amount * 255 + 0.5);

			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

					while (ptr < end)
					{
						// Note that the Y values here match the "Color.ApparentBrightness"
						// constants:  You get brightness weighting here whether you want it
						// or not.
						Color32 src = *ptr;
						float y = 0.299f * src.R + 0.587f * src.G + 0.114f * src.B;
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
		/// Invert the red, green, and blue values in the palette (i.e., replace each R with 255-R,
		/// and so on for the other channels).  Alpha will be left unchanged.
		/// </summary>
		public void InvertPalette()
		{
			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

					while (ptr < end)
					{
						Color32 src = *ptr;
						*ptr++ = new Color32((byte)~src.R, (byte)~src.G, (byte)~src.B, src.A);
					}
				}
			}
		}

		/// <summary>
		/// Invert one channel in the palette (i.e., replace each R with 255-R).
		/// </summary>
		public void InvertPalette(ColorChannel channel)
		{
			unsafe
			{
				fixed (Color32* imageBase = Palette)
				{
					Color32* ptr = imageBase;
					Color32* end = imageBase + Palette.Length;

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
		/// Adjust the hue, saturation, and lightness of the palette.  This converts each color
		/// in the palette to HSL, adds the given deltas, and then converts it back to RGB.
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

			for (int i = 0; i < Palette.Length; i++)
			{
				Color32 c = Palette[i];
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
				Palette[i] = c2;
			}
		}

		/// <summary>
		/// Adjust the hue, saturation, and brightness of the palette.  This converts each color
		/// in the palette to HSB, adds the given deltas, and then converts it back to RGB.
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

			for (int i = 0; i < Palette.Length; i++)
			{
				Color32 c = Palette[i];
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
				Palette[i] = c2;
			}
		}

		/// <summary>
		/// Convert the pixel values so that this is not only a grayscale image but
		/// also uses the standard Grayscale256 palette.  (Each pixel's value will
		/// therefore also be equal to its brightness after this.  The Grayscale()
		/// method converts only the palette, with the pixel values left unchanged.)
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
		public void ToGrayscale256(bool useRelativeBrightness = true,
			double r = Color32.ApparentRedBrightness,
			double g = Color32.ApparentGreenBrightness,
			double b = Color32.ApparentBlueBrightness)
		{
			byte[] paletteMap = new byte[256];

			if (useRelativeBrightness)
			{
				uint rc = (uint)(65536 * r + 0.5);
				uint gc = (uint)(65536 * g + 0.5);
				uint bc = (uint)(65536 * b + 0.5);
				if (rc + gc + bc > 65536)
					throw new ArgumentException("Sum of r+g+b brightness values must be less than or equal to 1.0.");

				for (int i = 0; i < Palette.Length; i++)
				{
					Color32 src = Palette[i];
					uint y = ((rc * src.R + gc * src.G + bc * src.B) + 32768) >> 16;
					paletteMap[i] = (byte)y;
				}
			}
			else
			{
				for (int i = 0; i < Palette.Length; i++)
				{
					Color32 src = Palette[i];
					// Multiply/shift here is faster than divide-by-three.
					uint y = (((uint)src.R + (uint)src.G + (uint)src.B) * 683) >> 11;
					paletteMap[i] = (byte)y;
				}
			}

			for (int i = 0; i < Data.Length; i++)
			{
				Data[i] = paletteMap[Data[i]];
			}

			Palette = Palettes.Grayscale256.ToArray();
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
				fixed (byte* srcBase = Data)
				{
					byte* dest = destBase;
					byte* src = srcBase;
					int count = Width * Height;
					int eight = count >> 3;
					int rest = count & 7;

					switch (channel)
					{
						case ColorChannel.Red:
							while (eight-- != 0)
							{
								dest[0] = Palette[src[0]].R;
								dest[1] = Palette[src[1]].R;
								dest[2] = Palette[src[2]].R;
								dest[3] = Palette[src[3]].R;
								dest[4] = Palette[src[4]].R;
								dest[5] = Palette[src[5]].R;
								dest[6] = Palette[src[6]].R;
								dest[7] = Palette[src[7]].R;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = Palette[*src++].R;
							break;

						case ColorChannel.Green:
							while (eight-- != 0)
							{
								dest[0] = Palette[src[0]].G;
								dest[1] = Palette[src[1]].G;
								dest[2] = Palette[src[2]].G;
								dest[3] = Palette[src[3]].G;
								dest[4] = Palette[src[4]].G;
								dest[5] = Palette[src[5]].G;
								dest[6] = Palette[src[6]].G;
								dest[7] = Palette[src[7]].G;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = Palette[*src++].G;
							break;

						case ColorChannel.Blue:
							while (eight-- != 0)
							{
								dest[0] = Palette[src[0]].B;
								dest[1] = Palette[src[1]].B;
								dest[2] = Palette[src[2]].B;
								dest[3] = Palette[src[3]].B;
								dest[4] = Palette[src[4]].B;
								dest[5] = Palette[src[5]].B;
								dest[6] = Palette[src[6]].B;
								dest[7] = Palette[src[7]].B;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = Palette[*src++].B;
							break;

						case ColorChannel.Alpha:
							while (eight-- != 0)
							{
								dest[0] = Palette[src[0]].A;
								dest[1] = Palette[src[1]].A;
								dest[2] = Palette[src[2]].A;
								dest[3] = Palette[src[3]].A;
								dest[4] = Palette[src[4]].A;
								dest[5] = Palette[src[5]].A;
								dest[6] = Palette[src[6]].A;
								dest[7] = Palette[src[7]].A;
								dest += 8;
								src += 8;
							}
							while (rest-- != 0)
								*dest++ = Palette[*src++].A;
							break;

						default:
							throw new ArgumentException(nameof(channel));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Swap color channels in the palette with other color channels in the same palette.
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

				int count = Palette.Length;

				fixed (Color32* destBase = Palette)
				{
					byte* dest = (byte*)destBase;

					int four = count >> 2;
					int rest = count & 3;
					while (four-- != 0)
					{
						((uint*)tmp)[0] = ((uint*)dest)[0];
						((uint*)tmp)[1] = ((uint*)dest)[1];
						((uint*)tmp)[2] = ((uint*)dest)[2];
						((uint*)tmp)[3] = ((uint*)dest)[3];

						dest[0] = tmp[r];
						dest[1] = tmp[g];
						dest[2] = tmp[b];
						dest[3] = tmp[a];

						dest[4] = tmp[4 + r];
						dest[5] = tmp[4 + g];
						dest[6] = tmp[4 + b];
						dest[7] = tmp[4 + a];

						dest[8] = tmp[8 + r];
						dest[9] = tmp[8 + g];
						dest[10] = tmp[8 + b];
						dest[11] = tmp[8 + a];

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
		public static Image8 operator +(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot add images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot add images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ + *bPtr++);
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
		public static Image8 operator -(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot subtract images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot subtract images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ - *bPtr++);
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
		public static Image8 operator *(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot multiply images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot multiply images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ * *bPtr++);
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
		public static Image8 operator *(Image8 image, int scalar)
		{
			Image8 result = new Image8(image.Size);

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ * scalar);
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
		public static Image8 operator *(Image8 image, double scalar)
		{
			Image8 result = new Image8(image.Size);

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ * scalar);
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
		public static Image8 operator /(Image8 image, int scalar)
		{
			Image8 result = new Image8(image.Size);

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ / scalar);
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
		public static Image8 operator /(Image8 image, double scalar)
		{
			Image8 result = new Image8(image.Size);

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ / scalar);
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
		public static Image8 operator ~(Image8 image)
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
		public static Image8 operator -(Image8 image)
		{
			Image8 result = new Image8(image.Size);

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(-*imagePtr++);
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
		public static Image8 operator |(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ | *bPtr++);
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
		public static Image8 operator &(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ & *bPtr++);
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
		public static Image8 operator ^(Image8 a, Image8 b)
		{
			if (a.Width != b.Width)
				throw new ArgumentException("Cannot combine images of different widths");
			if (a.Height != b.Height)
				throw new ArgumentException("Cannot combine images of different heights");

			Image8 result = new Image8(a.Size);

			unsafe
			{
				fixed (byte* aBase = a.Data)
				fixed (byte* bBase = b.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = a.Width * b.Width;
					byte* aPtr = aBase;
					byte* bPtr = bBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*aPtr++ ^ *bPtr++);
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted left by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted left.</returns>
		[Pure]
		public static Image8 operator <<(Image8 image, int amount)
		{
			if (amount < 0)
				return image >> -amount;
			else if (amount == 0)
				return image.Clone();

			Image8 result = new Image8(image.Size);
			if (amount >= 8)
				return result;

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ << amount);
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new image of the same size as the given images, where each
		/// pixel's R, G, and B components have been shifted logically right by the given number of bits.
		/// </summary>
		/// <param name="image">The image to scale.</param>
		/// <param name="amount">The number of bits to shift each value by.</param>
		/// <returns>A new image where all components have been shifted logically right.</returns>
		[Pure]
		public static Image8 operator >>(Image8 image, int amount)
		{
			if (amount < 0)
				return image << -amount;
			else if (amount == 0)
				return image.Clone();

			Image8 result = new Image8(image.Size);
			if (amount >= 8)
				return result;

			unsafe
			{
				fixed (byte* imageBase = image.Data)
				fixed (byte* resultBase = result.Data)
				{
					int count = image.Width * image.Width;
					byte* imagePtr = imageBase;
					byte* resultPtr = resultBase;
					while (count-- != 0)
						*resultPtr++ = (byte)(*imagePtr++ >> amount);
				}
			}

			return result;
		}

		#endregion

		#region Filling

		/// <summary>
		/// Fill the given image as fast as possible with the given color.
		/// </summary>
		public void Fill(byte color)
		{
			int count = Data.Length;
			if (count <= 0)
				return;

			unsafe
			{
				fixed (byte* bufferStart = Data)
				{
					byte* dest = bufferStart;

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

		#region Rectangle Filling

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
		public void FillRect(Rect rect, byte drawColor, BlitFlags blitFlags = BlitFlags.Copy)
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
		public void FillRect(int x, int y, int width, int height, byte drawColor,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			if ((blitFlags & BlitFlags.FastUnsafe) == 0
				&& !ClipRect(Size, ref x, ref y, ref width, ref height))
				return;

			unsafe
			{
				fixed (byte* destBase = Data)
				{
					byte* dest = destBase + Width * y + x;
					int nextLine = Width - width;

					BlitFlags drawMode = 0;
					switch (blitFlags & BlitFlags.ModeMask)
					{
						default:
						case BlitFlags.Copy:
							drawMode = BlitFlags.Copy;
							break;

						case BlitFlags.Transparent:
							if (drawColor == 0)
								return;
							drawMode = BlitFlags.Copy;
							break;
					}

					if (drawMode == BlitFlags.Copy)
					{
						while (height-- != 0)
						{
							int count = width;
							while (count-- != 0)
								*dest++ = drawColor;
							dest += nextLine;
						}
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
		public void DrawRect(Rect rect, byte color, int thickness = 1, BlitFlags blitFlags = BlitFlags.Copy)
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
		public void DrawRect(int x, int y, int width, int height, byte color, int thickness = 1,
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
		/// <param name="skipStart">Whether to skip the initial pixel (true)
		/// or draw the initial pixel of the line (false).</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping steps if you are certain that the given rectangle is
		/// fully contained within the image.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawLine(Vector2i p1, Vector2i p2, byte color, BlitFlags blitFlags = BlitFlags.Copy, bool skipStart = false)
			=> DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, blitFlags, skipStart);

		/// <summary>
		/// An implementation of Bresenham's line-drawing routine, with Cohen-Sutherland
		/// clipping to the image (unless you pass BlitFlags.FastUnsafe).
		/// </summary>
		/// <param name="x1">The starting X coordinate to draw from.</param>
		/// <param name="y1">The starting Y coordinate to draw from.</param>
		/// <param name="x2">The ending X coordinate to draw to.</param>
		/// <param name="y2">The ending Y coordinate to draw to.</param>
		/// <param name="color">The color to draw with.</param>
		/// <param name="skipStart">Whether to skip the initial pixel (true)
		/// or draw the initial pixel of the line (false).</param>
		/// <param name="blitFlags">Which mode to use to draw the color:  This method
		/// works in Copy and Transparent modes.  You can also apply the FastUnsafe flag
		/// to skip the clipping steps if you are certain that the given rectangle is
		/// fully contained within the image.</param>
		public void DrawLine(int x1, int y1, int x2, int y2, byte color, BlitFlags blitFlags = default, bool skipStart = false)
		{
			if (blitFlags == BlitFlags.Transparent && color == 0)
				return;

			if ((blitFlags & BlitFlags.FastUnsafe) != 0)
			{
				DrawLineFastUnsafe(x1, y1, x2, y2, color, skipStart);
				return;
			}

			OutCode code1 = ComputeOutCode(x1, y1);
			OutCode code2 = ComputeOutCode(x2, y2);

			while (true)
			{
				if ((code1 | code2) == 0)
				{
					DrawLineFastUnsafe(x1, y1, x2, y2, color, skipStart);
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
						skipStart = false;
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
		/// OutCode flags.
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
		/// <param name="skipStart">Whether to skip the initial pixel (true)
		/// or draw the initial pixel of the line (false).</param>
		private void DrawLineFastUnsafe(int x1, int y1, int x2, int y2, byte color, bool skipStart)
		{
			int dx = Math.Abs(x2 - x1);
			int sx = x1 < x2 ? 1 : -1;
			int dy = -Math.Abs(y2 - y1);
			int sy = y1 < y2 ? Width : -Width;
			int index = x1 + y1 * Width;
			int end = x2 + y2 * Width;

			int err = dx + dy;

			if (skipStart)
			{
				if (index == end)
					return;

				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; index += sx; }
				if (e2 <= dx) { err += dx; index += sy; }
			}

			while (true)
			{
				Data[index] = color;

				if (index == end)
					break;

				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; index += sx; }
				if (e2 <= dx) { err += dx; index += sy; }
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
		/// works in Copy and Transparent modes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawThickLine(int x1, int y1, int x2, int y2, double thickness,
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawThickLine(Vector2i start, Vector2i end, double thickness,
			byte color, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.</param>
		public void DrawThickLine(Vector2d start, Vector2d end, double thickness,
			byte color, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(IEnumerable<Vector2> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(IEnumerable<Vector2d> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(IEnumerable<Vector2i> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2i> points, byte color,
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
		/// works in Copy and Transparent modes.</param>
		public void FillPolygon(ReadOnlySpan<Vector2d> points, byte color,
			BlitFlags blitFlags = BlitFlags.Copy)
		{
			if ((blitFlags & BlitFlags.ModeMask) == BlitFlags.Transparent && color == 0
				|| (blitFlags & BlitFlags.ModeMask) != BlitFlags.Copy)
				return;

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
				FillSpansFast(y, usedXes, color);
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
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private void FillSpansFast(int y, ReadOnlySpan<double> points, byte color)
		{
			unsafe
			{
				fixed (byte* destBase = Data)
				{
					byte* row = destBase + Width * y;

					for (int i = 0; i < points.Length; i += 2)
					{
						if (points[i] >= Width)
							break;
						if (points[i + 1] <= 0)
							continue;
						int start = Math.Max((int)(points[i] + 0.5), 0);
						int end = Math.Min((int)(points[i + 1] + 0.5) + 1, Width - 1);
						int count = end - start;
						byte* dest = row + start;
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
			byte color, int steps = 0, BlitFlags blitFlags = BlitFlags.Copy)
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
		/// any nonzero value at all will be considered non-transparent.</param>
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
		/// any nonzero value at all will be considered non-transparent.</param>
		/// <returns>True if the row is entirely transparent, false if it contains at
		/// least one opaque or transparent pixel.</returns>
		[Pure]
		public bool IsRectTransparent(Rect rect, byte cutoff = 0)
		{
			int x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height;
			if (!Image32.ClipRect(Size, ref x, ref y, ref width, ref height))
				return true;

			unsafe
			{
				fixed (byte* basePtr = Data)
				{
					byte* src = basePtr + y * Width + x;
					int leftover = Width - width;
					for (int i = 0; i < height; i++)
					{
						for (int j = 0; j < width; j++)
						{
							if (*src > cutoff)
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
		/// any nonzero value at all will be considered non-transparent.</param>
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
				fixed (byte* basePtr = Data)
				{
					byte* src = basePtr + y * Width + x;
					for (int i = 0; i < width; i++)
					{
						if (*src > cutoff)
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
		/// any nonzero value at all will be considered non-transparent.</param>
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
				fixed (byte* basePtr = Data)
				{
					byte* src = basePtr + y * Width + x;
					for (int i = 0; i < height; i++)
					{
						if (*src > cutoff)
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
		/// Determine if this is a grayscale image.  For it to be grayscale, every palette
		/// entry must have R = G = B.
		/// </summary>
		/// <returns>True if this is a grayscale image.</returns>
		public bool IsGrayscale()
		{
			foreach (Color32 color in Palette)
			{
				if (color.R != color.G || color.R != color.B)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if this is a grayscale image that uses the fixed Grayscale256 palette.
		/// For it to be Grayscale256, every palette entry must have index = R = G = B, and
		/// all alpha values must be 255, and there must be 256 palette entries.
		/// </summary>
		/// <returns>True if this is a Grayscale256 image.</returns>
		public bool IsGrayscale256()
		{
			if (Palette.Length != 256)
				return false;
			for (int i = 0; i < Palette.Length; i++)
			{
				Color32 color = Palette[i];
				if (color.R != i || color.R != color.G || color.R != color.B || color.A != 255)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if this is a single-channel image, and if so, which color channel.
		/// This ignores the alpha component of each color.
		/// </summary>
		/// <returns>The single color channel used by this image (Red, Green, or Blue);
		/// or None if this image uses more than one color channel.</returns>
		public ColorChannel IsSingleChannel()
		{
			byte r = 0, g = 0, b = 0;

			foreach (Color32 color in Palette)
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public override bool Equals(object? obj)
			=> obj is Image32 other && Equals(other);

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
		public unsafe bool Equals(Image8? other)
		{
			if (ReferenceEquals(this, other))
				return true;
			if (ReferenceEquals(other, null))
				return false;

			if (Width != other.Width
				|| Height != other.Height
				|| Data.Length != other.Data.Length)
				return false;

			fixed (byte* dataBase = Data)
			fixed (byte* otherDataBase = other.Data)
			{
				byte* data = dataBase;
				byte* otherData = otherDataBase;
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
		public static bool operator ==(Image8? a, Image8? b)
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
		public static bool operator !=(Image8? a, Image8? b)
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
			fixed (byte* dataBase = Data)
			{
				uint hashCode = 0;

				byte* data = dataBase;
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
