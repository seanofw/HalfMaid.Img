using System;

namespace HalfMaid.Img
{
	/// <summary>
	/// Different algorithms for performing image resampling.
	/// </summary>
	/// <remarks>
	/// As with all resampling code, boundary conditions occur at the edge
	/// of the image.  Additional flags let you decide how the resample system
	/// will resolve those boundary conditions.  You can provide one of
	/// these flags for each edge, but you'll probably use the same value
	/// for every edge.  Combine these flags with a curve flag above to
	/// get a complete mode.
	/// <list>
	/// <item><b>Back</b> - If the resampler crosses an edge, it should reverse
	///              direction and continue sampling from pixels it has
	///              already sampled.</item>
	///
	/// <item><b>Wrap</b> - If the resampler crosses an edge, it should continue
	///              sampling from pixels in the same row or column
	///              starting at the opposite edge.</item>
	/// </list>
	/// </remarks>
	[Flags]
	public enum ResampleMode
	{
		/// <summary>
		/// Nearest-neighbor or "box" sampling.
		/// </summary>
		Box = 0,

		/// <summary>
		/// Bilinear filtering or "triangle" sampling.
		/// </summary>
		Triangle = 1,

		/// <summary>
		/// Filtering using the Hermite curve.
		/// </summary>
		Hermite = 2,

		/// <summary>
		/// The Bell function (3rd-order, or quadratic B-spline).
		/// </summary>
		Bell = 4,

		/// <summary>
		/// Fit and sample a 4th-order (cubic) B-spline.
		/// </summary>
		BSpline = 6,

		/// <summary>
		/// The two-parameter cubic function proposed by Mitchell &amp; Netravali (see SIGGRAPH 88).
		/// </summary>
		Mitchell = 8,

		/// <summary>
		/// 3-lobed sinusoidal Lanczos approximation.
		/// </summary>
		Lanczos3 = 3,

		/// <summary>
		/// 5-lobed sinusoidal Lanczos approximation.
		/// </summary>
		Lanczos5 = 5,

		/// <summary>
		/// 7-lobed sinusoidal Lanczos approximation.
		/// </summary>
		Lanczos7 = 7,

		/// <summary>
		/// 9-lobed sinusoidal Lanczos approximation.
		/// </summary>
		Lanczos9 = 9,

		/// <summary>
		/// 11-lobed sinusoidal Lanczos approximation.
		/// </summary>
		Lanczos11 = 11,

		/// <summary>
		/// A mask for the bits indicating the core resampling algorithm to use.
		/// </summary>
		CurveMask = 0xFF,

		/// <summary>
		/// At the top edge, sample going back down into the image.
		/// </summary>
		TopBack = 0x000000,

		/// <summary>
		/// At the top edge, wrap and sample from the bottom of the image.
		/// </summary>
		TopWrap = 0x000100,

		/// <summary>
		/// Mask bits for the top-edge mode.
		/// </summary>
		TopMode = 0x000F00,

		/// <summary>
		/// At the bottom edge, sample going back up into the image.
		/// </summary>
		BottomBack = 0x000000,

		/// <summary>
		/// At the bottom edge, wrap and sample from the top of the image.
		/// </summary>
		BottomWrap = 0x001000,

		/// <summary>
		/// Mask bits for the bottom-edge mode.
		/// </summary>
		BottomMode = 0x00F000,

		/// <summary>
		/// At the left edge, sample going rightward back into the image.
		/// </summary>
		LeftBack = 0x000000,

		/// <summary>
		/// At the left edge, wrap and sample from the right side of the image.
		/// </summary>
		LeftWrap = 0x010000,

		/// <summary>
		/// Mask bits for the left-edge mode.
		/// </summary>
		LeftMode = 0x0F0000,

		/// <summary>
		/// At the right edge, sample going leftward back into the image.
		/// </summary>
		RightBack = 0x000000,

		/// <summary>
		/// At the right edge, wrap and sample from the left side of the image.
		/// </summary>
		RightWrap = 0x100000,

		/// <summary>
		/// Mask bits for the right-edge mode.
		/// </summary>
		RightMode = 0xF00000,

		/// <summary>
		/// At the top or bottom edge, sample going back into the image.
		/// </summary>
		VertBack = (TopBack | BottomBack),

		/// <summary>
		/// At the top or bottom edge, wrap and sample from the other side of the image.
		/// </summary>
		VertWrap = (TopWrap | BottomWrap),

		/// <summary>
		/// At either horizontal edge, sample going back into the image.
		/// </summary>
		HorzBack = (LeftBack | RightBack),

		/// <summary>
		/// At the left or right edge, wrap and sample from the other side of the image.
		/// </summary>
		HorzWrap = (LeftWrap | RightWrap),

		/// <summary>
		/// At any edge, sample going back into the image.
		/// </summary>
		Back = (HorzBack | VertBack),

		/// <summary>
		/// At any edge, wrap to the far opposing edge.
		/// </summary>
		Wrap = (HorzWrap | VertWrap),
	}
}

