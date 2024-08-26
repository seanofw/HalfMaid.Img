namespace HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo
{
	/// <summary>
	/// JPEG colorspaces
	/// </summary>
	internal enum ColorSpace
	{
		/// <summary>
		/// RGB colorspace.  When compressing the JPEG image, the R, G, and B
		/// components in the source image are reordered into image planes, but no
		/// colorspace conversion or subsampling is performed.  RGB JPEG images can be
		/// compressed from and decompressed to packed-pixel images with any of the
		/// extended RGB or grayscale pixel formats, but they cannot be compressed
		/// from or decompressed to planar YUV images.
		/// </summary>
		Rgb = 0,

		/// <summary>
		/// YCbCr colorspace.  YCbCr is not an absolute colorspace but rather a
		/// mathematical transformation of RGB designed solely for storage and
		/// transmission.  YCbCr images must be converted to RGB before they can
		/// actually be displayed.  In the YCbCr colorspace, the Y (luminance)
		/// component represents the black &amp; white portion of the original image, and
		/// the Cb and Cr (chrominance) components represent the color portion of the
		/// original image.  Originally, the analog equivalent of this transformation
		/// allowed the same signal to drive both black &amp; white and color televisions,
		/// but JPEG images use YCbCr primarily because it allows the color data to be
		/// optionally subsampled for the purposes of reducing network or disk usage.
		/// YCbCr is the most common JPEG colorspace, and YCbCr JPEG images can be
		/// compressed from and decompressed to packed-pixel images with any of the
		/// extended RGB or grayscale pixel formats.  YCbCr JPEG images can also be
		/// compressed from and decompressed to planar YUV images.
		/// </summary>
		YCbCr = 1,

		/// <summary>
		/// Grayscale colorspace.  The JPEG image retains only the luminance data (Y
		/// component), and any color data from the source image is discarded.
		/// Grayscale JPEG images can be compressed from and decompressed to
		/// packed-pixel images with any of the extended RGB or grayscale pixel
		/// formats, or they can be compressed from and decompressed to planar YUV
		/// images.
		/// </summary>
		Gray = 2,

		/// <summary>
		/// CMYK colorspace.  When compressing the JPEG image, the C, M, Y, and K
		/// components in the source image are reordered into image planes, but no
		/// colorspace conversion or subsampling is performed.  CMYK JPEG images can
		/// only be compressed from and decompressed to packed-pixel images with the
		/// CMYK pixel format.
		/// </summary>
		Cmyk = 3,

		/// <summary>
		/// YCCK colorspace.  YCCK (AKA "YCbCrK") is not an absolute colorspace but
		/// rather a mathematical transformation of CMYK designed solely for storage
		/// and transmission.  It is to CMYK as YCbCr is to RGB.  CMYK pixels can be
		/// reversibly transformed into YCCK, and as with YCbCr, the chrominance
		/// components in the YCCK pixels can be subsampled without incurring major
		/// perceptual loss.  YCCK JPEG images can only be compressed from and
		/// decompressed to packed-pixel images with the CMYK pixel format.
		/// </summary>
		Ycck = 4,
	}
}
