using System;
using System.Linq;
using System.Reflection;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A few simple built-in fonts.  This doesn't attempt to provide fonts for every
	/// possible use case:  It's just a few highly-compressed bitmap fonts, which ensures
	/// that you never have *zero* fonts installed no matter where or how you're using
	/// the HalfMaid.Img library.
	/// </summary>
	public static class BuiltinFonts
	{
		/// <summary>
		/// Clean, in 15 points.
		/// </summary>
		public static Font Clean => _clean ??= LoadEmbeddedFont("Clean", 15);
		private static Font? _clean;

		/// <summary>
		/// Clean Mono, in 15 points.
		/// </summary>
		public static Font CleanMono => _cleanMono ??= LoadEmbeddedFont("Clean Mono", 15);
		private static Font? _cleanMono;

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
			public readonly FontMetrics FontMetrics;

			public BuiltinFontInfo(string name, int size,
				int startChar, int charCount,
				int charCols, int charRows, int charWidth, int charHeight,
				int startX, int startY, int padX, int padY,
				bool isColumns, bool isMonospace, FontMetrics fontMetrics)
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
				isColumns: false, isMonospace: false,
				fontMetrics: new FontMetrics(ascent: 11, maxAscent: 13, descent: 3, maxDescent: 3, baseline: 13,
					lowercaseAscent: 7, lineHeight: 16, emWidth: 10, exWidth: 7, space: 3, kerning: 2,
					monospace: false)),

			new BuiltinFontInfo(name: "Clean Mono", size: 15,
				startChar: 0, charCount: 256, charCols: 16, charRows: 16,
				charWidth: 7, charHeight: 16, startX: 0, startY: 0, padX: 9, padY: 0,
				isColumns: false, isMonospace: true,
				fontMetrics: new FontMetrics(ascent: 11, maxAscent: 13, descent: 3, maxDescent: 3, baseline: 13,
					lowercaseAscent: 7, lineHeight: 16, emWidth: 7, exWidth: 7, space: 7, kerning: 2,
					monospace: true)),
		};

		/// <summary>
		/// Load a builtin font.
		/// </summary>
		/// <param name="name">The name of the builtin font to load (case-insensitive).</param>
		/// <param name="size">The point size of the font.</param>
		/// <returns>The font, loaded from the embedded resource.</returns>
		/// <exception cref="ArgumentException">Thrown if the builtin font requested doesn't exist.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the builtin font exists but is damaged.</exception>
		private static Font LoadEmbeddedFont(string name, int size)
		{
			BuiltinFontInfo? builtinFontInfo = _fonts.FirstOrDefault(f =>
				string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) && f.Size == size);
			if (builtinFontInfo == null)
				throw new ArgumentException($"Unknown built-in typeface '{name}' or point size '{size}'.");
			
			Image8? image = Image8.LoadEmbeddedResource(Assembly.GetExecutingAssembly(),
				$"HalfMaid.Img.Resources.{builtinFontInfo.Name}-{builtinFontInfo.Size}.png");
			if (image == null)
				throw new InvalidOperationException($"Failed loading built-in font '{builtinFontInfo.Name}' at point size '{builtinFontInfo.Size}'.");

			// The builtin images are drawn as grayscale, but really that gray is supposed to be
			// the alpha channel, so we replace the pixels and palette so that the single channel
			// represents alpha.
			RemapBuiltinImageColors(image);

			// Construct the font, which will measure and extract the glyphs from the image.
			// We don't use the automatic metrics calculator here, but instead use the provided values above.
			return new Font(builtinFontInfo.Name, new FontInfo(builtinFontInfo.Size), builtinFontInfo.FontMetrics,
				new ImageFontPage(image,
					builtinFontInfo.StartChar, builtinFontInfo.CharCount,
					builtinFontInfo.CharCols, builtinFontInfo.CharRows,
					builtinFontInfo.CharWidth, builtinFontInfo.CharHeight,
					builtinFontInfo.StartX, builtinFontInfo.StartY,
					builtinFontInfo.PadX, builtinFontInfo.PadY,
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
