using System;
using System.Collections.Generic;
using System.IO;
using HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo;

namespace HalfMaid.Img.FileFormats.Jpeg
{
	/// <summary>
	/// This class knows how to load JPEG files using LibJpegTurbo.
	/// </summary>
	public class JpegLoader : IImageLoader
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Jpeg;

		/// <inheritdoc />
		public string Title => "JPEG";

		/// <inheritdoc />
		public string DefaultExtension => ".jpg";

		/// <inheritdoc />
		public ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data)
		{
			if (data.Length <= 11)
				return ImageCertainty.No;

			// Try a JFIF header.
			int certainty = 0;
			if (data[0] == 0xFF) certainty += 4;
			if (data[1] == 0xD8) certainty += 4;
			if (data[2] == 0xFF) certainty += 4;
			if (data[3] == 0xE0) certainty += 2;
			if (data[4] == 0x00) certainty++;
			if (data[5] == 0x10) certainty++;
			if (data[6] == 'J') certainty++;
			if (data[7] == 'F') certainty++;
			if (data[8] == 'I') certainty++;
			if (data[9] == 'F') certainty++;
			if (data[10] == 0x00) certainty++;

			// Try an Exif header.
			int certainty2 = 1;
			if (data[0] == 0xFF) certainty2 += 4;
			if (data[1] == 0xD8) certainty2 += 4;
			if (data[2] == 0xFF) certainty2 += 4;
			if (data[3] == 0xE1) certainty2 += 2;
			if (data[6] == 'E') certainty2++;
			if (data[7] == 'x') certainty2++;
			if (data[8] == 'i') certainty2++;
			if (data[9] == 'f') certainty2++;
			if (data[10] == 0x00) certainty2++;
			if (data[11] == 0x00) certainty2++;

			certainty = Math.Max(certainty, certainty2);

			return certainty >= 15 ? ImageCertainty.Yes
				: certainty >= 11 ? ImageCertainty.Probably
				: certainty >= 7 ? ImageCertainty.Maybe
				: ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageCertainty DoesNameMatch(string filename)
		{
			return filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
				|| filename.EndsWith(".jpe", StringComparison.OrdinalIgnoreCase)
				|| filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
				|| filename.EndsWith(".jfif", StringComparison.OrdinalIgnoreCase)
				? ImageCertainty.Yes
				: ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data)
		{
			IntPtr tjHandle = Tj3.Init(InitType.Decompress);
			try
			{
				Tj3.DecompressHeader(tjHandle, data);

				ColorSpace colorSpace = (ColorSpace)Tj3.Get(tjHandle, Param.ColorSpace);
				int bits = Tj3.Get(tjHandle, Param.Precision);
				int width = Tj3.Get(tjHandle, Param.JpegWidth);
				int height = Tj3.Get(tjHandle, Param.JpegHeight);

				return new ImageFileMetadata(width, height,
					colorSpace == ColorSpace.Gray
						? (bits <= 8 ? ImageFileColorFormat.Gray8Bit : ImageFileColorFormat.Gray16Bit)
						: ImageFileColorFormat.Rgb24Bit);
			}
			catch (InvalidDataException)
			{
				return null;
			}
			finally
			{
				Tj3.Destroy(tjHandle);
			}
		}

		private enum JpegDensityUnits
		{
			Unknown = 0,
			PixelsPerInch = 1,
			PixelsPerCm = 2,
		}

		/// <inheritdoc />
		public ImageLoadResult? LoadImage(ReadOnlySpan<byte> data)
		{
			IntPtr tjHandle = Tj3.Init(InitType.Decompress);
			try
			{
				Tj3.DecompressHeader(tjHandle, data);

				ColorSpace colorSpace = (ColorSpace)Tj3.Get(tjHandle, Param.ColorSpace);
				int bits = Tj3.Get(tjHandle, Param.Precision);
				int width = Tj3.Get(tjHandle, Param.JpegWidth);
				int height = Tj3.Get(tjHandle, Param.JpegHeight);

				Dictionary<string, object> metadata = new Dictionary<string, object>();

				metadata[ImageMetadataKey.JpegProgressive] = Tj3.Get(tjHandle, Param.Progressive) != 0;

				JpegDensityUnits densityUnits = (JpegDensityUnits)Tj3.Get(tjHandle, Param.DensityUnits);

				int pixelsPerUnitX = Tj3.Get(tjHandle, Param.XDensity);
				int pixelsPerUnitY = Tj3.Get(tjHandle, Param.YDensity);
				if (densityUnits == JpegDensityUnits.PixelsPerCm)
				{
					metadata[ImageMetadataKey.PixelsPerMeterX] = pixelsPerUnitX;
					metadata[ImageMetadataKey.PixelsPerMeterY] = pixelsPerUnitY;
				}
				else if (densityUnits == JpegDensityUnits.PixelsPerInch)
				{
					metadata[ImageMetadataKey.PixelsPerMeterX] = pixelsPerUnitX / 254.0;
					metadata[ImageMetadataKey.PixelsPerMeterY] = pixelsPerUnitY / 254.0;
				}

				if (colorSpace == ColorSpace.Gray)
				{
					metadata[ImageMetadataKey.NumChannels] = 1;
					metadata[ImageMetadataKey.BitsPerChannel] = 8;

					byte[] pixels = Tj3.Decompress8(tjHandle, data, PixelFormat.Gray);

					return new ImageLoadResult(ImageFileColorFormat.Gray8Bit,
						new OpenTK.Mathematics.Vector2i(width, height),
						new Image8(width, height, pixels, Palettes.Grayscale256.ToArray()), metadata);
				}
				else
				{
					metadata[ImageMetadataKey.NumChannels] = 3;
					metadata[ImageMetadataKey.BitsPerChannel] = 8;

					byte[] pixels = Tj3.Decompress8(tjHandle, data, PixelFormat.Rgba);

					return new ImageLoadResult(ImageFileColorFormat.Rgb24Bit,
						new OpenTK.Mathematics.Vector2i(width, height),
						new Image32(width, height, pixels), metadata);
				}
			}
			finally
			{
				Tj3.Destroy(tjHandle);
			}
		}
	}
}
