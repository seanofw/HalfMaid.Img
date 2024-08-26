using System;
using System.IO;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
    /// <summary>
    /// A PNG gAMA chunk.
    /// </summary>
    public class PngGamaChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "gAMA";

        /// <summary>
        /// The raw gamma value, as a fixed-point decimal with 5 digits after the decimal point.
        /// </summary>
        public uint RawGamma { get; }

        /// <summary>
        /// The gamma value, as a floating-point number.
        /// </summary>
        public double Gamma => RawGamma * (1.0 / 100000);

        /// <summary>
        /// Decode this chunk from the given raw byte array.
        /// </summary>
        /// <param name="data">The raw chunk data to decode.</param>
        public PngGamaChunk(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
                RawGamma = 0;
            else
                RawGamma = data[3] | (uint)data[2] << 8 | (uint)data[1] << 16 | (uint)data[0] << 24;
        }

		/// <summary>
		/// Construct a new gAMA chunk for the given raw value.
		/// </summary>
		/// <param name="value">The raw gamma value, as a fixed-point decimal with 5 digits after the decimal point.</param>
		public PngGamaChunk(uint value)
        {
            RawGamma = value;
        }

		/// <summary>
		/// Construct a new gAMA chunk for the given gamma value.
		/// </summary>
		/// <param name="value">The gamma value, as a floating-point number.</param>
		public PngGamaChunk(double value)
		{
			RawGamma = (uint)(value * 100000 + 0.5);
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.WriteByte((byte)((RawGamma >> 24) & 0xFF));
			output.WriteByte((byte)((RawGamma >> 16) & 0xFF));
			output.WriteByte((byte)((RawGamma >>  8) & 0xFF));
			output.WriteByte((byte)( RawGamma        & 0xFF));
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"gAMA: {Gamma:0.0#####} (raw value {RawGamma})";
	}
}
