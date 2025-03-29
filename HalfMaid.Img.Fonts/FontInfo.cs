using System;
using System.Text;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// Information describing which font is being used within a given
	/// font family.  This struct is intended to be at least passably compatible
	/// with similar concepts used in WPF and HTML to avoid substantial relearning.
	/// </summary>
	public readonly struct FontInfo : IEquatable<FontInfo>
	{
		/// <summary>
		/// The size of this font (in points), from 0.0 to 9999.9999.
		/// </summary>
		public double Size => _size * (1.0 / 10000);
		private readonly int _size;

		/// <summary>
		/// The weight of this font, from 1 (ultra light) to 999 (ultra heavy).
		/// </summary>
		public int Weight => _weight;
		private readonly ushort _weight;

		/// <summary>
		/// This font's stretch, from 1 to 9.
		/// </summary>
		public int Stretch => _stretch;
		private readonly byte _stretch;

		/// <summary>
		/// Whether this is considered a "bold" font.
		/// </summary>
		public bool Bold => _weight > FontWeight.Medium + 50;

		/// <summary>
		/// The style of this font (normal, italic, or oblique).
		/// </summary>
		public FontStyle Style => _style;
		private readonly FontStyle _style;

		/// <summary>
		/// Construct a font-info object for a given font.
		/// </summary>
		/// <param name="size">The size of this font (in points), from 0.0 to 9999.9999.</param>
		/// <param name="weight">The weight of this font, from 1 (ultra light) to 999 (ultra heavy).</param>
		/// <param name="style">The style of this font (normal, italic, or oblique).</param>
		/// <param name="stretch">This font's stretch, from 1 to 9.</param>
		public FontInfo(double size, int weight = 400, FontStyle style = default, int stretch = 5)
		{
			_size = (int)(Math.Max(Math.Min(size, 9999.9999), 0) * 10000 + 0.5);
			_weight = (ushort)Math.Min(Math.Max(weight, 1), 999);
			_style = style;
			_stretch = (byte)Math.Min(Math.Max(stretch, 1), 9);
		}

		/// <summary>
		/// Copy this struct, replacing one property.
		/// </summary>
		public FontInfo WithSize(double size)
			=> new FontInfo(size, Weight, Style, Stretch);

		/// <summary>
		/// Copy this struct, replacing one property.
		/// </summary>
		public FontInfo WithWeight(int weight)
			=> new FontInfo(Size, weight, Style, Stretch);

		/// <summary>
		/// Copy this struct, replacing one property.
		/// </summary>
		public FontInfo WithStyle(FontStyle style)
			=> new FontInfo(Size, Weight, style, Stretch);

		/// <summary>
		/// Copy this struct, replacing one property.
		/// </summary>
		public FontInfo WithStretch(int stretch)
			=> new FontInfo(Size, Weight, Style, stretch);

		/// <summary>
		/// Compare this font info struct against another for equality.
		/// </summary>
		/// <param name="obj">The other font info to compare against.</param>
		/// <returns>True if they are equal, false if they are different.</returns>
		public override bool Equals(object? obj)
			=> obj is FontInfo fontInfo && Equals(fontInfo);

		/// <summary>
		/// Compare this font info struct against another for equality.
		/// </summary>
		/// <param name="fontInfo">The other font info to compare against.</param>
		/// <returns>True if they are equal, false if they are different.</returns>
		public bool Equals(FontInfo fontInfo)
			=> _size == fontInfo._size
				&& _weight == fontInfo._weight
				&& _style == fontInfo._style
				&& _stretch == fontInfo._stretch;

		/// <summary>
		/// Get a hash code so that this object can be used as a dictionary key
		/// or in a hash table.
		/// </summary>
		/// <returns>A hash code suitable for keying against this struct.</returns>
		public override int GetHashCode()
			=> unchecked(((_stretch * 65599 + (int)_style) * 65599 + _weight) * 65599 + _size);

		/// <summary>
		/// Compare two font info structs for equality.
		/// </summary>
		/// <param name="a">The first font info struct.</param>
		/// <param name="b">The other font info struct to compare against.</param>
		/// <returns>True if they are equal, false if they are different.</returns>
		public static bool operator ==(FontInfo a, FontInfo b)
			=> a.Equals(b);

		/// <summary>
		/// Compare two font info structs for equality.
		/// </summary>
		/// <param name="a">The first font info struct.</param>
		/// <param name="b">The other font info struct to compare against.</param>
		/// <returns>False if they are equal, true if they are different.</returns>
		public static bool operator !=(FontInfo a, FontInfo b)
			=> !a.Equals(b);

		/// <summary>
		/// Convert this font info to a string, largely for debugging purposes.
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.AppendFormat("{0.0###} pt", _size);

			if (_weight != FontWeight.Normal)
			{
				stringBuilder.Append(" ");

				stringBuilder.Append(_weight switch
				{
					< 150 => "thin",
					< 250 => "extralight",
					< 350 => "light",
					< 450 => "normal",
					< 550 => "medium",
					< 650 => "semibold",
					< 750 => "bold",
					< 850 => "extrabold",
					_ => "black",
				});
				if (_weight % 100 != 0)
					stringBuilder.Append($" ({_weight})");
			}

			if (_style != FontStyle.Normal)
			{
				stringBuilder.Append(" ");

				stringBuilder.Append(_style switch
				{
					FontStyle.Italic => "italic",
					FontStyle.Oblique => "oblique",
					_ => "normal",
				});
			}

			if (_stretch != FontStretch.Medium)
			{
				stringBuilder.Append(" ");

				stringBuilder.Append(_stretch switch
				{
					1 => "ultracondensed",
					2 => "extracondensed",
					3 => "condensed",
					4 => "semicondensed",
					5 => "medium",
					6 => "semiexpanded",
					7 => "expanded",
					8 => "extraexpanded",
					9 => "ultraexpanded",
					_ => "unknown",
				});
			}

			return stringBuilder.ToString();
		}
	}
}
