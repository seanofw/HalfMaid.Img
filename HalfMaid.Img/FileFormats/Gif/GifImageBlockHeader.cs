using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Gif
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct GifImageBlockHeader
	{
		public short X;
		public short Y;
		public ushort Width;
		public ushort Height;
		public GifImageBlockFlags Flags;
	}
}