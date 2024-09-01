using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HalfMaid.Img.FileFormats.Targa
{
	/// <summary>
	/// A loader for uncompressed Targa files.
	/// </summary>
	public class TargaLoader : IImageLoader
	{
		private const string CommentCodePage = "ISO-8859-1";
		private const int HeaderSize = 18;			// = sizeof(TargaHeader)

		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Targa;

		/// <inheritdoc />
		public string Title => "Targa";

		/// <inheritdoc />
		public string DefaultExtension => ".tga";

		/// <inheritdoc />
		public ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data)
		{
			if (data.Length < HeaderSize)
				return ImageCertainty.No;
			ReadOnlySpan<TargaHeader> header = MemoryMarshal.Cast<byte, TargaHeader>(data);

			int quality = 0;

			quality += (header[0].PaletteType == TargaPaletteType.NoPalette
				|| header[0].PaletteType == TargaPaletteType.Palette ? 1 : 0);

			quality += (header[0].ImageType == TargaImageType.Paletted
				|| header[0].ImageType == TargaImageType.Truecolor
				|| header[0].ImageType == TargaImageType.Grayscale
				|| header[0].ImageType == TargaImageType.PalettedRle
				|| header[0].ImageType == TargaImageType.TruecolorRle
				|| header[0].ImageType == TargaImageType.GrayscaleRle
				? 1 : 0);

			quality += (header[0].BitsPerPixel == 8
				|| header[0].BitsPerPixel == 24
				|| header[0].BitsPerPixel == 32 ? 1 : 0);

			if (header[0].ImageType == TargaImageType.Paletted
				|| header[0].ImageType == TargaImageType.PalettedRle)
				quality += (header[0].PaletteBits == 15
					|| header[0].PaletteBits == 16
					|| header[0].PaletteBits == 24
					|| header[0].PaletteBits == 32 ? 1 : 0);
			else quality++;

			if (quality == 4) return ImageCertainty.Probably;
			else if (quality >= 2) return ImageCertainty.Maybe;
			else return ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageCertainty DoesNameMatch(string filename)
		{
			return filename.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
				? ImageCertainty.Yes : ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data)
		{
			if (DoesDataMatch(data) == ImageCertainty.No)
				return null;

			ReadOnlySpan<TargaHeader> header = MemoryMarshal.Cast<byte, TargaHeader>(data);

			if ((header[0].ImageType == TargaImageType.Paletted)
				&& header[0].BitsPerPixel <= 8)
			{
				return new ImageFileMetadata(header[0].Width.LE(), header[0].Height.LE(),
					ImageFileColorFormat.Paletted8Bit);
			}
			else if (header[0].ImageType == TargaImageType.Grayscale)
			{
				return new ImageFileMetadata(header[0].Width.LE(), header[0].Height.LE(),
					header[0].BitsPerPixel > 8
						? ImageFileColorFormat.Gray16Bit
						: ImageFileColorFormat.Gray8Bit);
			}
			else if (header[0].ImageType == TargaImageType.Truecolor)
			{
				return new ImageFileMetadata(header[0].Width.LE(), header[0].Height.LE(),
					header[0].BitsPerPixel == 16 || header[0].BitsPerPixel == 32
						? ImageFileColorFormat.Rgba32Bit
						: ImageFileColorFormat.Rgb24Bit);
			}
			else return null;
		}

		/// <inheritdoc />
		public ImageLoadResult? LoadImage(ReadOnlySpan<byte> data, PreferredImageType preferredImageType)
		{
			if (data.Length < HeaderSize)
				return null;

			ReadOnlySpan<TargaHeader> header = MemoryMarshal.Cast<byte, TargaHeader>(data);

			string? comment;
			if (header[0].IdLength == 0)
				comment = null;
			else
			{
				try
				{
					comment = Encoding.GetEncoding(CommentCodePage).GetString(
						data.Slice(HeaderSize, header[0].IdLength).ToArray());
				}
				catch
				{
					comment = null;
				}
			}

			if ((header[0].ImageDescriptor & TargaImageDescriptor.InterleavingMask) != 0)
				throw new NotSupportedException("Interleaved Targa images are not supported.");

			ImageLoadResult? result = null;
			if (header[0].ImageType == TargaImageType.Paletted)
				result = header[0].BitsPerPixel <= 8
					? DecodePaletted8Bit(data, header[0], comment)
					: DecodePaletted16BitToTruecolor(data, header[0], comment);
			else if (header[0].ImageType == TargaImageType.Grayscale)
				result = DecodeGrayscale(data, header[0], comment);
			else if (header[0].ImageType == TargaImageType.Truecolor)
				result = preferredImageType == PreferredImageType.Image24
					? DecodeTruecolor24(data, header[0], comment)
					: DecodeTruecolor32(data, header[0], comment);

			if (result == null)
				return null;

			// If the image descriptor says the image was stored flipped, then flip it
			// to be top-to-bottom, left-to-right like every other image.

			if ((header[0].ImageDescriptor & TargaImageDescriptor.FlipVert) == 0)
			{
				if (result.Image is Image32 image32)
					image32.FlipVert();
				else if (result.Image is Image32 image24)
					image24.FlipVert();
				else if (result.Image is Image8 image8)
					image8.FlipVert();
			}

			if ((header[0].ImageDescriptor & TargaImageDescriptor.FlipHorz) != 0)
			{
				if (result.Image is Image32 image32)
					image32.FlipHorz();
				else if (result.Image is Image24 image24)
					image24.FlipHorz();
				else if (result.Image is Image8 image8)
					image8.FlipHorz();
			}

			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte FiveToEight(byte value)
			=> (byte)((value << 3) | (value >> 2));

		private static ImageLoadResult? DecodeTruecolor32(ReadOnlySpan<byte> data, in TargaHeader header, string? comment)
		{
			data = data.Slice(HeaderSize + header.IdLength);

			ImageFileColorFormat format;
			Image32 image = new Image32(header.Width.LE(), header.Height.LE());
			Span<Color32> destData = image.Data;

			if (header.BitsPerPixel == 15)
			{
				// 15-bit RGB, in the form of "xRRRRRGG GGGBBBBB" (little-endian).
				format = ImageFileColorFormat.Rgb24Bit;
				ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);
				if (words.Length < destData.Length)
					return null;
				for (int i = 0; i < destData.Length; i++)
				{
					ushort value = words[i].LE();
					destData[i] = new Color32(FiveToEight((byte)(value >> 10)),
						FiveToEight((byte)(value >> 5)), FiveToEight((byte)value));
				}
			}
			else if (header.BitsPerPixel == 16)
			{
				// 16-bit RGB, in the form of "ARRRRRGG GGGBBBBB" (little-endian).
				format = ImageFileColorFormat.Rgba32Bit;
				ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);
				if (words.Length < destData.Length)
					return null;
				for (int i = 0; i < destData.Length; i++)
				{
					ushort value = words[i].LE();
					destData[i] = new Color32(FiveToEight((byte)(value >> 10)),
						FiveToEight((byte)(value >> 5)), FiveToEight((byte)value),
						(value & 0x8000) != 0 ? 255 : 0);
				}
				return new ImageLoadResult(ImageFileColorFormat.Rgb24Bit, image.Size, image,
					metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
			}
			else if (header.BitsPerPixel == 24)
			{
				// 24-bit RGB, in B, G, R order.
				format = ImageFileColorFormat.Rgb24Bit;
				if (data.Length < destData.Length * 3)
					return null;
				unsafe
				{
					fixed (Color32* destBase = destData)
					fixed (byte* srcBase = data)
					{
						byte* src = srcBase;
						byte* dest = (byte*)destBase;
						int length = destData.Length;
						for (int i = 0; i < length; i++)
						{
							dest[2] = src[0];
							dest[1] = src[1];
							dest[0] = src[2];
							src += 3;
							dest += 4;
						}
					}
				}
			}
			else if (header.BitsPerPixel == 32)
			{
				// 32-bit RGB, in B, G, R, A order.
				format = ImageFileColorFormat.Rgba32Bit;
				if (data.Length < destData.Length * 4)
					return null;
				unsafe
				{
					fixed (Color32* destBase = destData)
					fixed (byte* srcBase = data)
					{
						byte* src = srcBase;
						byte* dest = (byte*)destBase;
						int length = destData.Length;
						for (int i = 0; i < length; i++)
						{
							dest[2] = src[0];
							dest[1] = src[1];
							dest[0] = src[2];
							dest[3] = src[3];
							src += 4;
							dest += 4;
						}
					}
				}
			}
			else return null;

			return new ImageLoadResult(format, image.Size, image,
				metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
		}

		private static ImageLoadResult? DecodeTruecolor24(ReadOnlySpan<byte> data, in TargaHeader header, string? comment)
		{
			data = data.Slice(HeaderSize + header.IdLength);

			ImageFileColorFormat format;
			Image24 image = new Image24(header.Width.LE(), header.Height.LE());
			Span<Color24> destData = image.Data;

			if (header.BitsPerPixel == 15)
			{
				// 15-bit RGB, in the form of "xRRRRRGG GGGBBBBB" (little-endian).
				format = ImageFileColorFormat.Rgb24Bit;
				ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);
				if (words.Length < destData.Length)
					return null;
				for (int i = 0; i < destData.Length; i++)
				{
					ushort value = words[i].LE();
					destData[i] = new Color24(FiveToEight((byte)(value >> 10)),
						FiveToEight((byte)(value >> 5)), FiveToEight((byte)value));
				}
			}
			else if (header.BitsPerPixel == 16)
			{
				// 16-bit RGB, in the form of "ARRRRRGG GGGBBBBB" (little-endian).
				format = ImageFileColorFormat.Rgba32Bit;
				ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);
				if (words.Length < destData.Length)
					return null;
				for (int i = 0; i < destData.Length; i++)
				{
					ushort value = words[i].LE();
					destData[i] = new Color24(FiveToEight((byte)(value >> 10)),
						FiveToEight((byte)(value >> 5)), FiveToEight((byte)value));
				}
				return new ImageLoadResult(ImageFileColorFormat.Rgb24Bit, image.Size, image,
					metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
			}
			else if (header.BitsPerPixel == 24)
			{
				// 24-bit RGB, in B, G, R order.
				format = ImageFileColorFormat.Rgb24Bit;
				if (data.Length < destData.Length * 3)
					return null;
				unsafe
				{
					fixed (Color24* destBase = destData)
					fixed (byte* srcBase = data)
					{
						byte* src = srcBase;
						byte* dest = (byte*)destBase;
						int length = destData.Length;
						for (int i = 0; i < length; i++)
						{
							dest[2] = src[0];
							dest[1] = src[1];
							dest[0] = src[2];
							src += 3;
							dest += 3;
						}
					}
				}
			}
			else if (header.BitsPerPixel == 32)
			{
				// 32-bit RGB, in B, G, R, A order.
				format = ImageFileColorFormat.Rgba32Bit;
				if (data.Length < destData.Length * 4)
					return null;
				unsafe
				{
					fixed (Color24* destBase = destData)
					fixed (byte* srcBase = data)
					{
						byte* src = srcBase;
						byte* dest = (byte*)destBase;
						int length = destData.Length;
						for (int i = 0; i < length; i++)
						{
							dest[2] = src[0];
							dest[1] = src[1];
							dest[0] = src[2];
							src += 4;
							dest += 3;
						}
					}
				}
			}
			else return null;

			return new ImageLoadResult(format, image.Size, image,
				metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
		}

		private static ImageLoadResult? DecodeGrayscale(ReadOnlySpan<byte> data, in TargaHeader header, string? comment)
		{
			data = data.Slice(HeaderSize + header.IdLength);

			ImageFileColorFormat format;
			Image8 image = new Image8(header.Width.LE(), header.Height.LE(), Palettes.Grayscale256);
			Span<byte> destData = image.Data;

			if (header.BitsPerPixel == 8)
			{
				format = ImageFileColorFormat.Gray8Bit;
				if (data.Length < destData.Length)
					return null;
				for (int i = 0; i < destData.Length; i++)
					destData[i] = data[i];
			}
			else if (header.BitsPerPixel == 16)
			{
				format = ImageFileColorFormat.Gray16Bit;
				if (data.Length < destData.Length * 2)
					return null;
				for (int i = 0; i < destData.Length; i++)
					destData[i] = data[i * 2 + 1];
			}
			else return null;

			return new ImageLoadResult(format, image.Size, image,
				metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
		}

		private static ImageLoadResult? DecodePaletted8Bit(ReadOnlySpan<byte> data, in TargaHeader header, string? comment)
		{
			data = data.Slice(HeaderSize + header.IdLength);

			// First, decode the palette.
			(int offset, Color32[]? palette) = DecodePalette(data, header);
			if (palette == null)
				return null;
			data = data.Slice(offset);

			// Construct the image with the given palette.
			Image8 image = new Image8(header.Width.LE(), header.Height.LE(), palette.AsSpan());
			Span<byte> destData = image.Data;

			if (data.Length < destData.Length)
				return null;

			// Fast-copy all the bytes into the new image.
			data.Slice(destData.Length).CopyTo(destData);

			return new ImageLoadResult(ImageFileColorFormat.Paletted8Bit, image.Size, image,
				metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
		}

		private static ImageLoadResult? DecodePaletted16BitToTruecolor(ReadOnlySpan<byte> data, in TargaHeader header, string? comment)
		{
			data = data.Slice(HeaderSize + header.IdLength);

			// Weirdo format:  A palette with between 256 and 65536 entries.
			// We promote it up to a truecolor image.
			(int offset, Color32[]? palette) = DecodePalette(data, header);
			if (palette == null)
				return null;
			data = data.Slice(offset);

			Image32 image = new Image32(header.Width.LE(), header.Height.LE(), palette.AsSpan());
			Span<Color32> destData = image.Data;
			ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);

			if (words.Length < destData.Length)
				return null;

			// Promote each index value up to its equivalent color in the palette.
			for (int i = 0; i < destData.Length; i++)
				destData[i] = palette[words[i].LE()];

			return new ImageLoadResult(header.PaletteBits == 24
					? ImageFileColorFormat.Rgb24Bit : ImageFileColorFormat.Rgba32Bit, image.Size, image: image,
				metadata: comment != null ? new Dictionary<string, object> { { "comment", comment } } : null);
		}

		private static (int Offset, Color32[]? Palette) DecodePalette(ReadOnlySpan<byte> data, in TargaHeader header)
		{
			int paletteStart = header.PaletteStart.LE();
			int paletteLength = header.PaletteLength.LE();
			Color32[] palette = new Color32[paletteStart + paletteLength];
			if (header.PaletteBits == 16)
			{
				ReadOnlySpan<ushort> words = MemoryMarshal.Cast<byte, ushort>(data);
				for (int i = 0; i < paletteLength; i++)
				{
					ushort value = words[i].LE();
					palette[i + paletteStart] = new Color32(FiveToEight((byte)(value >> 10)),
						FiveToEight((byte)(value >> 5)), FiveToEight((byte)value),
						(value & 0x8000) != 0 ? 255 : 0);
				}
				return (paletteLength * 2, palette);
			}
			else if (header.PaletteBits == 24)
			{
				for (int i = 0; i < paletteLength; i++)
					palette[i + paletteStart] = new Color32(data[i * 3 + 2], data[i * 3 + 1], data[i * 3 + 0]);
				return (paletteLength * 3, palette);
			}
			else if (header.PaletteBits == 32)
			{
				for (int i = 0; i < paletteLength; i++)
					palette[i + paletteStart] = new Color32(data[i * 4 + 2], data[i * 4 + 1], data[i * 4 + 0], data[i * 4 + 3]);
				return (paletteLength * 4, palette);
			}
			else return (0, null);
		}
	}
}
