using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// The raw Aseprite header, from the Aseprite documentation.  See
	/// https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct AsepriteHeader
	{
		/// <summary>
		/// File size, in bytes.
		/// </summary>
		public uint Size;

		/// <summary>
		/// Magic number (always 0xA5E0 little-endian).
		/// </summary>
		public ushort Magic;

		/// <summary>
		/// Number of frames in the image's animation (if any).
		/// </summary>
		public ushort Frames;

		/// <summary>
		/// Width of the image, in pixels.
		/// </summary>
		public ushort Width;

		/// <summary>
		/// Height of the image, in pixels.
		/// </summary>
		public ushort Height;

		/// <summary>
		/// Bit depth of each pixel (32 = RGBA, 16 = grayscale, 8 = indexed color).
		/// </summary>
		public ushort Depth;

		/// <summary>
		/// Optional flags.
		/// </summary>
		public AsepriteHeaderFlags Flags;

		/// <summary>
		/// Speed of the animation, in milliseconds per frame [DEPRECATED].
		/// </summary>
		public ushort Speed;

		/// <summary>
		/// Always 0.
		/// </summary>
		public uint Reserved1;

		/// <summary>
		/// Always 0.
		/// </summary>
		public uint Reserved2;

		/// <summary>
		/// The palette entry that represents the transparent color in the palette.
		/// </summary>
		public byte TransparentIndex;

		/// <summary>
		/// Always 0.
		/// </summary>
		public fixed byte Ignore[3];

		/// <summary>
		/// Number of colors (0 means 256, for older files).
		/// </summary>
		public ushort NumColors;

		/// <summary>
		/// The relative width of a pixel, for aspect ratio purposes; if 0, pixels are 1:1.
		/// </summary>
		public byte PixelWidth;

		/// <summary>
		/// The relative height of a pixel, for aspect ratio purposes; if 0, pixels are 1:1.
		/// </summary>
		public byte PixelHeight;

		/// <summary>
		/// The X position of the grid.
		/// </summary>
		public short GridX;

		/// <summary>
		/// The Y position of the grid.
		/// </summary>
		public short GridY;

		/// <summary>
		/// The horizontal spacing of the grid.
		/// </summary>
		public ushort GridWidth;

		/// <summary>
		/// The vertical spacing of the grid.
		/// </summary>
		public ushort GridHeight;

		/// <summary>
		/// Reserved for future use (always 0).
		/// </summary>
		public fixed byte Reserved[84];
	}
}
