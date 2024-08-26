using System;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
    /// <summary>
    /// A PNG PLTE chunk, which stores the palette for paletted images.
    /// </summary>
    public class PngPlteChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "PLTE";

        /// <summary>
        /// The palette itself.  This should be treated as immutable data, but for
        /// performance reasons, it is presented to consumers as a plain array.
        /// </summary>
        public Color32[] Colors { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngPlteChunk(ReadOnlySpan<byte> data)
        {
			Color32[] colors = new Color32[data.Length / 3];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32(data[i * 3], data[i * 3 + 1], data[i * 3 + 2]);
            }
            Colors = colors;
        }

        /// <summary>
        /// Construct a new PLTE chunk from the given color palette.
        /// </summary>
        /// <param name="colors"></param>
        public PngPlteChunk(ReadOnlySpan<Color32> colors)
        {
            Colors = colors.ToArray();
        }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            byte[] data = new byte[Colors.Length * 3];
            for (int i = 0; i < Colors.Length; i++)
            {
                data[i * 3    ] = Colors[i].R;
				data[i * 3 + 1] = Colors[i].G;
				data[i * 3 + 2] = Colors[i].B;
			}
            output.Write(data);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"PLTE: {Colors.Length} palette entries";
	}
}
