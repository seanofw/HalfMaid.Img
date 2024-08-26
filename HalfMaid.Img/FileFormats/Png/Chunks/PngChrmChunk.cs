using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG chromaticity chunk.  Per the PNG spec, this takes precedence
	/// over an sRGB or gAMA chunk if all are included.
	/// </summary>
	public class PngChrmChunk : IPngChunk
	{
		/// <inheritdoc />
		public string Type => "cHRM";

		/// <summary>
		/// The raw white point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawWhitePointX { get; }

		/// <summary>
		/// The raw white point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawWhitePointY { get; }

		/// <summary>
		/// The raw red point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawRedX { get; }

		/// <summary>
		/// The raw red point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawRedY { get; }

		/// <summary>
		/// The raw green point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawGreenX { get; }

		/// <summary>
		/// The raw green point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawGreenY { get; }

		/// <summary>
		/// The raw blue point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawBlueX { get; }

		/// <summary>
		/// The raw blue point value, as a decimal fixed-point number with 5 digits after the decimal place.
		/// </summary>
		public uint RawBlueY { get; }

		/// <summary>
		/// The white point value, as a floating-point number.
		/// </summary>
		public double WhitePointX => RawWhitePointX * (1.0 / 100000);

		/// <summary>
		/// The white point value, as a floating-point number.
		/// </summary>
		public double WhitePointY => RawWhitePointY * (1.0 / 100000);

		/// <summary>
		/// The red point value, as a floating-point number.
		/// </summary>
		public double RedX => RawRedX * (1.0 / 100000);

		/// <summary>
		/// The red point value, as a floating-point number.
		/// </summary>
		public double RedY => RawRedY * (1.0 / 100000);

		/// <summary>
		/// The green point value, as a floating-point number.
		/// </summary>
		public double GreenX => RawGreenX * (1.0 / 100000);

		/// <summary>
		/// The green point value, as a floating-point number.
		/// </summary>
		public double GreenY => RawGreenY * (1.0 / 100000);

		/// <summary>
		/// The blue point value, as a floating-point number.
		/// </summary>
		public double BlueX => RawBlueX * (1.0 / 100000);

		/// <summary>
		/// The blue point value, as a floating-point number.
		/// </summary>
		public double BlueY => RawBlueY * (1.0 / 100000);

		/// <summary>
		/// Construct a new PNG chromaticity chunk from the given raw PNG byte data.
		/// </summary>
		/// <param name="data">The raw data to construct the chunk from.</param>
		public PngChrmChunk(ReadOnlySpan<byte> data)
		{
			if (data.Length < 32)
				return;

			ReadOnlySpan<uint> uints = MemoryMarshal.Cast<byte, uint>(data.Slice(0, 32));
			RawWhitePointX = uints[0].BE();
			RawWhitePointY = uints[1].BE();
			RawRedX = uints[2].BE();
			RawRedY = uints[3].BE();
			RawGreenX = uints[4].BE();
			RawGreenY = uints[5].BE();
			RawBlueX = uints[6].BE();
			RawBlueY = uints[7].BE();
		}

		/// <summary>
		/// Construct a PNG chromaticity chunk from raw decimal fixed-point numbers,
		/// each with 5 digits after the decimal place.
		/// </summary>
		/// <param name="whitePointX">The raw white point.</param>
		/// <param name="whitePointY">The raw white point.</param>
		/// <param name="redX">The raw red point.</param>
		/// <param name="redY">The raw red point.</param>
		/// <param name="greenX">The raw green point.</param>
		/// <param name="greenY">The raw green point.</param>
		/// <param name="blueX">The raw blue point.</param>
		/// <param name="blueY">The raw blue point.</param>
		public PngChrmChunk(uint whitePointX, uint whitePointY, uint redX, uint redY,
			uint greenX, uint greenY, uint blueX, uint blueY)
		{
			RawWhitePointX = whitePointX;
			RawWhitePointY = whitePointY;
			RawRedX = redX;
			RawRedY = redY;
			RawGreenX = greenX;
			RawGreenY = greenY;
			RawBlueX = blueX;
			RawBlueY = blueY;
		}

		/// <summary>
		/// Construct a PNG chromaticity chunk from its coordinates, in floating-point.
		/// </summary>
		/// <param name="whitePointX">The white point.</param>
		/// <param name="whitePointY">The white point.</param>
		/// <param name="redX">The red point.</param>
		/// <param name="redY">The red point.</param>
		/// <param name="greenX">The green point.</param>
		/// <param name="greenY">The green point.</param>
		/// <param name="blueX">The blue point.</param>
		/// <param name="blueY">The blue point.</param>
		public PngChrmChunk(double whitePointX, double whitePointY, double redX, double redY,
			double greenX, double greenY, double blueX, double blueY)
		{
			RawWhitePointX = (uint)(whitePointX * 100000 + 0.5);
			RawWhitePointY = (uint)(whitePointY * 100000 + 0.5);
			RawRedX = (uint)(redX * 100000 + 0.5);
			RawRedY = (uint)(redY * 100000 + 0.5);
			RawGreenX = (uint)(greenX * 100000 + 0.5);
			RawGreenY = (uint)(greenY * 100000 + 0.5);
			RawBlueX = (uint)(blueX * 100000 + 0.5);
			RawBlueY = (uint)(blueY * 100000 + 0.5);
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			Span<uint> rawValues = stackalloc uint[8];

			rawValues[0] = RawWhitePointX.BE();
			rawValues[1] = RawWhitePointY.BE();
			rawValues[2] = RawRedX.BE();
			rawValues[3] = RawRedY.BE();
			rawValues[4] = RawGreenX.BE();
			rawValues[5] = RawGreenY.BE();
			rawValues[6] = RawBlueX.BE();
			rawValues[7] = RawBlueY.BE();

			ReadOnlySpan<byte> rawBytes = MemoryMarshal.Cast<uint, byte>(rawValues);

			output.Write(rawBytes);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"cHRM: White:{WhitePointX:0.0#####},{WhitePointY:0.0#####} Red:{RedX:0.0#####},{RedY:0.0#####} Green:{GreenX:0.0#####},{GreenY:0.0#####} Blue:{BlueX:0.0#####},{BlueY:0.0#####}";
	}
}
