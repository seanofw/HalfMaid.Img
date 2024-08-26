using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG pHYs chunk, which describes the desired physical dimensions of the image.
	/// </summary>
    public class PngPhysChunk : IPngChunk
	{
		/// <inheritdoc />
		public string Type => "pHYs";
		
		/// <summary>
		/// The number of pixels in the X dimension, for the given units.
		/// </summary>
		public uint XAxis { get; }

		/// <summary>
		/// The number of pixels in the Y dimension, for the given units.
		/// </summary>
		public uint YAxis { get; }

		/// <summary>
		/// The units of measurement for the X and Y values.
		/// </summary>
		public PngPhysUnits Units { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngPhysChunk(ReadOnlySpan<byte> data)
		{
			if (data.Length < 9)
				return;
			ReadOnlySpan<uint> uints = MemoryMarshal.Cast<byte, uint>(data.Slice(0, 8));
			XAxis = uints[0].BE();
			YAxis = uints[1].BE();
			Units = (PngPhysUnits)data[8];
		}

		/// <summary>
		/// Construct a new pHYs chunk with the given physical dimensions.
		/// </summary>
		/// <param name="xaxis">The number of pixels in the X dimension, for the given units.</param>
		/// <param name="yaxis">The number of pixels in the Y dimension, for the given units.</param>
		/// <param name="units">The units of measurement for the X and Y values.</param>
		public PngPhysChunk(uint xaxis, uint yaxis, PngPhysUnits units)
		{
			XAxis = xaxis;
			YAxis = yaxis;
			Units = units;
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			Span<uint> data = stackalloc uint[2];
			data[0] = XAxis.BE();
			data[1] = YAxis.BE();
			output.Write(MemoryMarshal.Cast<uint, byte>(data));
			output.WriteByte((byte)Units);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"pHYS: {XAxis},{YAxis} Units:{Units}";
	}
}
