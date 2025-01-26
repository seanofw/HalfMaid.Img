using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A font page describes a set of one or more glyphs that can be used
	/// to render a font.  At its core, it is simply a dictionary, mapping Unicode
	/// code points to glyphs in the same source image, and it also includes
	/// metadata to provide for faster glyph lookup.
	/// </summary>
	public class FontPage : IEquatable<FontPage>, IFontPage
	{
		/// <summary>
		/// The image used to represent this page of the font.  This is treated
		/// as immutable, even though it is simply an IImage.
		/// </summary>
		public IImage Image { get; }

		/// <summary>
		/// All of the glyphs provided by this page, in no particular order, keyed by
		/// their Unicode code point.
		/// </summary>
		public IReadOnlyDictionary<int, Glyph> Glyphs { get; }

		/// <summary>
		/// The minimum code point value provided by this page, used for faster lookup.
		/// </summary>
		public int Start { get; }

		/// <summary>
		/// One more than the maximum code point value provided by this page.
		/// </summary>
		public int End { get; }

		/// <summary>
		/// The total range of code point values provided by this page.
		/// </summary>
		public int Count
		{
			[Pure]
			get => Glyphs.Count;
		}

		/// <summary>
		/// Construct a new font page, with glyphs at the provided positions
		/// in the given image.
		/// </summary>
		/// <param name="image">The image containing the glyphs.</param>
		/// <param name="glyphs">The glyphs in that image, in no particular order.</param>
		[Pure]
		public FontPage(IImage image, IEnumerable<Glyph> glyphs)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image), "Image cnanot be null when creating a font page.");

			Image = image;
			Glyphs = glyphs?.ToDictionary(g => g.CodePoint) ?? new Dictionary<int, Glyph>();

			(Start, End) = FindRange(Glyphs.Values);
		}

		/// <summary>
		/// Given a set of glyphs, find the minimum and maximum code points within
		/// that set of glyphs.
		/// </summary>
		/// <param name="glyphs">The glyphs to scan.</param>
		/// <returns>The minimum and maximum code points within that set of glyphs.</returns>
		[Pure]
		protected static (int Start, int End) FindRange(IEnumerable<Glyph> glyphs)
		{
			int start = int.MaxValue;
			int end = int.MinValue;

			foreach (Glyph glyph in glyphs)
			{
				start = Math.Min(start, glyph.CodePoint);
				end = Math.Max(end, glyph.CodePoint);
			}

			return (start, end + 1);
		}

		/// <summary>
		/// Construct a new font page from an image.
		/// </summary>
		/// <param name="image">The image that contains one or more character glyphs.</param>
		/// <param name="startChar">The starting code point represented in that image.</param>
		/// <param name="charCount">The number of consecutive code points represented in that image.</param>
		/// <param name="charCols">The number of columns of characters in the image grid.</param>
		/// <param name="charRows">The number of rows of characters in the image grid.</param>
		/// <param name="charWidth">The width of each character cell.</param>
		/// <param name="charHeight">The height of each character cell.</param>
		/// <param name="startX">The starting X offset of the first character cell.</param>
		/// <param name="startY">The starting Y offset of the first character cell.</param>
		/// <param name="padX">The X padding between each character cell.</param>
		/// <param name="padY">The Y padding between each character cell.</param>
		/// <param name="isColumns">Whether the characters are arranged in rows
		/// (false/default) or columns (true).</param>
		/// <param name="isMonospace">Whether the characters are monospace, which is to
		/// say, that the character sizes should all be treated as exactly 'charWidth'
		/// instead of being measured against transparent (or 0-valued) pixels.</param>
		[Pure]
		public FontPage(IImage image,
			int startChar, int charCount,
			int charCols, int charRows, int charWidth, int charHeight,
			int startX = 0, int startY = 0, int padX = 0, int padY = 0,
			bool isColumns = false, bool isMonospace = false)
		{
			// Simple form of a font page:  A grid of rows or columns of characters,
			// and the width of each is either taken as 'charWidth', or measured against
			// the pixels given.
			Image = image;

			// Extract and measure each glyph.
			List<Glyph> glyphs = new List<Glyph>();
			for (int ch = startChar; ch < startChar + charCount; ch++)
			{
				int index = ch - startChar;
				(int col, int row) = isColumns
					? (index / charRows, index % charRows)
					: (index % charCols, index / charCols);

				Vector2i topLeft = new Vector2i(
					startX + (charWidth + padX) * col,
					startY + (charHeight + padY) * row);
				Vector2i size = isMonospace ? new Vector2i(charWidth, charHeight)
					: new Vector2i(MeasureContentWidth(image, new Rect(topLeft.X, topLeft.Y, charWidth, charHeight)), charHeight);

				glyphs.Add(new Glyph(image, ch, topLeft.X, topLeft.Y, size.X, size.Y, default));
			}

			// Generate the resulting dictionary of glyphs.
			Glyphs = glyphs.ToDictionary(g => g.CodePoint);

			// And generate the start/end values.
			(Start, End) = FindRange(glyphs);
		}

		/// <summary>
		/// Measure the width of the given glyph.
		/// </summary>
		/// <param name="image">The image that contains the glyph.</param>
		/// <param name="rect">The rectangle that contains the glyph (and nothing else!)</param>
		/// <returns>The width of the measured glyph.</returns>
		[Pure]
		protected static int MeasureContentWidth(IImage image, Rect rect)
		{
			if (image is Image32 image32)
				return image32.MeasureContentWidth(rect);
			else if (image is Image24 image24)
				return image24.MeasureContentWidth(rect, Color24.Black);
			else if (image is Image8 image8)
				return image8.MeasureContentWidth(rect);
			else
				return rect.Width;
		}

		/// <summary>
		/// Measure the height of the given glyph.
		/// </summary>
		/// <param name="image">The image that contains the glyph.</param>
		/// <param name="rect">The rectangle that contains the glyph (and nothing else!)</param>
		/// <returns>height of the measured glyph.</returns>
		[Pure]
		protected static int MeasureContentHeight(IImage image, Rect rect)
		{
			if (image is Image32 image32)
				return image32.MeasureContentWidth(rect);
			else if (image is Image24 image24)
				return image24.MeasureContentWidth(rect, Color24.Black);
			else if (image is Image8 image8)
				return image8.MeasureContentWidth(rect);
			else
				return rect.Width;
		}

		/// <summary>
		/// Determine whether the font page contains the given code point.
		/// </summary>
		/// <param name="index">The code point to test.</param>
		/// <returns>True if this font page contains a definition for that
		/// code point, false if it does not.</returns>
		[Pure]
		public virtual bool ContainsKey(int index)
			=> Glyphs.ContainsKey(index);

		/// <summary>
		/// Get the glyph defined for the given code point.  Returns null
		/// if no glyph is defined.  (Must not throw exceptions.)
		/// </summary>
		/// <param name="index">The code point to retrieve a glyph for.</param>
		/// <returns>The glyph.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the font page does
		/// not contain the given index.</exception>
		public virtual Glyph this[int index]
		{
			[Pure]
			get => Glyphs[index];
		}

		/// <summary>
		/// Get the glyph defined for the given code point.  Returns 'default(FontGlyph)'
		/// if no glyph is defined.  (Must not throw exceptions.)
		/// </summary>
		/// <param name="index">The code point to retrieve a glyph for.</param>
		/// <param name="glyph">The glyph definition.</param>
		/// <returns>True if the glyph is defined, false if it is not defined.</returns>
		[Pure]
#if NETCOREAPP
		public virtual bool TryGetValue(int index, [MaybeNullWhen(false)] out Glyph glyph)
#else
		public virtual bool TryGetValue(int index, out Glyph glyph)
#endif
			=> Glyphs.TryGetValue(index, out glyph);

		/// <summary>
		/// Get a lazy enumerator that can yield every glyph in this font page, in order
		/// of their code points.
		/// </summary>
		/// <returns>An enumerator that produces every glyph in this font page, lazily,
		/// in order of their code points.</returns>
		public IEnumerator<KeyValuePair<int, Glyph>> GetEnumerator()
			=> Glyphs.OrderBy(pair => pair.Key).GetEnumerator();

		/// <summary>
		/// Get a lazy enumerator that can yield every glyph in this font page, in order
		/// of their code points.
		/// </summary>
		/// <returns>An enumerator that produces every glyph in this font page, lazily,
		/// in order of their code points.</returns>
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <summary>
		/// All of the code points in this font page, lazily enumerated.  (This exists
		/// primarily to provide a full implementation of IDictionary.)
		/// </summary>
		IEnumerable<int> IReadOnlyDictionary<int, Glyph>.Keys
		{
			[Pure]
			get => new KeyCollection(this);
		}

		/// <summary>
		/// All of the glyphs in this font page, lazily enumerated.  (This exists
		/// primarily to provide a full implementation of IDictionary.)
		/// </summary>
		IEnumerable<Glyph> IReadOnlyDictionary<int, Glyph>.Values
		{
			[Pure]
			get => new ValueCollection(this);
		}

		/// <summary>
		/// A lazy enumerator of the code points.  This uses Linq to do the hard work,
		/// and is not especially efficient, but it's not expected to be used much.
		/// </summary>
		private struct KeyCollection : IEnumerable<int>
		{
			private readonly FontPage _fontPage;

			[Pure]
			public KeyCollection(FontPage fontPage)
				=> _fontPage = fontPage;
			[Pure]
			public IEnumerator<int> GetEnumerator()
				=> _fontPage.OrderBy(p => p.Key).Select(p => p.Key).GetEnumerator();
			[Pure]
			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
		}

		/// <summary>
		/// A lazy enumerator of the glyphs.  This uses Linq to do the hard work,
		/// and is not especially efficient, but it's not expected to be used much.
		/// </summary>
		private struct ValueCollection : IEnumerable<Glyph>
		{
			private readonly FontPage _fontPage;

			[Pure]
			public ValueCollection(FontPage fontPage)
				=> _fontPage = fontPage;
			[Pure]
			public IEnumerator<Glyph> GetEnumerator()
				=> _fontPage.OrderBy(p => p.Key).Select(p => p.Value).GetEnumerator();
			[Pure]
			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
		}

		/// <summary>
		/// Compare this font page against another for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if they are equivalent objects, false if they are different.</returns>
		[Pure]
		public override bool Equals(object? obj)
			=> obj is FontPage other && Equals(other);

		/// <summary>
		/// Compare this font page against another font page for equality.
		/// </summary>
		/// <param name="other">The other font page to compare against.</param>
		/// <returns>True if they are equivalent pages, false if they are different.</returns>
		[Pure]
		public virtual bool Equals(FontPage? other)
		{
			if (ReferenceEquals(other, null))
				return false;
			if (ReferenceEquals(other, this))
				return true;

			// We don't do a deep comparison for the image, since that's rarely
			// what people want when comparing pages.  Two pages are considered
			// equal if their glyph definitions are the same, but not necessarily
			// if their images are the same.
			if (Image.Size != other.Image.Size)
				return false;

			// Quick shortcut tests first.
			if (Count != other.Count || Start != other.Start || End != other.End)
				return false;

			// Okay, we have to test each glyph the hard way.
			foreach (Glyph glyph in Glyphs.Values)
			{
				if (!other.Glyphs.TryGetValue(glyph.CodePoint, out Glyph? otherGlyph))
					return false;
				if (glyph != otherGlyph)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Get a hash code for this font page, so that this font page can be used as a key
		/// in dictionaries and as an entry in a hash table.
		/// </summary>
		/// <returns>A hash code for this font page.</returns>
		[Pure]
		public override int GetHashCode()
		{
			int hashCode = Glyphs.Count;
			foreach (Glyph glyph in Glyphs.Values)
			{
				hashCode = unchecked(hashCode * 65599 + glyph.GetHashCode());
			}
			return hashCode;
		}

		/// <summary>
		/// Compare one font page against another font page for equality.
		/// </summary>
		/// <param name="a">The first font page to compare.</param>
		/// <param name="b">The other font page to compare against.</param>
		/// <returns>True if they are equivalent pages, false if they are different.</returns>
		[Pure]
		public static bool operator ==(FontPage? a, FontPage? b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);

		/// <summary>
		/// Compare one font page against another font page for equality.
		/// </summary>
		/// <param name="a">The first font page to compare.</param>
		/// <param name="b">The other font page to compare against.</param>
		/// <returns>False if they are equivalent pages, true if they are different.</returns>
		[Pure]
		public static bool operator !=(FontPage? a, FontPage? b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		/// <summary>
		/// Convert this to a string, largely for debugging purposes.
		/// </summary>
		/// <returns></returns>
		[Pure]
		public override string ToString()
			=> $"{Count} chars from U+{Start:X4} to U+{End - 1:X4}";
	}
}
