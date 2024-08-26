using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Gif
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct GifHeader
	{
		public fixed byte Signature[3];
		public fixed byte Version[3];
		public ushort Width;
		public ushort Height;
		public GifHeaderFlags Flags;
		public byte BgColor;
		public byte AspectRatio;

		public bool HasGlobalPalette => (Flags & GifHeaderFlags.GlobalPalette) != 0;
		public int OriginalBits => (int)(Flags & GifHeaderFlags.OriginalBits) >> 4;
		public bool IsGlobalSorted => (Flags & GifHeaderFlags.GlobalSorted) != 0;
		public int BitsPerPixel => (int)(Flags & GifHeaderFlags.GlobalBits) + 1;
	}
}
