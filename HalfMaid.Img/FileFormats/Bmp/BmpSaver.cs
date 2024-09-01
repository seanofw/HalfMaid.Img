using System;
using System.Collections.Generic;

namespace HalfMaid.Img.FileFormats.Bmp
{
	/// <summary>
	/// A saver that can produce uncompressed Windows BMP files in 8 bits,
	/// 24 bits, and 32 bits.
	/// </summary>
	public class BmpSaver : IImageSaver
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Bmp;

		/// <inheritdoc />
		public string Title => "Windows Bitmap";

		/// <inheritdoc />
		public string DefaultExtension => ".bmp";

		private static readonly IReadOnlyDictionary<string, object> _emptyDictionary
			= new Dictionary<string, object>();

		/// <summary>
		/// Save the given 32-bit RGBA image as a BMP file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">If provided, this must be a BmpSaveOptions
		/// instance, which can control whether the alpha channel is saved.</param>
		public unsafe byte[] SaveImage(Image32 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			bool includeAlpha = fileSaveOptions is BmpSaveOptions bmpSaveOptions
				? bmpSaveOptions.IncludeAlpha : false;

			BitmapFileHeader fileHeader = default;
			BitmapInfoHeaderV4 infoHeaderV4 = default;
			ref BitmapInfoHeader infoHeader = ref infoHeaderV4.v1Header;

			int headerSize = includeAlpha ? sizeof(BitmapInfoHeaderV4) : sizeof(BitmapInfoHeader);
			int bytesPerPixel = includeAlpha ? 4 : 3;
			int rowSpan = (image.Width * bytesPerPixel + 3) & ~3;
			int dataSize = rowSpan * image.Height;

			fileHeader.bfType[0] = (byte)'B';
			fileHeader.bfType[1] = (byte)'M';
			fileHeader.bfSize = (sizeof(BitmapFileHeader) + headerSize + dataSize).LE();
			fileHeader.bfOffBits = (sizeof(BitmapFileHeader) + headerSize).LE();

			imageMetadata ??= _emptyDictionary;

			double pixelsPerMeterX = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterX, out object? v) ? (double)v : 2835;
			double pixelsPerMeterY = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterY, out v) ? (double)v : 2835;

			infoHeader.biSize = headerSize.LE();     // V4 header for RGBA, V1 header for RGB.
			infoHeader.biPlanes = ((short)1).LE();
			infoHeader.biWidth = image.Width.LE();
			infoHeader.biHeight = image.Height.LE();
			infoHeader.biBitCount = ((short)(includeAlpha ? 32 : 24)).LE();
			infoHeader.biCompression = (includeAlpha ? 6 : 0).LE();
			infoHeader.biSizeImage = dataSize;
			infoHeader.biXPelsPerMeter = ((short)(pixelsPerMeterX + 0.5)).LE();
			infoHeader.biYPelsPerMeter = ((short)(pixelsPerMeterY + 0.5)).LE();
			infoHeader.biClrImportant = 0;
			infoHeader.biClrUsed = 0;

			if (includeAlpha)
			{
				infoHeaderV4.bV4RedMask   = 0x00FF0000u;
				infoHeaderV4.bV4GreenMask = 0x0000FF00u;
				infoHeaderV4.bV4BlueMask  = 0x000000FFu;
				infoHeaderV4.bV4AlphaMask = 0xFF000000u;
			}

			using OutputWriter outputWriter = new OutputWriter();

			byte* dest = outputWriter.FastWrite(sizeof(BitmapFileHeader));
			Buffer.MemoryCopy(&fileHeader, dest, sizeof(BitmapFileHeader), sizeof(BitmapFileHeader));

			dest = outputWriter.FastWrite(headerSize);
			Buffer.MemoryCopy(&infoHeaderV4, dest, headerSize, headerSize);

			dest = outputWriter.FastWrite(dataSize);
			fixed (Color32* data = image.Data)
			{
				if (includeAlpha)
				{
					byte* rowStart = dest;
					for (int y = image.Height - 1; y >= 0; y--)
					{
						Color32* src = data + image.Width * y;
						byte* writePtr = rowStart;
						for (int x = 0; x < image.Width; x++)
						{
							Color32 c = *src++;
							writePtr[0] = c.B;
							writePtr[1] = c.G;
							writePtr[2] = c.R;
							writePtr[3] = c.A;
							writePtr += 4;
						}
						rowStart += rowSpan;
					}
				}
				else
				{
					byte* rowStart = dest;
					for (int y = image.Height - 1; y >= 0; y--)
					{
						Color32* src = data + image.Width * y;
						byte* writePtr = rowStart;
						for (int x = 0; x < image.Width; x++)
						{
							Color32 c = *src++;
							writePtr[0] = c.B;
							writePtr[1] = c.G;
							writePtr[2] = c.R;
							writePtr += 3;
						}
						rowStart += rowSpan;
					}
				}
			}

			return outputWriter.Finish();
		}

		/// <summary>
		/// Save the given 24-bit RGB image as a BMP file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">If provided, this must be a BmpSaveOptions
		/// instance, which will otherwise be ignored.</param>
		public unsafe byte[] SaveImage(Image24 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			BitmapFileHeader fileHeader = default;
			BitmapInfoHeaderV4 infoHeaderV4 = default;
			ref BitmapInfoHeader infoHeader = ref infoHeaderV4.v1Header;

			int headerSize = sizeof(BitmapInfoHeader);
			int bytesPerPixel = 3;
			int rowSpan = (image.Width * bytesPerPixel + 3) & ~3;
			int dataSize = rowSpan * image.Height;

			fileHeader.bfType[0] = (byte)'B';
			fileHeader.bfType[1] = (byte)'M';
			fileHeader.bfSize = (sizeof(BitmapFileHeader) + headerSize + dataSize).LE();
			fileHeader.bfOffBits = (sizeof(BitmapFileHeader) + headerSize).LE();

			imageMetadata ??= _emptyDictionary;

			double pixelsPerMeterX = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterX, out object? v) ? (double)v : 2835;
			double pixelsPerMeterY = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterY, out v) ? (double)v : 2835;

			infoHeader.biSize = headerSize.LE();     // V4 header for RGBA, V1 header for RGB.
			infoHeader.biPlanes = ((short)1).LE();
			infoHeader.biWidth = image.Width.LE();
			infoHeader.biHeight = image.Height.LE();
			infoHeader.biBitCount = ((short)24).LE();
			infoHeader.biCompression = (0).LE();
			infoHeader.biSizeImage = dataSize;
			infoHeader.biXPelsPerMeter = ((short)(pixelsPerMeterX + 0.5)).LE();
			infoHeader.biYPelsPerMeter = ((short)(pixelsPerMeterY + 0.5)).LE();
			infoHeader.biClrImportant = 0;
			infoHeader.biClrUsed = 0;

			using OutputWriter outputWriter = new OutputWriter();

			byte* dest = outputWriter.FastWrite(sizeof(BitmapFileHeader));
			Buffer.MemoryCopy(&fileHeader, dest, sizeof(BitmapFileHeader), sizeof(BitmapFileHeader));

			dest = outputWriter.FastWrite(headerSize);
			Buffer.MemoryCopy(&infoHeaderV4, dest, headerSize, headerSize);

			dest = outputWriter.FastWrite(dataSize);
			fixed (Color24* data = image.Data)
			{
				byte* rowStart = dest;
				for (int y = image.Height - 1; y >= 0; y--)
				{
					Color24* src = data + image.Width * y;
					byte* writePtr = rowStart;
					for (int x = 0; x < image.Width; x++)
					{
						Color24 c = *src++;
						writePtr[0] = c.B;
						writePtr[1] = c.G;
						writePtr[2] = c.R;
						writePtr += 3;
					}
					rowStart += rowSpan;
				}
			}

			return outputWriter.Finish();
		}

		/// <summary>
		/// Save the given 8-bit image as a BMP file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">If provided, this should be a BmpSaveOptions
		/// instance, but it is otherwise ignored by this method.</param>
		/// <returns>The image converted to a BMP file.</returns>
		public unsafe byte[] SaveImage(Image8 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			Color32[] palette = image.Palette;

			if (palette.Length > 256)
				throw new ArgumentException("Palette is too large for a BMP file.");

			BitmapFileHeader fileHeader = default;
			BitmapInfoHeader infoHeader = default;

			int rowSpan = (image.Width + 3) & ~3;
			int dataSize = rowSpan * image.Height;
			int paletteSize = palette.Length * 4;

			fileHeader.bfType[0] = (byte)'B';
			fileHeader.bfType[1] = (byte)'M';
			fileHeader.bfSize = (sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader) + paletteSize + dataSize).LE();
			fileHeader.bfOffBits = (sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader)).LE();

			imageMetadata ??= _emptyDictionary;

			double pixelsPerMeterX = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterX, out object? v) ? (double)v : 2835;
			double pixelsPerMeterY = imageMetadata.TryGetValue(ImageMetadataKey.PixelsPerMeterY, out v) ? (double)v : 2835;

			infoHeader.biSize = (40).LE();     // Simple V1 header.
			infoHeader.biPlanes = ((short)1).LE();
			infoHeader.biWidth = image.Width.LE();
			infoHeader.biHeight = image.Height.LE();
			infoHeader.biBitCount = ((short)8).LE();
			infoHeader.biCompression = 0;
			infoHeader.biXPelsPerMeter = ((short)(pixelsPerMeterX + 0.5)).LE();
			infoHeader.biYPelsPerMeter = ((short)(pixelsPerMeterY + 0.5)).LE();
			infoHeader.biClrImportant = 0;
			infoHeader.biClrUsed = image.Palette.Length;

			using OutputWriter outputWriter = new OutputWriter();

			byte* dest = outputWriter.FastWrite(sizeof(BitmapFileHeader));
			Buffer.MemoryCopy(&fileHeader, dest, sizeof(BitmapFileHeader), sizeof(BitmapFileHeader));

			dest = outputWriter.FastWrite(sizeof(BitmapInfoHeader));
			Buffer.MemoryCopy(&infoHeader, dest, sizeof(BitmapInfoHeader), sizeof(BitmapInfoHeader));

			dest = outputWriter.FastWrite(paletteSize);
			for (int i = 0; i < palette.Length; i++)
			{
				Color32 c = palette[i];
				dest[0] = c.B;
				dest[1] = c.G;
				dest[2] = c.R;
				dest[3] = 0;
				dest += 4;
			}

			dest = outputWriter.FastWrite(dataSize);
			fixed (byte* data = image.Data)
			{
				byte* src = data + image.Height * image.Width;
				byte* rowStart = dest;
				for (int y = image.Height - 1; y >= 0; y--)
				{
					src -= image.Width;
					Buffer.MemoryCopy(src, rowStart, rowSpan, image.Width);
					rowStart += rowSpan;
				}
			}

			return outputWriter.Finish();
		}
	}
}
