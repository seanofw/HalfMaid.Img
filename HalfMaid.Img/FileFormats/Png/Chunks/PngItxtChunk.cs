using System;
using System.Text;
using HalfMaid.Img.Compression;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG iTXt chunk.
	/// </summary>
	public class PngItxtChunk : IPngTextChunk
	{
		/// <inheritdoc />
		public string Type => "iTXt";

		/// <inheritdoc />
		public string Keyword { get; }

		/// <summary>
		/// The language this internationalized text was written in (optional).
		/// </summary>
		public string Language { get; }

		/// <summary>
		/// A translated version of the keyword (optional).
		/// </summary>
		public string TranslatedKeyword { get; }

		/// <summary>
		/// Whether the bytes are stored compressed (true) or uncompressed (false).
		/// </summary>
		public bool CompressionFlag { get; }

		/// <summary>
		/// The compression method that was used (Deflate).
		/// </summary>
		public PngCompressionMethod CompressionMethod { get; }

		/// <summary>
		/// The compressed bytes of the text for that keyword.
		/// </summary>
		public byte[] CompressedBytes { get; }

		/// <summary>
		/// The uncompressed version of the raw text data.
		/// </summary>
		public byte[] UncompressedBytes => _uncompressedBytes
			??= CompressionFlag ? Zlib.Inflate(CompressedBytes) : CompressedBytes;
		private byte[]? _uncompressedBytes;

		/// <inheritdoc />
		public string Text => _text ??= Encoding.UTF8.GetString(UncompressedBytes);
		private string? _text;

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngItxtChunk(ReadOnlySpan<byte> data)
		{
			int i;
			for (i = 0; i < data.Length; i++)
				if (data[i] == 0)
					break;
			Keyword = PngLoader.Latin1.GetString(data.Slice(0, i).ToArray());
			i++;

			CompressionFlag = i < data.Length ? data[i] != 0 : default;
			i++;

			CompressionMethod = i < data.Length ? (PngCompressionMethod)data[i] : default;
			i++;

			int start = i;
			for (; i < data.Length; i++)
				if (data[i] == 0)
					break;
			Language = PngLoader.Latin1.GetString(data.Slice(start, i - start).ToArray());
			i++;

			start = i;
			for (; i < data.Length; i++)
				if (data[i] == 0)
					break;
			TranslatedKeyword = Encoding.UTF8.GetString(data.Slice(start, i - start).ToArray());
			i++;

			if (i < data.Length)
				CompressedBytes = data.Slice(i, data.Length - i).ToArray();
			else
				CompressedBytes = new byte[0];
		}

		/// <summary>
		/// Construct a new PNG text chunk from compressed Deflated bytes.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="language">The language this internationalized text was written in (optional).</param>
		/// <param name="translatedKeyword">A translated version of the keyword (optional).</param>
		/// <param name="compressionFlag">Whether the bytes are stored compressed (true) or uncompressed (false).</param>
		/// <param name="method">The compression method that was used (Deflate).</param>
		/// <param name="compressedBytes">The compressed bytes of the text for that keyword.</param>
		public PngItxtChunk(string keyword, string language, string translatedKeyword,
			bool compressionFlag, PngCompressionMethod method, byte[] compressedBytes)
		{
			Keyword = keyword;
			Language = language;
			TranslatedKeyword = translatedKeyword;
			CompressionFlag = compressionFlag;
			CompressionMethod = method;
			CompressedBytes = compressedBytes;
		}

		/// <summary>
		/// Construct a new PNG text chunk from the given text.
		/// </summary>
		/// <param name="keyword">The keyword describing this text.</param>
		/// <param name="language">The language this internationalized text was written in (optional).</param>
		/// <param name="translatedKeyword">A translated version of the keyword (optional).</param>
		/// <param name="text">The text for that keyword.</param>
		public PngItxtChunk(string keyword, string language, string translatedKeyword, string text)
		{
			Keyword = keyword;
			Language = language;
			TranslatedKeyword = translatedKeyword;
			CompressionFlag = true;
			CompressionMethod = PngCompressionMethod.Deflate;
			CompressedBytes = Zlib.Deflate(Encoding.UTF8.GetBytes(text));
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			byte[] keyword = PngLoader.Latin1.GetBytes(Keyword);
			output.Write(keyword);
			output.WriteByte(0);

			output.WriteByte((byte)(CompressionFlag ? 1 : 0));
			output.WriteByte((byte)CompressionMethod);

			byte[] language = PngLoader.Latin1.GetBytes(Language);
			output.Write(language);
			output.WriteByte(0);

			byte[] translatedKeyword = PngLoader.Latin1.GetBytes(TranslatedKeyword);
			output.Write(translatedKeyword);
			output.WriteByte(0);

			output.Write(CompressedBytes);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"iTXt: '{Keyword}': \"{Text}\" ({Language})";
	}
}
