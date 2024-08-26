namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// The shared shape of all text-based PNG chunks.
	/// </summary>
	public interface IPngTextChunk : IPngChunk
	{
		/// <summary>
		/// A keyword describing what kind of data this contains.  Per the PNG
		/// specification, this must be more than 0 characters and less than or
		/// equal to 79 characters.
		/// </summary>
		string Keyword { get; }

		/// <summary>
		/// The custom text for this keyword.
		/// </summary>
		string Text { get; }
	}
}
