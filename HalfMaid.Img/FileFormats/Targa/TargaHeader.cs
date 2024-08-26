using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Targa
{
	/// <summary>
	/// The shape of a Targa file's header.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct TargaHeader
	{
		public byte IdLength;
		public TargaPaletteType PaletteType;
		public TargaImageType ImageType;
		public ushort PaletteStart;
		public ushort PaletteLength;
		public byte PaletteBits;
		public ushort XOrigin;
		public ushort YOrigin;
		public ushort Width;
		public ushort Height;
		public byte BitsPerPixel;
		public TargaImageDescriptor ImageDescriptor;
	}
}
