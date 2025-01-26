using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// This class holds a set of simple text-shaping routines.  It doesn't
	/// attempt to be a complete text shaper like HarfBuzz, but just attempts
	/// to handle text that consists of disjoint glyphs that read left-to-right,
	/// top-to-bottom and that doesn't require ligatures.  That covers a lot of
	/// Western European languages, as well as some East Asian languages, which
	/// means it's "good enough" for a lot of real-world use cases.
	/// </summary>
	public static class SimpleTextPositioner
	{
		/// <summary>
		/// Simple aligned-text-positioning routine.  This positions the given text, in the given font,
		/// starting at the given point (for the top-left corner of the text).  It advances
		/// to the right after drawing each character.  The '\n' character (code point 10)
		/// will advance to the next line.
		/// </summary>
		/// <param name="result">The shaped glyphs will be written to this collection.</param>
		/// <param name="rect">The containing rectangle for the text.</param>
		/// <param name="text">The text to shape.</param>
		/// <param name="font">The font to use to shape the text.</param>
		/// <param name="textAlignment">How to align the text relative to the given rectangle.</param>
		/// <returns>The Y coordinate of the next line.</returns>
		public static double PositionText(ICollection<PositionedGlyph> result,
			Rectd rect, ReadOnlySpan<char> text, Font font, TextAlignment textAlignment)
		{
			// Fast-calculate the height of the text, if it matters to do so.
			double textHeight = 0;
			if ((textAlignment & TextAlignment.VertMask) > TextAlignment.Top)
			{
				int numLines = 1;
				foreach (char ch in text)
					if (ch == '\n')
						numLines++;

				// Simple multiplication to figure out the height of the text.
				textHeight = numLines * font.Metrics.LineHeight;
			}

			// Calculate where we're starting the rendering.
			double y = (textAlignment & TextAlignment.VertMask) switch
			{
				TextAlignment.Default => rect.Y,
				TextAlignment.Top => rect.Y,
				TextAlignment.Bottom => rect.Y + rect.Height - textHeight,
				TextAlignment.VertCenter => (rect.Y + rect.Height - textHeight) * 0.5,
				TextAlignment.Baseline => rect.Y - font.Metrics.Baseline,
				_ => rect.Y,
			};

			for (int i = 0; i < text.Length;)
			{
				// Extract the next line.
				int lineStart = i;
				while (i < text.Length && text[i] != '\n')
					i++;
				int lineLength = i - lineStart;
				if (i < text.Length && text[i] == '\n')
					i++;
				ReadOnlySpan<char> line = text.Slice(lineStart, lineLength);

				// If the horizontal alignment is anything other than default/left,
				// measure the text so we can properly align it.
				double textWidth = ((textAlignment & TextAlignment.HorzMask) > TextAlignment.Left)
					? font.MeasureText(line).X
					: 0;

				// Calculate where we're starting the rendering.
				double x = (textAlignment & TextAlignment.HorzMask) switch
				{
					TextAlignment.Default => rect.X,
					TextAlignment.Left => rect.X,
					TextAlignment.Right => rect.X + rect.Width - textWidth,
					TextAlignment.HorzCenter => (rect.Y + rect.Width - textWidth) * 0.5,
					_ => rect.X,
				};

				// Position the next line of text.
				PositionText(result, new Vector2d(x, y), line, font);

				y += font.Metrics.LineHeight;
			}

			return y;
		}

		/// <summary>
		/// Simple text-positioning routine.  This positions the given text, in the given font,
		/// starting at the given point (for the top-left corner of the text).  It advances
		/// to the right after drawing each character.  This doesn't use fancy font
		/// shaping, but instead just positions glyphs left-to-right starting at the
		/// given point.
		/// </summary>
		/// <param name="result">The positioned glyphs will be written to this collection.</param>
		/// <param name="point">Where the text</param>
		/// <param name="text">The text to position.</param>
		/// <param name="font">The font to use to position the text.</param>
		public static Vector2d PositionText(ICollection<PositionedGlyph> result,
			Vector2d point, ReadOnlySpan<char> text, Font font)
		{
			double x = point.X;
			double defaultKerning = font.Metrics.Kerning;
			IReadOnlyDictionary<(int, int), double> kerningPairs = font.KerningPairs;

			StringAsUnicode str = new StringAsUnicode(text);

			int ch;
			int prev = -1;

			while ((ch = str.Next()) >= 0)
			{
				// If this is the second character in a kerning pair, adjust kerning.
				if (kerningPairs.TryGetValue((prev, ch), out double kerningPairValue))
					x += kerningPairValue * font.Metrics.EmWidth;

				// Handle whitespace characters specially.
				if (ch == 32 || ch == 160)
				{
					// Space.
					x += font.Metrics.Space + defaultKerning;
					prev = ch;
					continue;
				}

				// Get the glyph for this character.
				if (!font.TryGetValue(ch, out Glyph? glyph))
					continue;

				// Actually position the glyph.
				result.Add(new PositionedGlyph(glyph, new Vector2((float)x, (float)point.Y)));

				// Move forward past the glyph, plus the default kerning.
				x += glyph.Width + defaultKerning;

				prev = ch;
			}

			return new Vector2d(x, point.Y);
		}
	}
}
