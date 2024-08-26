using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// The header data for an frame in the Aseprite metafile.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct AsepriteFrameHeader
	{
		public uint Size;
		public ushort Magic;
		public ushort Chunks;
		public ushort Duration;
		public ushort Padding;
		public uint NChunks;
	}
}
