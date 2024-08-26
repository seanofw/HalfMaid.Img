using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Targa
{
	/// <summary>
	/// A saver for Targa image files.
	/// </summary>
	public class TargaSaver : IImageSaver
	{
		private const int HeaderSize = 18;

		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Targa;

		/// <inheritdoc />
		public string Title => "Targa";

		/// <inheritdoc />
		public string DefaultExtension => ".tga";

		/// <summary>
		/// Save the given 24/32-bit RGBA image as a Targa file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">If provided, this must be a TargaSaveOptions
		/// instance, which can control whether the alpha channel is saved.</param>
		public byte[] SaveImage(Image32 image, IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			bool includeAlpha = fileSaveOptions is TargaSaveOptions targaSaveOptions
				? targaSaveOptions.IncludeAlpha : false;

			byte[] result = new byte[HeaderSize + image.Width * image.Height * (includeAlpha ? 4 : 3)];

			Span<TargaHeader> header = MemoryMarshal.Cast<byte, TargaHeader>(result.AsSpan());
			header[0].IdLength = 0;
			header[0].ImageType = TargaImageType.Truecolor;
			header[0].PaletteType = TargaPaletteType.NoPalette;
			header[0].PaletteStart = 0;
			header[0].PaletteLength = 0;
			header[0].PaletteBits = 0;
			header[0].XOrigin = 0;
			header[0].YOrigin = 0;
			header[0].Width = (ushort)image.Width.LE();
			header[0].Height = (ushort)image.Height.LE();
			header[0].BitsPerPixel = (byte)(includeAlpha ? 32 : 24);
			header[0].ImageDescriptor = (TargaImageDescriptor)(byte)(includeAlpha ? 8 : 0);

			Span<byte> dest = result.AsSpan().Slice(HeaderSize);
			int width = image.Width, height = image.Height;

			// Copy the pixels, in BGR or BGRA format, upside-down the way Targa prefers them.
			// We can technically use the image descriptor to flip the image, but the default
			// "0" value is stored upside-down, and we want to stick with that, since some readers
			// assume it.
			int destPtr = 0;
			if (includeAlpha)
			{
				for (int y = height - 1; y >= 0; y--)
				{
					ReadOnlySpan<Color32> src = image.Data.AsSpan().Slice(y * width);
					for (int x = 0; x < width; x++)
					{
						Color32 c = src[x];
						dest[destPtr + 0] = c.B;
						dest[destPtr + 1] = c.G;
						dest[destPtr + 2] = c.R;
						dest[destPtr + 3] = c.A;
						destPtr += 4;
					}
				}
			}
			else
			{
				for (int y = height - 1; y >= 0; y--)
				{
					ReadOnlySpan<Color32> src = image.Data.AsSpan().Slice(y * width);
					for (int x = 0; x < width; x++)
					{
						Color32 c = src[x];
						dest[destPtr + 0] = c.B;
						dest[destPtr + 1] = c.G;
						dest[destPtr + 2] = c.R;
						destPtr += 3;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Save the given 8-bit image as a Targa file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">If provided, this should be a TargaSaveOptions
		/// instance, but it is otherwise ignored by this method.</param>
		/// <returns>The image converted to a Targa file.</returns>
		public byte[] SaveImage(Image8 image, IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			ReadOnlySpan<Color32> palette = image.Palette;

			bool includeAlpha = false;
			for (int i = 0; i < palette.Length; i++)
				if (palette[i].A != 255)
					includeAlpha = true;

			byte[] result = new byte[HeaderSize + palette.Length * (includeAlpha ? 4 : 3)
				+ image.Width * image.Height];

			Span<TargaHeader> header = MemoryMarshal.Cast<byte, TargaHeader>(result.AsSpan());
			header[0].IdLength = 0;
			header[0].ImageType = TargaImageType.Paletted;
			header[0].PaletteType = TargaPaletteType.Palette;
			header[0].PaletteStart = 0;
			header[0].PaletteLength = (ushort)palette.Length;
			header[0].PaletteBits = (byte)(includeAlpha ? 32 : 24);
			header[0].XOrigin = 0;
			header[0].YOrigin = 0;
			header[0].Width = (ushort)image.Width.LE();
			header[0].Height = (ushort)image.Height.LE();
			header[0].BitsPerPixel = 8;
			header[0].ImageDescriptor = 0;

			// Copy the palette in BGR or BGRA format.
			Span<byte> paletteDest = result.AsSpan().Slice(HeaderSize);
			if (includeAlpha)
			{
				for (int i = 0; i < palette.Length; i++)
				{
					paletteDest[i * 4 + 0] = palette[i].B;
					paletteDest[i * 4 + 1] = palette[i].G;
					paletteDest[i * 4 + 2] = palette[i].R;
					paletteDest[i * 4 + 3] = palette[i].A;
				}
			}
			else
			{
				for (int i = 0; i < palette.Length; i++)
				{
					paletteDest[i * 3 + 0] = palette[i].B;
					paletteDest[i * 3 + 1] = palette[i].G;
					paletteDest[i * 3 + 2] = palette[i].R;
				}
			}

			int width = image.Width, height = image.Height;
			Span<byte> dest = result.AsSpan().Slice(HeaderSize + palette.Length * (includeAlpha ? 4 : 3));

			// Copy the pixels, in BGR or BGRA format, upside-down the way Targa prefers them.
			// We can technically use the image descriptor to flip the image, but the default
			// "0" value is stored upside-down, and we want to stick with that, since some readers
			// assume it.
			int destPtr = 0;
			for (int y = height - 1; y >= 0; y--)
			{
				ReadOnlySpan<byte> src = image.Data.AsSpan().Slice(y * width);
				for (int x = 0; x < width; x++)
					dest[destPtr++] = src[x];
			}

			return result;
		}
	}
}
