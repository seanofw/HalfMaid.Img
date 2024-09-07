using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HalfMaid.Img
{
	/// <summary>
	/// A color, in 32-bit truecolor, with lots of operations to manipulate it.<br />
	/// <br />
	/// This is intentionally laid out in memory as a single 32-bit word, with the
	/// bytes always in the order of [R, G, B, A].  This allows for many optimizations,
	/// such as treating it as a uint (or even pairs of colors as a ulong), or by
	/// performing direct pointer read/write operations to pinned memory, or even
	/// pinning the memory so external libraries written in C or C++ can interact
	/// with it directly.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly struct Color32 : IEquatable<Color32>
	{
		#region Constants

		/// <summary>
		/// How bright the red channel appears to the human eye, where black = 0.0 and white = 1.0,
		/// per ITU-R Recommendation BT.601 (NTSC/MPEG).
		/// </summary>
		public const double ApparentRedBrightness = 0.299;

		/// <summary>
		/// How bright the green channel appears to the human eye, where black = 0.0 and white = 1.0,
		/// per ITU-R Recommendation BT.601 (NTSC/MPEG).
		/// </summary>
		public const double ApparentGreenBrightness = 0.587;

		/// <summary>
		/// How bright the blue channel appears to the human eye, where black = 0.0 and white = 1.0,
		/// per ITU-R Recommendation BT.601 (NTSC/MPEG).
		/// </summary>
		public const double ApparentBlueBrightness = 0.114;

		#endregion

		#region Core storage: Exactly four bytes in sequence.

		/// <summary>
		/// The red component of the color.
		/// </summary>
		public readonly byte R;

		/// <summary>
		/// The green component of the color.
		/// </summary>
		public readonly byte G;

		/// <summary>
		/// The blue component of the color.
		/// </summary>
		public readonly byte B;

		/// <summary>
		/// The alpha component of the color.
		/// </summary>
		public readonly byte A;

		#endregion

		#region Properties

		/// <summary>
		/// The red component, as a floating-point value, clamped to [0.0f, 1.0f].
		/// </summary>
		public readonly float Rf => R * (1.0f / 255);

		/// <summary>
		/// The green component, as a floating-point value, clamped to [0.0f, 1.0f].
		/// </summary>
		public readonly float Gf => G * (1.0f / 255);

		/// <summary>
		/// The blue component, as a floating-point value, clamped to [0.0f, 1.0f].
		/// </summary>
		public readonly float Bf => B * (1.0f / 255);

		/// <summary>
		/// The alpha component, as a floating-point value, clamped to [0.0f, 1.0f].
		/// </summary>
		public readonly float Af => A * (1.0f / 255);

		/// <summary>
		/// The red component, as a double-precision floating-point value, clamped to [0.0, 1.0].
		/// </summary>
		public readonly double Rd => R * (1.0 / 255);

		/// <summary>
		/// The green component, as a double-precision floating-point value, clamped to [0.0, 1.0].
		/// </summary>
		public readonly double Gd => G * (1.0 / 255);

		/// <summary>
		/// The blue component, as a double-precision floating-point value, clamped to [0.0, 1.0].
		/// </summary>
		public readonly double Bd => B * (1.0 / 255);

		/// <summary>
		/// The alpha component, as a double-precision floating-point value, clamped to [0.0, 1.0].
		/// </summary>
		public readonly double Ad => A * (1.0 / 255);

		/// <summary>
		/// Retrieve a color value by its traditional index in the sequence of R, G, B, A.
		/// </summary>
		/// <param name="index">The index of the color value to retrieve, in the range of 0 to 3
		/// (inclusive).</param>
		/// <returns>The color value at the given index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not in the
		/// range of 0 to 3.</exception>
		public byte this[int index] => index switch
		{
			0 => R,
			1 => G,
			2 => B,
			3 => A,
			_ => throw new ArgumentOutOfRangeException(nameof(index)),
		};

		/// <summary>
		/// Retrieve a color value by its channel enum.
		/// </summary>
		/// <param name="channel">The channel of the color value to retrieve.</param>
		/// <returns>The color value of that channel.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the channel is unknown.</exception>
		public byte this[ColorChannel channel] => channel switch
		{
			ColorChannel.Red => R,
			ColorChannel.Green => G,
			ColorChannel.Blue => B,
			ColorChannel.Alpha => A,
			_ => throw new ArgumentOutOfRangeException(nameof(channel)),
		};

		#endregion

		#region Construction

		/// <summary>
		/// Construct a color from four floating-point values, R, G, B, and A, in the range
		/// of [0.0f, 1.0f] each.
		/// </summary>
		/// <remarks>
		/// Values outside [0.0f, 1.0f] but still in the range of [-2^23, 2^23] will be clamped
		/// to [0.0f, 1.0f].  Values larger than 2^23 or smaller than -2^23 will produce garbage
		/// and have undefined results.
		/// </remarks>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32(float r, float g, float b, float a = 1.0f)
			: this((int)(r * 255 + 0.5f), (int)(g * 255 + 0.5f), (int)(b * 255 + 0.5f), (int)(a * 255 + 0.5f))
		{
		}

		/// <summary>
		/// Construct a color from four double-precision floating-point values,
		/// R, G, B, and A, in the range of [0.0, 1.0] each.
		/// </summary>
		/// <remarks>
		/// Values outside [0.0, 1.0] but still in the range of [-2^23, 2^23] will be clamped
		/// to [0.0, 1.0].  Values larger than 2^23 or smaller than -2^23 will produce garbage
		/// and have undefined results.
		/// </remarks>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32(double r, double g, double b, double a = 1.0)
			: this((int)(r * 255 + 0.5), (int)(g * 255 + 0.5), (int)(b * 255 + 0.5), (int)(a * 255 + 0.5))
		{
		}

		/// <summary>
		/// Construct a color from four integer values, R, G, B, and A, in the range of 0-255
		/// each.  Values outside 0-255 will be clamped to that range.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32(int r, int g, int b, int a = 255)
		{
			if (((r | g | b | a) & 0xFFFFFF00) == 0)
			{
				R = (byte)r;
				G = (byte)g;
				B = (byte)b;
				A = (byte)a;
			}
			else
			{
				R = (byte)Math.Min(Math.Max(r, 0), 255);
				G = (byte)Math.Min(Math.Max(g, 0), 255);
				B = (byte)Math.Min(Math.Max(b, 0), 255);
				A = (byte)Math.Min(Math.Max(a, 0), 255);
			}
		}

		/// <summary>
		/// Construct a color from four byte values, R, G, B, and A, in the range of 0-255
		/// each.  This overload sets the bytes directly, so it may be faster than the other
		/// constructors.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32(byte r, byte g, byte b, byte a = 255)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		#endregion

		#region Equality and hash codes

		/// <summary>
		/// Compare this color to another object on the heap for equality.
		/// </summary>
		public override bool Equals(object? obj)
			=> obj is Color32 color && this == color;

		/// <summary>
		/// Compare this color to another for equality.
		/// </summary>
		public bool Equals(Color32 other)
			=> this == other;

		/// <summary>
		/// Compare this color to another color for equality.
		/// </summary>
		public static bool operator ==(Color32 a, Color32 b)
			=> a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

		/// <summary>
		/// Compare this color to another color for inequality.
		/// </summary>
		public static bool operator !=(Color32 a, Color32 b)
			=> a.R != b.R || a.G != b.G || a.B != b.B || a.A != b.A;

		/// <summary>
		/// Calculate a hash code for this color so that it can be used in
		/// hash tables and dictionaries efficiently.
		/// </summary>
		public override int GetHashCode()
			=> unchecked(((A * 65599 + B) * 65599 + G) * 65599 + R);

		#endregion

		#region Stringification

		/// <summary>
		/// Get a string form of this color, as a 6-digit hex code, preceded by a
		/// sharp sign, like in CSS:  #RRGGBB
		/// </summary>
		public string Hex6
		{
			get
			{
				Span<char> buffer = stackalloc char[7];
				buffer[0] = '#';
				WriteByte(buffer, 1, R);
				WriteByte(buffer, 3, G);
				WriteByte(buffer, 5, B);
				return buffer.ToString();
			}
		}

		/// <summary>
		/// Get a string form of this color, as an 8-digit hex code,
		/// preceded by a sharp sign, like extended CSS:  #RRGGBBAA
		/// 
		/// (Browsers have supported this form since 2016, so we're
		/// in good company supporting it here.)
		/// 
		/// (Note that this is NOT the same as the 8-digit form that
		/// WPF/XAML uses, which is #AARRGGBB.  Microsoft may have done
		/// theirs first, but the world standard is #RRGGBBAA.)
		/// </summary>
		public string Hex8
		{
			get
			{
				Span<char> buffer = stackalloc char[9];
				buffer[0] = '#';
				WriteByte(buffer, 1, R);
				WriteByte(buffer, 3, G);
				WriteByte(buffer, 5, B);
				WriteByte(buffer, 7, A);
				return buffer.ToString();
			}
		}

		/// <summary>
		/// Write a single hex nybble out to the given buffer.
		/// </summary>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="position">The position in that buffer to write.</param>
		/// <param name="value">The nybble to write, which must be in the range of [0, 15].</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WriteNybble(Span<char> buffer, int position, byte value)
			=> buffer[position] = value < 10
				? (char)(value + '0')
				: (char)(value - 10 + 'A');

		/// <summary>
		/// Write a single hex byte out to the given buffer.
		/// </summary>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="position">The position in that buffer to write.</param>
		/// <param name="value">The byte to write.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WriteByte(Span<char> buffer, int position, byte value)
		{
			WriteNybble(buffer, position, (byte)(value >> 4));
			WriteNybble(buffer, position + 1, (byte)(value & 0xF));
		}

		/// <summary>
		/// Convert this color to its most natural string representation.
		/// </summary>
		/// <returns>A "natural" string representation, which will be the color's name if
		/// it has one, or an #RRGGBB(AA) form if it doesn't have a name.</returns>
		public override string ToString()
			=> NamesByColor.TryGetValue(this, out string? name) ? name : ToHexString();

		/// <summary>
		/// Convert this to a string of the form rgb(R,G,B,A), omitting the 'A' if it's 255, and
		/// converting 'A' to a value in the range of [0, 1].
		/// </summary>
		/// <returns>A string form of this color, as a CSS rgb() three-valued or four-valued vector.</returns>
		public string ToRgbString()
			=> A < 255 ? $"rgba({R},{G},{B},{A * (1.0 / 255)})" : $"rgb({R},{G},{B})";

		/// <summary>
		/// Convert this to a string of the form (R,G,B,A), omitting the 'A' if it's 255.
		/// </summary>
		/// <returns>A string form of this color, as a three-valued or four-valued vector.</returns>
		public string ToVectorString()
			=> A < 255 ? $"({R},{G},{B},{A})" : $"({R},{G},{B})";

		/// <summary>
		/// Convert this to a string of the form #RRGGBBAA, omitting the AA if 'A' is 255.
		/// </summary>
		/// <returns>This color converted to a hex string.</returns>
		public string ToHexString()
			=> A < 255 ? Hex8 : Hex6;

		#endregion

		#region Color mixing

		/// <summary>
		/// The same color, but with the red channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 MaxR() => new Color32((byte)255, G, B, A);

		/// <summary>
		/// The same color, but with the green channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 MaxG() => new Color32(R, (byte)255, B, A);

		/// <summary>
		/// The same color, but with the blue channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 MaxB() => new Color32(R, G, (byte)255, A);

		/// <summary>
		/// The same color, but with the alpha channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 MaxA() => new Color32(R, G, A, (byte)255);

		/// <summary>
		/// The same color, but with the alpha channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 Opaque() => new Color32(R, G, B, (byte)255);

		/// <summary>
		/// The same color, but with the red channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 ZeroR() => new Color32((byte)0, G, B, A);

		/// <summary>
		/// The same color, but with the green channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 ZeroG() => new Color32(R, (byte)0, B, A);

		/// <summary>
		/// The same color, but with the blue channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 ZeroB() => new Color32(R, G, (byte)0, A);

		/// <summary>
		/// The same color, but with the alpha channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color32 ZeroA() => new Color32(R, G, B, (byte)0);

		/// <summary>
		/// Combine two colors, with equal amounts of each.
		/// </summary>
		/// <param name="other">The other color to merge with this color.</param>
		/// <returns>The new, fused color.</returns>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32 Merge(Color32 other)
			=> new Color32(
				(byte)((R + other.R) >> 1),
				(byte)((G + other.G) >> 1),
				(byte)((B + other.B) >> 1),
				(byte)((A + other.A) >> 1)
			);

		/// <summary>
		/// Perform linear interpolation between this and another color.  'amount'
		/// describes how much of the other color is included in the result, on a
		/// scale of [0.0, 1.0].
		/// </summary>
		/// <param name="other">The other color to mix with this color.</param>
		/// <param name="amount">How much of the other color to mix with this color,
		/// on a range of 0 (entirely this color) to 1 (entirely the other color).
		/// Exactly 0.5 is equivalent to calling the Merge() method instead.</param>
		/// <remarks>Merge() runs faster if you need exactly 50% of each color.</remarks>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public Color32 Mix(Color32 other, double amount)
		{
			int am = (int)(Math.Min(Math.Max(amount, 0), 1) * 65536);
			int ia = 65536 - am;

			byte r = (byte)((R * ia + other.R * am + 32768) >> 16);
			byte g = (byte)((G * ia + other.G * am + 32768) >> 16);
			byte b = (byte)((B * ia + other.B * am + 32768) >> 16);
			byte a = (byte)((A * ia + other.A * am + 32768) >> 16);

			return new Color32(r, g, b, a);
		}

		/// <summary>
		/// Transform a color that uses non-premultipled alpha to one that contains premultiplied alpha.
		/// Note that this is a lossy transformation:  Unpremultiply(Premultiply(x)) will result in a
		/// *similar* color, but not the *same* color as the original.
		/// </summary>
		/// <param name="r">The red value, which should be from 0 to 255.</param>
		/// <param name="g">The green value, which should be from 0 to 255.</param>
		/// <param name="b">The blue value, which should be from 0 to 255.</param>
		/// <param name="a">The alpha value, which should be from 0 to 255.</param>
		public static Color32 Premultiply(int r, int g, int b, int a)
			=> a >= 255 ? new Color32(r, g, b, 255)
				: new Color32(Div255(r * a), Div255(g * a), Div255(b * a), a);

		/// <summary>
		/// Transform a color that uses premultiplied color components into one that contains
		/// non-premultiplied color components.  Note that premultiplication is a lossy transformation:
		/// Unpremultiply(Premultiply(x)) will result in a *similar* color, but not the *same* color
		/// as the original.
		/// </summary>
		/// <param name="r">The red value, which should be from 0 to 255.</param>
		/// <param name="g">The green value, which should be from 0 to 255.</param>
		/// <param name="b">The blue value, which should be from 0 to 255.</param>
		/// <param name="a">The alpha value, which should be from 0 to 255.</param>
		/// <returns>The un-premultiplied color.</returns>
		public static Color32 Unpremultiply(int r, int g, int b, int a)
		{
			if (a == 255) return new Color32(r, g, b);
			if (a == 0) return new Color32((byte)0, (byte)0, (byte)0, (byte)0);

			long ooa = (255 * 0x100000000L) / a;
			return new Color32((int)((r * ooa) >> 32), (int)((g * ooa) >> 32), (int)((b * ooa) >> 32), a);
		}

		/// <summary>
		/// Transform a color that uses non-premultipled alpha to one that contains premultiplied alpha.
		/// Note that this is a lossy transformation:  Unpremultiply(Premultiply(x)) will result in a
		/// *similar* color, but not the *same* color as the original.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32 Premultiply()
			=> Premultiply(R, G, B, A);

		/// <summary>
		/// Transform a color that uses premultiplied color components into one that contains
		/// non-premultiplied color components.  Note that premultiplication is a lossy transformation:
		/// Unpremultiply(Premultiply(x)) will result in a *similar* color, but not the *same* color
		/// as the original.
		/// </summary>
		/// <returns>The un-premultiplied color.</returns>
		public Color32 Unpremultiply()
			=> Unpremultiply(R, G, B, A);

		/// <summary>
		/// Multiply each channel of this color by the same channel of the other color,
		/// then divide by 255 (with proper rounding).
		/// </summary>
		/// <param name="other">The other color to scale this color.</param>
		/// <returns>The new, scaled color.</returns>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color32 Scale(Color32 other)
			=> new Color32(
				(byte)(Div255(R * other.R + 128)),
				(byte)(Div255(G * other.G + 128)),
				(byte)(Div255(B * other.B + 128)),
				(byte)(Div255(A * other.A + 128))
			);

		/// <summary>
		/// Divide the given value by 255, faster than '/' can divide on most CPUs.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		internal static int Div255(int x)
			=> (x + 1 + (x >> 8)) >> 8;

		#endregion

		#region Over operation

		/// <summary>
		/// Calculate layering of one color over another color.
		/// </summary>
		/// <param name="over">The color that is on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static Color32 Over(Color32 over, Color32 under)
		{
			int a = over.A;

			if (a == 255)
				return over;

			if (a == 0)
				return under;

			// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
			// we do the math for real, but exclusively using integers (for performance).

			int ia = 255 - a;
			int r = over.R * a + under.R * ia + 127;
			int g = over.G * a + under.G * ia + 127;
			int b = over.B * a + under.B * ia + 127;
			a = under.A * 255 + a * 255 - under.A * a + 127;

			return new Color32((byte)Div255(r), (byte)Div255(g), (byte)Div255(b), (byte)Div255(a));
		}

		/// <summary>
		/// Calculate layering of one color over another color.  This version applies
		/// the alpha of the 'over' color, but ignores the alpha of the 'under' color,
		/// always resulting in an alpha of 255.  This can avoid multiplications in
		/// some high-performance scenarios where the composited alpha isn't needed.
		/// </summary>
		/// <param name="over">The color that is on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static Color32 OverOpaque(Color32 over, Color32 under)
		{
			int a = over.A;

			if (a == 255)
				return over;

			if (a == 0)
				return new Color32(under.R, under.G, under.B, (byte)255);

			// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
			// we do the math for real, but exclusively using integers (for performance).

			int ia = 255 - a;
			int r = over.R * a + under.R * ia + 127;
			int g = over.G * a + under.G * ia + 127;
			int b = over.B * a + under.B * ia + 127;

			return new Color32((byte)Div255(r), (byte)Div255(g), (byte)Div255(b), (byte)255);
		}

		/// <summary>
		/// Calculate layering of one color over another color.  Both colors
		/// will be treated as though their alphas have been premultiplied.
		/// </summary>
		/// <param name="over">The color that is on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static Color32 OverPM(Color32 over, Color32 under)
		{
			int a = over.A;

			if (a == 255)
				return over;

			if (a == 0)
				return under;

			// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
			// we do the math for real, but exclusively using integers (for performance).

			int ia = 255 - a;
			int r = over.R + under.R * ia + 127;
			int g = over.G + under.G * ia + 127;
			int b = over.B + under.B * ia + 127;
			a = under.A * 255 + a * 255 - under.A * a + 127;

			return new Color32((byte)Div255(r), (byte)Div255(g), (byte)Div255(b), (byte)Div255(a));
		}

		/// <summary>
		/// Calculate layering of one color over another color.  Both colors
		/// will be treated as though their alphas have been premultiplied.  This version applies
		/// the alpha of the 'over' color, but ignores the alpha of the 'under' color,
		/// always resulting in an alpha of 255.  This can avoid multiplications in
		/// some high-performance scenarios where the composited alpha isn't needed.
		/// </summary>
		/// <param name="over">The color that is on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static Color32 OverOpaquePM(Color32 over, Color32 under)
		{
			int a = over.A;

			if (a == 255)
				return over;

			if (a == 0)
				return new Color32(under.R, under.G, under.B, (byte)255);

			// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
			// we do the math for real, but exclusively using integers (for performance).

			int ia = 255 - a;
			int r = over.R + under.R * ia + 127;
			int g = over.G + under.G * ia + 127;
			int b = over.B + under.B * ia + 127;

			return new Color32((byte)Div255(r), (byte)Div255(g), (byte)Div255(b), (byte)255);
		}

		/// <summary>
		/// Calculate layering of black over another color, with the black at the given
		/// alpha level.  This is like calling Over(Black, under) with the alpha value
		/// modified, but is optimized for the special case where the color is black.
		/// </summary>
		/// <param name="a">The alpha value of the black color on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static Color32 BlackOver(int a, Color32 under)
		{
			if (a >= 255)
				return Black;

			if (a <= 0)
				return under;

			// We only apply alpha math if this pixel isn't 100% transparent.  Otherwise,
			// we do the math for real, but exclusively using integers (for performance).

			int ia = 255 - a;
			int r = under.R * ia + 127;
			int g = under.G * ia + 127;
			int b = under.B * ia + 127;
			a = under.A * 255 + a * 255 - under.A * a + 127;

			return new Color32((byte)Div255(r), (byte)Div255(g), (byte)Div255(b), (byte)Div255(a));
		}

		/// <summary>
		/// Calculate layering of black over another color, with the black at the given
		/// alpha level.  This is exactly the same as calling BlackOver(a, under).
		/// </summary>
		/// <param name="a">The alpha value of the white color on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 BlackOverPM(int a, Color32 under)
			=> BlackOver(a, under);

		/// <summary>
		/// Calculate layering of white over another color, with the white at the given
		/// alpha level.  This is the same as calling Over(White, under), with the alpha
		/// value modified, but is a convenient shorthand.
		/// </summary>
		/// <param name="a">The alpha value of the white color on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 WhiteOver(int a, Color32 under)
		{
			byte ba = (byte)Math.Min(Math.Max(a, 0), 255);
			return Over(new Color32((byte)255, (byte)255, (byte)255, ba), under);
		}

		/// <summary>
		/// Calculate layering of white over another color, with the white at the given
		/// alpha level.  This is the same as calling Over(White, under), with the alpha
		/// value modified, but is a convenient shorthand.
		/// </summary>
		/// <param name="a">The alpha value of the white color on top.</param>
		/// <param name="under">The color that is on the bottom.</param>
		/// <returns>The combined colors, with the alpha applied correctly.</returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 WhiteOverPM(int a, Color32 under)
		{
			byte ba = (byte)Math.Min(Math.Max(a, 0), 255);
			return OverPM(new Color32(ba, ba, ba, ba), under);
		}

		#endregion

		#region Deconstruction and tuple-conversion

		/// <summary>
		/// Convert this color to a tuple of floating-point values.
		/// </summary>
		/// <returns>A four-valued tuple of floating-point values in the range of [0, 1], in the
		/// form (r, g, b, a).</returns>
		public (float R, float G, float B, float A) ToFloats()
			=> (R * (1.0f / 255), G * (1.0f / 255), B * (1.0f / 255), A * (1.0f / 255));

		/// <summary>
		/// Convert this color to a tuple of double-precision floating-point values.
		/// </summary>
		/// <returns>A four-valued tuple of double-precision floating-point values
		/// in the range of [0, 1], in the form (r, g, b, a).</returns>
		public (double R, double G, double B, double A) ToDoubles()
			=> (R * (1.0 / 255), G * (1.0 / 255), B * (1.0 / 255), A * (1.0 / 255));

		/// <summary>
		/// Convert this to a tuple of integer values.
		/// </summary>
		/// <returns>A four-valued tuple of integer values in the range of [0, 255], in the
		/// form (r, g, b, a).</returns>
		public (int R, int G, int B, int A) ToInts()
			=> (R, G, B, A);

		/// <summary>
		/// Deconstruct the individual color components (a method to support modern
		/// C#'s deconstruction syntax).
		/// </summary>
		/// <param name="r">The resulting red value.</param>
		/// <param name="g">The resulting green value.</param>
		/// <param name="b">The resulting blue value.</param>
		public void Deconstruct(out byte r, out byte g, out byte b)
		{
			r = R;
			g = G;
			b = B;
		}

		/// <summary>
		/// Deconstruct the individual color components (a method to support modern
		/// C#'s deconstruction syntax).
		/// </summary>
		/// <param name="r">The resulting red value.</param>
		/// <param name="g">The resulting green value.</param>
		/// <param name="b">The resulting blue value.</param>
		/// <param name="a">The resulting alpha value.</param>
		public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
		{
			r = R;
			g = G;
			b = B;
			a = A;
		}

		#endregion

		#region Other color spaces

		/// <summary>
		/// Convert this color to a hue/saturation/brightness (HSB/HSV) model.
		/// HSB represents a color as hue (or family), saturation (degree of strength
		/// or purity, or distance from white) and brightness/value (distance from black).
		/// </summary>
		/// <returns>The HSB equivalent of this color.  Hue will be in the
		/// range of [0, 360), and saturation and brightness will be in the range of
		/// [0, 1].</returns>
		/// <remarks>This uses the efficient lolengine.net algorithm.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public (float Hue, float Sat, float Brt) ToHsv()
			=> ToHsb();

		/// <summary>
		/// Convert this color to a hue/saturation/brightness (HSB/HSV) model.
		/// HSB represents a color as hue (or family), saturation (degree of strength
		/// or purity, or distance from white) and brightness/value (distance from black).
		/// </summary>
		/// <returns>The HSB equivalent of this color.  Hue will be in the
		/// range of [0, 360), and saturation and brightness will be in the range of
		/// [0, 1].</returns>
		public (float Hue, float Sat, float Brt) ToHsb()
		{
			float r = R * (1f / 255);
			float g = G * (1f / 255);
			float b = B * (1f / 255);

			float min = Math.Min(Math.Min(r, g), b);
			float max = Math.Max(Math.Max(r, g), b);
			float delta = max - min;

			float hue;
			if (delta <= 0)
				hue = 0;
			else if (r == max)
				hue = ((g - b) / delta) % 6;
			else if (g == max)
				hue = ((b - r) / delta) + 2;
			else
				hue = ((r - g) / delta) + 4;
			hue *= 60;
			if (hue < 0)
				hue += 360;

			float sat = max != 0 ? delta / max : 0;

			return (hue, sat, max);
		}

		/// <summary>
		/// Convert this color to a hue/saturation/lightness (HSL) model.
		/// HSB represents a color as hue (or family), saturation (degree of strength
		/// or purity, or distance from gray) and lightness (distance between black and white).
		/// </summary>
		/// <returns>The HSL equivalent of this color.  Hue will be in the
		/// range of [0, 360), and saturation and lightness will be in the range of
		/// [0, 1].</returns>
		public (float Hue, float Sat, float Lit) ToHsl()
		{
			float r = R * (1f / 255);
			float g = G * (1f / 255);
			float b = B * (1f / 255);

			float min = Math.Min(Math.Min(r, g), b);
			float max = Math.Max(Math.Max(r, g), b);
			float delta = max - min;

			float hue;
			if (delta <= 0)
				hue = 0;
			else if (r == max)
				hue = ((g - b) / delta) % 6;
			else if (g == max)
				hue = ((b - r) / delta) + 2;
			else
				hue = ((r - g) / delta) + 4;
			hue *= 60;
			if (hue < 0)
				hue += 360;

			float lit = (max + min) * 0.5f;
			float sat = delta != 0 ? delta / (1 - MathF.Abs(2 * lit - 1)) : 0;

			return (hue, sat, lit);
		}

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB) model to
		/// a displayable RGB color.  HSB represents a color as hue (or family) in the
		/// range of 0 to 360, saturation (degree of strength or purity, or distance
		/// from gray) and brightness/value (distance from black).
		/// </summary>
		/// <param name="color">The HSB color to convert to RGB.  Hue must be in the
		/// range of [0, 360), and saturation and brightness must be in the range of
		/// [0, 1].</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color32 FromHsv((float Hue, float Sat, float Brt) color)
			=> FromHsb(color.Hue, color.Sat, color.Brt);

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB) model to
		/// a displayable RGB color.  HSB represents a color as hue (or family) in the
		/// range of 0 to 360, saturation (degree of strength or purity, or distance
		/// from gray) and brightness/value (distance from black).
		/// </summary>
		/// <param name="color">The HSB color to convert to RGB.  Hue must be in the
		/// range of [0, 360), and saturation and brightness must be in the range of
		/// [0, 1].</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color32 FromHsb((float Hue, float Sat, float Brt) color)
			=> FromHsb(color.Hue, color.Sat, color.Brt);

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB/HSV) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="brt">The brightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color32 FromHsv(float hue, float sat, float brt)
			=> FromHsb(hue, sat, brt);

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB/HSV) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="brt">The brightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color32 FromHsb(float hue, float sat, float brt)
		{
			float s = Math.Min(Math.Max(sat, 0), 1);
			float v = Math.Min(Math.Max(brt, 0), 1);

			float h = (float)hue % 360.0f;
			if (h < 0)
				h += 360.0f;

			h /= 60;

			float c = v * s;
			float x = c * (1 - MathF.Abs(h % 2 - 1));
			float m = v - c;

			int hexant = (int)h;
			float r, g, b;
			switch (hexant)
			{
				default:
				case 0: r = c; g = x; b = 0; break;
				case 1: r = x; g = c; b = 0; break;
				case 2: r = 0; g = c; b = x; break;
				case 3: r = 0; g = x; b = c; break;
				case 4: r = x; g = 0; b = c; break;
				case 5: r = c; g = 0; b = x; break;
			}

			return new Color32(r + m, g + m, b + m);
		}

		/// <summary>
		/// Convert the given color from the hue/saturation/lightness (HSB/HSL) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="lit">The lightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color32 FromHsl(float hue, float sat, float lit)
		{
			float s = Math.Min(Math.Max(sat, 0), 1);
			float l = Math.Min(Math.Max(lit, 0), 1);

			float h = (float)hue % 360.0f;
			if (h < 0)
				h += 360.0f;

			h /= 60;

			float c = (1 - MathF.Abs(2 * l - 1)) * s;
			float x = c * (1 - MathF.Abs(h % 2 - 1));
			float m = l - c * 0.5f;

			int hexant = (int)h;
			float r, g, b;
			switch (hexant)
			{
				default:
				case 0: r = c; g = x; b = 0; break;
				case 1: r = x; g = c; b = 0; break;
				case 2: r = 0; g = c; b = x; break;
				case 3: r = 0; g = x; b = c; break;
				case 4: r = x; g = 0; b = c; break;
				case 5: r = c; g = 0; b = x; break;
			}

			return new Color32(r + m, g + m, b + m);
		}

		/// <summary>
		/// Apply gamma correction by raising each color component to the given power.
		/// </summary>
		/// <param name="gamma">The power to raise by.</param>
		/// <returns>The gamma-corrected color.</returns>
		public Color32 Gamma(double gamma)
			=> new Color32(Math.Pow(Rd, gamma),
				Math.Pow(Gd, gamma),
				Math.Pow(Bd, gamma),
				Ad);

		#endregion

		#region Color "arithmetic" operators

		/// <summary>
		/// Perform componentwise addition on the R, G, B, and A values of the given colors.
		/// </summary>
		/// <param name="x">The first color to add.</param>
		/// <param name="y">The second color to add.</param>
		/// <returns>The "sum" of those colors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator +(Color32 x, Color32 y)
			=> new Color32(x.R + y.R, x.G + y.G, x.B + y.B, x.A + y.A);

		/// <summary>
		/// Perform componentwise subtraction on the R, G, B, and A values of the given colors.
		/// </summary>
		/// <param name="x">The source color.</param>
		/// <param name="y">The color to subtract from the source.</param>
		/// <returns>The "difference" of those colors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator -(Color32 x, Color32 y)
			=> new Color32(x.R - y.R, x.G - y.G, x.B - y.B, x.A - y.A);

		/// <summary>
		/// Calculate the "inverse" of the given color, replacing each value V
		/// with (256 - V) % 256.  Note that the result of this operation is
		/// usually only meaningful for non-premultiplied colors.
		/// </summary>
		/// <param name="c">The original color.</param>
		/// <returns>The "inverse" of that color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator -(Color32 c)
			=> new Color32((byte)(-c.R), (byte)(-c.G), (byte)(-c.B), c.A);

		/// <summary>
		/// Calculate the "inverse" of the given color, replacing each value V
		/// with 255 - V.  The alpha will be left unchanged.  Note that the result
		/// of this operation is usually only meaningful for non-premultiplied colors.
		/// </summary>
		/// <param name="c">The original color.</param>
		/// <returns>The "inverse" of that color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator ~(Color32 c)
			=> new Color32((byte)~c.R, (byte)~c.G, (byte)~c.B, c.A);

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors, treating each as though it is a fractional value in the range of [0, 1].
		/// </summary>
		/// <param name="x">The first color to multiply.</param>
		/// <param name="y">The second color to multiply.</param>
		/// <returns>The "product" of those colors.</returns>
		public static Color32 operator *(Color32 x, Color32 y)
			=> new Color32(Div255(x.R * y.R), Div255(x.G * y.G), Div255(x.B * y.B), Div255(x.A * y.A));

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color32 operator *(Color32 c, int scalar)
			=> new Color32(c.R * scalar, c.G * scalar, c.B * scalar, c.A);

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color32 operator *(Color32 c, double scalar)
		{
			double s = scalar * (1.0 / 255);
			return new Color32((int)(c.R * s + 0.5), (int)(c.G * s + 0.5), (int)(c.B * s + 0.5), c.A);
		}

		/// <summary>
		/// Perform componentwise division on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color32 operator /(Color32 c, int scalar)
			=> new Color32(c.R / scalar, c.G / scalar, c.B / scalar, c.A);

		/// <summary>
		/// Perform componentwise division on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color32 operator /(Color32 c, double scalar)
		{
			double s = 1.0 / (scalar * 255);
			return new Color32((int)(c.R * s + 0.5), (int)(c.G * s + 0.5), (int)(c.B * s + 0.5), c.A);
		}

		/// <summary>
		/// Calculate the bitwise-or of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are or'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator |(Color32 a, Color32 b)
			=> new Color32((byte)(a.R | b.R), (byte)(a.G | b.G), (byte)(a.B | b.B), (byte)(a.A | b.A));

		/// <summary>
		/// Calculate the bitwise-and of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are and'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator &(Color32 a, Color32 b)
			=> new Color32((byte)(a.R & b.R), (byte)(a.G & b.G), (byte)(a.B & b.B), (byte)(a.A & b.A));

		/// <summary>
		/// Calculate the bitwise-xor of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are xor'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator ^(Color32 a, Color32 b)
			=> new Color32((byte)(a.R ^ b.R), (byte)(a.G ^ b.G), (byte)(a.B ^ b.B), (byte)(a.A ^ b.A));

		/// <summary>
		/// Shift each integer color component logically to the right.  This does not
		/// affect the alpha channel.
		/// </summary>
		/// <param name="c">The color to shift.</param>
		/// <param name="amount">The number of bits to shift each component by.</param>
		/// <returns>A color where each channel has been shifted right by the given amount.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator >>(Color32 c, int amount)
			=> new Color32((byte)(c.R >> amount), (byte)(c.G >> amount), (byte)(c.B >> amount), c.A);

		/// <summary>
		/// Shift each integer color component logically to the left.  This does not
		/// affect the alpha channel.
		/// </summary>
		/// <param name="c">The color to shift.</param>
		/// <param name="amount">The number of bits to shift each component by.</param>
		/// <returns>A color where each channel has been shifted right by the given amount.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 operator <<(Color32 c, int amount)
			=> new Color32((byte)(c.R << amount), (byte)(c.G << amount), (byte)(c.B << amount), c.A);

		/// <summary>
		/// Calculate the distance between this color and another color in RGB-space.
		/// </summary>
		/// <param name="other">The other color to measure the distance to.</param>
		/// <returns>The distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(Color32 other)
			=> Math.Sqrt(DistanceSquared(other));

		/// <summary>
		/// Calculate the square of the distance between this color and another color in RGB-space.
		/// </summary>
		/// <param name="other">The other color to measure the square of the distance to.</param>
		/// <returns>The distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DistanceSquared(Color32 other)
		{
			int r2 = (R - other.R) * (R - other.R);
			int g2 = (G - other.G) * (G - other.G);
			int b2 = (B - other.B) * (B - other.B);
			return r2 + g2 + b2;
		}

		/// <summary>
		/// Calculate the square of the weighted distance between this color and another
		/// color in RGB-space.  Weighting the values produces distances that take into
		/// account perceptual differences between the color changes.
		/// </summary>
		/// <param name="other">The other color to measure the square of the distance to.</param>
		/// <param name="rWeight">The weight to use for the red distance (0.299 by default).</param>
		/// <param name="gWeight">The weight to use for the green distance (0.587 by default).</param>
		/// <param name="bWeight">The weight to use for the blue distance (0.114 by default).</param>
		/// <returns>The distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double WeightedDistance(Color32 other,
			double rWeight = ApparentRedBrightness,
			double gWeight = ApparentGreenBrightness,
			double bWeight = ApparentBlueBrightness)
			=> Math.Sqrt(WeightedDistanceSquared(other, rWeight, gWeight, bWeight));

		/// <summary>
		/// Calculate the square of the weighted distance between this color and another
		/// color in RGB-space.  Weighting the values produces distances that take into
		/// account perceptual differences between the color changes.
		/// </summary>
		/// <param name="other">The other color to measure the square of the distance to.</param>
		/// <param name="rWeight">The weight to use for the red distance (0.299 by default).</param>
		/// <param name="gWeight">The weight to use for the green distance (0.587 by default).</param>
		/// <param name="bWeight">The weight to use for the blue distance (0.114 by default).</param>
		/// <returns>The square of the distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double WeightedDistanceSquared(Color32 other,
			double rWeight = ApparentRedBrightness,
			double gWeight = ApparentGreenBrightness,
			double bWeight = ApparentBlueBrightness)
		{
			double r2 = (R - other.R) * rWeight;
			double g2 = (G - other.G) * gWeight;
			double b2 = (B - other.B) * bWeight;
			return (r2 * r2) + (g2 * g2) + (b2 * b2);
		}

		#endregion

		#region Static colors (CSS colors)

#pragma warning disable 1591

		public static readonly Color32 Transparent = new Color32((byte)0, (byte)0, (byte)0, (byte)0);

		public static readonly Color32 AntiqueWhite = new Color32((byte)250, (byte)235, (byte)215, (byte)255);
		public static readonly Color32 Aqua = new Color32((byte)0, (byte)255, (byte)255, (byte)255);
		public static readonly Color32 Aquamarine = new Color32((byte)127, (byte)255, (byte)212, (byte)255);
		public static readonly Color32 Azure = new Color32((byte)240, (byte)255, (byte)255, (byte)255);
		public static readonly Color32 Beige = new Color32((byte)245, (byte)245, (byte)220, (byte)255);
		public static readonly Color32 Bisque = new Color32((byte)255, (byte)228, (byte)196, (byte)255);
		public static readonly Color32 Black = new Color32((byte)0, (byte)0, (byte)0, (byte)255);
		public static readonly Color32 BlanchedAlmond = new Color32((byte)255, (byte)235, (byte)205, (byte)255);
		public static readonly Color32 Blue = new Color32((byte)0, (byte)0, (byte)255, (byte)255);
		public static readonly Color32 BlueViolet = new Color32((byte)138, (byte)43, (byte)226, (byte)255);
		public static readonly Color32 Brown = new Color32((byte)165, (byte)42, (byte)42, (byte)255);
		public static readonly Color32 Burlywood = new Color32((byte)222, (byte)184, (byte)135, (byte)255);
		public static readonly Color32 CadetBlue = new Color32((byte)95, (byte)158, (byte)160, (byte)255);
		public static readonly Color32 Chartreuse = new Color32((byte)127, (byte)255, (byte)0, (byte)255);
		public static readonly Color32 Chocolate = new Color32((byte)210, (byte)105, (byte)30, (byte)255);
		public static readonly Color32 Coral = new Color32((byte)255, (byte)127, (byte)80, (byte)255);
		public static readonly Color32 CornflowerBlue = new Color32((byte)100, (byte)149, (byte)237, (byte)255);
		public static readonly Color32 Cornsilk = new Color32((byte)255, (byte)248, (byte)220, (byte)255);
		public static readonly Color32 Crimson = new Color32((byte)220, (byte)20, (byte)60, (byte)255);
		public static readonly Color32 Cyan = new Color32((byte)0, (byte)255, (byte)255, (byte)255);
		public static readonly Color32 DarkBlue = new Color32((byte)0, (byte)0, (byte)139, (byte)255);
		public static readonly Color32 DarkCyan = new Color32((byte)0, (byte)139, (byte)139, (byte)255);
		public static readonly Color32 DarkGoldenrod = new Color32((byte)184, (byte)134, (byte)11, (byte)255);
		public static readonly Color32 DarkGray = new Color32((byte)169, (byte)169, (byte)169, (byte)255);
		public static readonly Color32 DarkGreen = new Color32((byte)0, (byte)100, (byte)0, (byte)255);
		public static readonly Color32 DarkGrey = new Color32((byte)169, (byte)169, (byte)169, (byte)255);
		public static readonly Color32 DarkKhaki = new Color32((byte)189, (byte)183, (byte)107, (byte)255);
		public static readonly Color32 DarkMagenta = new Color32((byte)139, (byte)0, (byte)139, (byte)255);
		public static readonly Color32 DarkOliveGreen = new Color32((byte)85, (byte)107, (byte)47, (byte)255);
		public static readonly Color32 DarkOrange = new Color32((byte)255, (byte)140, (byte)0, (byte)255);
		public static readonly Color32 DarkOrchid = new Color32((byte)153, (byte)50, (byte)204, (byte)255);
		public static readonly Color32 DarkRed = new Color32((byte)139, (byte)0, (byte)0, (byte)255);
		public static readonly Color32 DarkSalmon = new Color32((byte)233, (byte)150, (byte)122, (byte)255);
		public static readonly Color32 DarkSeaGreen = new Color32((byte)143, (byte)188, (byte)143, (byte)255);
		public static readonly Color32 DarkSlateBlue = new Color32((byte)72, (byte)61, (byte)139, (byte)255);
		public static readonly Color32 DarkSlateGray = new Color32((byte)47, (byte)79, (byte)79, (byte)255);
		public static readonly Color32 DarkSlateGrey = new Color32((byte)47, (byte)79, (byte)79, (byte)255);
		public static readonly Color32 DarkTurquoise = new Color32((byte)0, (byte)206, (byte)209, (byte)255);
		public static readonly Color32 DarkViolet = new Color32((byte)148, (byte)0, (byte)211, (byte)255);
		public static readonly Color32 DeepPink = new Color32((byte)255, (byte)20, (byte)147, (byte)255);
		public static readonly Color32 DeepSkyBlue = new Color32((byte)0, (byte)191, (byte)255, (byte)255);
		public static readonly Color32 DimGray = new Color32((byte)105, (byte)105, (byte)105, (byte)255);
		public static readonly Color32 DimGrey = new Color32((byte)105, (byte)105, (byte)105, (byte)255);
		public static readonly Color32 DodgerBlue = new Color32((byte)30, (byte)144, (byte)255, (byte)255);
		public static readonly Color32 FireBrick = new Color32((byte)178, (byte)34, (byte)34, (byte)255);
		public static readonly Color32 FloralWhite = new Color32((byte)255, (byte)250, (byte)240, (byte)255);
		public static readonly Color32 ForestGreen = new Color32((byte)34, (byte)139, (byte)34, (byte)255);
		public static readonly Color32 Fuchsia = new Color32((byte)255, (byte)0, (byte)255, (byte)255);
		public static readonly Color32 Gainsboro = new Color32((byte)220, (byte)220, (byte)220, (byte)255);
		public static readonly Color32 GhostWhite = new Color32((byte)248, (byte)248, (byte)255, (byte)255);
		public static readonly Color32 Gold = new Color32((byte)255, (byte)215, (byte)0, (byte)255);
		public static readonly Color32 Goldenrod = new Color32((byte)218, (byte)165, (byte)32, (byte)255);
		public static readonly Color32 Gray = new Color32((byte)128, (byte)128, (byte)128, (byte)255);
		public static readonly Color32 Green = new Color32((byte)0, (byte)128, (byte)0, (byte)255);
		public static readonly Color32 GreenYellow = new Color32((byte)173, (byte)255, (byte)47, (byte)255);
		public static readonly Color32 Grey = new Color32((byte)128, (byte)128, (byte)128, (byte)255);
		public static readonly Color32 Honeydew = new Color32((byte)240, (byte)255, (byte)240, (byte)255);
		public static readonly Color32 HotPink = new Color32((byte)255, (byte)105, (byte)180, (byte)255);
		public static readonly Color32 IndianRed = new Color32((byte)205, (byte)92, (byte)92, (byte)255);
		public static readonly Color32 Indigo = new Color32((byte)75, (byte)0, (byte)130, (byte)255);
		public static readonly Color32 Ivory = new Color32((byte)255, (byte)255, (byte)240, (byte)255);
		public static readonly Color32 Khaki = new Color32((byte)240, (byte)230, (byte)140, (byte)255);
		public static readonly Color32 Lavender = new Color32((byte)230, (byte)230, (byte)250, (byte)255);
		public static readonly Color32 LavenderBlush = new Color32((byte)255, (byte)240, (byte)245, (byte)255);
		public static readonly Color32 LawnGreen = new Color32((byte)124, (byte)252, (byte)0, (byte)255);
		public static readonly Color32 LemonChiffon = new Color32((byte)255, (byte)250, (byte)205, (byte)255);
		public static readonly Color32 LightBlue = new Color32((byte)173, (byte)216, (byte)230, (byte)255);
		public static readonly Color32 LightCoral = new Color32((byte)240, (byte)128, (byte)128, (byte)255);
		public static readonly Color32 LightCyan = new Color32((byte)224, (byte)255, (byte)255, (byte)255);
		public static readonly Color32 LightGoldenrodYellow = new Color32((byte)250, (byte)250, (byte)210, (byte)255);
		public static readonly Color32 LightGray = new Color32((byte)211, (byte)211, (byte)211, (byte)255);
		public static readonly Color32 LightGreen = new Color32((byte)144, (byte)238, (byte)144, (byte)255);
		public static readonly Color32 LightGrey = new Color32((byte)211, (byte)211, (byte)211, (byte)255);
		public static readonly Color32 LightPink = new Color32((byte)255, (byte)182, (byte)193, (byte)255);
		public static readonly Color32 LightSalmon = new Color32((byte)255, (byte)160, (byte)122, (byte)255);
		public static readonly Color32 LightSeaGreen = new Color32((byte)32, (byte)178, (byte)170, (byte)255);
		public static readonly Color32 LightSkyBlue = new Color32((byte)135, (byte)206, (byte)250, (byte)255);
		public static readonly Color32 LightSlateGray = new Color32((byte)119, (byte)136, (byte)153, (byte)255);
		public static readonly Color32 LightSlateGrey = new Color32((byte)119, (byte)136, (byte)153, (byte)255);
		public static readonly Color32 LightSteelBlue = new Color32((byte)176, (byte)196, (byte)222, (byte)255);
		public static readonly Color32 LightYellow = new Color32((byte)255, (byte)255, (byte)224, (byte)255);
		public static readonly Color32 Lime = new Color32((byte)0, (byte)255, (byte)0, (byte)255);
		public static readonly Color32 LimeGreen = new Color32((byte)50, (byte)205, (byte)50, (byte)255);
		public static readonly Color32 Linen = new Color32((byte)250, (byte)240, (byte)230, (byte)255);
		public static readonly Color32 Magenta = new Color32((byte)255, (byte)0, (byte)255, (byte)255);
		public static readonly Color32 Maroon = new Color32((byte)128, (byte)0, (byte)0, (byte)255);
		public static readonly Color32 MediumAquamarine = new Color32((byte)102, (byte)205, (byte)170, (byte)255);
		public static readonly Color32 MediumBlue = new Color32((byte)0, (byte)0, (byte)205, (byte)255);
		public static readonly Color32 MediumOrchid = new Color32((byte)186, (byte)85, (byte)211, (byte)255);
		public static readonly Color32 MediumPurple = new Color32((byte)147, (byte)112, (byte)219, (byte)255);
		public static readonly Color32 MediumSeaGreen = new Color32((byte)60, (byte)179, (byte)113, (byte)255);
		public static readonly Color32 MediumSlateBlue = new Color32((byte)123, (byte)104, (byte)238, (byte)255);
		public static readonly Color32 MediumSpringGreen = new Color32((byte)0, (byte)250, (byte)154, (byte)255);
		public static readonly Color32 MediumTurquoise = new Color32((byte)72, (byte)209, (byte)204, (byte)255);
		public static readonly Color32 MediumVioletRed = new Color32((byte)199, (byte)21, (byte)133, (byte)255);
		public static readonly Color32 MidnightBlue = new Color32((byte)25, (byte)25, (byte)112, (byte)255);
		public static readonly Color32 MintCream = new Color32((byte)245, (byte)255, (byte)250, (byte)255);
		public static readonly Color32 MistyRose = new Color32((byte)255, (byte)228, (byte)225, (byte)255);
		public static readonly Color32 Moccasin = new Color32((byte)255, (byte)228, (byte)181, (byte)255);
		public static readonly Color32 NavajoWhite = new Color32((byte)255, (byte)222, (byte)173, (byte)255);
		public static readonly Color32 Navy = new Color32((byte)0, (byte)0, (byte)128, (byte)255);
		public static readonly Color32 OldLace = new Color32((byte)253, (byte)245, (byte)230, (byte)255);
		public static readonly Color32 Olive = new Color32((byte)128, (byte)128, (byte)0, (byte)255);
		public static readonly Color32 OliveDrab = new Color32((byte)107, (byte)142, (byte)35, (byte)255);
		public static readonly Color32 Orange = new Color32((byte)255, (byte)165, (byte)0, (byte)255);
		public static readonly Color32 Orangered = new Color32((byte)255, (byte)69, (byte)0, (byte)255);
		public static readonly Color32 Orchid = new Color32((byte)218, (byte)112, (byte)214, (byte)255);
		public static readonly Color32 PaleGoldenrod = new Color32((byte)238, (byte)232, (byte)170, (byte)255);
		public static readonly Color32 PaleGreen = new Color32((byte)152, (byte)251, (byte)152, (byte)255);
		public static readonly Color32 PaleTurquoise = new Color32((byte)175, (byte)238, (byte)238, (byte)255);
		public static readonly Color32 PaleVioletRed = new Color32((byte)219, (byte)112, (byte)147, (byte)255);
		public static readonly Color32 PapayaWhip = new Color32((byte)255, (byte)239, (byte)213, (byte)255);
		public static readonly Color32 Peach = new Color32((byte)255, (byte)192, (byte)128, (byte)255);
		public static readonly Color32 PeachPuff = new Color32((byte)255, (byte)218, (byte)185, (byte)255);
		public static readonly Color32 Peru = new Color32((byte)205, (byte)133, (byte)63, (byte)255);
		public static readonly Color32 Pink = new Color32((byte)255, (byte)192, (byte)203, (byte)255);
		public static readonly Color32 Plum = new Color32((byte)221, (byte)160, (byte)221, (byte)255);
		public static readonly Color32 PowderBlue = new Color32((byte)176, (byte)224, (byte)230, (byte)255);
		public static readonly Color32 Purple = new Color32((byte)128, (byte)0, (byte)128, (byte)255);
		public static readonly Color32 RebeccaPurple = new Color32((byte)102, (byte)51, (byte)153, (byte)255);
		public static readonly Color32 Red = new Color32((byte)255, (byte)0, (byte)0, (byte)255);
		public static readonly Color32 RosyBrown = new Color32((byte)188, (byte)143, (byte)143, (byte)255);
		public static readonly Color32 RoyalBlue = new Color32((byte)65, (byte)105, (byte)225, (byte)255);
		public static readonly Color32 SaddleBrown = new Color32((byte)139, (byte)69, (byte)19, (byte)255);
		public static readonly Color32 Salmon = new Color32((byte)250, (byte)128, (byte)114, (byte)255);
		public static readonly Color32 SandyBrown = new Color32((byte)244, (byte)164, (byte)96, (byte)255);
		public static readonly Color32 SeaGreen = new Color32((byte)46, (byte)139, (byte)87, (byte)255);
		public static readonly Color32 Seashell = new Color32((byte)255, (byte)245, (byte)238, (byte)255);
		public static readonly Color32 Sienna = new Color32((byte)160, (byte)82, (byte)45, (byte)255);
		public static readonly Color32 Silver = new Color32((byte)192, (byte)192, (byte)192, (byte)255);
		public static readonly Color32 SkyBlue = new Color32((byte)135, (byte)206, (byte)235, (byte)255);
		public static readonly Color32 SlateBlue = new Color32((byte)106, (byte)90, (byte)205, (byte)255);
		public static readonly Color32 SlateGray = new Color32((byte)112, (byte)128, (byte)144, (byte)255);
		public static readonly Color32 Snow = new Color32((byte)255, (byte)250, (byte)250, (byte)255);
		public static readonly Color32 SpringGreen = new Color32((byte)0, (byte)255, (byte)127, (byte)255);
		public static readonly Color32 SteelBlue = new Color32((byte)70, (byte)130, (byte)180, (byte)255);
		public static readonly Color32 Tan = new Color32((byte)210, (byte)180, (byte)140, (byte)255);
		public static readonly Color32 Tea = new Color32((byte)0, (byte)128, (byte)128, (byte)255);
		public static readonly Color32 Thistle = new Color32((byte)216, (byte)191, (byte)216, (byte)255);
		public static readonly Color32 Tomato = new Color32((byte)255, (byte)99, (byte)71, (byte)255);
		public static readonly Color32 Turquoise = new Color32((byte)64, (byte)224, (byte)208, (byte)255);
		public static readonly Color32 Violet = new Color32((byte)238, (byte)130, (byte)238, (byte)255);
		public static readonly Color32 Wheat = new Color32((byte)245, (byte)222, (byte)179, (byte)255);
		public static readonly Color32 White = new Color32((byte)255, (byte)255, (byte)255, (byte)255);
		public static readonly Color32 WhiteSmoke = new Color32((byte)245, (byte)245, (byte)245, (byte)255);
		public static readonly Color32 Yellow = new Color32((byte)255, (byte)255, (byte)0, (byte)255);
		public static readonly Color32 YellowGreen = new Color32((byte)154, (byte)205, (byte)50, (byte)255);

#pragma warning restore 1591

		#endregion

		#region Color names

		private static readonly (string Name, Color32 Color)[] _colorList = new (string Name, Color32 Color)[]
		{
			("transparent", Transparent),

			("antiquewhite", AntiqueWhite),
			("aqua", Aqua),
			("aquamarine", Aquamarine),
			("azure", Azure),
			("beige", Beige),
			("bisque", Bisque),
			("black", Black),
			("blanchedalmond", BlanchedAlmond),
			("blue", Blue),
			("blueviolet", BlueViolet),
			("brown", Brown),
			("burlywood", Burlywood),
			("cadetblue", CadetBlue),
			("chartreuse", Chartreuse),
			("chocolate", Chocolate),
			("coral", Coral),
			("cornflowerblue", CornflowerBlue),
			("cornsilk", Cornsilk),
			("crimson", Crimson),
			("cyan", Cyan),
			("darkblue", DarkBlue),
			("darkcyan", DarkCyan),
			("darkgoldenrod", DarkGoldenrod),
			("darkgray", DarkGray),
			("darkgreen", DarkGreen),
			("darkgrey", DarkGrey),
			("darkkhaki", DarkKhaki),
			("darkmagenta", DarkMagenta),
			("darkolivegreen", DarkOliveGreen),
			("darkorange", DarkOrange),
			("darkorchid", DarkOrchid),
			("darkred", DarkRed),
			("darksalmon", DarkSalmon),
			("darkseagreen", DarkSeaGreen),
			("darkslateblue", DarkSlateBlue),
			("darkslategray", DarkSlateGray),
			("darkslategrey", DarkSlateGrey),
			("darkturquoise", DarkTurquoise),
			("darkviolet", DarkViolet),
			("deeppink", DeepPink),
			("deepskyblue", DeepSkyBlue),
			("dimgray", DimGray),
			("dimgrey", DimGrey),
			("dodgerblue", DodgerBlue),
			("firebrick", FireBrick),
			("floralwhite", FloralWhite),
			("forestgreen", ForestGreen),
			("fuchsia", Fuchsia),
			("gainsboro", Gainsboro),
			("ghostwhite", GhostWhite),
			("gold", Gold),
			("goldenrod", Goldenrod),
			("gray", Gray),
			("green", Green),
			("greenyellow", GreenYellow),
			("grey", Grey),
			("honeydew", Honeydew),
			("hotpink", HotPink),
			("indianred", IndianRed),
			("indigo", Indigo),
			("ivory", Ivory),
			("khaki", Khaki),
			("lavender", Lavender),
			("lavenderblush", LavenderBlush),
			("lawngreen", LawnGreen),
			("lemonchiffon", LemonChiffon),
			("lightblue", LightBlue),
			("lightcoral", LightCoral),
			("lightcyan", LightCyan),
			("lightgoldenrodyellow", LightGoldenrodYellow),
			("lightgray", LightGray),
			("lightgreen", LightGreen),
			("lightgrey", LightGrey),
			("lightpink", LightPink),
			("lightsalmon", LightSalmon),
			("lightseagreen", LightSeaGreen),
			("lightskyblue", LightSkyBlue),
			("lightslategray", LightSlateGray),
			("lightslategrey", LightSlateGrey),
			("lightsteelblue", LightSteelBlue),
			("lightyellow", LightYellow),
			("lime", Lime),
			("limegreen", LimeGreen),
			("linen", Linen),
			("magenta", Magenta),
			("maroon", Maroon),
			("mediumaquamarine", MediumAquamarine),
			("mediumblue", MediumBlue),
			("mediumorchid", MediumOrchid),
			("mediumpurple", MediumPurple),
			("mediumseagreen", MediumSeaGreen),
			("mediumslateblue", MediumSlateBlue),
			("mediumspringgreen", MediumSpringGreen),
			("mediumturquoise", MediumTurquoise),
			("mediumvioletred", MediumVioletRed),
			("midnightblue", MidnightBlue),
			("mintcream", MintCream),
			("mistyrose", MistyRose),
			("moccasin", Moccasin),
			("navajowhite", NavajoWhite),
			("navy", Navy),
			("oldlace", OldLace),
			("olive", Olive),
			("olivedrab", OliveDrab),
			("orange", Orange),
			("orangered", Orangered),
			("orchid", Orchid),
			("palegoldenrod", PaleGoldenrod),
			("palegreen", PaleGreen),
			("paleturquoise", PaleTurquoise),
			("palevioletred", PaleVioletRed),
			("papayawhip", PapayaWhip),
			("peach", Peach),
			("peachpuff", PeachPuff),
			("peru", Peru),
			("pink", Pink),
			("plum", Plum),
			("powderblue", PowderBlue),
			("purple", Purple),
			("rebeccapurple", RebeccaPurple),
			("red", Red),
			("rosybrown", RosyBrown),
			("royalblue", RoyalBlue),
			("saddlebrown", SaddleBrown),
			("salmon", Salmon),
			("sandybrown", SandyBrown),
			("seagreen", SeaGreen),
			("seashell", Seashell),
			("sienna", Sienna),
			("silver", Silver),
			("skyblue", SkyBlue),
			("slateblue", SlateBlue),
			("slategray", SlateGray),
			("snow", Snow),
			("springgreen", SpringGreen),
			("steelblue", SteelBlue),
			("tan", Tan),
			("tea", Tea),
			("thistle", Thistle),
			("tomato", Tomato),
			("turquoise", Turquoise),
			("violet", Violet),
			("wheat", Wheat),
			("white", White),
			("whitesmoke", WhiteSmoke),
			("yellow", Yellow),
			("yellowgreen", YellowGreen),
		};

		/// <summary>
		/// The list of colors above, turned into a dictionary keyed by name.
		/// </summary>
		private static IReadOnlyDictionary<string, Color32>? _colorsByName;

		/// <summary>
		/// The list of colors above, turned into a dictionary keyed by color.
		/// </summary>
		private static IReadOnlyDictionary<Color32, string>? _namesByColor;

		private static IReadOnlyDictionary<string, Color32> MakeColorsByName()
		{
			// Fanciness to project the color list to a dictionary, without taking a dependency on Linq.
			Dictionary<string, Color32> colorsByName = new Dictionary<string, Color32>();
			foreach ((string Name, Color32 Color) pair in _colorList)
			{
				colorsByName.Add(pair.Name, pair.Color);
			}
			return colorsByName;
		}

		private static IReadOnlyDictionary<Color32, string> MakeNamesByColor()
		{
			// Fanciness to project the color list to a dictionary, without taking a dependency on Linq.
			Dictionary<Color32, string> namesByColor = new Dictionary<Color32, string>();
			foreach ((string Name, Color32 Color) pair in _colorList)
			{
				if (!namesByColor.ContainsKey(pair.Color))
					namesByColor[pair.Color] = pair.Name;
			}
			return namesByColor;
		}

		/// <summary>
		/// Retrieve the full list of defined colors, in their definition order.
		/// </summary>
		public static IReadOnlyList<(string Name, Color32 Color)> ColorList => _colorList;

		/// <summary>
		/// A dictionary that maps color values to their matching names, in lowercase.
		/// </summary>
		public static IReadOnlyDictionary<Color32, string> NamesByColor => _namesByColor ??= MakeNamesByColor();

		/// <summary>
		/// A dictionary that maps color names to their matching color values.
		/// </summary>
		public static IReadOnlyDictionary<string, Color32> ColorsByName => _colorsByName ??= MakeColorsByName();

		#endregion

		#region Color parsing

		/// <summary>
		/// Parse a 32-bit RGBA color (8 bits per channel) in one of several commmon CSS-style formats:
		///    - "#RGB"
		///    - "#RGBA"
		///    - "#RRGGBB"
		///    - "#RRGGBBAA"
		///    - "rgb(123, 45, 67)"
		///    - "rgba(123, 45, 67, 0.5)"
		///    - "name"   (standard web color names)
		/// </summary>
		/// <param name="value">The color value to parse.</param>
		/// <returns>The resulting actual color.</returns>
		/// <exception cref="ArgumentException">Thrown if the color string cannot be parsed in one
		/// of the known formats.</exception>
		public static Color32 Parse(string value)
		{
			if (!TryParse(value, out Color32 color))
				throw new ArgumentException($"Invalid color value '{value}'.");
			return color;
		}

		/// <summary>
		/// Attempt to parse a 32-bit RGBA color (8 bits per channel) in one of several commmon CSS-style formats:
		///    - "#RGB"
		///    - "#RGBA"
		///    - "#RRGGBB"
		///    - "#RRGGBBAA"
		///    - "rgb(123, 45, 67)"
		///    - "rgba(123, 45, 67, 0.5)"
		///    - "name"   (standard web color names)
		///    
		/// (Browsers have supported the 4-digit and 8-digit hex forms since 2016,
		/// so we're in good company supporting it here.  Note that these are NOT the
		/// same as the 8-digit form that WPF/XAML uses, which is #AARRGGBB.  Microsoft
		/// may have done theirs first, but the world standard is #RRGGBBAA.)
		/// </summary>
		/// <param name="value">The color value to parse.</param>
		/// <param name="color">The resulting actual color; if the string cannot be parsed,
		/// this will be set to Color.Transparent.</param>
		/// <returns>True if the color could be parsed, false if it could not.</returns>
		public static bool TryParse(string value, out Color32 color)
		{
			ReadOnlySpan<char> input = value.AsSpan().Trim();

			if (input.Length >= 2 && input[0] == '#' && IsHexDigits(input.Slice(1)))
				return TryParseHexColor(input.Slice(1), out color);

			char ch;
			if (input.Length >= 4
				&& ((ch = input[0]) == 'r' || ch == 'R')
				&& ((ch = input[1]) == 'g' || ch == 'G')
				&& ((ch = input[2]) == 'b' || ch == 'B'))
			{
				if (!TryLexRgba(input.Slice(3), out color))
					return false;
				return true;
			}

			if (input.Length > 20)
			{
				// All of the color names are 20 characters or less.
				color = Transparent;
				return false;
			}

			Span<char> lowerName = stackalloc char[input.Length];
			for (int i = 0; i < input.Length; i++)
			{
				ch = input[i];
				if (ch >= 'A' && ch <= 'Z')
					ch = (char)(ch + 32);       // Faster than ToLowerInvariant() for ASCII.
				lowerName[i] = ch;
			}

			if (ColorsByName.TryGetValue(lowerName.ToString(), out Color32 c))
			{
				color = c;
				return true;
			}

			color = Transparent;
			return false;
		}

		/// <summary>
		/// Attempt to parse a 32-bit RGBA color (8 bits per channel) in one of several commmon CSS-style formats:
		///    - "RGB"
		///    - "RGBA"
		///    - "RRGGBB"
		///    - "RRGGBBAA"
		///    
		/// (Browsers have supported the 4-digit and 8-digit hex forms since 2016,
		/// so we're in good company supporting it here.  Note that these are NOT the
		/// same as the 8-digit form that WPF/XAML uses, which is #AARRGGBB.  Microsoft
		/// may have done theirs first, but the world standard is #RRGGBBAA.)
		/// </summary>
		/// <param name="hex">The color value to parse.</param>
		/// <param name="color">The resulting actual color; if the string cannot be parsed,
		/// this will be set to Color.Transparent.</param>
		/// <returns>True if the color could be parsed, false if it could not.</returns>
		public static bool TryParseHexColor(ReadOnlySpan<char> hex, out Color32 color)
		{
			switch (hex.Length)
			{
				case 3:
					int r = ParseHexDigit(hex[0]);
					int g = ParseHexDigit(hex[1]);
					int b = ParseHexDigit(hex[2]);
					r = (r << 4) | r;
					g = (g << 4) | g;
					b = (b << 4) | b;
					color = new Color32(r, g, b, 255);
					return true;

				case 4:
					r = ParseHexDigit(hex[0]);
					g = ParseHexDigit(hex[1]);
					b = ParseHexDigit(hex[2]);
					int a = ParseHexDigit(hex[3]);
					r = (r << 4) | r;
					g = (g << 4) | g;
					b = (b << 4) | b;
					a = (a << 4) | a;
					color = new Color32(r, g, b, a);
					return true;

				case 6:
					r = ParseHexPair(hex[0], hex[1]);
					g = ParseHexPair(hex[2], hex[3]);
					b = ParseHexPair(hex[4], hex[5]);
					color = new Color32(r, g, b, 255);
					return true;

				case 8:
					r = ParseHexPair(hex[0], hex[1]);
					g = ParseHexPair(hex[2], hex[3]);
					b = ParseHexPair(hex[4], hex[5]);
					a = ParseHexPair(hex[6], hex[7]);
					color = new Color32(r, g, b, a);
					return true;

				default:
					color = Transparent;
					return false;
			}
		}

		#endregion

		#region Internal number parsing

		private static readonly sbyte[] _hexDigitValues =
		{
			-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
			-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,

			-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
			-1, 1, 2, 3, 4, 5, 6, 7, 8, 9,-1,-1,-1,-1,-1,-1,

			-1,10,11,12,13,14,15,-1,-1,-1,-1,-1,-1,-1,-1,-1,
			-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,

			-1,10,11,12,13,14,15,-1,-1,-1,-1,-1,-1,-1,-1,-1,
			-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint ParseHexUInt(string text)
		{
			uint value = 0;
			foreach (char ch in text)
			{
				value <<= 4;
				value |= (uint)ParseHexDigit(ch);
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ParseHexPair(char v1, char v2)
			=> (ParseHexDigit(v1) << 4) | ParseHexDigit(v2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ParseHexDigit(char v)
			   => v < 128 ? _hexDigitValues[v] : 0;

#if NETCOREAPP
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		private static bool IsHexDigits(ReadOnlySpan<char> chars)
		{
			foreach (char ch in chars)
				if (ch >= 128 || _hexDigitValues[ch] < 0)
					return false;
			return true;
		}

		private static bool TryLexNumber(ReadOnlySpan<char> input, ref int ptr, out int value)
		{
			value = 0;
			int start = ptr;

			char ch;
			while (ptr < input.Length && (ch = input[ptr]) >= '0' && ch <= '9')
			{
				value *= 10;
				value += ch - '0';
				ptr++;
			}

			return start < ptr && ptr < start + 8;
		}

#if NETCOREAPP
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		private static void SkipWhitespace(ReadOnlySpan<char> input, ref int ptr)
		{
			char ch;
			while (ptr < input.Length
				&& (ch = input[ptr]) >= '\x0' && ch <= '\x20')
				ptr++;
		}

		private static bool TryLexRgba(ReadOnlySpan<char> input, out Color32 color)
		{
			// Before entering this method, we assume that the 'rgb' letters have
			// been tested and matched.
			int ptr = 3;
			color = default;

			// Consume an optional 'a' after the 'rgb'.
			char ch;
			if ((ch = input[ptr]) == 'a' || ch == 'A')
				ptr++;

			SkipWhitespace(input, ref ptr);

			// Require a '(' next.
			if (!(ptr < input.Length && input[ptr++] == '('))
				return false;

			SkipWhitespace(input, ref ptr);

			// Read the red value.
			if (!TryLexNumber(input, ref ptr, out int red))
				return false;

			// Require a ',' next.
			if (!(ptr < input.Length && input[ptr++] == ','))
				return false;

			SkipWhitespace(input, ref ptr);

			// Read the green value.
			if (!TryLexNumber(input, ref ptr, out int green))
				return false;

			// Require a ',' next.
			if (!(ptr < input.Length && input[ptr++] == ','))
				return false;

			SkipWhitespace(input, ref ptr);

			// Read the blue value.
			if (!TryLexNumber(input, ref ptr, out int blue))
				return false;

			SkipWhitespace(input, ref ptr);

			int alpha = 255;
			if (ptr < input.Length && input[ptr] == ',')
			{
				// Got a comma, so prepare to read the alpha value as a fraction.
				ptr++;

				SkipWhitespace(input, ref ptr);

				if (!TryLexFractionFrom0To1(input, ref ptr, out alpha))
					return false;

				SkipWhitespace(input, ref ptr);
			}

			// Require a ')' next.
			if (!(ptr < input.Length && input[ptr++] == ')'))
				return false;

			SkipWhitespace(input, ref ptr);

			// Finally, we should be at the end of the string.
			if (ptr != input.Length)
				return false;

			// We got it, and the data parsed cleanly.  Return it!
			color = new Color32(red, green, blue, alpha);
			return true;
		}

		/// <summary>
		/// Lex and parse a fractional decimal value, clamping (saturating) the
		/// result to [0.0, 1.0].
		/// </summary>
		/// <param name="input">The input character string.</param>
		/// <param name="ptr">The current source pointer in the input string, which will
		/// be updated by this method to point after the decimal value after it is read.</param>
		/// <param name="value">The resulting fractional value, represented as [0, 255].</param>
		/// <returns>True for a successful parse, false for a bad input.</returns>
		private static bool TryLexFractionFrom0To1(ReadOnlySpan<char> input, ref int ptr, out int value)
		{
			int start = ptr;

			// Read digits before the decimal point.
			int prefix = 0;
			char ch;
			while (ptr < input.Length && (ch = input[ptr]) >= '0' && ch <= '9')
			{
				prefix |= ch - '0';
				ptr++;
			}

			int alpha = 0;

			if (!(ptr < input.Length && input[ptr] == '.'))
			{
				// No fractional digits, so we're done.
				value = prefix != 0 ? 255 : 0;
				return ptr > start;
			}

			ptr++;

			// Read digits after the decimal point, with 24 bits of precision (same as a float,
			// but we do it in all-integer math, which is faster to compute).
			int multiplier = 0x1000000;
			while (ptr < input.Length && (ch = input[ptr]) >= '0' && ch <= '9')
			{
				ptr++;
				if (multiplier > 0)
				{
					// Divide the multiplier by 10, using fast multiplication.
					multiplier = (int)((multiplier * ((1L << 32) / 10)) >> 32);

					// Add in the fractional value.
					alpha += multiplier * (ch - '0');
				}
			}

			// We now have a value from 0 to 0x1000000 instead of the desired value of 0 to 255.
			// So use multiplication and shifting to turn it into the range of 0 to 255.
			alpha = ((alpha >> 1) * 255) >> 23;

			// For 0.xxx, we use the fractional value, but anything bigger is clamped to 1.
			value = prefix != 0 ? 255 : alpha;
			return ptr > start;
		}

		#endregion
	}
}
