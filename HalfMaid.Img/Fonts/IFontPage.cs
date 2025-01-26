using System.Collections.Generic;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A font page describes a set of one or more glyphs that can be used
	/// to render a font.  At its core, it is simply a dictionary, mapping Unicode
	/// code points to glyphs in the same source image, and it also includes
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
	}
}