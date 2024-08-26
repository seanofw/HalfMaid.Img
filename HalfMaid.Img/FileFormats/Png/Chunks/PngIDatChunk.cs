using System.IO;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A single IDAT chunk in a PNG file.
	/// </summary>
    public class PngIDatChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "IDAT";

		/// <summary>
		/// The raw, compressed data in the IDAT chunk.  This is a portion of a
		/// Deflate stream, but not necessarily the entire Deflate stream.
		/// </summary>
        public byte[] CompressedData { get; }

		/// <summary>
		/// Construct a new IDAT chunk.
		/// </summary>
		/// <param name="compressedData">The raw, compressed data in the IDAT chunk.
		/// This is a portion of a Deflate stream, but not necessarily the entire
		/// Deflate stream.</param>
		public PngIDatChunk(byte[] compressedData)
            => CompressedData = compressedData;

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.Write(CompressedData);
        }

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"IDAT: {CompressedData.Length} bytes";
	}
}
