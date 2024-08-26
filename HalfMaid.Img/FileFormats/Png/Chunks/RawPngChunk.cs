using System;
using System.IO;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A "raw" PNG chunk, used for unknown chunks and chunks whose contents aren't
	/// important to this library's functionality.
	/// </summary>
    public class RawPngChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type { get; }

		/// <summary>
		/// The raw data of this chunk.
		/// </summary>
        public byte[] Data { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="type">What kind of chunk this is.</param>
		/// <param name="data">The raw chunk data to decode.</param>
		public RawPngChunk(string type, ReadOnlySpan<byte> data)
        {
            Type = type;
            Data = data.ToArray();
        }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.Write(Data);
        }

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"{Type}: Length {Data.Length}";
    }
}
