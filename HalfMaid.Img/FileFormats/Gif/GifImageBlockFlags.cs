using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// GIF image block flags.
	/// </summary>
	[Flags]
	public enum GifImageBlockFlags : byte
	{
		/// <summary>
		/// Block has a local palette
		/// </summary>
		LocalPalette = 0x80,

		/// <summary>
		/// Block is interlaced
		/// </summary>
		Interlaced = 0x40,

		/// <summary>
		/// Local palette is sorted with most important colors first (not really relevant anymore).
		/// </summary>
		LocalSorted = 0x20,

		/// <summary>
		/// Palette size = (1 &lt;&lt; (LocalBits + 1))
		/// </summary>
		LocalBits = 0x07,
	};
}
