using System;
using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A single glyph in a font, which is a visual representation of a single
	/// displayable character.  This describes a rectangle of a source image that
	/// should be blitted to present that character to a user.  This is abstract,
	/// in the sense that it describes what the character looks like, but not *where*
	/// or *when* to display it.  Instances of this class are immutable (read-only).
	/// </summary>
	public sealed class Glyph : IEquatable<Glyph>
	{
		/// <summary>
		/// The image this glyph comes from.
		/// </summary>
		public IImage Image => _image;
		private readonly IImage _image;

		/// <summary>
		/// The Unicode code-point that this glyph represents.
		/// </summary>
		public int CodePoint => _codePoint;
		private readonly int _codePoint;

		/// <summary>
		/// The X coordinate of the rectangle of the source image to copy for this glyph,
		/// in whole pixels.
		/// </summary>
		public int X => _x;
		private readonly short _x;

		/// <summary>
		/// The Y coordinate of the rectangle of the source image to copy for this glyph,
		/// in whole pixels.
		/// </summary>
		public int Y => _y;
		private readonly short _y;

		/// <summary>
		/// The width of the rectangle of the source image to copy for this glyph,
		/// in whole pixels.
		/// </summary>
		public int Width => _width;
		private readonly short _width;

		/// <summary>
		/// The height of the rectangle of the source image to copy for this glyph,
		/// in whole pixels.
		/// </summary>
		public int Height => _height;
		private readonly short _height;

		/// <summary>
		/// How far to advance after rendering this glyph.
		/// </summary>
		public Vector2d Advance => _advance;
		private readonly Vector2 _advance;

		/// <summary>
		/// The position of the origin coordinate relative to this glyph's drawing
		/// rectangle.  For typical letters and numbers, this is the position of the
		/// top-left corner of the glyph's render box.
		/// </summary>
		public Vector2i Origin => _origin;
		private readonly Vector2i _origin;

		/// <summary>
		/// The offset of the top-left corner of the glyph relative to its source image, in pixels.
		/// </summary>
		public Vector2i Offset => new Vector2i(X, Y);

		/// <summary>
		/// The size of glyph, in pixels.
		/// </summary>
		public Vector2i Size => new Vector2i(Width, Height);

		/// <summary>
		/// The rectangle of the glyph, as an actual rectangle struct.
		/// </summary>
		public Rect Rect => new Rect(X, Y, Width, Height);

		/// <summary>
		/// A static glyph that doesn't reference any actual pixels of
		/// anything meaningful and is size zero.
		/// </summary>
		public static Glyph Empty { get; } = new Glyph(new Image32(0, 0), 0, 0, 0, 0, 0, default, default);

		/// <summary>
		/// Construct a new Glyph struct.
		/// </summary>
		/// <param name="image">The image this glyph comes from.</param>
		/// <param name="codePoint">The Unicode code-point that this glyph represents.</param>
		/// <param name="rect">The the rectangle of the source image to copy for this glyph.</param>
		/// <param name="origin">The position of the origin coordinate relative to this glyph's
		/// drawing rectangle.  For typical letters and numbers, this is the position of the
		/// glyph's left edge and the baseline, respectively.</param>
		public Glyph(IImage image, int codePoint, Rect rect, Vector2i origin)
		{
			_image = image;
			_codePoint = codePoint;
			_x = (short)rect.X;
			_y = (short)rect.Y;
			_width = (short)rect.Width;
			_height = (short)rect.Height;
			_origin = origin;
		}

		/// <summary>
		/// Construct a new Glyph struct.
		/// </summary>
		/// <param name="image">The image this glyph comes from.</param>
		/// <param name="codePoint">The Unicode code-point that this glyph represents.</param>
		/// <param name="offset">The offset of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="size">The size of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="origin">The position of the origin coordinate relative to this glyph's
		/// drawing rectangle.  For typical letters and numbers, this is the position of the
		/// glyph's left edge and the baseline, respectively.</param>
		/// <param name="advance">How far to advance in the X and Y directions after rendering this glyph.</param>
		public Glyph(IImage image, int codePoint,
			Vector2i offset, Vector2i size, Vector2i origin, Vector2d advance)
		{
			_image = image;
			_codePoint = codePoint;
			_x = (short)offset.X;
			_y = (short)offset.Y;
			_width = (short)size.X;
			_height = (short)size.Y;
			_origin = origin;
			_advance = (Vector2)advance;
		}

		/// <summary>
		/// Construct a new Glyph struct.
		/// </summary>
		/// <param name="image">The image this glyph comes from.</param>
		/// <param name="codePoint">The Unicode code-point that this glyph represents.</param>
		/// <param name="x">The X coordinate of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="y">The Y coordinate of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="width">The width of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="height">The height of the rectangle of the source image to copy for this glyph.</param>
		/// <param name="origin">The position of the origin coordinate relative to this glyph's
		/// drawing rectangle.  For typical letters and numbers, this is the position of the
		/// glyph's left edge and the baseline, respectively.</param>
		/// <param name="advance">How far to advance in the X and Y directions after rendering this glyph.</param>
		public Glyph(IImage image, int codePoint,
			int x, int y, int width, int height, Vector2i origin, Vector2d advance)
		{
			_image = image;
			_codePoint = codePoint;
			_x = (short)x;
			_y = (short)y;
			_width = (short)width;
			_height = (short)height;
			_origin = origin;
			_advance = (Vector2)advance;
		}

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithImage(IImage image)
			=> new Glyph(image, CodePoint, X, Y, Width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithCodePoint(int codePoint)
			=> new Glyph(Image, codePoint, X, Y, Width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithX(int x)
			=> new Glyph(Image, CodePoint, x, Y, Width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithY(int y)
			=> new Glyph(Image, CodePoint, X, y, Width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithWidth(int width)
			=> new Glyph(Image, CodePoint, X, Y, width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithHeight(int height)
			=> new Glyph(Image, CodePoint, X, Y, Width, height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing two properties (in bulk).
		/// </summary>
		public Glyph WithOffset(Vector2i offset)
			=> new Glyph(Image, CodePoint, offset.X, offset.Y, Width, Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing two properties (in bulk).
		/// </summary>
		public Glyph WithSize(Vector2i size)
			=> new Glyph(Image, CodePoint, X, Y, size.X, size.Y, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing four properties (in bulk).
		/// </summary>
		public Glyph WithRect(Rect rect)
			=> new Glyph(Image, CodePoint, rect.X, rect.Y, rect.Width, rect.Height, Origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithOrigin(Vector2i origin)
			=> new Glyph(Image, CodePoint, X, Y, Width, Height, origin, Advance);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public Glyph WithAdvance(Vector2d advance)
			=> new Glyph(Image, CodePoint, X, Y, Width, Height, Origin, advance);

		/// <summary>
		/// Compare this glyph against another object for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if the are the same glyph, false if they are different.</returns>
		public override bool Equals(object? obj)
			=> obj is Glyph glyph && Equals(glyph);

		/// <summary>
		/// Compare this glyph against another glyph for equality.
		/// </summary>
		/// <param name="other">The other glyph to compare against.</param>
		/// <returns>True if they are the same glyph, false if they are different.</returns>
		public bool Equals(Glyph? other)
			=> !ReferenceEquals(other, null)
				&& (ReferenceEquals(other, this)
					|| (ReferenceEquals(_image, other._image)
						&& _codePoint == other._codePoint
						&& _x == other._x && _y == other._y
						&& _width == other._width && _height == other._height
						&& _origin == other._origin
						&& _advance == other._advance));

		/// <summary>
		/// Get a hash code for this glyph, so that this glyph can be used as a key
		/// in dictionaries and as an entry in a hash table.
		/// </summary>
		/// <returns>A hash code for this glyph.</returns>
		public override int GetHashCode()
			=> unchecked((((_x * 65599) + _y) * 65599) + _codePoint);

		/// <summary>
		/// Compare one glyph against another glyph for equality.
		/// </summary>
		/// <param name="a">The first glyph to compare.</param>
		/// <param name="b">The other glyph to compare against.</param>
		/// <returns>True if they are the same glyph, false if they are different.</returns>
		public static bool operator ==(Glyph? a, Glyph? b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);

		/// <summary>
		/// Compare one glyph against another glyph for equality.
		/// </summary>
		/// <param name="a">The first glyph to compare.</param>
		/// <param name="b">The other glyph to compare against.</param>
		/// <returns>False if they are the same glyph, true if they are different.</returns>
		public static bool operator !=(Glyph? a, Glyph? b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		/// <summary>
		/// Convert this glyph to a string, mostly for debugging purposes.
		/// </summary>
		/// <returns>A string representation of the glyph.</returns>
		public override string ToString()
			=> $"'{(char)_codePoint}' {Width}x{Height} at ({X},{Y}) (origin at {Origin.X:0.###},{Origin.Y:0.###}; advance by ({Advance.X:0.###},{Advance.Y:0.###}))";
	}
}
