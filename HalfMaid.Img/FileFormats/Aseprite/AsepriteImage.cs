using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// An Aseprite image (or animation).
	/// </summary>
	[DebuggerDisplay("Aseprite image: {Width}x{Height} {ColorMode} color, {Frames.Count} frame(s)")]
	public class AsepriteImage
	{
		/// <summary>
		/// The width of this image, in pixels.
		/// </summary>
		public ushort Width => Header.Width;

		/// <summary>
		/// The height of this image, in pixels.
		/// </summary>
		public ushort Height => Header.Height;

		/// <summary>
		/// Bit depth of each pixel (32 = RGBA, 16 = grayscale, 8 = indexed color).
		/// </summary>
		public ushort Depth => Header.Depth;

		/// <summary>
		/// Optional flags describing this image.
		/// </summary>
		public AsepriteHeaderFlags Flags => Header.Flags;

		/// <summary>
		/// The palette entry that represents the transparent color in the palette.
		/// </summary>
		public byte TransparentIndex => Header.TransparentIndex;

		/// <summary>
		/// The relative width of a pixel, for aspect ratio purposes; if 0, pixels are 1:1.
		/// </summary>
		public byte PixelWidth => Header.PixelWidth;

		/// <summary>
		/// The relative height of a pixel, for aspect ratio purposes; if 0, pixels are 1:1.
		/// </summary>
		public byte PixelHeight => Header.PixelHeight;

		/// <summary>
		/// The X position of the grid.
		/// </summary>
		public short GridX => Header.GridX;

		/// <summary>
		/// The Y position of the grid.
		/// </summary>
		public short GridY => Header.GridY;

		/// <summary>
		/// The horizontal spacing of the grid.
		/// </summary>
		public ushort GridWidth => Header.GridWidth;

		/// <summary>
		/// The vertical spacing of the grid.
		/// </summary>
		public ushort GridHeight => Header.GridHeight;

		/// <summary>
		/// The color mode of this image (RGB, grayscale, paletted, etc.).
		/// </summary>
		public AsepriteColorMode ColorMode { get; }

		private AsepriteHeader Header;

		/// <summary>
		/// The frames of this image, in order.
		/// </summary>
		public IReadOnlyList<AsepriteFrame> Frames { get; }

		/// <summary>
		/// Construct a new Aseprite image by decoding the provided Aseprite file data.
		/// </summary>
		/// <param name="sourceData">The raw data of the Aseprite file.</param>
		public AsepriteImage(ReadOnlySpan<byte> sourceData)
		{
			ReadHeader(sourceData, out Header);
			sourceData = sourceData.Slice(128);

			ColorMode = Header.Depth == 32 ? AsepriteColorMode.Rgb
				: Header.Depth == 16 ? AsepriteColorMode.Grayscale
				: AsepriteColorMode.Indexed;

			List<AsepriteFrame> frames = new List<AsepriteFrame>();

			for (int i = 0; i < Header.Frames; i++)
			{
				sourceData = AsepriteFrame.ReadFrame(this, i,
					Header, ColorMode, sourceData, out AsepriteFrame frame);
				frames.Add(frame);
			}

			Frames = frames;
		}

		private static void ReadHeader(ReadOnlySpan<byte> sourceData, out AsepriteHeader header)
		{
			if (sourceData.Length < 128)
				throw new ArgumentException("Source data is too small to be a valid Aseprite image file.");

			ReadOnlySpan<AsepriteHeader> headers = MemoryMarshal.Cast<byte, AsepriteHeader>(sourceData);
			header = headers[0];

			if (header.Size > sourceData.Length)
				throw new ArgumentException($"Aseprite header is corrupt; invalid size '{header.Size}'.");
			if (header.Magic != 0xA5E0)
				throw new ArgumentException($"Aseprite header is corrupt; invalid magic number '{header.Magic:X4}'.");
		}

		internal static string ReadString(ReadOnlySpan<byte> sourceData)
		{
			short nameLength = (short)(sourceData[0] | (sourceData[1] << 8));
			if (nameLength <= 0)
				return string.Empty;

			return StringFromLatin1Bytes(sourceData.Slice(2, nameLength));
		}

		private static string StringFromLatin1Bytes(ReadOnlySpan<byte> bytes)
		{
			// Scary-looking code, but the fastest way to construct a Latin-1
			// string, and provably safe.
			string result = new string('\0', bytes.Length);
			unsafe
			{
				fixed (char* resultBase = result)
				fixed (byte* srcBase = bytes)
				{
					char* dest = resultBase;
					byte* src = srcBase;

					for (int i = 0; i < bytes.Length; i++)
						*dest++ = (char)*src++;
				}
			}

			return result;
		}
	}
}
