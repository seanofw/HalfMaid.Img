using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Bmp
{
	#pragma warning disable CS0649

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct BitmapFileHeader
	{
		public fixed byte bfType[2];
		public int bfSize;
		public short bfReserved1;
		public short bfReserved2;
		public int bfOffBits;
	}
}
