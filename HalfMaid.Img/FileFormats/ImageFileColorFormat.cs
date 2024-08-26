namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// Different color-format types that can be returned by the image loader.  This
	/// is provided as a hint to indicate what the original file data is.
	/// </summary>
	public enum ImageFileColorFormat
	{
		/// <summary>
		/// Unknown also means "unsupported" or "unrecognized."
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// This image is black-and-white (or color A-on-color B), and uses 1 bit per pixel.
		/// </summary>
		BlackAndWhite1Bit,

		/// <summary>
		/// This file is paletted, and each pixel can be represented in 4 bits or less.
		/// </summary>
		Paletted4Bit,

		/// <summary>
		/// This file is paletted, and each pixel can be represented in 5 to 8 bits.
		/// </summary>
		Paletted8Bit,

		/// <summary>
		/// This file is paletted, and each pixel can be represented in 9 to 16 bits.
		/// </summary>
		Paletted16Bit,

		/// <summary>
		/// This file is grayscale, and it uses 8 bits or less for its single channel.
		/// </summary>
		Gray8Bit,

		/// <summary>
		/// This file is grayscale, and it uses 9 or more bits for its single channel.
		/// </summary>
		Gray16Bit,

		/// <summary>
		/// This file is grayscale-with-alpha, and it uses 8 bits or less each of its two channels.
		/// </summary>
		GrayAlpha8Bit,

		/// <summary>
		/// This file is grayscale-with-alpha, and it uses 9 or more bits for each of its two channels.
		/// </summary>
		GrayAlpha16Bit,

		/// <summary>
		/// This file is RGB.  Each channel uses 8 bits or less.
		/// </summary>
		Rgb24Bit,

		/// <summary>
		/// This file is RGB.  Each channel uses 9 or more bits.
		/// </summary>
		Rgb48Bit,

		/// <summary>
		/// This file is RGBA.  Each channel uses 8 bits or less.
		/// </summary>
		Rgba32Bit,

		/// <summary>
		/// This file is RGBA.  Each channel uses 9 or more bits.
		/// </summary>
		Rgba64Bit,
	}
}
