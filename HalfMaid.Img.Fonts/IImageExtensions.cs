using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// Image extensions for text drawing using bitmap fonts.
	/// </summary>
	public static class IImageExtensions
	{
		/// <summary>
		/// Simple text-drawing-with-alignment routine.  This draws the given text, in the
		/// given font, aligned as chosen within the given rectangle.  It advances to the
		/// right after drawing each character.  A '\n' character (code point 10) or '\r\n'
		/// pair (code points 13 and 10) will advance to the next line.  By default, this
		/// copies from the font in color-alpha mode, so if the font image is properly
		/// constructed, the color parameter will determine the color of the text.  This
		/// doesn't use fancy font shaping, but instead just draws left-to-right within each
		/// line of text, and top-to-bottom for successive lines.
		/// </summary>
		/// <param name="image">The image to draw the text on.</param>
		/// <param name="rect">The containing rectangle for the text.</param>
		/// <param name="text">The text to draw.</param>
		/// <param name="font">The font to use to draw the text.</param>
		/// <param name="color">The color of the text.</param>
		/// <param name="blitFlags">Which blit mode to use when drawing each glyph.</param>
		/// <param name="textAlignment">How to align the text relative to the given rectangle.
		/// The default alignment is to place the text in the top-left corner of the
		/// given rectangle.</param>
		public static void DrawMultilineText(this IImage image, Rectd rect, ReadOnlySpan<char> text, Font font,
			Color32 color, BlitFlags blitFlags = BlitFlags.ColorAlpha,
			TextAlignment textAlignment = default)
		{
			List<PositionedGlyph> positionedGlyphs = new List<PositionedGlyph>();
			SimpleTextPositioner.PositionMultilineText(positionedGlyphs,
				new Rectd(0, 0, rect.Width, rect.Height), text, font, textAlignment);
			image.DrawText(rect.TopLeft, positionedGlyphs, color, blitFlags);
		}

		/// <summary>
		/// Simple text-drawing routine.  This draws the given text, in the given font,
		/// starting at the given point (for the top-left corner of the text).  It advances
		/// to the right after drawing each character.  By default, this copies from the
		/// font in color-alpha mode, so if the font image is properly constructed, the color
		/// parameter will determine the color of the text.  This doesn't use fancy font
		/// shaping, but instead just draws left-to-right starting at the given point.
		/// </summary>
		/// <param name="image">The image to draw the text on.</param>
		/// <param name="point">The top-left corner of the text to draw.</param>
		/// <param name="text">The text to draw.</param>
		/// <param name="font">The font to use to draw the text.</param>
		/// <param name="color">The color of the text.</param>
		/// <param name="blitFlags">Which blit mode to use when drawing each glyph.</param>
		/// <returns>The point immediately after the text, where more text would begin.</returns>
		public static Vector2d DrawText(this IImage image, Vector2d point, ReadOnlySpan<char> text, Font font,
			Color32 color, BlitFlags blitFlags = BlitFlags.ColorAlpha)
		{
			List<PositionedGlyph> positionedGlyphs = new List<PositionedGlyph>();
			Vector2d result = SimpleTextPositioner.PositionText(positionedGlyphs, default, text, font);
			image.DrawText(point, positionedGlyphs, color, blitFlags);
			return result;
		}

		/// <summary>
		/// Draw a sequence of positioned (and fully shaped) glyphs.  This allows for highly
		/// flexible text-shaping, including using sophisticated external libraries to support
		/// complex orthographies, and is designed to be roughly HarfBuzz-compatible.  This
		/// method is very powerful, but it doesn't actually do all that much, deferring all
		/// of the positioning and layout and font-selection logic to the caller.
		/// </summary>
		/// <param name="image">The image to draw the text on.</param>
		/// <param name="point">The starting point for drawing the glyphs.</param>
		/// <param name="glyphs">The glyphs to draw, pre-positioned relative to the given point.</param>
		/// <param name="color">The color of the text.</param>
		/// <param name="blitFlags">Which blit mode to use when drawing each glyph.</param>
		public static void DrawText(this IImage image, Vector2d point, IEnumerable<PositionedGlyph> glyphs,
			Color32 color, BlitFlags blitFlags = BlitFlags.ColorAlpha)
		{
			foreach (PositionedGlyph positionedGlyph in glyphs)
			{
				Vector2d drawPoint = point + positionedGlyph.Position;
				Glyph glyph = positionedGlyph.Glyph;
				image.Blit(glyph.Image, glyph.X, glyph.Y, (int)(drawPoint.X + 0.5), (int)(drawPoint.Y + 0.5),
					glyph.Width, glyph.Height, blitFlags, color);
			}
		}
	}
}
