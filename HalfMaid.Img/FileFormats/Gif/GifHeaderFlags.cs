using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// GIF header flags.
	/// </summary>
	[Flags]
	public enum GifHeaderFlags : byte
	{
		/// <summary>
		/// This file has a palette
		/// </summary>
		GlobalPalette = 0x80,

		/// <summary>
		/// Original number of color bits (useless)
		/// </summary>
		OriginalBits = 0x70,

		/// <summary>
		/// The palette has been sorted
		/// </summary>
		GlobalSorted = 0x08,

		/// <summary>
		/// Palette size = (1 &lt;&lt; (GlobalBits + 1))
		/// </summary>
		GlobalBits = 0x07,
	};
}
