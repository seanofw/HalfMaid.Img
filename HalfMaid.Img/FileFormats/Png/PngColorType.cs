namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// PNG color types.
	/// </summary>
	public enum PngColorType : byte
	{
		/// <summary>
		/// Grayscale image, from 1 to 16 bits per pixel, and no alpha channel.
		/// </summary>
		Grayscale = 0,

		/// <summary>
		/// RGB image, from 1 to 16 bits per color channel, and no alpha channel.
		/// </summary>
		Rgb = 2,

		/// <summary>
		/// Paletted image, from 1 to 8 bits per pixel, and a required PLTE chunk.
		/// </summary>
		Paletted = 3,

		/// <summary>
		/// Grayscale image, from 1 to 16 bits per pixel, and an alpha channel.
		/// </summary>
		GrayscaleAlpha = 4,

		/// <summary>
		/// RGBA image, from 1 to 16 bits per color channel, and an alpha channel.
		/// </summary>
		Rgba = 6,
	}
}
