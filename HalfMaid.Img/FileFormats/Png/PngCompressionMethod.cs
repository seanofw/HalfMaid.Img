namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// Available PNG compression methods (all one of it!).
	/// </summary>
	public enum PngCompressionMethod : byte
	{
		/// <summary>
		/// This file or chunk uses Deflate compression with a Zlib header.
		/// </summary>
		Deflate = 0,
	}
}
