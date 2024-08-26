using System;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG sRGB chunk, for optional color calibration of the image when displayed.
	/// </summary>
	public class PngSrgbChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "sRGB";

		/// <summary>
		/// The sRGB rendering intent, for optional color calibration of the image when
		/// displayed.  This specifies one of four major classes of "intents," and it is
		/// up to the consuming application to interpret their meaning if it supports
		/// display or printing color profiles.
		/// </summary>
		public PngRenderingIntent RenderingIntent { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngSrgbChunk(ReadOnlySpan<byte> data)
        {
            if (data.Length <= 0)
                return;
            RenderingIntent = (PngRenderingIntent)data[0];
        }

		/// <summary>
		/// Construct a new sRGB chunk, with the given rendering intent.
		/// </summary>
		/// <param name="renderingIntent">The sRGB rendering intent, for optional color
		/// calibration of the image when displayed.  This specifies one of four major
		/// classes of "intents," and it is up to the consuming application to interpret
		/// their meaning if it supports display or printing color profiles.</param>
		public PngSrgbChunk(PngRenderingIntent renderingIntent)
        {
            RenderingIntent = renderingIntent;
        }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.WriteByte((byte)RenderingIntent);
        }

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"sRGB: Rendering intent: {RenderingIntent}";
	}
}
