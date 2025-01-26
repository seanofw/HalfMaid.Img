using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// A 2D double-valued rectangle.  This uses the (x,y) to +(width,height) form,
	/// where Box2d (where available) represents a rectangle as (x1,y1) to (x2,y2).
	/// This form is often easier to work with, which is why the HalfMaid.Img library
	/// prefers it for most operations.
	/// </summary>
	public struct Rectd : IEquatable<Rectd>
	{
		#region Public Fields

		/// <summary>
		/// The leftmost coordinate of this rectangle.
		/// </summary>
		public double X;

		/// <summary>
		/// The topmost coordinate of this rectangle.
		/// </summary>
		public double Y;

		/// <summary>
		/// The width of this rectangle.
		/// </summary>
		public double Width;

		/// <summary>
		/// The height of this rectangle.
		/// </summary>
		public double Height;

		#endregion

		#region Public Properties

		/// <summary>
		/// The leftmost coordinate of this rectangle (equal to X).
		/// </summary>
		public double Left
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => X;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Width = Right - value;
				X = value;
			}
		}

		/// <summary>
		/// The rightmost coordinate of this rectangle (equal to X + Width).
		/// </summary>
		public double Right
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => X + Width;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Width = value - X;
		}

		/// <summary>
		/// The topmost coordinate of this rectangle (equal to Y).
		/// </summary>
		public double Top
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Y;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Height = Bottom - value;
				Y = value;
			}
		}

		/// <summary>
		/// The bottommost coordinate of this rectangle (equal to Y + Height).
		/// </summary>
		public double Bottom
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Y + Height;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Height = value - Y;
		}

		/// <summary>
		/// The top-left corner of this rectangle, formed from X and Y.
		/// </summary>
		public Vector2d TopLeft
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X, Y);
		}

		/// <summary>
		/// The top-right corner of this rectangle, formed from X+Width and Y.
		/// Note that this is *outside* the actual rectangle's contents, as it's
		/// X+Width, not X+Width-1.
		/// </summary>
		public Vector2d TopRight
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X + Width, Y);
		}

		/// <summary>
		/// The bottom-left corner of this rectangle, formed from X and Y+Height.
		/// Note that this is *outside* the actual rectangle's contents, as it's
		/// Y+Height, not Y+Height-1.
		/// </summary>
		public Vector2d BottomLeft
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X, Y + Height);
		}

		/// <summary>
		/// The bottom-right corner of this rectangle, formed from X+Width and Y+Height.
		/// Note that this is *outside* the actual rectangle's contents, as it's
		/// X+Width and Y+Height, not X+Width-1 and Y+Height-1.
		/// </summary>
		public Vector2d BottomRight
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X + Width, Y + Height);
		}

		/// <summary>
		/// The top-left corner of this rectangle, formed from X and Y.
		/// </summary>
		public Vector2d Point
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X, Y);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}

		/// <summary>
		/// The size of this rectangle, formed from Width and Height.
		/// </summary>
		public Vector2d Size
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(Width, Height);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Width = value.X;
				Height = value.Y;
			}
		}

		/// <summary>
		/// The center of this rectangle.
		/// </summary>
		public Vector2d Center
		{
			[Pure]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2d(X + Width / 2, Y + Height / 2);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Construct a new rectangle from a point and a size, expressed as four distinct values.
		/// </summary>
		/// <param name="x">The leftmost coordinate of the rectangle.</param>
		/// <param name="y">The topmost coordinate of the rectangle.</param>
		/// <param name="width">The width of the rectangle.</param>
		/// <param name="height">The height of the rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd(double x, double y, double width, double height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Construct a new rectangle from a point and a size, expressed as two structs.
		/// </summary>
		/// <param name="point">The top-left coordinate of the rectangle.</param>
		/// <param name="size">The size of the rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd(Vector2d point, Vector2d size)
		{
			X = point.X;
			Y = point.Y;
			Width = size.X;
			Height = size.Y;
		}

		/// <summary>
		/// Construct a rectangle by converting it from a Rect.
		/// </summary>
		/// <param name="rect">The integer-valued rectangle to convert to a double-valued rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd(Rect rect)
		{
			X = rect.X;
			Y = rect.Y;
			Width = rect.Width;
			Height = rect.Height;
		}

		/// <summary>
		/// Promote an integer-valued rectangle to a double-valued rectangle.
		/// </summary>
		/// <param name="rect">The integer-valued rectangle to convert to a double-valued rectangle.</param>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rectd(Rect rect)
			=> new Rectd(rect.Left, rect.Top, rect.Right, rect.Bottom);

		/// <summary>
		/// Demote a double-valued rectangle to an integer-valued rectangle.  This will
		/// round each component to the nearest integer value.  Note that this rounds
		/// the dimensions, not the edges.
		/// </summary>
		/// <param name="rect">The double-valued rectangle to convert to an integer-valued rectangle.</param>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Rect(Rectd rect)
			=> new Rect((int)(rect.X + 0.5), (int)(rect.Y + 0.5),
				(int)(rect.Width + 0.5), (int)(rect.Height + 0.5));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
		/// <summary>
		/// Construct a rectangle by converting it from a Box2i.
		/// </summary>
		/// <param name="box">The box to convert to a rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd(Box2d box)
		{
			X = box.Min.X;
			Y = box.Min.Y;
			Width = box.Max.X - box.Min.X;
			Height = box.Max.Y - box.Min.Y;
		}

		/// <summary>
		/// Rectangles are exactly equivalent to Box2ds -- they are both double-valued
		/// representations of a rectangle of 2D space -- so this is an implicit operator that
		/// can convert between these types at any time.
		/// </summary>
		/// <param name="rect">The rectangle to convert to a Box2d.</param>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Box2d(Rectd rect)
			=> new Box2d(rect.Left, rect.Top, rect.Right, rect.Bottom);

		/// <summary>
		/// Rectangles are exactly equivalent to Box2ds -- they are both double-valued
		/// representations of a rectangle of 2D space -- so this is an implicit operator that
		/// can convert between these types at any time.
		/// </summary>
		/// <param name="box">The Box2d to convert to a rectangle.</param>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rectd(Box2d box)
			=> new Rectd(box);
#endif

		/// <summary>
		/// Create a new rectangle from edges, instead of X/Y/Width/Height.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectd FromEdges(double left, double top, double right, double bottom)
			=> new Rectd(left, top, right - left, bottom - top);

		#endregion

		#region Operators

		/// <summary>
		/// Compare one rectangle to another rectangle for equality.
		/// </summary>
		/// <param name="a">The first rectangle to compare.</param>
		/// <param name="b">The second rectangle to compare.</param>
		/// <returns>True if they are equal, false if they are not.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Rectd a, Rectd b)
			=> a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

		/// <summary>
		/// Compare one rectangle to another rectangle for equality.
		/// </summary>
		/// <param name="a">The first rectangle to compare.</param>
		/// <param name="b">The second rectangle to compare.</param>
		/// <returns>False if they are equal, true if they are not.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Rectd a, Rectd b)
			=> !(a == b);

		#endregion

		#region Public Methods

		/// <summary>
		/// Determine if this rectangle contains the given point.
		/// </summary>
		/// <param name="x">The X coordinate of the point to test.</param>
		/// <param name="y">The Y coordinate of the point to test.</param>
		/// <returns>True if the rectangle contains the point, false otherwise.  Note that
		/// at the right/bottom edges of the rectangle, the point is considered to be within
		/// the rectangle if it is *less than* X+Width and Y+Height, not less-than-or-equal-to
		/// X+Width and Y+Height.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(double x, double y)
			=> X <= x && x < X + Width && Y <= y && y < Y + Height;

		/// <summary>
		/// Determine if this rectangle contains the given point.
		/// </summary>
		/// <param name="value">The point to test.</param>
		/// <returns>True if the rectangle contains the point, false otherwise.  Note that
		/// at the right/bottom edges of the rectangle, the point is considered to be within
		/// the rectangle if it is *less than* X+Width and Y+Height, not less-than-or-equal-to
		/// X+Width and Y+Height.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2d value)
			=> X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

		/// <summary>
		/// Determine if this rectangle fully contains the given other rectangle.
		/// </summary>
		/// <param name="other">The rectangle to test.</param>
		/// <returns>True if the rectangle fully contains the other rectangle, false otherwise
		/// (i.e., the intersection of this rectangle and the other rectangle equals the
		/// other rectangle).</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Rectd other)
			=> X <= other.X && other.X + other.Width <= X + Width
				&& Y <= other.Y && other.Y + other.Height <= Y + Height;

		/// <summary>
		/// Compare this rectangle to another object for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if they are equal, false if they are not.</returns>
		[Pure]
		public override bool Equals(object? obj)
			=> obj is Rectd rect && this == rect;

		/// <summary>
		/// Compare this rectangle to another rectangle for equality.
		/// </summary>
		/// <param name="other">The other rectangle to compare.</param>
		/// <returns>True if they are equal, false if they are not.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Rectd other)
			=> this == other;

		/// <summary>
		/// Calculate a hash code so that this rectangle may be used as a
		/// dictionary key or added to a hash table.  Note that in these use
		/// cases, the rectangle's fields should *not* be modified while it
		/// is being used as a key, or the results will be unpredictable.
		/// </summary>
		/// <returns>A suitable hash code for this rectangle.</returns>
		[Pure]
		public override int GetHashCode()
			=> unchecked(((X.GetHashCode() * 65599 + Y.GetHashCode()) * 65599
				+ Width.GetHashCode()) * 65599 + Height.GetHashCode());

		/// <summary>
		/// Expand this rectangle by adding 'horizontalAmount' to both the left edge
		/// and the right edge, and by adding 'verticalAmount' to both the top edge
		/// and bottom edge.
		/// </summary>
		/// <param name="horizontalAmount">The amount to subtract from Left and add to Right.</param>
		/// <param name="verticalAmount">The amount to subtract from Top and add to Bottom.</param>
		/// <returns>A new rectangle that has been expanded by the given amount.</returns>
		[Pure]
		public Rectd Grow(double horizontalAmount, double verticalAmount)
			=> new Rectd(X - horizontalAmount, Y - verticalAmount,
				Width + horizontalAmount * 2, Height + verticalAmount * 2);

		/// <summary>
		/// Add amounts individually to each of the core rectangle values of X, Y, Width, and Height.
		/// </summary>
		/// <param name="dx">The amount to add to X.</param>
		/// <param name="dy">The amount to add to Y.</param>
		/// <param name="dw">The amount to add to Width.</param>
		/// <param name="dh">The amount to add to Height.</param>
		/// <returns>A new rectangle where the given amounts have been added to this rectangle's values.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Add(double dx = 0, double dy = 0, double dw = 0, double dh = 0)
			=> new Rectd(X + dx, Y + dy, Width + dw, Height + dh);

		/// <summary>
		/// Subtract amounts individually from each of the core rectangle values of X, Y, Width, and Height.
		/// </summary>
		/// <param name="dx">The amount to subtract from X.</param>
		/// <param name="dy">The amount to subtract from Y.</param>
		/// <param name="dw">The amount to subtract from Width.</param>
		/// <param name="dh">The amount to subtract from Height.</param>
		/// <returns>A new rectangle where the given amounts have been subtracted from this rectangle's values.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Sub(double dx = 0, double dy = 0, double dw = 0, double dh = 0)
			=> new Rectd(X - dx, Y - dy, Width - dw, Height - dh);

		/// <summary>
		/// Change the size of the given rectangle by the given scalar amount.
		/// </summary>
		/// <param name="rect">The rectangle to expand or contract.</param>
		/// <param name="scalar">A scalar that will be multiplied against each of the Width and Height.</param>
		/// <returns>A new rectangle with the same X and Y whose size has been scaled by the given amount.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectd operator *(Rectd rect, double scalar)
			=> new Rectd(rect.X, rect.Y, rect.Width * scalar, rect.Height * scalar);

		/// <summary>
		/// Change the size of the given rectangle by the given scalar amount.
		/// </summary>
		/// <param name="rect">The rectangle to expand or contract.</param>
		/// <param name="scalar">A scalar that will be divided into each of the Width and Height.</param>
		/// <returns>A new rectangle with the same X and Y whose size has been contracted by the given amount.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectd operator /(Rectd rect, double scalar)
			=> new Rectd(rect.X, rect.Y, rect.Width / scalar, rect.Height / scalar);

		/// <summary>
		/// Determine if this rectangle intersects with another rectangle.
		/// </summary>
		/// <param name="other">The other rectangle to test for intersection.</param>
		/// <returns>True if this rectangle intersects the other, false if they do not intersect.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(Rectd other)
			=> other.Left < Right && Left < other.Right
				&& other.Top < Bottom && Top < other.Bottom;

		/// <summary>
		/// Clip this rectangle to a container whose origin is (0, 0) and whose size is
		/// (maxWidth, maxHeight).  This is like Intersect(), but faster for the special
		/// (but common!) case where the container's origin is at (0, 0).
		/// </summary>
		/// <param name="maxWidth">The width of the container to clip to.</param>
		/// <param name="maxHeight">The height of the container to clip to.</param>
		/// <returns>A new rectangle that has been clipped to fit within the given container.
		/// This may be all zeros if this rectangle lies outside the container.</returns>
		[Pure]
		public Rectd Clip(double maxWidth, double maxHeight)
		{
			double x = X, y = Y, width = Width, height = Height;

			if (width <= 0 || height <= 0 || x >= maxWidth || y >= maxHeight)
				return new Rectd(0, 0, 0, 0);

			if (x < 0)
			{
				width += x;
				x = 0;
			}
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (width > maxWidth - x)
				width = maxWidth - x;
			if (height > maxHeight - y)
				height = maxHeight - y;

			return new Rectd(x, y, width, height);
		}

		/// <summary>
		/// Calculate the intersection of this rectangle with another.
		/// </summary>
		/// <param name="other">The other rectangle to intersect with.</param>
		/// <returns>The intersection, which will be all zeros if the rectangles
		/// do not intersect.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Intersect(Rectd other)
			=> TryIntersect(this, other, out Rectd result) ? result : result;

		/// <summary>
		/// Calculate the intersection of two rectangles.
		/// </summary>
		/// <param name="a">The first rectangle to intersect.</param>
		/// <param name="b">The other rectangle to intersect it with.</param>
		/// <returns>The intersection, which will be all zeros if the rectangles
		/// do not intersect.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectd Intersect(Rectd a, Rectd b)
			=> TryIntersect(a, b, out Rectd result) ? result : result;

		/// <summary>
		/// Calculate the intersection of one rectangle with another.
		/// </summary>
		/// <param name="a">The first rectangle to intersect.</param>
		/// <param name="b">The other rectangle to intersect it with.</param>
		/// <param name="result">The intersection, which will be all zeros if the rectangles
		/// do not intersect.</param>
		/// <returns>True if the rectangles intersect, false if they do not.</returns>
		[Pure]
		public static bool TryIntersect(Rectd a, Rectd b, out Rectd result)
		{
			if (!a.Intersects(b))
			{
				result = new Rectd(0, 0, 0, 0);
				return false;
			}

			double left = Math.Max(a.Left, b.Left);
			double top = Math.Max(a.Top, b.Top);
			double right = Math.Min(a.Right, b.Right);
			double bottom = Math.Min(a.Bottom, b.Bottom);

			result = FromEdges(left, top, right, bottom);
			return true;
		}

		/// <summary>
		/// Shift this rectangle by an offset, in place.  (This is a mutating operation;
		/// for the pure operation, see Offsetted() instead.)
		/// </summary>
		/// <param name="dx">An amount to add to X, in place.</param>
		/// <param name="dy">An amount to add to Y, in place.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Offset(double dx, double dy)
		{
			X += dx;
			Y += dy;
		}

		/// <summary>
		/// Shift this rectangle by an offset, in place.  (This is a mutating operation;
		/// for the pure operation, see Offsetted() instead.)
		/// </summary>
		/// <param name="v">A vector to add to X and Y, in place.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Offset(Vector2d v)
		{
			X += v.X;
			Y += v.Y;
		}

		/// <summary>
		/// Convert this rectangle to a string representation.
		/// </summary>
		/// <returns>A simple string representation of this rectangle, in the
		/// form "(X,Y,Width,Height)".</returns>
		[Pure]
		public override string ToString() =>
			$"({X},{Y},{Width},{Height})";

		/// <summary>
		/// Calculate the union of this rectangle with another, the smallest rectangle
		/// that can contain both of them.
		/// </summary>
		/// <param name="other">The other rectangle to union with this.</param>
		/// <returns>The smallest rectangle that contains both this rectangle and the other.</returns>
		public Rectd Union(Rectd other)
			=> Union(this, other);

		/// <summary>
		/// Calculate the union of two rectangles, the smallest rectangle that can
		/// contain both of them.
		/// </summary>
		/// <param name="a">The first rectangle to union.</param>
		/// <param name="b">The second rectangle to union.</param>
		/// <returns>The smallest rectangle that contains both rectangles A and B.</returns>
		[Pure]
		public static Rectd Union(Rectd a, Rectd b)
		{
			double x = Math.Min(a.X, b.X);
			double y = Math.Min(a.Y, b.Y);
			return new Rectd(x, y,
				Math.Max(a.Right, b.Right) - x,
				Math.Max(a.Bottom, b.Bottom) - y);
		}

		/// <summary>
		/// Deconstruct this rectangle, for compatibility with the destruction operator.
		/// </summary>
		/// <param name="x">The X coordinate of this rectangle.</param>
		/// <param name="y">The Y coordinate of this rectangle.</param>
		/// <param name="width">The width of this rectangle.</param>
		/// <param name="height">The height of this rectangle.</param>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Deconstruct(out double x, out double y, out double width, out double height)
		{
			x = X;
			y = Y;
			width = Width;
			height = Height;
		}

		/// <summary>
		/// Transpose the rectangle by swapping its X/Y and Width/Height.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Transposed()
			=> new Rectd(Y, X, Height, Width);

		/// <summary>
		/// Move the given rectangle by the given delta, making a copy of it.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Offseted(double dx, double dy)
			=> new Rectd(X + dx, Y + dy, Width, Height);

		/// <summary>
		/// Move the given rectangle by the given delta, making a copy of it.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd Offseted(Vector2d delta)
			=> new Rectd(X + delta.X, Y + delta.Y, Width, Height);

		/// <summary>
		/// Flip the rectangle horizontally over the given X coordinate.  The resulting
		/// rectangle will still be normalized in its new position.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd FlippedHorz(double axis = 0)
			=> FromEdges(axis + (axis - Right), Top, axis + (axis - Left), Bottom);

		/// <summary>
		/// Flip the rectangle vertically over the given Y coordinate.  The resulting
		/// rectangle will still be normalized in its new position.
		/// </summary>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Rectd FlippedVert(double axis = 0)
			=> FromEdges(Left, axis + (axis - Bottom), Right, axis + (axis - Top));

		/// <summary>
		/// Scale the given rectangle by multiplying each of its components by the given amount.
		/// </summary>
		[Pure]
		public Rectd Scaled(double sx = 1.0, double sy = 1.0, double swidth = 1.0, double sheight = 1.0)
			=> new Rectd(X * sx, Y * sy, Width * swidth, Height * sheight);

		#endregion
	}

}
