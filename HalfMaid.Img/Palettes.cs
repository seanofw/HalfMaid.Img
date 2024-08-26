using System;

namespace HalfMaid.Img
{
	/// <summary>
	/// This class contains various standard palettes to make it easier to
	/// dither or map to various common targets.
	/// </summary>
	public class Palettes
	{
		#region Grayscale palettes

		/// <summary>
		/// A grayscale 256-color palette, from 0 (black) to 255 (white).
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale256 => _grayscale256;
		private static Color32[] _grayscale256 = CreateGrayscale();

		/// <summary>
		/// A grayscale 4-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale4A => _grayscale4A;
		private static Color32[] _grayscale4A = CreateGrayscale4(true);

		/// <summary>
		/// A grayscale 4-color palette, from 0 (black) to 192 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale4B => _grayscale4B;
		private static Color32[] _grayscale4B = CreateGrayscale4(false);

		/// <summary>
		/// A grayscale 8-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale8A => _grayscale8A;
		private static Color32[] _grayscale8A = CreateGrayscale8(true);

		/// <summary>
		/// A grayscale 8-color palette, from 0 (black) to 224 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale8B => _grayscale8B;
		private static Color32[] _grayscale8B = CreateGrayscale8(false);

		/// <summary>
		/// A grayscale 16-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale16A => _grayscale16A;
		private static Color32[] _grayscale16A = CreateGrayscale16(true);

		/// <summary>
		/// A grayscale 16-color palette, from 0 (black) to 240 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale16B => _grayscale16B;
		private static Color32[] _grayscale16B = CreateGrayscale16(false);

		/// <summary>
		/// A grayscale 32-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale32A => _grayscale32A;
		private static Color32[] _grayscale32A = CreateGrayscale32(true);

		/// <summary>
		/// A grayscale 32-color palette, from 0 (black) to 248 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale32B => _grayscale32B;
		private static Color32[] _grayscale32B = CreateGrayscale32(false);

		/// <summary>
		/// A grayscale 64-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale64A => _grayscale64A;
		private static Color32[] _grayscale64A = CreateGrayscale64(true);

		/// <summary>
		/// A grayscale 64-color palette, from 0 (black) to 252 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale64B => _grayscale64B;
		private static Color32[] _grayscale64B = CreateGrayscale64(false);

		/// <summary>
		/// A grayscale 128-color palette, from 0 (black) to 255 (white), where
		/// low bits mirror the high bits.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale128A => _grayscale128A;
		private static Color32[] _grayscale128A = CreateGrayscale128(true);

		/// <summary>
		/// A grayscale 128-color palette, from 0 (black) to 254 (white), where
		/// low bits are zero.
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale128B => _grayscale128B;
		private static Color32[] _grayscale128B = CreateGrayscale128(false);

		/// <summary>
		/// A grayscale 65536-color palette, from 0 (black) to 65535 (white).
		/// </summary>
		public static ReadOnlySpan<Color32> Grayscale65536 => _grayscale65536;
		private static Color32[] _grayscale65536 = CreateGrayscale65536();

		/// <summary>
		/// A 2-color black-and-white palette.
		/// </summary>
		public static ReadOnlySpan<Color32> BlackAndWhite => _blackAndWhite;
		private static Color32[] _blackAndWhite = new Color32[] { Color32.Black, Color32.White };

		private static Color32[] CreateGrayscale()
		{
			Color32[] palette = new Color32[256];
			for (int i = 0; i < 256; i++)
				palette[i] = new Color32((byte)i, (byte)i, (byte)i, (byte)255);
			return palette;
		}

		private static Color32[] CreateGrayscale4(bool mirror)
		{
			Color32[] palette = new Color32[4];
			for (int i = 0; i < 4; i++)
			{
				int v = mirror ? (i << 6) | (i << 4) | (i << 2) | i : (i << 6);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale8(bool mirror)
		{
			Color32[] palette = new Color32[8];
			for (int i = 0; i < 8; i++)
			{
				int v = mirror ? (i << 5) | (i << 3) | (i >> 1) : (i << 5);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale16(bool mirror)
		{
			Color32[] palette = new Color32[16];
			for (int i = 0; i < 16; i++)
			{
				int v = mirror ? (i << 4) | i : (i << 4);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale32(bool mirror)
		{
			Color32[] palette = new Color32[32];
			for (int i = 0; i < 32; i++)
			{
				int v = mirror ? (i << 3) | (i >> 2) : (i << 3);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale64(bool mirror)
		{
			Color32[] palette = new Color32[64];
			for (int i = 0; i < 64; i++)
			{
				int v = mirror ? (i << 2) | (i >> 4) : (i << 2);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale128(bool mirror)
		{
			Color32[] palette = new Color32[128];
			for (int i = 0; i < 128; i++)
			{
				int v = mirror ? (i << 1) | (i >> 6) : (i << 1);
				palette[i] = new Color32((byte)v, (byte)v, (byte)v, (byte)255);
			}
			return palette;
		}

		private static Color32[] CreateGrayscale65536()
		{
			Color32[] palette = new Color32[65536];
			for (int i = 0; i < 65536; i++)
				palette[i] = new Color32((byte)(i >> 8), (byte)(i >> 8), (byte)(i >> 8), (byte)255);
			return palette;
		}

		#endregion

		#region Common palettes from old 8-bit and 16-bit systems

		/// <summary>
		/// The classic CGA 16-color palette (with brown, not dark yellow).
		/// </summary>
		public static ReadOnlySpan<Color32> Cga16 => _cga16;
		private static Color32[] _cga16 = new Color32[] {
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0xAA),
			new Color32(0x00, 0xAA, 0x00),
			new Color32(0x00, 0xAA, 0xAA),

			new Color32(0xAA, 0x00, 0x00),
			new Color32(0xAA, 0x00, 0xAA),
			new Color32(0xAA, 0x55, 0x00),	// Brown, not dark yellow.
			new Color32(0xAA, 0xAA, 0xAA),

			new Color32(0x55, 0x55, 0x55),
			new Color32(0x55, 0x55, 0xFF),
			new Color32(0x55, 0xFF, 0x55),
			new Color32(0x55, 0xFF, 0xFF),

			new Color32(0xFF, 0x55, 0x55),
			new Color32(0xFF, 0x55, 0xFF),
			new Color32(0xFF, 0xFF, 0x55),
			new Color32(0xFF, 0xFF, 0xFF),
		};

		/// <summary>
		/// The CGA 16-color "alternate" palette (with dark yellow, not brown).
		/// </summary>
		public static ReadOnlySpan<Color32> Cga16Alt => _cga16Alt;
		private static Color32[] _cga16Alt = new Color32[] {
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0xAA),
			new Color32(0x00, 0xAA, 0x00),
			new Color32(0x00, 0xAA, 0xAA),

			new Color32(0xAA, 0x00, 0x00),
			new Color32(0xAA, 0x00, 0xAA),
			new Color32(0xAA, 0xAA, 0x00),	// Dark yellow, not brown.
			new Color32(0xAA, 0xAA, 0xAA),

			new Color32(0x55, 0x55, 0x55),
			new Color32(0x55, 0x55, 0xFF),
			new Color32(0x55, 0xFF, 0x55),
			new Color32(0x55, 0xFF, 0xFF),

			new Color32(0xFF, 0x55, 0x55),
			new Color32(0xFF, 0x55, 0xFF),
			new Color32(0xFF, 0xFF, 0x55),
			new Color32(0xFF, 0xFF, 0xFF),
		};

		/// <summary>
		/// The full EGA 64-color palette.
		/// </summary>
		public static ReadOnlySpan<Color32> Ega64 => _ega64;
		private static Color32[] _ega64 = CreateEga64();

		private static Color32[] CreateEga64()
		{
			Color32[] palette = new Color32[64];

			// Convert the two-bit form to an 8-bit form by repeating bit pairs.
			byte[] _colorMapping = new byte[4]
			{
				0x00,
				0x55,
				0xAA,
				0xFF,
			};

			// Create the palette in standard EGA order, with brightest bits first,
			// in order of B, then G, then R.  This is kind of weird, but it's how
			// the EGA hardware did it.
			int index = 0;
			for (int B = 0; B < 2; B++)
			{
				for (int G = 0; G < 2; G++)
				{
					for (int R = 0; R < 2; R++)
					{
						for (int b = 0; b < 2; b++)
						{
							int bv = _colorMapping[(B << 1) | b];
							for (int g = 0; g < 2; g++)
							{
								int gv = _colorMapping[(G << 1) | g];
								for (int r = 0; r < 2; r++)
								{
									int rv = _colorMapping[(R << 1) | r];
									palette[index++] = new Color32(rv, gv, bv);
								}
							}
						}
					}
				}
			}

			return palette;
		}

		/// <summary>
		/// The Windows 16-color palette is the same as the "Web Basic" 16-color palette.
		/// </summary>
		public static ReadOnlySpan<Color32> Windows16 => _webBasic16;

		/// <summary>
		/// The classic Commodore 64 16-color palette (per the C64 wiki).
		/// </summary>
		public static ReadOnlySpan<Color32> Commodore64_16 => _commodore64_16;
		private static Color32[] _commodore64_16 = new Color32[] {
			new Color32(0x00, 0x00, 0x00),
			new Color32(0xFF, 0xFF, 0xFF),
			new Color32(0x88, 0x00, 0x00),
			new Color32(0xAA, 0xFF, 0xEE),

			new Color32(0xCC, 0x44, 0xCC),
			new Color32(0x00, 0xCC, 0x55),
			new Color32(0x00, 0x00, 0xAA),
			new Color32(0xEE, 0xEE, 0x77),

			new Color32(0xDD, 0x88, 0x55),
			new Color32(0x66, 0x44, 0x00),
			new Color32(0xFF, 0x77, 0x77),
			new Color32(0x33, 0x33, 0x33),

			new Color32(0x77, 0x77, 0x77),
			new Color32(0xAA, 0xFF, 0x66),
			new Color32(0x00, 0x88, 0xFF),
			new Color32(0xBB, 0xBB, 0xBB),
		};

		/// <summary>
		/// The NES 64-color palette.  There are several possible palettes, since the
		/// original not only used NTSC YIQ but used invalid YIQ values, and there's no such
		/// thing as exact-match RGB for that.  So this is FirebrandX's "Smooth" NES
		/// palette, as it's about as good as an NES palette can get in RGB.  Note that
		/// this palette has multiple black and multiple white entries, just like the
		/// real NES does.
		/// </summary>
		public static ReadOnlySpan<Color32> NES64 => _nes64;
		private static Color32[] _nes64 = new Color32[] {
			new Color32(0x6A, 0x6D, 0x6A),
			new Color32(0x00, 0x13, 0x80),
			new Color32(0x1E, 0x00, 0x8A),
			new Color32(0x39, 0x00, 0x7A),
			new Color32(0x55, 0x00, 0x56),
			new Color32(0x5A, 0x00, 0x18),
			new Color32(0x4F, 0x10, 0x00),
			new Color32(0x3D, 0x1C, 0x00),
			new Color32(0x25, 0x32, 0x00),
			new Color32(0x00, 0x3D, 0x00),
			new Color32(0x00, 0x40, 0x00),
			new Color32(0x00, 0x39, 0x24),
			new Color32(0x00, 0x2E, 0x55),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),

			new Color32(0xB9, 0xBC, 0xB9),
			new Color32(0x18, 0x50, 0xC7),
			new Color32(0x4B, 0x30, 0xE3),
			new Color32(0x73, 0x22, 0xD6),
			new Color32(0x95, 0x1F, 0xA9),
			new Color32(0x9D, 0x28, 0x5C),
			new Color32(0x98, 0x37, 0x00),
			new Color32(0x7F, 0x4C, 0x00),
			new Color32(0x5E, 0x64, 0x00),
			new Color32(0x22, 0x77, 0x00),
			new Color32(0x02, 0x7E, 0x02),
			new Color32(0x00, 0x76, 0x45),
			new Color32(0x00, 0x6E, 0x8A),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),

			new Color32(0xFF, 0xFF, 0xFF),
			new Color32(0x68, 0xA6, 0xFF),
			new Color32(0x8C, 0x9C, 0xFF),
			new Color32(0xB5, 0x86, 0xFF),
			new Color32(0xD9, 0x75, 0xFD),
			new Color32(0xE3, 0x77, 0xB9),
			new Color32(0xE5, 0x8D, 0x68),
			new Color32(0xD4, 0x9D, 0x29),
			new Color32(0xB3, 0xAF, 0x0C),
			new Color32(0x7B, 0xC2, 0x11),
			new Color32(0x55, 0xCA, 0x47),
			new Color32(0x46, 0xCB, 0x81),
			new Color32(0x47, 0xC1, 0xC5),
			new Color32(0x4A, 0x4D, 0x4A),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),

			new Color32(0xFF, 0xFF, 0xFF),
			new Color32(0xCC, 0xEA, 0xFF),
			new Color32(0xDD, 0xDE, 0xFF),
			new Color32(0xEC, 0xDA, 0xFF),
			new Color32(0xF8, 0xD7, 0xFE),
			new Color32(0xFC, 0xD6, 0xF5),
			new Color32(0xFD, 0xDB, 0xCF),
			new Color32(0xF9, 0xE7, 0xB5),
			new Color32(0xF1, 0xF0, 0xAA),
			new Color32(0xDA, 0xFA, 0xA9),
			new Color32(0xC9, 0xFF, 0xBC),
			new Color32(0xC3, 0xFB, 0xD7),
			new Color32(0xC4, 0xF6, 0xF6),
			new Color32(0xBE, 0xC1, 0xBE),
			new Color32(0x00, 0x00, 0x00),
			new Color32(0x00, 0x00, 0x00),
		};

		/// <summary>
		/// The NES 54-color palette.  There are several possible palettes, since the
		/// original not only used NTSC YIQ but used invalid YIQ values, and there's no such
		/// thing as exact-match RGB for that.  So this is FirebrandX's "Smooth" NES
		/// palette, as it's about as good as an NES palette can get in RGB.  This has no
		/// duplicate entries like the NES64 palette, but it is not in NES order.
		/// </summary>
		public static ReadOnlySpan<Color32> NES54 => _nes54;
		private static Color32[] _nes54 = new Color32[] {
			new Color32(0x6A, 0x6D, 0x6A),
			new Color32(0x00, 0x13, 0x80),
			new Color32(0x1E, 0x00, 0x8A),
			new Color32(0x39, 0x00, 0x7A),
			new Color32(0x55, 0x00, 0x56),
			new Color32(0x5A, 0x00, 0x18),
			new Color32(0x4F, 0x10, 0x00),
			new Color32(0x3D, 0x1C, 0x00),
			new Color32(0x25, 0x32, 0x00),
			new Color32(0x00, 0x3D, 0x00),
			new Color32(0x00, 0x40, 0x00),
			new Color32(0x00, 0x39, 0x24),
			new Color32(0x00, 0x2E, 0x55),
			new Color32(0x00, 0x00, 0x00),

			new Color32(0xB9, 0xBC, 0xB9),
			new Color32(0x18, 0x50, 0xC7),
			new Color32(0x4B, 0x30, 0xE3),
			new Color32(0x73, 0x22, 0xD6),
			new Color32(0x95, 0x1F, 0xA9),
			new Color32(0x9D, 0x28, 0x5C),
			new Color32(0x98, 0x37, 0x00),
			new Color32(0x7F, 0x4C, 0x00),
			new Color32(0x5E, 0x64, 0x00),
			new Color32(0x22, 0x77, 0x00),
			new Color32(0x02, 0x7E, 0x02),
			new Color32(0x00, 0x76, 0x45),
			new Color32(0x00, 0x6E, 0x8A),

			new Color32(0xFF, 0xFF, 0xFF),
			new Color32(0x68, 0xA6, 0xFF),
			new Color32(0x8C, 0x9C, 0xFF),
			new Color32(0xB5, 0x86, 0xFF),
			new Color32(0xD9, 0x75, 0xFD),
			new Color32(0xE3, 0x77, 0xB9),
			new Color32(0xE5, 0x8D, 0x68),
			new Color32(0xD4, 0x9D, 0x29),
			new Color32(0xB3, 0xAF, 0x0C),
			new Color32(0x7B, 0xC2, 0x11),
			new Color32(0x55, 0xCA, 0x47),
			new Color32(0x46, 0xCB, 0x81),
			new Color32(0x47, 0xC1, 0xC5),
			new Color32(0x4A, 0x4D, 0x4A),

			new Color32(0xCC, 0xEA, 0xFF),
			new Color32(0xDD, 0xDE, 0xFF),
			new Color32(0xEC, 0xDA, 0xFF),
			new Color32(0xF8, 0xD7, 0xFE),
			new Color32(0xFC, 0xD6, 0xF5),
			new Color32(0xFD, 0xDB, 0xCF),
			new Color32(0xF9, 0xE7, 0xB5),
			new Color32(0xF1, 0xF0, 0xAA),
			new Color32(0xDA, 0xFA, 0xA9),
			new Color32(0xC9, 0xFF, 0xBC),
			new Color32(0xC3, 0xFB, 0xD7),
			new Color32(0xC4, 0xF6, 0xF6),
			new Color32(0xBE, 0xC1, 0xBE),
		};

		#endregion

		#region Web palettes

		/// <summary>
		/// The Web-safe 16-color "basic sRGB" palette, which is a little like
		/// CGA/EGA but just different enough to be different.
		/// </summary>
		public static ReadOnlySpan<Color32> WebBasic16 => _webBasic16;
		private static Color32[] _webBasic16 = new Color32[] {
			new Color32(0xFF, 0xFF, 0xFF),
			new Color32(0xC0, 0xC0, 0xC0),
			new Color32(0x80, 0x80, 0x80),
			new Color32(0x00, 0x00, 0x00),

			new Color32(0xFF, 0x00, 0x00),
			new Color32(0x80, 0x00, 0x00),
			new Color32(0xFF, 0xFF, 0x00),
			new Color32(0x80, 0x80, 0x00),
			new Color32(0x00, 0xFF, 0x00),
			new Color32(0x00, 0x80, 0x00),
			new Color32(0x00, 0xFF, 0xFF),
			new Color32(0x00, 0x80, 0x80),
			new Color32(0x00, 0x00, 0xFF),
			new Color32(0x00, 0x00, 0x80),
			new Color32(0xFF, 0x00, 0xFF),
			new Color32(0x80, 0x00, 0x80),
		};

		/// <summary>
		/// The Web-safe 216-color palette, in the common order of blue-green-red.
		/// </summary>
		public static ReadOnlySpan<Color32> Web216 => _web216;
		private static Color32[] _web216 = CreateWeb216();

		private static Color32[] CreateWeb216()
		{
			Color32[] palette = new Color32[216];

			// Convert the values 0-5 to the equivalent hex codes.
			byte[] _colorMapping = new byte[6]
			{
				0x00,
				0x33,
				0x66,
				0x99,
				0xCC,
				0xFF,
			};

			// Create the palette in in blue-green-red order.
			int index = 0;
			for (int b = 0; b < 6; b++)
			{
				int bv = _colorMapping[b];
				for (int g = 0; g < 6; g++)
				{
					int gv = _colorMapping[g];
					for (int r = 0; r < 6; r++)
					{
						int rv = _colorMapping[r];
						palette[index++] = new Color32(rv, gv, bv);
					}
				}
			}

			return palette;
		}

		#endregion
	}
}
