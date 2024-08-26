using System;

namespace HalfMaid.Img.FileFormats.Targa
{
	/// <summary>
	/// The Targa image-descriptor flags byte.
	/// </summary>
	[Flags]
	public enum TargaImageDescriptor : byte
	{
		/// <summary>
		/// A mask for the bits describing how many bits there are in an alpha channel, if any.
		/// </summary>
		AlphaChannelMask = (0xF << 0),

		/// <summary>
		/// Whether this image data is flipped horizontally.
		/// </summary>
		FlipHorz = (1 << 4),

		/// <summary>
		/// Whether this image data is flipped vertically.
		/// </summary>
		FlipVert = (1 << 5),

		/// <summary>
		/// A mask for what kind of interleaving this image uses.
		/// </summary>
		InterleavingMask = (0x3 << 6),

		/// <summary>
		/// This image is not interleaved.
		/// </summary>
		NotInterleaved = (0 << 6),

		/// <summary>
		/// This image uses two-level interleaving.
		/// </summary>
		TwoWayInterleaved = (1 << 6),

		/// <summary>
		/// This image uses both four-level interleaving.
		/// </summary>
		FourWayInterleaved = (2 << 6),
	}
}
