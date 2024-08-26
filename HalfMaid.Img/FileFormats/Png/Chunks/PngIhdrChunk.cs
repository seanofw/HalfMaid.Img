using System;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
    /// <summary>
    /// A PNG IHDR header chunk.
    /// </summary>
    public class PngIhdrChunk : IPngChunk
    {
		/// <inheritdoc />
		public string Type => "IHDR";

        /// <summary>
        /// The width of the image, in pixels.
        /// </summary>
        public int Width { get; }

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		public int Height { get; }

        /// <summary>
        /// The bit depth of the image, from 1 to 16 bits per channel.
        /// </summary>
        public byte BitDepth { get; }

        /// <summary>
        /// Which color type this image uses, and whether it has an alpha channel.
        /// </summary>
        public PngColorType ColorType { get; }

        /// <summary>
        /// Which compression method this image uses (always Deflate).
        /// </summary>
        public PngCompressionMethod CompressionMethod { get; }

        /// <summary>
        /// Which filter method this image uses (always Filtered).
        /// </summary>
        public PngFilterMethod FilterMethod { get; }

        /// <summary>
        /// Which interlace method this image uses (either no interlacing or Adam7).
        /// </summary>
        public PngInterlaceMethod InterlaceMethod { get; }

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="bytes">The raw chunk data to decode.</param>
		public PngIhdrChunk(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 13)
                throw new PngDecodeException("Cannot construct a PNG IHDR chunk from fewer than 13 bytes.");

            ReadOnlySpan<PngIhdr> ihdr = MemoryMarshal.Cast<byte, PngIhdr>(bytes);

            Width = (int)ihdr[0].Width.BE();
            Height = (int)ihdr[0].Height.BE();
            BitDepth = ihdr[0].BitDepth;
            ColorType = (PngColorType)ihdr[0].ColorType;
            CompressionMethod = (PngCompressionMethod)ihdr[0].CompressionMethod;
            FilterMethod = (PngFilterMethod)ihdr[0].FilterMethod;
            InterlaceMethod = (PngInterlaceMethod)ihdr[0].InterlaceMethod;
        }

		/// <summary>
		/// Construct a new PNG header chunk.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="bitDepth">The bit depth of the image, from 1 to 16 bits per channel.</param>
		/// <param name="colorType">Which color type this image uses, and whether it has an alpha channel.</param>
		/// <param name="compressionMethod">Which compression method this image uses (always Deflate).</param>
		/// <param name="filterMethod">Which filter method this image uses (always Filtered).</param>
		/// <param name="interlaceMethod">Which interlace method this image uses (either no interlacing or Adam7).</param>
		public PngIhdrChunk(int width, int height, int bitDepth, PngColorType colorType,
            PngCompressionMethod compressionMethod, PngFilterMethod filterMethod, PngInterlaceMethod interlaceMethod)
        {
            Width = width;
            Height = height;
            BitDepth = (byte)bitDepth;
            ColorType = colorType;
            CompressionMethod = compressionMethod;
            FilterMethod = filterMethod;
            InterlaceMethod = interlaceMethod;
        }

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
        {
            output.WriteByte((byte)((Width  >> 24) & 0xFF));
            output.WriteByte((byte)((Width  >> 16) & 0xFF));
            output.WriteByte((byte)((Width  >>  8) & 0xFF));
			output.WriteByte((byte)( Width         & 0xFF));

            output.WriteByte((byte)((Height >> 24) & 0xFF));
            output.WriteByte((byte)((Height >> 16) & 0xFF));
            output.WriteByte((byte)((Height >>  8) & 0xFF));
			output.WriteByte((byte)( Height        & 0xFF));

			output.WriteByte((byte)BitDepth);
			output.WriteByte((byte)ColorType);
			output.WriteByte((byte)CompressionMethod);
			output.WriteByte((byte)FilterMethod);
			output.WriteByte((byte)InterlaceMethod);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
            => $"IHDR: {Width}x{Height} Bits:{BitDepth} Color:{ColorType} Filter:{FilterMethod}";
    }
}
