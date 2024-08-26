namespace HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo
{
	/// <summary>
	/// Pixel formats
	/// </summary>
	internal enum PixelFormat
	{
		/// <summary>
		/// RGB pixel format.  The red, green, and blue components in the image are
		/// stored in 3-sample pixels in the order R, G, B from lowest to highest
		/// memory address within each pixel.
		/// </summary>
		Rgb = 0,

		/// <summary>
		/// BGR pixel format.  The red, green, and blue components in the image are
		/// stored in 3-sample pixels in the order B, G, R from lowest to highest
		/// memory address within each pixel.
		/// </summary>
		Bgr = 1,

		/// <summary>
		/// RGBX pixel format.  The red, green, and blue components in the image are
		/// stored in 4-sample pixels in the order R, G, B from lowest to highest
		/// memory address within each pixel.  The X component is ignored when
		/// compressing and undefined when decompressing.
		/// </summary>
		Rgbx = 2,

		/// <summary>
		/// BGRX pixel format.  The red, green, and blue components in the image are
		/// stored in 4-sample pixels in the order B, G, R from lowest to highest
		/// memory address within each pixel.  The X component is ignored when
		/// compressing and undefined when decompressing.
		/// </summary>
		Bgrx = 3,

		/// <summary>
		/// XBGR pixel format.  The red, green, and blue components in the image are
		/// stored in 4-sample pixels in the order R, G, B from highest to lowest
		/// memory address within each pixel.  The X component is ignored when
		/// compressing and undefined when decompressing.
		/// </summary>
		Xbgr = 4,

		/// <summary>
		/// XRGB pixel format.  The red, green, and blue components in the image are
		/// stored in 4-sample pixels in the order B, G, R from highest to lowest
		/// memory address within each pixel.  The X component is ignored when
		/// compressing and undefined when decompressing.
		/// </summary>
		Xrgb = 5,

		/// <summary>
		/// Grayscale pixel format.  Each 1-sample pixel represents a luminance
		/// (brightness) level from 0 to the maximum sample value (255 for 8-bit
		/// samples, 4095 for 12-bit samples, and 65535 for 16-bit samples.)
		/// </summary>
		Gray = 6,

		/// <summary>
		/// RGBA pixel format.  This is the same as @ref RGBX, except that when
		/// decompressing, the X component is guaranteed to be equal to the maximum
		/// sample value, which can be interpreted as an opaque alpha channel.
		/// </summary>
		Rgba = 7,

		/// <summary>
		/// BGRA pixel format.  This is the same as @ref BGRX, except that when
		/// decompressing, the X component is guaranteed to be equal to the maximum
		/// sample value, which can be interpreted as an opaque alpha channel.
		/// </summary>
		Bgra = 8,

		/// <summary>
		/// ABGR pixel format.  This is the same as @ref XBGR, except that when
		/// decompressing, the X component is guaranteed to be equal to the maximum
		/// sample value, which can be interpreted as an opaque alpha channel.
		/// </summary>
		Abgr = 9,

		/// <summary>
		/// ARGB pixel format.  This is the same as @ref XRGB, except that when
		/// decompressing, the X component is guaranteed to be equal to the maximum
		/// sample value, which can be interpreted as an opaque alpha channel.
		/// </summary>
		Argb = 10,

		/// <summary>
		/// CMYK pixel format.  Unlike RGB, which is an additive color model used
		/// primarily for display, CMYK (Cyan/Magenta/Yellow/Key) is a subtractive
		/// color model used primarily for printing.  In the CMYK color model, the
		/// value of each color component typically corresponds to an amount of cyan,
		/// magenta, yellow, or black ink that is applied to a white background.  In
		/// order to convert between CMYK and RGB, it is necessary to use a color
		/// management system (CMS.)  A CMS will attempt to map colors within the
		/// printer's gamut to perceptually similar colors in the display's gamut and
		/// vice versa, but the mapping is typically not 1:1 or reversible, nor can it
		/// be defined with a simple formula.  Thus, such a conversion is out of scope
		/// for a codec library.  However, the TurboJPEG API allows for compressing
		/// packed-pixel CMYK images into YCCK JPEG images (see #TJCS_YCCK) and
		/// decompressing YCCK JPEG images into packed-pixel CMYK images.
		/// </summary>
		Cmyk = 11,

		/// <summary>
		/// Unknown pixel format.  Currently this is only used by #tj3LoadImage8(),
		/// #tj3LoadImage12(), and #tj3LoadImage16().
		/// </summary>
		Unknown = -1,
	}
}
