using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo;

namespace HalfMaid.Img.FileFormats.Jpeg
{
	/// <summary>
	/// This class knows how to save a JPEG file using LibJpegTurbo.
	/// </summary>
	public class JpegSaver : IImageSaver
	{
		/// <summary>
		/// The default JPEG quality level used if no JpegSaveOptions are provided.
		/// </summary>
		public const int DefaultQuality = 85;

		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Jpeg;

		/// <inheritdoc />
		public string Title => "JPEG";

		/// <inheritdoc />
		public string DefaultExtension => ".jpg";

		/// <summary>
		/// Save a 32-bit truecolor image as a JPEG.  This always discards the alpha channel,
		/// since JPEG does not support alpha.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  Only the
		/// Comment property of this is taken, if provided.</param>
		/// <param name="fileSaveOptions">Save options.  This must be a JpegSaveOptions object
		/// if not null.</param>
		/// <returns>The resulting JPEG file, as a byte array.</returns>
		public byte[] SaveImage(Image32 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			JpegSaveOptions? options = fileSaveOptions as JpegSaveOptions;

			// JPEG doesn't do alpha, so we ignore the 'includeAlpha' option here.
			// The rest is handled directly by libjpeg-turbo pretty much as-is.

			IntPtr tjHandle = Tj3.Init(InitType.Compress);
			try
			{
				Tj3.Set(tjHandle, Param.ColorSpace, (int)ColorSpace.YCbCr);
				ApplyJpegOptions(tjHandle, options);
				ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<Color32, byte>(image.Data);
				byte[] compressed = Tj3.Compress8(tjHandle, bytes, image.Width, image.Width * 4,
					image.Height, PixelFormat.Rgba);
				return compressed;
			}
			finally
			{
				Tj3.Destroy(tjHandle);
			}
		}

		/// <summary>
		/// Save a 24-bit truecolor image as a JPEG.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  Only the
		/// Comment property of this is taken, if provided.</param>
		/// <param name="fileSaveOptions">Save options.  This must be a JpegSaveOptions object
		/// if not null.</param>
		/// <returns>The resulting JPEG file, as a byte array.</returns>
		public byte[] SaveImage(Image24 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			JpegSaveOptions? options = fileSaveOptions as JpegSaveOptions;

			// JPEG doesn't do alpha, so we ignore the 'includeAlpha' option here.
			// The rest is handled directly by libjpeg-turbo pretty much as-is.

			IntPtr tjHandle = Tj3.Init(InitType.Compress);
			try
			{
				Tj3.Set(tjHandle, Param.ColorSpace, (int)ColorSpace.YCbCr);
				ApplyJpegOptions(tjHandle, options);
				ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<Color24, byte>(image.Data);
				byte[] compressed = Tj3.Compress8(tjHandle, bytes, image.Width, image.Width * 3,
					image.Height, PixelFormat.Rgb);
				return compressed;
			}
			finally
			{
				Tj3.Destroy(tjHandle);
			}
		}

		/// <summary>
		/// Save an 8-bit paletted image as a JPEG.  This generally doesn't produce
		/// good results, but it's not prohibited to do it.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  Only the
		/// Comment property of this is taken, if provided.</param>
		/// <param name="fileSaveOptions">Save options.  This must be a JpegSaveOptions object
		/// if not null.</param>
		/// <returns>The resulting JPEG file, as a byte array.</returns>
		public byte[] SaveImage(Image8 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			JpegSaveOptions? options = fileSaveOptions as JpegSaveOptions;

			if (!image.IsGrayscale())
				return SaveImage(image.ToImage32(), imageMetadata, fileSaveOptions);

			if (!image.IsGrayscale256())
			{
				// It's grayscale, but the pixel values aren't in order.  We need
				// to fix that before TJ3 can save the data.
				image = image.Clone();
				image.ToGrayscale256();
			}

			// It's a legitimate 8-bit grayscale image, so use TJ3 to save it.
			IntPtr tjHandle = Tj3.Init(InitType.Compress);
			try
			{
				Tj3.Set(tjHandle, Param.ColorSpace, (int)ColorSpace.Gray);
				ApplyJpegOptions(tjHandle, options);
				byte[] compressed = Tj3.Compress8(tjHandle, image.Data, image.Width, image.Width, image.Height, PixelFormat.Gray);
				return compressed;
			}
			finally
			{
				Tj3.Destroy(tjHandle);
			}
		}

		private static void ApplyJpegOptions(IntPtr tjHandle, JpegSaveOptions? options)
		{
			Tj3.Set(tjHandle, Param.Quality, Math.Max(Math.Min(options?.Quality ?? DefaultQuality, 100), 1));

			Tj3.Set(tjHandle, Param.FastDct, (options?.FastDCT ?? false) ? 1 : 0);
			Tj3.Set(tjHandle, Param.Optimize, (options?.Optimize ?? false) ? 1 : 0);
			Tj3.Set(tjHandle, Param.Progressive, (options?.Progressive ?? false) ? 1 : 0);
			Tj3.Set(tjHandle, Param.Arithmetic, (options?.Arithmetic ?? false) ? 1 : 0);

			Tj3.Set(tjHandle, Param.SubSamp, (int)(options?.SubsamplingMode ?? JpegSubsamplingMode.Samp444));
		}
	}
}
