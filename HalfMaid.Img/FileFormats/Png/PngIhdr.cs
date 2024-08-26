using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// The PNG raw header, as a struct with fixed layout.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct PngIhdr
	{
		/// <summary>
		/// The width of the image, in pixels.
		/// </summary>
		public uint Width;

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		public uint Height;

		/// <summary>
		/// The bit depth of the image, from 1 to 16 bits per channel.
		/// </summary>
		public byte BitDepth;

		/// <summary>
		/// Which color type this image uses, and whether it has an alpha channel.
		/// </summary>
		public byte ColorType;

		/// <summary>
		/// Which compression method this image uses (always Deflate).
		/// </summary>
		public byte CompressionMethod;

		/// <summary>
		/// Which filter method this image uses (always Filtered).
		/// </summary>
		public byte FilterMethod;

		/// <summary>
		/// Which interlace method this image uses (either no interlacing or Adam7).
		/// </summary>
		public byte InterlaceMethod;
	}
}
