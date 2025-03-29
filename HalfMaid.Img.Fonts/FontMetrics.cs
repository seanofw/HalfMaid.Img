using System;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// Measurements describing a font's overall characteristics.
	/// </summary>
	public sealed class FontMetrics : IEquatable<FontMetrics>
	{
		/// <summary>
		/// The ascent of uppercase letters/numbers in this font, in pixels.
		/// Some characters may extend above this, but this is generally where
		/// uppercase letters would not be cut off if a line were drawn here,
		/// and is the safe position to draw an overline for this font.
		/// This is always a positive number or zero, and always &lt;= MaxAscent.
		/// </summary>
		public double Ascent => _ascent;
		private readonly float _ascent;

		/// <summary>
		/// The maximum ascent of all characters in this font, in pixels, from
		/// the baseline.  This is always a positive number or zero.  No characters
		/// extend above this.
		/// </summary>
		public double MaxAscent => _maxAscent;
		private readonly float _maxAscent;

		/// <summary>
		/// The descent of lowercase letters in this font, in pixels.  Some characters
		/// may extend below this, but this is generally where Latin letters would not
		/// appear to be cut off if a line were drawn here, and is the safe position to
		/// draw an underline for this font.  This is always a positive number or zero,
		/// and always &lt;= MaxDescent.
		/// </summary>
		public double Descent => _descent;
		private readonly float _descent;

		/// <summary>
		/// The maximum descent of all characters in this font, in pixels, from
		/// the baseline.  This is always a positive number or zero.  No characters
		/// extend below this.
		/// </summary>
		public double MaxDescent => _maxDescent;
		private readonly float _maxDescent;

		/// <summary>
		/// For fonts that use prerendered glyphs, this is the position of the baseline,
		/// relative to the top of the render box, in pixels.
		/// </summary>
		public double Baseline => _baseline;
		private readonly float _baseline;

		/// <summary>
		/// The ascent of typical lowercase letters in this font, in pixels, excluding
		/// letters like d/l/t/f that have ascenders above the lowercase height.
		/// Some characters may extend above this, but this is generally where
		/// most lowercase letters would not be cut off if a line were drawn here.
		/// This is always a positive number, and always &lt;= MaxAscent, and typically
		/// &lt;= Ascent for most fonts.
		/// </summary>
		public double LowercaseAscent => _lowercaseAscent;
		private readonly float _lowercaseAscent;

		/// <summary>
		/// The default space between one line and the next, in pixels.
		/// </summary>
		public double LineHeight => _lineHeight;
		private readonly float _lineHeight;

		/// <summary>
		/// The width of an em (a capital M), in pixels.
		/// </summary>
		public double EmWidth => _emWidth;
		private readonly float _emWidth;

		/// <summary>
		/// The width of an ex (a lowercase x), in pixels.
		/// </summary>
		public double ExWidth => _exWidth;
		private readonly float _exWidth;

		/// <summary>
		/// The width of a space character, in pixels.  Typically 1/4 to 1/3 of an em.
		/// </summary>
		public double Space => _space;
		private readonly float _space;

		/// <summary>
		/// The amount of kerning to add between non-space characters.
		/// </summary>
		public double Kerning => _kerning;
		private readonly float _kerning;

		/// <summary>
		/// Whether this is a monospace font, or a proportional/fixed-width font.
		/// </summary>
		public bool Monospace => _monospace;
		private readonly bool _monospace;

		/// <summary>
		/// Construct a new FontMetrics object.
		/// </summary>
		/// <param name="ascent">The ascent of uppercase letters/numbers in this font, in pixels.
		/// Some characters may extend above this, but this is generally where
		/// uppercase letters would not be cut off if a line were drawn here.
		/// This is always a positive number, and always &lt;= MaxAscent.</param>
		/// <param name="maxAscent">The maximum ascent of all characters in this font, in pixels,
		/// from the baseline.  This is always a positive number or zero.  No characters
		/// extend above this.</param>
		/// <param name="descent">The descent of lowercase letters in this font, in pixels.  Some characters
		/// may extend below this, but this is generally where Latin letters would not
		/// appear to be cut off if a line were drawn here.
		/// This is always a positive number, and always &lt;= MaxDescent.</param>
		/// <param name="maxDescent">The maximum descent of all characters in this font,
		/// in pixels, from the baseline.  This is always a positive number or zero.  No
		/// characters extend below this.</param>
		/// <param name="baseline">For fonts that use prerendered glyphs, this is the position of the baseline,
		/// relative to the top of the render box, in pixels.</param>
		/// <param name="lowercaseAscent">The ascent of typical lowercase letters in this font, in pixels, excluding
		/// letters like d/l/t/f that have ascenders above the lowercase height.
		/// Some characters may extend above this, but this is generally where
		/// most lowercase letters would not be cut off if a line were drawn here.
		/// This is always a positive number, and always &lt;= MaxAscent, and typically
		/// &lt;= Ascent for most fonts.</param>
		/// <param name="lineHeight">The default space between the baseline of one line
		/// and the baseline of the next, in pixels.</param>
		/// <param name="emWidth">The width of an em (a capital M), in pixels.</param>
		/// <param name="exWidth">The width of an ex (a lowercase x), in pixels.</param>
		/// <param name="space">The width of a space character, in pixels.  Typically 1/4 to 1/3 of an em.</param>
		/// <param name="kerning">The amount of kerning to add between non-space characters.</param>
		/// <param name="monospace">Whether this is a monospace font, or a proportional/fixed-width font.</param>
		public FontMetrics(double ascent = 0, double maxAscent = 0,
			double descent = 0, double maxDescent = 0, double baseline = 0,
			double lowercaseAscent = 0, double lineHeight = 0,
			double emWidth = 0, double exWidth = 0, double space = 0, double kerning = 0,
			bool monospace = false)
		{
			_ascent = (float)ascent;
			_maxAscent = (float)maxAscent;
			_descent = (float)descent;
			_maxDescent = (float)maxDescent;
			_baseline = (float)baseline;
			_lowercaseAscent = (float)lowercaseAscent;
			_lineHeight = (float)lineHeight;
			_emWidth = (float)emWidth;
			_exWidth = (float)exWidth;
			_space = (float)space;
			_kerning = (float)kerning;
			_monospace = monospace;
		}

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithAscent(double ascent)
			=> new FontMetrics(ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithDescent(double descent)
			=> new FontMetrics(Ascent, MaxAscent, descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithBaseline(double baseline)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithLowercaseAscent(double lowercaseAscent)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, lowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithLineHeight(double lineHeight)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, lineHeight,
				EmWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithEmWidth(double emWidth)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				emWidth, ExWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithExWidth(double exWidth)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, exWidth, Space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithSpace(double space)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, space, Kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithKerning(double kerning)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, kerning, Monospace);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public FontMetrics WithMonospace(bool monospace)
			=> new FontMetrics(Ascent, MaxAscent, Descent, MaxDescent, Baseline, LowercaseAscent, LineHeight,
				EmWidth, ExWidth, Space, Kerning, monospace);

		/// <summary>
		/// Compare this FontMetrics to another object, for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if the other object is a FontMetrics and has the same
		/// values as this FontMetrics; false if it is not or does not.</returns>
		public override bool Equals(object? obj)
			=> obj is FontMetrics other && Equals(other);

		/// <summary>
		/// Compare this FontMetrics to another FontMetrics, for equality.
		/// </summary>
		/// <param name="other">The other FontMetrics to compare against.</param>
		/// <returns>True if the other object is a FontMetrics and has the same
		/// values as this FontMetrics; false if it is not or does not.</returns>
		public bool Equals(FontMetrics? other)
			=> !ReferenceEquals(other, null)
				&& (ReferenceEquals(other, this)
					|| (_ascent == other._ascent
						&& _maxAscent == other._maxAscent
						&& _descent == other._descent
						&& _maxDescent == other._maxDescent
						&& _baseline == other._baseline
						&& _lowercaseAscent == other._lowercaseAscent
						&& _lineHeight == other._lineHeight
						&& _emWidth == other._emWidth
						&& _exWidth == other._exWidth
						&& _space == other._space
						&& _kerning == other._kerning
						&& _monospace == other._monospace));

		/// <summary>
		/// Generate a hash code that can be used for efficiently comparing FontMetrics
		/// instances for equality.
		/// </summary>
		/// <returns>A suitable hash code for this FontMetrics instance.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = 0;
				hashCode = (hashCode        ) + _ascent.GetHashCode();
				hashCode = (hashCode * 65599) + _maxAscent.GetHashCode();
				hashCode = (hashCode * 65599) + _descent.GetHashCode();
				hashCode = (hashCode * 65599) + _maxDescent.GetHashCode();
				hashCode = (hashCode * 65599) + _baseline.GetHashCode();
				hashCode = (hashCode * 65599) + _lowercaseAscent.GetHashCode();
				hashCode = (hashCode * 65599) + _lineHeight.GetHashCode();
				hashCode = (hashCode * 65599) + _emWidth.GetHashCode();
				hashCode = (hashCode * 65599) + _exWidth.GetHashCode();
				hashCode = (hashCode * 65599) + _space.GetHashCode();
				hashCode = (hashCode * 65599) + _kerning.GetHashCode();
				hashCode = (hashCode * 65599) + _monospace.GetHashCode();
				return hashCode;
			}
		}

		/// <summary>
		/// Convert this to a string, primarily for debugging purposes.
		/// </summary>
		/// <returns>A string form of this same data.</returns>
		public override string ToString()
			=> $"asc:{Ascent:0.##},max:{MaxAscent:0.##} desc:{Descent:0.##},max:{MaxDescent:0.##}"
				+ $" base:{Baseline:0.##} lower:{LowercaseAscent:0.##}"
				+ $" line:{LineHeight:0.##} em:{EmWidth:0.##} ex:{ExWidth:0.##} spc:{Space:0.##} kern:{Kerning:0.##}"
				+ $" {(_monospace ? " mono" : " prop")}";
	}
}
