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
	/// A font, which is an ordered dictionary-like collection of glyphs (pieces of
	/// one or more images that are mapped to Unicode code points and can be rendered
	/// individually on-demand).  It is primarily a container of font pages, but it
	/// also *is* a kind of font page, just one with more metadata than most font pages
	/// have.
	/// </summary>
	public sealed class Font : IFontPage
	{
		/// <summary>
		/// The name of this font (the font family or typeface).
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Metadata about which font in the font family this is.
		/// </summary>
		public FontInfo Info { get; }

		/// <summary>
		/// Metrics data describing this font's measurements.  These can be
		/// explicitly provided by the font, or it can be automatically (lazily)
		/// estimated from whatever image(s) describe the font's pixels.
		/// </summary>
		public FontMetrics Metrics { get; }

		/// <summary>
		/// The point size of this font.
		/// </summary>
		public double Size => Info.Size;

		/// <summary>
		/// The weight of this font (1-9).
		/// </summary>
		public int Weight => Info.Weight;

		/// <summary>
		/// Whether this font is a "bold" font.
		/// </summary>
		public bool Bold => Info.Bold;

		/// <summary>
		/// Whether this font is italic or oblique.
		/// </summary>
		public FontStyle Style => Info.Style;

		/// <summary>
		/// The set of font page(s) that describe the glyphs of this font, in order
		/// by their start code points.
		/// </summary>
		public IReadOnlyList<IFontPage> Pages => _pages;
		private IFontPage[] _pages;

		/// <summary>
		/// The code point of the first defined glyph in this font.
		/// </summary>
		public int Start => _pages.First().Start;

		/// <summary>
		/// One more than the code point of the last defined glyph in this font.
		/// </summary>
		public int End => _pages.Last().End;

		/// <summary>
		/// The total number of defined glyphs in this font.
		/// </summary>
		public int Count => _count ??= _pages.Sum(p => p.Count);
		private int? _count;

		/// <summary>
		/// Optional kerning pairs for this font.
		/// </summary>
		public IReadOnlyDictionary<(int PrevChar, int NextChar), double> KerningPairs => _kerningPairs;
		private IReadOnlyDictionary<(int PrevChar, int NextChar), double> _kerningPairs;

		/// <summary>
		/// A permanently-empty dictionary of kerning pairs.
		/// </summary>
		private static IReadOnlyDictionary<(int PrevChar, int NextChar), double> EmptyKerningPairs { get; }
			= new Dictionary<(int PrevChar, int NextChar), double>();

		/// <summary>
		/// Construct a new font from the given set of font pages and provided metadata.
		/// This is the most general constructor, and can create a font from almost any
		/// data source.
		/// </summary>
		/// <param name="name">The name of this font (font family).</param>
		/// <param name="info">Metadata about which font in the font family this is.</param>
		/// <param name="metrics">Metrics data describing this font's measurements.
		/// If null, the metrics will be estimated based on analyzing the glyph shapes of
		/// the letters and numbers in the ASCII range, which are presumed to represent a
		/// proportional (not monospaced) font.</param>
		/// <param name="pages">The set of font page(s) that describe the glyphs of this font.</param>
		public Font(string name, FontInfo info, FontMetrics? metrics, params IFontPage[] pages)
			: this(name, info, metrics, (IEnumerable<IFontPage>)pages)
		{
		}

		/// <summary>
		/// Construct a new font from the given set of font pages and provided metadata.
		/// This is the most general constructor, and can create a font from almost any
		/// data source.
		/// </summary>
		/// <param name="name">The name of this font (font family).</param>
		/// <param name="info">Metadata about which font in the font family this is.</param>
		/// <param name="metrics">Metrics data describing this font's measurements.
		/// If null, the metrics will be estimated based on analyzing the glyph shapes of
		/// the letters and numbers in the ASCII range, which are presumed to represent a
		/// proportional (not monospaced) font.</param>
		/// <param name="pages">The set of font page(s) that describe the glyphs of this font.</param>
		/// <param name="kerningPairs">An optional set of kerning pairs for more advanced
		/// typography.  When provided, the keys describe character pairs to recognize, and
		/// the values describe how much additional advancement should be applied between them,
		/// where 1.0 equals the defined Em width.  Negative values place the glyphs closer together,
		/// while zero is default kerning, and positive values place them farther apart.
		/// Note that this only supports basic kerning *pairs*, not kerning for special ligature
		/// forms like "ffi" or "ffl".</param>
		public Font(string name, FontInfo info, FontMetrics? metrics, IEnumerable<IFontPage> pages,
			IReadOnlyDictionary<(int PrevChar, int NextChar), double>? kerningPairs = null)
		{
			Name = name;
			Info = info;
			_pages = pages.OrderBy(p => p.Start).ToArray();
			_kerningPairs = kerningPairs ?? EmptyKerningPairs;

			ValidateFontPages();

			Metrics = metrics ?? EstimateMetrics(this, false);
		}

		/// <summary>
		/// Perform sanity checks on the font pages to make sure they're valid, and throw
		/// exceptions if they are not.
		/// </summary>
		private void ValidateFontPages()
		{
			IFontPage? prev = null;

			for (int i = 0; i < _pages.Length; i++)
			{
				IFontPage? cur = _pages[i];

				if (cur.End <= cur.Start)
					throw new ArgumentException("A font page must not have its end code point before or equal to its start code point.");

				if (cur is Font)
					throw new ArgumentException("A font cannot contain another font.");

				if (prev != null && cur.Start < prev.End)
					throw new ArgumentException($"Code point ranges must be distinct: Font page from {prev.Start} to {prev.End} overlaps font page from {cur.Start} to {cur.End}.");

				prev = cur;
			}
		}

		/// <summary>
		/// Construct a font from a single image of glyphs.  This form is used by
		/// software that only requires one "page" of glyphs, typically just enough glyphs
		/// to support one or a few European languages.
		/// </summary>
		/// <param name="name">The name of this font (font family).</param>
		/// <param name="info">Metadata about which font in the font family this is.</param>
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
		public Font(string name, FontInfo info, IImage image,
			int startChar, int charCount,
			int charCols, int charRows, int charWidth, int charHeight,
			int startX = 0, int startY = 0, int padX = 0, int padY = 0,
			bool isColumns = false, bool isMonospace = false)
		{
			Name = name;
			Info = info;

			ImageFontPage fontPage = new ImageFontPage(image, startChar, charCount,
				charCols, charRows, charWidth, charHeight, startX, startY, padX, padY,
				isColumns, isMonospace);

			_pages = new[] { fontPage };

			_kerningPairs = EmptyKerningPairs;

			Metrics = EstimateMetrics(fontPage, isMonospace);
		}

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Font WithName(string name)
			=> new Font(name, Info, Metrics, Pages);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Font WithInfo(FontInfo info)
			=> new Font(Name, info, Metrics, Pages);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Font WithMetrics(FontMetrics metrics)
			=> new Font(Name, Info, metrics, Pages);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Font WithPages(IEnumerable<IFontPage> pages)
			=> new Font(Name, Info, Metrics, pages);

		/// <summary>
		/// Retrieve a single glyph by its code point.
		/// </summary>
		/// <param name="codePoint">The Unicode code point of the glyph to retrieve.</param>
		/// <returns>The glyph, or null if no such glyph exists.</returns>
		public Glyph this[int codePoint]
		{
			[Pure]
			get
			{
				int page = FindPage(_pages, codePoint);
				return page >= 0
					? _pages[page][codePoint]
					: throw new KeyNotFoundException($"Code point {codePoint} is not found in font {Name}.");
			}
		}

		/// <summary>
		/// Retrieve a glyph from this font.
		/// </summary>
		/// <param name="codePoint">The Unicode code point of the glyph to retrieve.</param>
		/// <param name="topLeft">The target pixel coordinate at which the glyph will
		/// be displayed.  Fonts that support pixel-perfect font rendering may return
		/// different Glyph objects for different coordinates.  Image-based (bitmap-based)
		/// fonts will typically return the same Glyph regardless of which coordinate
		/// is provided.</param>
		/// <returns>The glyph, or null if no such glyph exists at this code point.</returns>
		[Pure]
		public Glyph? GetGlyph(int codePoint, Vector2d topLeft)
		{
			int page = FindPage(_pages, codePoint);
			return page >= 0 ? _pages[page].GetGlyph(codePoint, topLeft) : null;
		}

		/// <summary>
		/// Retrieve a single glyph by its code point.
		/// </summary>
		/// <param name="codePoint">The Unicode code point of the glyph to retrieve.</param>
		/// <param name="glyph">The glyph, or null if no such glyph exists.</param>
		/// <returns>True if the glyph is found, false if the glyph was not found.</returns>
		[Pure]
#if NETCOREAPP
		public bool TryGetValue(int codePoint, [MaybeNullWhen(false)] out Glyph glyph)
#else
		public bool TryGetValue(int codePoint, out Glyph glyph)
#endif
		{
			int page = FindPage(_pages, codePoint);
			if (page < 0)
			{
				glyph = null!;
				return false;
			}

			return _pages[page].TryGetValue(codePoint, out glyph);
		}

		/// <summary>
		/// Return true if the font contains the provided code point.
		/// </summary>
		/// <param name="codePoint">The Unicode code point to search for.</param>
		/// <returns>True if this font contains the given Unicode code point.</returns>
		[Pure]
		public bool ContainsKey(int codePoint)
		{
			int page = FindPage(_pages, codePoint);
			return page >= 0 && _pages[page].ContainsKey(codePoint);
		}

		/// <summary>
		/// Efficiently locate the page containing the given Unicode code point.
		/// </summary>
		/// <param name="pages">The complete set of font pages for this font.</param>
		/// <param name="codePoint">The Unicode code point to locate.</param>
		/// <returns>If the code point is found, the zero-based index of the page that
		/// contains it.  If the code point is not found, returns -1.</returns>
		[Pure]
		private static int FindPage(IFontPage[] pages, int codePoint)
		{
			int start = 0, end = pages.Length;

			// Binary search while we have 8 or more pages to search through.
			while (start + 8 < end)
			{
				int midpt = (end + start) / 2;
				if (codePoint < pages[midpt].Start)
					end = midpt;
				else if (codePoint >= pages[midpt].End)
					start = midpt + 1;
				else
					break;
			}

			// Linear search through short ranges of 8 or fewer pages.
			for (int i = start; i < end; i++)
			{
				if (codePoint >= pages[i].Start && codePoint < pages[i].End)
					return i;
			}

			// Didn't find it.
			return -1;
		}

		/// <summary>
		/// Measure the text if it were rendered in this font.  This supports
		/// the newline '\n' and '\r' characters to separate lines, but all other
		/// characters are rendered verbatim.  This applies kerning and kerning pairs
		/// when measuring the text, but does not allow for custom kerning or leading.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <param name="wrapAtNewlines">Whether the newline '\n' and '\r' characters
		/// should be treated as line breaks (true) or as ordinary printable characters
		/// (false).</param>
		/// <param name="includeFinalKerning">Whether to include kerning after the last
		/// glyph on each line.  By default, the "tail kerning" is omitted, but if this
		/// text is to be joined to other text, you may want to include it.</param>
		/// <returns>The maximum width and height of the text.</returns>
		[Pure]
		public Vector2d MeasureText(ReadOnlySpan<char> text,
			bool wrapAtNewlines = false, bool includeFinalKerning = false)
		{
			double x = 0;
			double y = 0;
			double maxX = 0;
			double kerning = Metrics.Kerning;
			IReadOnlyDictionary<(int, int), double> kerningPairs = _kerningPairs;
			StringAsUnicode str = new StringAsUnicode(text);

			int ch;
			int prev = -1;
			bool hasCharsOnThisLine = false;

			while ((ch = str.Next()) >= 0)
			{
				// If this is the second character in a kerning pair, adjust kerning.
				if (kerningPairs.TryGetValue((prev, ch), out double kerningPair))
					x += kerningPair;

				// Handle whitespace characters specially.
				if (ch == 32 || ch == 160)
				{
					// Space.
					hasCharsOnThisLine = true;
					x += Metrics.Space + kerning;
					prev = ch;
					continue;
				}
				else if (wrapAtNewlines)
				{
					if (ch == 10)
					{
						if (str.Peek() == 13)
							str.Next();

						// Newline.
						if (hasCharsOnThisLine && !includeFinalKerning)
							x -= kerning;
						maxX = Math.Max(maxX, x);
						x = 0;
						y += Metrics.LineHeight;
					}
					else if (ch == 13)
					{
						if (str.Peek() == 10)
							str.Next();

						// Newline.
						if (hasCharsOnThisLine && !includeFinalKerning)
							x -= kerning;
						maxX = Math.Max(maxX, x);
						x = 0;
						y += Metrics.LineHeight;
					}
				}

				// Get the glyph for this character.
				Glyph? glyph = GetGlyph(ch, new Vector2d(x, y));
				if (glyph == null)
					continue;

				// Move forward past the glyph, plus the default kerning.
				hasCharsOnThisLine = true;
				x += glyph.Advance.X + kerning;
				y += glyph.Advance.Y;

				prev = ch;
			}

			if (hasCharsOnThisLine && !includeFinalKerning)
				x -= kerning;
			maxX = Math.Max(maxX, x);

			return new Vector2d(maxX, y);
		}

		/// <summary>
		/// Get an enumerator that will lazily yield every glyph in this font, in order.
		/// </summary>
		/// <returns>An enumerator that will yield the entire font.</returns>
		[Pure]
		public IEnumerator<KeyValuePair<int, Glyph>> GetEnumerator()
		{
			foreach (IFontPage fontPage in _pages)
				foreach (KeyValuePair<int, Glyph> pair in fontPage)
					yield return pair;
		}

		/// <summary>
		/// Get an enumerator that will lazily yield every glyph in this font, in order.
		/// </summary>
		/// <returns>An enumerator that will yield the entire font.</returns>
		[Pure]
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <summary>
		/// All of the code points in this font, lazily enumerated.  (This exists
		/// primarily to provide a full implementation of IDictionary.)
		/// </summary>
		IEnumerable<int> IReadOnlyDictionary<int, Glyph>.Keys
		{
			[Pure]
			get => new KeyCollection(this);
		}

		/// <summary>
		/// All of the glyphs in this font, lazily enumerated.  (This exists
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
			private readonly Font _font;

			[Pure]
			public KeyCollection(Font font)
				=> _font = font;
			[Pure]
			public IEnumerator<int> GetEnumerator()
				=> _font._pages.SelectMany(p => p).OrderBy(p => p.Key).Select(p => p.Key).GetEnumerator();
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
			private readonly Font _font;

			[Pure]
			public ValueCollection(Font font)
				=> _font = font;
			[Pure]
			public IEnumerator<Glyph> GetEnumerator()
				=> _font._pages.SelectMany(p => p).OrderBy(p => p.Key).Select(p => p.Value).GetEnumerator();
			[Pure]
			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
		}

		// These are the letters we use for measuring a font's ascent and descent.
		private static readonly HashSet<char> _ascentChars =
			new HashSet<char>("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZbdfhijklt");
		private static readonly HashSet<char> _descentChars =
			new HashSet<char>("gjpqy");
		private static readonly HashSet<char> _lowerAscentChars =
			new HashSet<char>("acegmnopqrsuvwxyz");
		private static readonly HashSet<char> _baselineChars =
			new HashSet<char>("0123456789ABCDEFGHIJKLMNOPRSTUVWXYZabcdefhiklmnorstuvwxz");

		/// <summary>
		/// Estimate the font's height metrics by examining the ascents and descents
		/// of the (non-accented) letters and numbers.
		/// </summary>
		/// <param name="glyphs">The glyphs to examine.</param>
		/// <param name="isMonospace">Whether this is a monospace or proportional font.</param>
		/// <returns>Metrics for the font's ascent, descent, baseline, and more.</returns>
		[Pure]
		public static FontMetrics EstimateMetrics(IFontPage glyphs, bool isMonospace)
		{
			const string LettersAndNumbers = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

			List<(int Ch, double Y, double Width, double Height)> measurements =
				new List<(int Ch, double Y, double Width, double Height)>();

			// Measure every letter and number.
			for (int i = 0; i < LettersAndNumbers.Length; i++)
			{
				char ch = LettersAndNumbers[i];
				if (!glyphs.TryGetValue(ch, out Glyph? glyph))
					continue;

				(int y, int width, int height) = MeasureGlyph(glyph);

				if (width <= 0 || height <= 0)
					continue;
				measurements.Add((ch, y, width, height));
			}

			// Sum up the positions found for each of ascent, descent, lower ascent, and baseline.
			double avgAscent = 0, avgDescent = 0, avgLowerAscent = 0, avgBaseline = 0;
			int ascentCount = 0, descentCount = 0, lowerAscentCount = 0, baselineCount = 0;
			foreach ((int ch, double y, double width, double height) in measurements)
			{
				if (ch < 0 || ch > 0xFFFF)
					continue;

				double top = y, bottom = y + height;

				if (_baselineChars.Contains((char)ch))
				{
					avgBaseline += bottom;
					baselineCount++;
				}
				if (_descentChars.Contains((char)ch))
				{
					avgDescent += bottom;
					descentCount++;
				}
				if (_ascentChars.Contains((char)ch))
				{
					avgAscent += top;
					ascentCount++;
				}
				if (_lowerAscentChars.Contains((char)ch))
				{
					avgLowerAscent += top;
					lowerAscentCount++;
				}
			}

			// Calculate the averages.
			avgAscent /= Math.Max(ascentCount, 1);
			avgBaseline /= Math.Max(baselineCount, 1);
			avgDescent /= Math.Max(descentCount, 1);
			avgLowerAscent /= Math.Max(lowerAscentCount, 1);

			// There might be outliers, and we don't want those skewing the averages.
			// So calculate the standard deviation, which requires a second pass over
			// the data.
			double stdevAscent = 0, stdevDescent = 0, stdevLowerAscent = 0, stdevBaseline = 0;
			foreach ((int ch, double y, double width, double height) in measurements)
			{
				if (ch < 0 || ch > 0xFFFF)
					continue;

				double top = y, bottom = y + height;

				if (_baselineChars.Contains((char)ch))
					stdevBaseline += (avgBaseline - bottom) * (avgBaseline - bottom);
				if (_descentChars.Contains((char)ch))
					stdevDescent += (avgDescent - bottom) * (avgDescent - bottom);
				if (_ascentChars.Contains((char)ch))
					stdevAscent += (avgAscent - top) * (avgAscent - top);
				if (_lowerAscentChars.Contains((char)ch))
					stdevLowerAscent += (avgLowerAscent - top) * (avgLowerAscent - top);
			}
			stdevAscent = Math.Sqrt(stdevAscent / Math.Max(ascentCount, 1));
			stdevDescent = Math.Sqrt(stdevDescent / Math.Max(descentCount, 1));
			stdevLowerAscent = Math.Sqrt(stdevLowerAscent / Math.Max(lowerAscentCount, 1));
			stdevBaseline = Math.Sqrt(stdevBaseline / Math.Max(baselineCount, 1));

			// Okay, now do a second pass over the data, but this time, exclude anything
			// outside 1.5 standard deviations from the previous average.  This will exclude
			// values that are even *slightly* outliers while still covering ~7/8 of the
			// measurements, so we'll be left with values that should be very close to
			// meaningful measurements.
			double avgAscent2 = 0, avgDescent2 = 0, avgLowerAscent2 = 0, avgBaseline2 = 0;
			int ascentCount2 = 0, descentCount2 = 0, lowerAscentCount2 = 0, baselineCount2 = 0;
			double emWidth = 0;
			double exWidth = 0;
			double rangeAscent = stdevAscent * 1.5;
			double rangeDescent = stdevDescent * 1.5;
			double rangeLowerAscent = stdevLowerAscent * 1.5;
			double rangeBaseline = stdevBaseline * 1.5;
			foreach ((int ch, double y, double width, double height) in measurements)
			{
				if (ch < 0 || ch > 0xFFFF)
					continue;

				double top = y, bottom = y + height;

				if ((char)ch == 'M')
					emWidth = width;

				if ((char)ch == 'x')
					exWidth = width;

				if (_baselineChars.Contains((char)ch)
					&& avgBaseline - rangeBaseline <= bottom && bottom <= avgBaseline + rangeBaseline)
				{
					avgBaseline2 += bottom;
					baselineCount2++;
				}
				if (_descentChars.Contains((char)ch)
					&& avgDescent - rangeDescent <= bottom && bottom <= avgDescent + rangeDescent)
				{
					avgDescent2 += bottom;
					descentCount2++;
				}
				if (_ascentChars.Contains((char)ch)
					&& avgAscent - rangeAscent <= top && top <= avgAscent + rangeAscent)
				{
					avgAscent2 += top;
					ascentCount2++;
				}
				if (_lowerAscentChars.Contains((char)ch)
					&& avgLowerAscent - rangeLowerAscent <= top && top <= avgLowerAscent + rangeLowerAscent)
				{
					avgLowerAscent2 += top;
					lowerAscentCount2++;
				}
			}

			// Calculate the new averages.
			avgAscent2 /= Math.Max(ascentCount2, 1);
			avgBaseline2 /= Math.Max(baselineCount2, 1);
			avgDescent2 /= Math.Max(descentCount2, 1);
			avgLowerAscent2 /= Math.Max(lowerAscentCount2, 1);

			double baseline = avgBaseline2;

			// Find the true maxima.
			double maxAscent = 0;
			double maxDescent = 0;
			Dictionary<int, (int Ch, double Y, double Width, double Height)> measurementsLookup =
				measurements.ToDictionary(m => m.Ch);
			foreach (KeyValuePair<int, Glyph> pair in glyphs)
			{
				double y, width, height;
				if (measurementsLookup.TryGetValue(pair.Key, out var m))
					(_, y, width, height) = m;
				else
					(y, width, height) = MeasureGlyph(pair.Value);
				if (width <= 0 || height <= 0)
					continue;

				double descent = Math.Max(0, y + height - baseline);
				double ascent = Math.Max(0, baseline - y);

				maxAscent = Math.Max(maxAscent, ascent);
				maxDescent = Math.Max(maxDescent, descent);
			}

			// We need the M glyph's box for the proper widths for monospace fonts.
			Glyph? emGlyph = glyphs['M'] ?? Glyph.Empty;

			// Return the calculated metrics.
			return new FontMetrics(
				ascent: baseline - avgAscent2,
				maxAscent: maxAscent,
				descent: avgDescent2 - baseline,
				maxDescent: maxDescent,
				baseline: baseline,
				lowercaseAscent: baseline - avgLowerAscent2,
				lineHeight: emWidth * 1.25,
				emWidth: isMonospace ? emWidth : emWidth,
				exWidth: isMonospace ? emWidth : exWidth,
				space: Math.Max(2.0, isMonospace ? emGlyph.Width : emWidth * 3 / 8),
				kerning: isMonospace ? 0 : Math.Max(1.0, emWidth / 8),
				monospace: isMonospace
			);
		}

		[Pure]
		private static (int YOffset, int Width, int Height) MeasureGlyph(Glyph glyph)
		{
			Rect rect = glyph.Image is Image32 image32
					? image32.MeasureContent(glyph.Rect, 0)
				: glyph.Image is Image24 image24
					? image24.MeasureContent(glyph.Rect, Color24.Black)
				: glyph.Image is Image8 image8
					? image8.MeasureContent(glyph.Rect, 0)
				: default;

			return (rect.Y - glyph.Y - glyph.Origin.Y, rect.Width, rect.Height);
		}
	}
}
