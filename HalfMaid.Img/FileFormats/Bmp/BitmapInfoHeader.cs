using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Bmp
{
	#pragma warning disable CS0649

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct BitmapInfoHeader
	{
		public int biSize;
		public int biWidth;
		public int biHeight;
		public short biPlanes;
		public short biBitCount;
		public int biCompression;
		public int biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public int biClrUsed;
		public int biClrImportant;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct CieXyz
	{
		public int X;
		public int Y;
		public int Z;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct CieXyzTriple
	{
		public CieXyz Red;
		public CieXyz Green;
		public CieXyz Blue;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct BitmapInfoHeaderV4
	{
		public BitmapInfoHeader v1Header;

		public uint bV4RedMask;
		public uint bV4GreenMask;
		public uint bV4BlueMask;
		public uint bV4AlphaMask;
		public int bV4CSType;
		public CieXyzTriple bV4EndPoints;
		public int bV4GammaRed;
		public int bV4GammaGreen;
		public int bV4GammaBlue;
	}
}
