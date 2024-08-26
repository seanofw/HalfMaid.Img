namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Possible kinds of cels, as they are found in the Aseprite file.
	/// </summary>
	public enum AsepriteCelKind : ushort
	{
		/// <summary>
		/// A raw, uncompressed cel (a plain image).
		/// </summary>
		Raw = 0,

		/// <summary>
		/// A link (reference) to another cel.
		/// </summary>
		Link = 1,

		/// <summary>
		/// A compressed cel (using deflate compression).
		/// </summary>
		Compressed = 2,
	}
}