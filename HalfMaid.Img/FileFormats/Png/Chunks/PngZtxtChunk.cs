using System;
using HalfMaid.Img.Compression;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG zTXt chunk.
	/// </summary>
	public class PngZtxtChunk : IPngTextChunk
	{
		/// <inheritdoc />
		public string Type => "zTXt";

		/// <inheritdoc />
		public string Keyword { get; }

		/// <summary>
		/// The compression method that was used (Deflate).
		/// </summary>
		public PngCompressionMethod CompressionMethod { get; }

		/// <summary>
		/// The raw compressed bytes of this chunk.
		/// </summary>
		public byte[] CompressedBytes { get; }

		/// <summary>
		/// The uncompressed bytes of this chunk.
		/// </summary>
		public byte[] UncompressedBytes => _uncompressedBytes ??= Zlib.Inflate(CompressedBytes);
		private byte[]? _uncompressedBytes;

		/// <inheritdoc />
		public string Text => _text ??= PngLoader.Latin1.GetString(UncompressedBytes);
		private string? _text;

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngZtxtChunk(ReadOnlySpan<byte> data)
		{
			int i;
			for (i = 0; i < data.Length; i++)
				if (data[i] == 0)
					break;
			Keyword = PngLoader.Latin1.GetString(data.Slice(0, i).ToArray());
			i++;

			CompressionMethod = i < data.Length ? (PngCompressionMethod)data[i] : default;
			i++;

			if (i < data.Length)
				CompressedBytes = data.Slice(i, data.Length - i).ToArray();
			else
				CompressedBytes = new byte[0];
		}

		/// <summary>
		/// Construct a new PNG text chunk.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="text">The text for that keyword.</param>
		public PngZtxtChunk(string keyword, string text)
		{
			Keyword = keyword;
			CompressionMethod = PngCompressionMethod.Deflate;
			CompressedBytes = Zlib.Deflate(PngLoader.Latin1.GetBytes(text));
		}

		/// <summary>
		/// Construct a new PNG text chunk from compressed Deflated bytes.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="method">The compression method that was used (Deflate).</param>
		/// <param name="compressedBytes">The compressed bytes of the text for that keyword.</param>
		public PngZtxtChunk(string keyword, PngCompressionMethod method, byte[] compressedBytes)
		{
			Keyword = keyword;
			CompressionMethod = method;
			CompressedBytes = compressedBytes;
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			output.Write(PngLoader.Latin1.GetBytes(Keyword));
			output.WriteByte(0);

			output.WriteByte((byte)CompressionMethod);

			output.Write(CompressedBytes);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"zTXt: '{Keyword}': \"{Text}\"";
	}
}
