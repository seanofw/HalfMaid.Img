using System;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG tEXt chunk.
	/// </summary>
	public class PngTextChunk : IPngTextChunk
	{
		/// <inheritdoc />
		public string Type => "tEXt";

		/// <inheritdoc />
		public string Keyword { get; }

		/// <summary>
		/// The raw bytes of the text.
		/// </summary>
		public byte[] RawBytes { get; }

		/// <inheritdoc />
		public string Text => _text ??= PngLoader.Latin1.GetString(RawBytes);
		private string? _text;

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngTextChunk(ReadOnlySpan<byte> data)
		{
			int i;
			for (i = 0; i < data.Length; i++)
				if (data[i] == 0)
					break;
			Keyword = PngLoader.Latin1.GetString(data.Slice(0, i).ToArray());
			i++;

			if (i < data.Length)
				RawBytes = data.Slice(i, data.Length - i).ToArray();
			else
				RawBytes = new byte[0];
		}

		/// <summary>
		/// Construct a new PNG text chunk.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="rawBytes">The raw bytes of the text.</param>
		public PngTextChunk(string keyword, byte[] rawBytes)
		{
			Keyword = keyword;
			RawBytes = rawBytes;
		}

		/// <summary>
		/// Construct a new PNG text chunk.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="text">The text for that keyword.</param>
		public PngTextChunk(string keyword, string text)
		{
			Keyword = keyword;
			RawBytes = PngLoader.Latin1.GetBytes(text);
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			output.Write(PngLoader.Latin1.GetBytes(Keyword));
			output.WriteByte(0);

			output.Write(RawBytes);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"tEXt: '{Keyword}': \"{Text}\"";
	}
}
