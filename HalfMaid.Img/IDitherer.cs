using System;

namespace HalfMaid.Img
{
	/// <summary>
	/// Each of the different dither routines implements this simple interface.
	/// 
	/// Dithering is a fairly expensive operation that is often separable:  The setup
	/// for a given palette may be far more complex than the dither operation itself,
	/// which can then be quickly repeated for other images after the setup has been
	/// performed.
	/// </summary>
	public interface IDitherer
	{
		/// <summary>
		/// Set up the ditherer to use the given color palette as its target.
		/// </summary>
		/// <param name="palette">The palette to use.</param>
		/// <param name="useWeightedDistances">Whether to use linear distances (which is
		/// usually faster) or to prefer some kind of psychovisual weighting (slower, but
		/// more accurate).</param>
		void Setup(ReadOnlySpan<Color32> palette, bool useWeightedDistances = false);

		/// <summary>
		/// Perform dithering to 256 colors using the current color palette.
		/// </summary>
		/// <param name="image">The truecolor image to dither to the assigned palette.</param>
		/// <returns>A remapped or dithered image that uses only 8 bits per pixel and
		/// the currently-assigned color palette.</returns>
		Image8 Dither(Image32 image);

		/// <summary>
		/// Perform dithering to 256 colors using the current color palette.
		/// </summary>
		/// <param name="image">The truecolor image to dither to the assigned palette.</param>
		/// <returns>A remapped or dithered image that uses only 8 bits per pixel and
		/// the currently-assigned color palette.</returns>
		Image8 Dither(Image24 image);
	}
}
