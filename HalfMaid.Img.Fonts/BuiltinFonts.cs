using System;
using System.Linq;
using System.Reflection;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A few simple built-in fonts.  This doesn't attempt to provide fonts for every
	/// possible use case:  It's just a few highly-compressed bitmap fonts, which ensures
	/// that you never have *zero* fonts installed no matter where or how you're using
	/// the HalfMaid.Img library.<br />
	/// <br />
	/// These fonts support the Latin-1 (Windows 1252) code page, which is loosely
	/// equivalent to Unicode code points 32-255.  For good typography, or for support
	/// of other languages, consider using TrueType/OpenType fonts instead.<br />
	/// <br />
	/// Collectively, these require ~15 Kb of storage, which is about 3% of the compiled
	/// DLL, and which is a fairly small price to pay to ensure that several fonts are
	/// always available.<br />
	/// <br />
	/// Importantly, these are free:  Fee as in speech, and free as in beer:  Clean and
	/// Clean Mono were designed for this library, and Pixelly and Tiny were ported for
	/// use by this library by their creator.  They are hereby released into the public
	/// domain, and may be used for any purpose and for any reason.
	/// </summary>
	public static class BuiltinFonts
	{
		/// <summary>
		/// Clean, in 15 points.  This is an antialiased bitmap font, designed for readability.
		/// </summary>
		public static Font Clean => _clean ??= LoadEmbeddedFont("Clean", 15);
		private static Font? _clean;

		/// <summary>
		/// Clean Mono, in 15 points.  This is an antialiased monospace bitmap font, designed for readability.
		/// </summary>
		public static Font CleanMono => _cleanMono ??= LoadEmbeddedFont("Clean Mono", 15);
		private static Font? _cleanMono;

		/// <summary>
		/// Pixelly, in 9 points.  This is a bitmap font that only uses opaque and transparent pixels.
		/// It is comparable to the fonts typically used on PC games in the '90s.
		/// </summary>
		public static Font Pixelly => _pixelly ??= LoadEmbeddedFont("Pixelly", 9);
		private static Font? _pixelly;

		/// <summary>
		/// Pixelly Bold, in 9 points.  This is a bitmap font that only uses opaque and transparent pixels.
		/// It is comparable to the fonts typically used on PC games in the '90s.
		/// </summary>
		public static Font PixellyBold => _pixellyBold ??= LoadEmbeddedFont("Pixelly", 9, isBold: true);
		private static Font? _pixellyBold;

		/// <summary>
		/// Pixelly, in 18 points.  This is Pixelly, but with every pixel doubled.
		/// </summary>
		public static Font Pixellyx2 => _pixellyx2 ??= LoadEmbeddedFont("Pixelly", 9, scale: 2);
		private static Font? _pixellyx2;

		/// <summary>
		/// Pixelly Bold, in 18 points.  This is Pixelly Bold, but with every pixel doubled.
		/// </summary>
		public static Font PixellyBoldx2 => _pixellyBoldx2 ??= LoadEmbeddedFont("Pixelly", 9, isBold: true, scale: 2);
		private static Font? _pixellyBoldx2;

		/// <summary>
		/// Tiny, in 6 points.  This is a bitmap font that only uses opaque and transparent pixels.
		/// It is designed so that each character fits within an 8x8 pixel block.
		/// </summary>
		public static Font Tiny => _tiny ??= LoadEmbeddedFont("Tiny", 6);
		private static Font? _tiny;

		/// <summary>
		/// Tiny, in 12 points.  This is tiny, with every pixel doubled.
		/// </summary>
		public static Font Tinyx2 => _tinyx2 ??= LoadEmbeddedFont("Tiny", 6, scale: 2);
		private static Font? _tinyx2;

		/// <summary>
		/// Simple data structure for recording bitmap font layouts for the builtin fonts.
		/// </summary>
		private class BuiltinFontInfo
		{
			public readonly string Name;
			public readonly int Size;
			public readonly int StartChar;
			public readonly int CharCount;
			public readonly int CharCols;
			public readonly int CharRows;
			public readonly int CharWidth;
			public readonly int CharHeight;
			public readonly int StartX;
			public readonly int StartY;
			public readonly int PadX;
			public readonly int PadY;
			public readonly bool IsColumns;
			public readonly bool IsMonospace;
			public readonly bool IsBold;
			public readonly FontMetrics FontMetrics;

			public BuiltinFontInfo(string name, int size,
				int startChar, int charCount,
				int charCols, int charRows, int charWidth, int charHeight,
				int startX, int startY, int padX, int padY,
				bool isColumns, bool isMonospace, bool isBold,
				FontMetrics fontMetrics)
			{
				Name = name;
				Size = size;
				StartChar = startChar;
				CharCount = charCount;
				CharCols = charCols;
				CharRows = charRows;
				CharWidth = charWidth;
				CharHeight = charHeight;
				StartX = startX;
				StartY = startY;
				PadX = padX;
				PadY = padY;
				IsColumns = isColumns;
				IsMonospace = isMonospace;
				IsBold = isBold;
				FontMetrics = fontMetrics;
			}
		}

		/// <summary>
		/// The list of known builtin fonts.
		/// </summary>
		private static readonly BuiltinFontInfo[] _fonts = new BuiltinFontInfo[]
		{
			new BuiltinFontInfo(name: "Clean", size: 15,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 16, charHeight: 16, startX: 0, startY: 0, padX: 0, padY: 0,
				isColumns: false, isMonospace: false, isBold: false,
				fontMetrics: new FontMetrics(ascent: 11, maxAscent: 13, descent: 3, maxDescent: 3, baseline: 13,
					lowercaseAscent: 7, lineHeight: 16, emWidth: 10, exWidth: 7, space: 3, kerning: 2,
					monospace: false)),

			new BuiltinFontInfo(name: "Clean Mono", size: 15,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 7, charHeight: 16, startX: 0, startY: 0, padX: 9, padY: 0,
				isColumns: false, isMonospace: true, isBold: false,
				fontMetrics: new FontMetrics(ascent: 11, maxAscent: 13, descent: 3, maxDescent: 3, baseline: 13,
					lowercaseAscent: 7, lineHeight: 16, emWidth: 7, exWidth: 7, space: 7, kerning: 2,
					monospace: true)),

			new BuiltinFontInfo(name: "Pixelly", size: 9,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 16, charHeight: 12, startX: 0, startY: 0, padX: 0, padY: 0,
				isColumns: true, isMonospace: false, isBold: true,
				fontMetrics: new FontMetrics(ascent: 7, maxAscent: 9, descent: 2, maxDescent: 2, baseline: 9,
					lowercaseAscent: 5, lineHeight: 11, emWidth: 7, exWidth: 5, space: 2, kerning: 1,
					monospace: false)),

			new BuiltinFontInfo(name: "Pixelly", size: 9,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 16, charHeight: 12, startX: 0, startY: 0, padX: 0, padY: 0,
				isColumns: true, isMonospace: false, isBold: false,
				fontMetrics: new FontMetrics(ascent: 7, maxAscent: 9, descent: 2, maxDescent: 2, baseline: 9,
					lowercaseAscent: 5, lineHeight: 11, emWidth: 7, exWidth: 5, space: 2, kerning: 1,
					monospace: false)),

			new BuiltinFontInfo(name: "Tiny", size: 6,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 8, charHeight: 8, startX: 0, startY: 0, padX: 0, padY: 0,
				isColumns: true, isMonospace: false, isBold: false,
				fontMetrics: new FontMetrics(ascent: 5, maxAscent: 5, descent: 1, maxDescent: 1, baseline: 6,
					lowercaseAscent: 4, lineHeight: 8, emWidth: 8, exWidth: 3, space: 2, kerning: 1,
					monospace: false)),
		};

		/// <summary>
		/// Load a builtin font.
		/// </summary>
		/// <param name="name">The name of the builtin font to load (case-insensitive).</param>
		/// <param name="size">The point size of the font.</param>
		/// <param name="isBold">Whether to load the bold or regular weight.</param>
		/// <param name="scale">How big to rescale this font.</param>
		/// <returns>The font, loaded from the embedded resource.</returns>
		/// <exception cref="ArgumentException">Thrown if the builtin font requested doesn't exist.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the builtin font exists but is damaged.</exception>
		private static Font LoadEmbeddedFont(string name, int size, bool isBold = false, int scale = 1)
		{
			BuiltinFontInfo? builtinFontInfo = _fonts.FirstOrDefault(f =>
				string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)
					&& f.Size == size && f.IsBold == isBold);
			if (builtinFontInfo == null)
				throw new ArgumentException($"Unknown built-in typeface '{name}' or point size '{size}'.");

			string weight = isBold ? " Bold" : string.Empty;
			Image8? image = Image8.LoadEmbeddedResource(Assembly.GetExecutingAssembly(),
				$"HalfMaid.Img.Fonts.Resources.{builtinFontInfo.Name}{weight}-{builtinFontInfo.Size}.png");
			if (image == null)
				throw new InvalidOperationException($"Failed loading built-in font '{builtinFontInfo.Name}{weight}' at point size '{builtinFontInfo.Size}'.");

			// The builtin images are drawn as grayscale, but really that gray is supposed to be
			// the alpha channel, so we replace the pixels and palette so that the single channel
			// represents alpha.
			RemapBuiltinImageColors(image);

			if (scale > 1)
				image.Resize(image.Size * scale);

			// Construct the font, which will measure and extract the glyphs from the image.
			// We don't use the automatic metrics calculator here, but instead use the provided values above.
			return new Font(builtinFontInfo.Name,
				new FontInfo(
					size: builtinFontInfo.Size * scale,
					weight: isBold ? 700 : 400,
					style: FontStyle.Normal,
					stretch: 5),
				new FontMetrics(
					ascent: builtinFontInfo.FontMetrics.Ascent * scale,
					maxAscent: builtinFontInfo.FontMetrics.MaxAscent * scale,
					descent: builtinFontInfo.FontMetrics.Descent * scale,
					maxDescent: builtinFontInfo.FontMetrics.MaxDescent * scale,
					baseline: (builtinFontInfo.FontMetrics.Baseline + 1) * scale - 1,
					lowercaseAscent: builtinFontInfo.FontMetrics.LowercaseAscent * scale,
					lineHeight: builtinFontInfo.FontMetrics.LineHeight * scale,
					emWidth: builtinFontInfo.FontMetrics.EmWidth * scale,
					exWidth: builtinFontInfo.FontMetrics.ExWidth * scale,
					space: builtinFontInfo.FontMetrics.Space * scale,
					kerning: builtinFontInfo.FontMetrics.Kerning * scale,
					monospace: builtinFontInfo.FontMetrics.Monospace
				),
				new ImageFontPage(image,
					builtinFontInfo.StartChar, builtinFontInfo.CharCount,
					builtinFontInfo.CharCols, builtinFontInfo.CharRows,
					builtinFontInfo.CharWidth * scale, builtinFontInfo.CharHeight * scale,
					builtinFontInfo.StartX * scale, builtinFontInfo.StartY * scale,
					builtinFontInfo.PadX * scale, builtinFontInfo.PadY * scale,
					builtinFontInfo.IsColumns, builtinFontInfo.IsMonospace));
		}

		private unsafe static void RemapBuiltinImageColors(Image8 image)
		{
			// Replace each pixel value with the matching red channel from the palette.
			fixed (byte* basePtr = image.Data)
			fixed (Color32* basePalette = image.Palette)
			{
				int count = image.Width * image.Height;
				byte* ptr = basePtr, end = basePtr + count;
				for (; ptr < end; ptr++)
					*ptr = basePalette[*ptr].R;
			}

			// Generate a 256-color alpha palette.
			Color32[] alphaPalette = new Color32[256];
			for (int i = 0; i < 256; i++)
				alphaPalette[i] = new Color32((byte)255, (byte)255, (byte)255, (byte)i);

			// Replace the image's palette with the 256-color alpha palette, so that the
			// byte value in the image effectively becomes just the alpha value.
			image.ReplacePalette(alphaPalette.AsSpan());
		}
	}
}
