
namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// The basic structure of every PNG chunk, when loaded in memory.
	/// </summary>
	public interface IPngChunk
	{
		/// <summary>
		/// The chunk's four-character type code, as a string.
		/// </summary>
		string Type { get; }

		/// <summary>
		/// Write this chunk's data as binary output, not including the chunk size, the chunk
		/// type, or the chunk's CRC-32.  (i.e., this writes *just* the chunk's own data.)
		/// </summary>
		/// <param name="output">The output writer to write the chunk's data to.</param>
		void WriteData(OutputWriter output);
	}
}