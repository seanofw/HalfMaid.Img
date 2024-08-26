using System;
using System.IO;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG tIMe chunk, which specifies when an image was created.
	/// </summary>
	public class PngTimeChunk : IPngChunk
	{
		/// <inheritdoc />
		public string Type => "tIME";

		#pragma warning disable CS1591
		public readonly ushort Year;
		public readonly byte Month;
		public readonly byte Day;
		public readonly byte Hour;
		public readonly byte Minute;
		public readonly byte Second;
		#pragma warning restore CS1591

		/// <summary>
		/// The full date-and-time, as a standard .NET DateTime struct, always in UTC.
		/// </summary>
		public readonly DateTime DateTime;

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngTimeChunk(ReadOnlySpan<byte> data)
		{
			if (data.Length < 7)
				throw new ArgumentException(nameof(data));

			Year = (ushort)(data[0] | (data[1] << 8));
			Month = data[2];
			Day = data[3];
			Hour = data[4];
			Minute = data[5];
			Second = data[6];

			DateTime = new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Utc);
		}

		/// <summary>
		/// Construct a new PNG tIMe chunk for the given .NET DateTime struct.
		/// </summary>
		/// <param name="dateTime">The date-and-time for this image.  If this is not in UTC,
		/// it will be converted to UTC.</param>
		public PngTimeChunk(DateTime dateTime)
		{
			if (dateTime.Kind == DateTimeKind.Local)
				dateTime = dateTime.ToUniversalTime();

			Year = (ushort)dateTime.Year;
			Month = (byte)dateTime.Month;
			Day = (byte)dateTime.Day;
			Hour = (byte)dateTime.Hour;
			Minute = (byte)dateTime.Minute;
			Second = (byte)dateTime.Second;

			DateTime = new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Utc);
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			output.WriteByte((byte)((Year >> 8) & 0xFF));
			output.WriteByte((byte)( Year       & 0xFF));
			output.WriteByte(Month);
			output.WriteByte(Day);
			output.WriteByte(Hour);
			output.WriteByte(Minute);
			output.WriteByte(Second);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"tIME: {DateTime}";
	}
}
