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
	/// A font page that describes a set of one or more glyphs that can be used
	/// to render a font from a single image.  At its core, it is simply a dictionary,
	/// mapping Unicode code points to glyphs in the same source image, and it also
	/// includes metadata to provide for faster glyph lookup.
	/// </summary>
	public class ImageFontPage : IEquatable<ImageFontPage>, IFontPage
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
		public ImageFontPage(IImage image, IEnumerable<Glyph> glyphs)
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
		public ImageFontPage(IImage image,
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
				Rect glyphRect = isMonospace
					? new Rect(topLeft.X, topLeft.Y, charWidth, charHeight)
					: MeasureContentRect(image, new Rect(topLeft.X, topLeft.Y, charWidth, charHeight));

				int advanceX = glyphRect.Width;
				if (!isMonospace)
					if (IsColumnLessThanOrEqualTo(image, glyphRect.X + glyphRect.Width - 1, glyphRect.Y, glyphRect.Height, 85))
						advanceX--;

				glyphs.Add(new Glyph(image, ch, glyphRect.X, glyphRect.Y, glyphRect.Width, glyphRect.Height,
					new Vector2i(0, topLeft.Y - glyphRect.Y), new Vector2d(advanceX, 0)));
			}

			// Generate the resulting dictionary of glyphs.
			Glyphs = glyphs.ToDictionary(g => g.CodePoint);

			// And generate the start/end values.
			(Start, End) = FindRange(glyphs);
		}

		/// <summary>
		/// Measure the bounding box of the given glyph.
		/// </summary>
		/// <param name="image">The image that contains the glyph.</param>
		/// <param name="rect">A rectangle that contains the glyph, and possibly empty padding.</param>
		/// <returns>A rectangle that contains *only* the glyph's pixels.</returns>
		[Pure]
		protected static Rect MeasureContentRect(IImage image, Rect rect)
		{
			if (image is Image32 image32)
				return image32.MeasureContent(rect);
			else if (image is Image8 image8)
				return image8.MeasureContent(rect);
			else if (image is Image24 image24)
				return image24.MeasureContent(rect, Color24.Black);
			else
				return rect;
		}

		/// <summary>
		/// Determine if the given column of the image is 
		/// </summary>
		/// <param name="image">The image to scan.</param>
		/// <param name="x">The horizontal position of the column.</param>
		/// <param name="y">The topmost Y coordinate in the column to test.</param>
		/// <param name="height">The number of vertical pixels to test.</param>
		/// <param name="cutoff">The cutoff value, which is treated as the alpha for 32-bit images,
		/// the grayscale brightness for 24-bit images, and the literal byte value for 8-bit images.</param>
		/// <returns>True if all of the values tested in the given column are less than
		/// or equal to the cutoff value.</returns>
		[Pure]
		protected static bool IsColumnLessThanOrEqualTo(IImage image, int x, int y, int height, byte cutoff)
		{
			if (image is Image32 image32)
				return image32.IsColumnTransparent(x, y, height, cutoff);
			else if (image is Image24 image24)
				return IsColumnBelowGray(image24, x, y, height, cutoff);
			else if (image is Image8 image8)
				return image8.IsColumnTransparent(x, y, height, cutoff);
			else
				return false;
		}

		/// <summary>
		/// For 24-bit images, there's no built-in test to see if a given column is below a given
		/// grayscale brightness, so we implement one here.
		/// </summary>
		/// <param name="image">The image to scan.</param>
		/// <param name="x">The horizontal position of the column.</param>
		/// <param name="y">The topmost Y coordinate in the column to test.</param>
		/// <param name="height">The number of vertical pixels to test.</param>
		/// <param name="cutoff">The cutoff value, which is treated as the alpha for 32-bit images,
		/// the grayscale brightness for 24-bit images, and the literal byte value for 8-bit images.</param>
		/// <returns>True if all of the values tested in the given column are less than
		/// or equal to the cutoff value.</returns>
		[Pure]
		private static bool IsColumnBelowGray(Image24 image, int x, int y, int height, byte cutoff)
		{
			// Exclude columns outside the image.
			if (x < 0 || x >= image.Width || y + height < 0 || y >= image.Height)
				return true;

			// Clamp the start and end to the image dimension.
			int imageWidth = image.Width;
			int imageHeight = image.Height;
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (height > imageHeight - y)
				height = imageHeight - y;

			// Do the actual scan as fast as possible in an unsafe loop,
			// since all coordinates are now validated.
			unsafe
			{
				fixed (Color24* dataBase = image.Data)
				{
					int count = height;
					Color24* data = dataBase + x + y * imageWidth;
					do
					{
						if (data->Grayscale > cutoff)
							return false;
						data += imageWidth;
					}
					while (--count != 0);
				}
			}

			return true;
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
		/// Retrieve a glyph from this page of defined glyphs.
		/// </summary>
		/// <param name="codePoint">The Unicode code point of the glyph to retrieve.</param>
		/// <param name="topLeft">The target pixel coordinate at which the glyph will
		/// be displayed.  Fonts that support pixel-perfect font rendering may return
		/// different Glyph objects for different coordinates.  Image-based (bitmap-based)
		/// fonts will typically return the same Glyph regardless of which coordinate
		/// is provided.</param>
		/// <returns>The glyph, or null if no such glyph exists at this code point.</returns>
		[Pure]
		public virtual Glyph? GetGlyph(int codePoint, Vector2d topLeft)
			=> Glyphs.TryGetValue(codePoint, out Glyph? glyph) ? glyph : null;

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
			private readonly ImageFontPage _fontPage;

			[Pure]
			public KeyCollection(ImageFontPage fontPage)
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
			private readonly ImageFontPage _fontPage;

			[Pure]
			public ValueCollection(ImageFontPage fontPage)
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
			=> obj is ImageFontPage other && Equals(other);

		/// <summary>
		/// Compare this font page against another font page for equality.
		/// </summary>
		/// <param name="other">The other font page to compare against.</param>
		/// <returns>True if they are equivalent pages, false if they are different.</returns>
		[Pure]
		public virtual bool Equals(ImageFontPage? other)
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
		public static bool operator ==(ImageFontPage? a, ImageFontPage? b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);

		/// <summary>
		/// Compare one font page against another font page for equality.
		/// </summary>
		/// <param name="a">The first font page to compare.</param>
		/// <param name="b">The other font page to compare against.</param>
		/// <returns>False if they are equivalent pages, true if they are different.</returns>
		[Pure]
		public static bool operator !=(ImageFontPage? a, ImageFontPage? b)
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

