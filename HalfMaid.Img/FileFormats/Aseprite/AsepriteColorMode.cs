namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Possible color modes for the image in an Aseprite cel.
	/// </summary>
	public enum AsepriteColorMode
	{
		/// <summary>
		/// A sequence of 32-bit RGBA tuples.
		/// </summary>
		Rgb,

		/// <summary>
		/// A grayscale image (up to 16 bits per pixel in the Aseprite file).
		/// </summary>
		Grayscale,

		/// <summary>
		/// Indexed color, 8 bits per pixel, and a palette.
		/// </summary>
		Indexed,

		/// <summary>
		/// A black-and-white bitmap, one bit per pixel.
		/// </summary>
		Bitmap,
	}
}