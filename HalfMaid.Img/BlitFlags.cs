using System;

namespace HalfMaid.Img
{
	/// <summary>
	/// Flags that control how a blit or a drawing operation is performed.
	/// Not all flags are supported by all methods.
	/// </summary>
	[Flags]
	public enum BlitFlags
	{
		/// <summary>
		/// Copy or write pixels verbatim.  The source color will replace the
		/// destination pixel on all channels.  This is the default for most operations.
		/// </summary>
		Copy = 0,

		/// <summary>
		/// Copy or write pixels with simple transparency.  For RGBA images,
		/// pixels will be written when A != 0, and pixels will be skipped when
		/// A == 0.  For paletted images, nonzero pixels will be written, while
		/// zero-valued pixels will be skipped.
		/// </summary>
		Transparent = 1,

		/// <summary>
		/// Copy or write pixels, applying alpha.  This assumes that any RGB values
		/// are natural values (*not* premultiplied).  This mode is only meaningful for
		/// RGBA images and is not supported on paletted images.
		/// </summary>
		Alpha = 2,

		/// <summary>
		/// Copy or write pixels, applying alpha.  This assumes that any RGB values
		/// have been premultiplied by the alpha, which results in a faster operation at the
		/// cost of requiring the source data to be modified.  This mode is only meaningful for
		/// RGBA images and is not supported on paletted images.
		/// </summary>
		PMAlpha = 3,

		/// <summary>
		/// Multiply the source by the destination; the operation is D = D * S, where each
		/// of D and S are integer values in a range of 0 to 255 (or 0 to 65535 for 16-bit
		/// images).  This applies to all channels in RGBA modes, and to the single value
		/// in paletted or grayscale modes.
		/// </summary>
		Multiply = 4,

		/// <summary>
		/// Add the source to the destination; the operation is D = D + S, where each
		/// of D and S are integer values in a range of 0 to 255 (or 0 to 65535 for 16-bit
		/// images).  This applies to all channels in RGBA modes, and to the single value
		/// in paletted or grayscale modes.
		/// </summary>
		Add = 5,

		/// <summary>
		/// Subtract the source from the destination; the operation is D = D - S, where each
		/// of D and S are integer values in a range of 0 to 255 (or 0 to 65535 for 16-bit
		/// images).  This applies to all channels in RGBA modes, and to the single value
		/// in paletted or grayscale modes.
		/// </summary>
		Sub = 6,

		/// <summary>
		/// Reverse-subtract the destination from the source; the operation is D = S - D,
		/// where each of D and S are integer values in a range of 0 to 255 (or 0 to 65535
		/// for 16-bit images).  This applies to all channels in RGBA modes, and to the
		/// single value in paletted or grayscale modes.
		/// </summary>
		RSub = 7,

		/// <summary>
		/// This is the mask for the "mode" portion of the BlitFlags.  All bits outside
		/// this represent optional additional flags that can be applied to the mode.
		/// </summary>
		ModeMask = 0xFF << 0,

		/// <summary>
		/// Whether to *not* draw the very first pixel of a line.  This flag is useful
		/// if you are drawing a sequence of lines and you do not want each vertex pixel
		/// to be drawn more than once.  This flag is meaningful only for DrawLine and
		/// DrawBezier.
		/// </summary>
		SkipStart = 1 << 12,

		/// <summary>
		/// Flip the copied section horizontally during the blit.  This is supported
		/// by Blit() methods using Copy, Transparent, Alpha, and PMAlpha modes.
		/// </summary>
		FlipHorz = 1 << 13,

		/// <summary>
		/// Flip the copied section vertically during the blit.  This is supported
		/// by Blit() methods using Copy, Transparent, Alpha, and PMAlpha modes.
		/// </summary>
		FlipVert = 1 << 14,

		/// <summary>
		/// Skip the extra computations required to test a blit or draw
		/// operation to ensure that is within the bounds of the image, and to clip
		/// it if it lies outside.  This can increase performance, but that comes
		/// at a substantial cost in memory-safety:  Using this flag can corrupt
		/// the heap if you're not careful, or worse.<br />
		/// <br />
		/// You should only use this if both (A) performance is absolutely critical
		/// and (B) you can be 100% certain that your drawing or blitting coordinates
		/// lie within the image.<br />
		/// <br />
		/// This flag is potentially dangerous.  Don't use it unless you know
		/// what you're doing.
		/// </summary>
		FastUnsafe = 1 << 15, // Don't clip, just trust that the values are right
	}
}
