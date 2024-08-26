using System;
using System.IO;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// An IEND PNG chunk.
	/// </summary>
    public class PngIEndChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "IEND";

		/// <summary>
		/// Construct a new, empty IEND chunk.
		/// </summary>
        public PngIEndChunk() { }
		
		/// <summary>
		/// Construct a new, empty IEND chunk from the given bytes (which are ignored).
		/// </summary>
		public PngIEndChunk(ReadOnlySpan<byte> data) { }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
        }

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> "IEND";
	}
}
