using System;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG tRNS transparency chunk, which provides alpha channels for paletted
	/// modes.  This can also identify a single transparent pixel value for grayscale
	/// and RGB modes, but that case is not supported:  If you want transparency,
	/// use an alpha-supporting mode instead of grafting transparency onto the side
	/// of an opaque mode.
	/// </summary>
    public class PngTrnsChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "tRNS";

		/// <summary>
		/// The alpha values.  For paletted modes, there will usually be one entry here
		/// per palette entry; but this may be shorter than the palette.  If it is shorter,
		/// all palette entries after those defined here will have an alpha of 255.
		/// </summary>
        public byte[] Alphas { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngTrnsChunk(ReadOnlySpan<byte> data)
        {
            Alphas = data.ToArray();
        }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.Write(Alphas);
        }

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"tRNS: {Alphas.Length} alpha values";
	}
}
