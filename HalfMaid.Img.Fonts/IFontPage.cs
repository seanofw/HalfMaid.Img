using System.Collections.Generic;
using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A font page describes a set of one or more glyphs that can be used
	/// to render a font.  At its core, it is a kind of dictionary, mapping Unicode
	/// code points to glyphs in some source image, and it also includes
	/// metadata to provide for faster glyph lookup.  This interface is the
	/// abstract representation of a font page, which allows both static and
	/// very dynamic versions of a range of glyphs to exist.
	/// </summary>
	public interface IFontPage : IReadOnlyDictionary<int, Glyph>
	{
		/// <summary>
		/// The lowest-numbered code point defined by this page.
		/// </summary>
		int Start { get; }

		/// <summary>
		/// One more than the highest-numbered code point defined by this page.
		/// </summary>
		int End { get; }

		/// <summary>
		/// Retrieve a glyph from this page of defined glyphs.
		/// </summary>
		/// <param name="codePoint">The Unicode code point of the glyph to retrieve.</param>
		/// <param name="topLeft">The target pixel coordinate at which the glyph will
		/// be displayed.  Fonts that support pixel-perfect font rendering may return
		/// different Glyph objects for different coordinates.  Image-based (bitmap-based)
		/// fonts will typically return the same Glyph regardless of which coordinate
		/// is provided.</param>
		/// <returns>The glyph, or null if no such glyph exists at this code point.</returns>
		Glyph? GetGlyph(int codePoint, Vector2d topLeft);
	}
}