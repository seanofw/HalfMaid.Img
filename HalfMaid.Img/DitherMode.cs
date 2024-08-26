namespace HalfMaid.Img
{
	/// <summary>
	/// What kind of dithering algorithm to use, either ordered dithering or one of
	/// the error-diffused techniques.
	/// </summary>
	public enum DitherMode
	{
		/// <summary>
		/// Map each source color to the nearest color in the palette.
		/// </summary>
		Nearest = 0,

		/// <summary>
		/// Use an ordered 2x2 dither.
		/// </summary>
		Ordered2x2,

		/// <summary>
		/// Use an ordered 4x4 dither.
		/// </summary>
		Ordered4x4,

		/// <summary>
		/// Use an ordered 8x8 dither.
		/// </summary>
		Ordered8x8,

		/// <summary>
		/// Use Floyd-Steinberg error-diffusion.
		/// </summary>
		FloydSteinberg,

		/// <summary>
		/// Use Atkinson error-diffusion.
		/// </summary>
		Atkinson,

		/// <summary>
		/// Use Stucki error-diffusion.
		/// </summary>
		Stucki,

		/// <summary>
		/// Use Burkes error-diffusion.
		/// </summary>
		Burkes,

		/// <summary>
		/// Use Jarvis error-diffusion.
		/// </summary>
		Jarvis,
	}
}