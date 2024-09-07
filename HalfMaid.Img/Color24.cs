using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HalfMaid.Img
{
	/// <summary>
	/// A color, in 24-bit truecolor, with lots of operations to manipulate it.<br />
	/// <br />
	/// This is intentionally laid out in memory as a 24-bit tuple of 8-bit values, with the
	/// bytes always in the order of [R, G, B].  This allows predictable memory layout, and
	/// lets you perform direct pointer read/write operations to pinned memory, or even
	/// pinning the memory so external libraries written in C or C++ can interact
	/// with it directly.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly struct Color24 : IEquatable<Color24>
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
		/// Retrieve a color value by its traditional index in the sequence of R, G, B.
		/// </summary>
		/// <param name="index">The index of the color value to retrieve, in the range of 0 to 2
		/// (inclusive).</param>
		/// <returns>The color value at the given index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not in the
		/// range of 0 to 2.</exception>
		public byte this[int index] => index switch
		{
			0 => R,
			1 => G,
			2 => B,
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
			_ => throw new ArgumentOutOfRangeException(nameof(channel)),
		};

		#endregion

		#region Construction

		/// <summary>
		/// Construct a color from three floating-point values, R, G, and B, in the range
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
		public Color24(float r, float g, float b)
			: this((int)(r * 255 + 0.5f), (int)(g * 255 + 0.5f), (int)(b * 255 + 0.5f))
		{
		}

		/// <summary>
		/// Construct a color from four double-precision floating-point values,
		/// R, G, and B, in the range of [0.0, 1.0] each.
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
		public Color24(double r, double g, double b)
			: this((int)(r * 255 + 0.5), (int)(g * 255 + 0.5), (int)(b * 255 + 0.5))
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
		public Color24(int r, int g, int b)
		{
			if (((r | g | b) & 0xFFFFFF00) == 0)
			{
				R = (byte)r;
				G = (byte)g;
				B = (byte)b;
			}
			else
			{
				R = (byte)Math.Min(Math.Max(r, 0), 255);
				G = (byte)Math.Min(Math.Max(g, 0), 255);
				B = (byte)Math.Min(Math.Max(b, 0), 255);
			}
		}

		/// <summary>
		/// Construct a color from four byte values, R, G, and B, in the range of 0-255
		/// each.  This overload sets the bytes directly, so it may be faster than the other
		/// constructors.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public Color24(byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
		}

		/// <summary>
		/// A 24-bit RGB color can be directly converted into a 32-bit RGBA color at any time;
		/// the alpha value is always 255 (opaque).
		/// </summary>
		/// <param name="color">The color to convert to RGBA form.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Color32(Color24 color)
			=> new Color32(color.R, color.G, color.B);

		/// <summary>
		/// A 32-bit RGBA color can be converted into a 24-bit RGB color by dropping the alpha
		/// component, which is an irreversible operation, so this operation requires an
		/// explicit cast.
		/// </summary>
		/// <param name="color">The color to convert to RGB form.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Color24(Color32 color)
			=> new Color24(color.R, color.G, color.B);

		#endregion

		#region Equality and hash codes

		/// <summary>
		/// Compare this color to another object on the heap for equality.
		/// </summary>
		public override bool Equals(object? obj)
			=> obj is Color24 color && this == color;

		/// <summary>
		/// Compare this color to another for equality.
		/// </summary>
		public bool Equals(Color24 other)
			=> this == other;

		/// <summary>
		/// Compare this color to another color for equality.
		/// </summary>
		public static bool operator ==(Color24 a, Color24 b)
			=> a.R == b.R && a.G == b.G && a.B == b.B;

		/// <summary>
		/// Compare this color to another color for inequality.
		/// </summary>
		public static bool operator !=(Color24 a, Color24 b)
			=> a.R != b.R || a.G != b.G || a.B != b.B;

		/// <summary>
		/// Calculate a hash code for this color so that it can be used in
		/// hash tables and dictionaries efficiently.
		/// </summary>
		public override int GetHashCode()
			=> unchecked((B * 65599 + G) * 65599 + R);

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
		/// it has one, or an #RRGGBB form if it doesn't have a name.</returns>
		public override string ToString()
			=> NamesByColor.TryGetValue(this, out string? name) ? name : ToHexString();

		/// <summary>
		/// Convert this to a string of the form rgb(R,G,B).
		/// </summary>
		/// <returns>A string form of this color, as a CSS rgb() three-valued vector.</returns>
		public string ToRgbString()
			=> $"rgb({R},{G},{B})";

		/// <summary>
		/// Convert this to a string of the form (R,G,B).
		/// </summary>
		/// <returns>A string form of this color, as a three-valued or four-valued vector.</returns>
		public string ToVectorString()
			=> $"({R},{G},{B})";

		/// <summary>
		/// Convert this to a string of the form #RRGGBB.
		/// </summary>
		/// <returns>This color converted to a hex string.</returns>
		public string ToHexString()
			=> Hex6;

		#endregion

		#region Color mixing

		/// <summary>
		/// The same color, but with the red channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 MaxR() => new Color24((byte)255, G, B);

		/// <summary>
		/// The same color, but with the green channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 MaxG() => new Color24(R, (byte)255, B);

		/// <summary>
		/// The same color, but with the blue channel set to 255.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 MaxB() => new Color24(R, G, (byte)255);

		/// <summary>
		/// The same color, but with the red channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 ZeroR() => new Color24((byte)0, G, B);

		/// <summary>
		/// The same color, but with the green channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 ZeroG() => new Color24(R, (byte)0, B);

		/// <summary>
		/// The same color, but with the blue channel set to 0.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color24 ZeroB() => new Color24(R, G, (byte)0);

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
		public Color24 Merge(Color24 other)
			=> new Color24(
				(byte)((R + other.R) >> 1),
				(byte)((G + other.G) >> 1),
				(byte)((B + other.B) >> 1)
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
		public Color24 Mix(Color24 other, double amount)
		{
			int am = (int)(Math.Min(Math.Max(amount, 0), 1) * 65536);
			int ia = 65536 - am;

			byte r = (byte)((R * ia + other.R * am + 32768) >> 16);
			byte g = (byte)((G * ia + other.G * am + 32768) >> 16);
			byte b = (byte)((B * ia + other.B * am + 32768) >> 16);

			return new Color24(r, g, b);
		}

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
		public Color24 Scale(Color32 other)
			=> new Color24(
				(byte)(Div255(R * other.R + 128)),
				(byte)(Div255(G * other.G + 128)),
				(byte)(Div255(B * other.B + 128))
			);

		/// <summary>
		/// Divide the given value by 255, faster than '/' can divide on most CPUs.
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		private static int Div255(int x)
			=> (x + 1 + (x >> 8)) >> 8;

		#endregion

		#region Deconstruction and tuple-conversion

		/// <summary>
		/// Convert this color to a tuple of floating-point values.
		/// </summary>
		/// <returns>A four-valued tuple of floating-point values in the range of [0, 1], in the
		/// form (r, g, b, a).</returns>
		public (float R, float G, float B) ToFloats()
			=> (R * (1.0f / 255), G * (1.0f / 255), B * (1.0f / 255));

		/// <summary>
		/// Convert this color to a tuple of double-precision floating-point values.
		/// </summary>
		/// <returns>A three-valued tuple of double-precision floating-point values
		/// in the range of [0, 1], in the form (r, g, b).</returns>
		public (double R, double G, double B) ToDoubles()
			=> (R * (1.0 / 255), G * (1.0 / 255), B * (1.0 / 255));

		/// <summary>
		/// Convert this to a tuple of integer values.
		/// </summary>
		/// <returns>A four-valued tuple of integer values in the range of [0, 255], in the
		/// form (r, g, b, a).</returns>
		public (int R, int G, int B) ToInts()
			=> (R, G, B);

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
		public static Color24 FromHsv((float Hue, float Sat, float Brt) color)
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
		public static Color24 FromHsb((float Hue, float Sat, float Brt) color)
			=> FromHsb(color.Hue, color.Sat, color.Brt);

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB/HSV) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="brt">The brightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color24 FromHsv(float hue, float sat, float brt)
			=> FromHsb(hue, sat, brt);

		/// <summary>
		/// Convert the given color from the hue/saturation/brightness (HSB/HSV) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="brt">The brightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color24 FromHsb(float hue, float sat, float brt)
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

			return new Color24(r + m, g + m, b + m);
		}

		/// <summary>
		/// Convert the given color from the hue/saturation/lightness (HSB/HSL) model to
		/// a displayable RGB color.
		/// </summary>
		/// <param name="hue">The hue, in the range of [0.0, 360.0).</param>
		/// <param name="sat">The saturation, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <param name="lit">The lightness, in the range of 0.0 to 1.0 (inclusive).</param>
		/// <returns>The equivalent RGB color.</returns>
		public static Color24 FromHsl(float hue, float sat, float lit)
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

			return new Color24(r + m, g + m, b + m);
		}

		/// <summary>
		/// Apply gamma correction by raising each color component to the given power.
		/// </summary>
		/// <param name="gamma">The power to raise by.</param>
		/// <returns>The gamma-corrected color.</returns>
		public Color24 Gamma(double gamma)
			=> new Color24(Math.Pow(Rd, gamma),
				Math.Pow(Gd, gamma),
				Math.Pow(Bd, gamma));

		#endregion

		#region Color "arithmetic" operators

		/// <summary>
		/// Perform componentwise addition on the R, G, and B values of the given colors.
		/// </summary>
		/// <param name="x">The first color to add.</param>
		/// <param name="y">The second color to add.</param>
		/// <returns>The "sum" of those colors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator +(Color24 x, Color24 y)
			=> new Color24(x.R + y.R, x.G + y.G, x.B + y.B);

		/// <summary>
		/// Perform componentwise subtraction on the R, G, and B values of the given colors.
		/// </summary>
		/// <param name="x">The source color.</param>
		/// <param name="y">The color to subtract from the source.</param>
		/// <returns>The "difference" of those colors.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator -(Color24 x, Color24 y)
			=> new Color24(x.R - y.R, x.G - y.G, x.B - y.B);

		/// <summary>
		/// Calculate the "inverse" of the given color, replacing each value V
		/// with (256 - V) % 256.
		/// </summary>
		/// <param name="c">The original color.</param>
		/// <returns>The "inverse" of that color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator -(Color24 c)
			=> new Color24((byte)(-c.R), (byte)(-c.G), (byte)(-c.B));

		/// <summary>
		/// Calculate the "inverse" of the given color, replacing each value V
		/// with 255 - V.
		/// </summary>
		/// <param name="c">The original color.</param>
		/// <returns>The "inverse" of that color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator ~(Color24 c)
			=> new Color24((byte)~c.R, (byte)~c.G, (byte)~c.B);

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors, treating each as though it is a fractional value in the range of [0, 1].
		/// </summary>
		/// <param name="x">The first color to multiply.</param>
		/// <param name="y">The second color to multiply.</param>
		/// <returns>The "product" of those colors.</returns>
		public static Color24 operator *(Color24 x, Color24 y)
			=> new Color24(Div255(x.R * y.R), Div255(x.G * y.G), Div255(x.B * y.B));

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color24 operator *(Color24 c, int scalar)
			=> new Color24(c.R * scalar, c.G * scalar, c.B * scalar);

		/// <summary>
		/// Perform componentwise multiplication on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color24 operator *(Color24 c, double scalar)
		{
			double s = scalar * (1.0 / 255);
			return new Color24((int)(c.R * s + 0.5), (int)(c.G * s + 0.5), (int)(c.B * s + 0.5));
		}

		/// <summary>
		/// Perform componentwise division on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color24 operator /(Color24 c, int scalar)
			=> new Color24(c.R / scalar, c.G / scalar, c.B / scalar);

		/// <summary>
		/// Perform componentwise division on the R, G, and B values of the given
		/// colors by the given scalar.  The resulting values will be clamped to the usual
		/// range of [0, 255].
		/// </summary>
		/// <param name="c">The color to multiply.</param>
		/// <param name="scalar">The scalar to multiply that color by.</param>
		/// <returns>The scaled color.</returns>
		public static Color24 operator /(Color24 c, double scalar)
		{
			double s = 1.0 / (scalar * 255);
			return new Color24((int)(c.R * s + 0.5), (int)(c.G * s + 0.5), (int)(c.B * s + 0.5));
		}

		/// <summary>
		/// Calculate the bitwise-or of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are or'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator |(Color24 a, Color24 b)
			=> new Color24((byte)(a.R | b.R), (byte)(a.G | b.G), (byte)(a.B | b.B));

		/// <summary>
		/// Calculate the bitwise-and of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are and'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator &(Color24 a, Color24 b)
			=> new Color24((byte)(a.R & b.R), (byte)(a.G & b.G), (byte)(a.B & b.B));

		/// <summary>
		/// Calculate the bitwise-xor of a pair of colors, on all four channels.
		/// </summary>
		/// <param name="a">The first color to combine.</param>
		/// <param name="b">The second color to combine.</param>
		/// <returns>A color where the bits of each channel of the two source colors are xor'ed together.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator ^(Color24 a, Color24 b)
			=> new Color24((byte)(a.R ^ b.R), (byte)(a.G ^ b.G), (byte)(a.B ^ b.B));

		/// <summary>
		/// Shift each integer color component logically to the right.  This does not
		/// affect the alpha channel.
		/// </summary>
		/// <param name="c">The color to shift.</param>
		/// <param name="amount">The number of bits to shift each component by.</param>
		/// <returns>A color where each channel has been shifted right by the given amount.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator >>(Color24 c, int amount)
			=> new Color24((byte)(c.R >> amount), (byte)(c.G >> amount), (byte)(c.B >> amount));

		/// <summary>
		/// Shift each integer color component logically to the left.  This does not
		/// affect the alpha channel.
		/// </summary>
		/// <param name="c">The color to shift.</param>
		/// <param name="amount">The number of bits to shift each component by.</param>
		/// <returns>A color where each channel has been shifted right by the given amount.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color24 operator <<(Color24 c, int amount)
			=> new Color24((byte)(c.R << amount), (byte)(c.G << amount), (byte)(c.B << amount));

		/// <summary>
		/// Calculate the distance between this color and another color in RGB-space.
		/// </summary>
		/// <param name="other">The other color to measure the distance to.</param>
		/// <returns>The distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Distance(Color24 other)
			=> Math.Sqrt(DistanceSquared(other));

		/// <summary>
		/// Calculate the square of the distance between this color and another color in RGB-space.
		/// </summary>
		/// <param name="other">The other color to measure the square of the distance to.</param>
		/// <returns>The distance to the other color.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DistanceSquared(Color24 other)
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
		public double WeightedDistance(Color24 other,
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
		public double WeightedDistanceSquared(Color24 other,
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

		public static readonly Color24 AntiqueWhite = new Color24((byte)250, (byte)235, (byte)215);
		public static readonly Color24 Aqua = new Color24((byte)0, (byte)255, (byte)255);
		public static readonly Color24 Aquamarine = new Color24((byte)127, (byte)255, (byte)212);
		public static readonly Color24 Azure = new Color24((byte)240, (byte)255, (byte)255);
		public static readonly Color24 Beige = new Color24((byte)245, (byte)245, (byte)220);
		public static readonly Color24 Bisque = new Color24((byte)255, (byte)228, (byte)196);
		public static readonly Color24 Black = new Color24((byte)0, (byte)0, (byte)0);
		public static readonly Color24 BlanchedAlmond = new Color24((byte)255, (byte)235, (byte)205);
		public static readonly Color24 Blue = new Color24((byte)0, (byte)0, (byte)255);
		public static readonly Color24 BlueViolet = new Color24((byte)138, (byte)43, (byte)226);
		public static readonly Color24 Brown = new Color24((byte)165, (byte)42, (byte)42);
		public static readonly Color24 Burlywood = new Color24((byte)222, (byte)184, (byte)135);
		public static readonly Color24 CadetBlue = new Color24((byte)95, (byte)158, (byte)160);
		public static readonly Color24 Chartreuse = new Color24((byte)127, (byte)255, (byte)0);
		public static readonly Color24 Chocolate = new Color24((byte)210, (byte)105, (byte)30);
		public static readonly Color24 Coral = new Color24((byte)255, (byte)127, (byte)80);
		public static readonly Color24 CornflowerBlue = new Color24((byte)100, (byte)149, (byte)237);
		public static readonly Color24 Cornsilk = new Color24((byte)255, (byte)248, (byte)220);
		public static readonly Color24 Crimson = new Color24((byte)220, (byte)20, (byte)60);
		public static readonly Color24 Cyan = new Color24((byte)0, (byte)255, (byte)255);
		public static readonly Color24 DarkBlue = new Color24((byte)0, (byte)0, (byte)139);
		public static readonly Color24 DarkCyan = new Color24((byte)0, (byte)139, (byte)139);
		public static readonly Color24 DarkGoldenrod = new Color24((byte)184, (byte)134, (byte)11);
		public static readonly Color24 DarkGray = new Color24((byte)169, (byte)169, (byte)169);
		public static readonly Color24 DarkGreen = new Color24((byte)0, (byte)100, (byte)0);
		public static readonly Color24 DarkGrey = new Color24((byte)169, (byte)169, (byte)169);
		public static readonly Color24 DarkKhaki = new Color24((byte)189, (byte)183, (byte)107);
		public static readonly Color24 DarkMagenta = new Color24((byte)139, (byte)0, (byte)139);
		public static readonly Color24 DarkOliveGreen = new Color24((byte)85, (byte)107, (byte)47);
		public static readonly Color24 DarkOrange = new Color24((byte)255, (byte)140, (byte)0);
		public static readonly Color24 DarkOrchid = new Color24((byte)153, (byte)50, (byte)204);
		public static readonly Color24 DarkRed = new Color24((byte)139, (byte)0, (byte)0);
		public static readonly Color24 DarkSalmon = new Color24((byte)233, (byte)150, (byte)122);
		public static readonly Color24 DarkSeaGreen = new Color24((byte)143, (byte)188, (byte)143);
		public static readonly Color24 DarkSlateBlue = new Color24((byte)72, (byte)61, (byte)139);
		public static readonly Color24 DarkSlateGray = new Color24((byte)47, (byte)79, (byte)79);
		public static readonly Color24 DarkSlateGrey = new Color24((byte)47, (byte)79, (byte)79);
		public static readonly Color24 DarkTurquoise = new Color24((byte)0, (byte)206, (byte)209);
		public static readonly Color24 DarkViolet = new Color24((byte)148, (byte)0, (byte)211);
		public static readonly Color24 DeepPink = new Color24((byte)255, (byte)20, (byte)147);
		public static readonly Color24 DeepSkyBlue = new Color24((byte)0, (byte)191, (byte)255);
		public static readonly Color24 DimGray = new Color24((byte)105, (byte)105, (byte)105);
		public static readonly Color24 DimGrey = new Color24((byte)105, (byte)105, (byte)105);
		public static readonly Color24 DodgerBlue = new Color24((byte)30, (byte)144, (byte)255);
		public static readonly Color24 FireBrick = new Color24((byte)178, (byte)34, (byte)34);
		public static readonly Color24 FloralWhite = new Color24((byte)255, (byte)250, (byte)240);
		public static readonly Color24 ForestGreen = new Color24((byte)34, (byte)139, (byte)34);
		public static readonly Color24 Fuchsia = new Color24((byte)255, (byte)0, (byte)255);
		public static readonly Color24 Gainsboro = new Color24((byte)220, (byte)220, (byte)220);
		public static readonly Color24 GhostWhite = new Color24((byte)248, (byte)248, (byte)255);
		public static readonly Color24 Gold = new Color24((byte)255, (byte)215, (byte)0);
		public static readonly Color24 Goldenrod = new Color24((byte)218, (byte)165, (byte)32);
		public static readonly Color24 Gray = new Color24((byte)128, (byte)128, (byte)128);
		public static readonly Color24 Green = new Color24((byte)0, (byte)128, (byte)0);
		public static readonly Color24 GreenYellow = new Color24((byte)173, (byte)255, (byte)47);
		public static readonly Color24 Grey = new Color24((byte)128, (byte)128, (byte)128);
		public static readonly Color24 Honeydew = new Color24((byte)240, (byte)255, (byte)240);
		public static readonly Color24 HotPink = new Color24((byte)255, (byte)105, (byte)180);
		public static readonly Color24 IndianRed = new Color24((byte)205, (byte)92, (byte)92);
		public static readonly Color24 Indigo = new Color24((byte)75, (byte)0, (byte)130);
		public static readonly Color24 Ivory = new Color24((byte)255, (byte)255, (byte)240);
		public static readonly Color24 Khaki = new Color24((byte)240, (byte)230, (byte)140);
		public static readonly Color24 Lavender = new Color24((byte)230, (byte)230, (byte)250);
		public static readonly Color24 LavenderBlush = new Color24((byte)255, (byte)240, (byte)245);
		public static readonly Color24 LawnGreen = new Color24((byte)124, (byte)252, (byte)0);
		public static readonly Color24 LemonChiffon = new Color24((byte)255, (byte)250, (byte)205);
		public static readonly Color24 LightBlue = new Color24((byte)173, (byte)216, (byte)230);
		public static readonly Color24 LightCoral = new Color24((byte)240, (byte)128, (byte)128);
		public static readonly Color24 LightCyan = new Color24((byte)224, (byte)255, (byte)255);
		public static readonly Color24 LightGoldenrodYellow = new Color24((byte)250, (byte)250, (byte)210);
		public static readonly Color24 LightGray = new Color24((byte)211, (byte)211, (byte)211);
		public static readonly Color24 LightGreen = new Color24((byte)144, (byte)238, (byte)144);
		public static readonly Color24 LightGrey = new Color24((byte)211, (byte)211, (byte)211);
		public static readonly Color24 LightPink = new Color24((byte)255, (byte)182, (byte)193);
		public static readonly Color24 LightSalmon = new Color24((byte)255, (byte)160, (byte)122);
		public static readonly Color24 LightSeaGreen = new Color24((byte)32, (byte)178, (byte)170);
		public static readonly Color24 LightSkyBlue = new Color24((byte)135, (byte)206, (byte)250);
		public static readonly Color24 LightSlateGray = new Color24((byte)119, (byte)136, (byte)153);
		public static readonly Color24 LightSlateGrey = new Color24((byte)119, (byte)136, (byte)153);
		public static readonly Color24 LightSteelBlue = new Color24((byte)176, (byte)196, (byte)222);
		public static readonly Color24 LightYellow = new Color24((byte)255, (byte)255, (byte)224);
		public static readonly Color24 Lime = new Color24((byte)0, (byte)255, (byte)0);
		public static readonly Color24 LimeGreen = new Color24((byte)50, (byte)205, (byte)50);
		public static readonly Color24 Linen = new Color24((byte)250, (byte)240, (byte)230);
		public static readonly Color24 Magenta = new Color24((byte)255, (byte)0, (byte)255);
		public static readonly Color24 Maroon = new Color24((byte)128, (byte)0, (byte)0);
		public static readonly Color24 MediumAquamarine = new Color24((byte)102, (byte)205, (byte)170);
		public static readonly Color24 MediumBlue = new Color24((byte)0, (byte)0, (byte)205);
		public static readonly Color24 MediumOrchid = new Color24((byte)186, (byte)85, (byte)211);
		public static readonly Color24 MediumPurple = new Color24((byte)147, (byte)112, (byte)219);
		public static readonly Color24 MediumSeaGreen = new Color24((byte)60, (byte)179, (byte)113);
		public static readonly Color24 MediumSlateBlue = new Color24((byte)123, (byte)104, (byte)238);
		public static readonly Color24 MediumSpringGreen = new Color24((byte)0, (byte)250, (byte)154);
		public static readonly Color24 MediumTurquoise = new Color24((byte)72, (byte)209, (byte)204);
		public static readonly Color24 MediumVioletRed = new Color24((byte)199, (byte)21, (byte)133);
		public static readonly Color24 MidnightBlue = new Color24((byte)25, (byte)25, (byte)112);
		public static readonly Color24 MintCream = new Color24((byte)245, (byte)255, (byte)250);
		public static readonly Color24 MistyRose = new Color24((byte)255, (byte)228, (byte)225);
		public static readonly Color24 Moccasin = new Color24((byte)255, (byte)228, (byte)181);
		public static readonly Color24 NavajoWhite = new Color24((byte)255, (byte)222, (byte)173);
		public static readonly Color24 Navy = new Color24((byte)0, (byte)0, (byte)128);
		public static readonly Color24 OldLace = new Color24((byte)253, (byte)245, (byte)230);
		public static readonly Color24 Olive = new Color24((byte)128, (byte)128, (byte)0);
		public static readonly Color24 OliveDrab = new Color24((byte)107, (byte)142, (byte)35);
		public static readonly Color24 Orange = new Color24((byte)255, (byte)165, (byte)0);
		public static readonly Color24 Orangered = new Color24((byte)255, (byte)69, (byte)0);
		public static readonly Color24 Orchid = new Color24((byte)218, (byte)112, (byte)214);
		public static readonly Color24 PaleGoldenrod = new Color24((byte)238, (byte)232, (byte)170);
		public static readonly Color24 PaleGreen = new Color24((byte)152, (byte)251, (byte)152);
		public static readonly Color24 PaleTurquoise = new Color24((byte)175, (byte)238, (byte)238);
		public static readonly Color24 PaleVioletRed = new Color24((byte)219, (byte)112, (byte)147);
		public static readonly Color24 PapayaWhip = new Color24((byte)255, (byte)239, (byte)213);
		public static readonly Color24 Peach = new Color24((byte)255, (byte)192, (byte)128);
		public static readonly Color24 PeachPuff = new Color24((byte)255, (byte)218, (byte)185);
		public static readonly Color24 Peru = new Color24((byte)205, (byte)133, (byte)63);
		public static readonly Color24 Pink = new Color24((byte)255, (byte)192, (byte)203);
		public static readonly Color24 Plum = new Color24((byte)221, (byte)160, (byte)221);
		public static readonly Color24 PowderBlue = new Color24((byte)176, (byte)224, (byte)230);
		public static readonly Color24 Purple = new Color24((byte)128, (byte)0, (byte)128);
		public static readonly Color24 RebeccaPurple = new Color24((byte)102, (byte)51, (byte)153);
		public static readonly Color24 Red = new Color24((byte)255, (byte)0, (byte)0);
		public static readonly Color24 RosyBrown = new Color24((byte)188, (byte)143, (byte)143);
		public static readonly Color24 RoyalBlue = new Color24((byte)65, (byte)105, (byte)225);
		public static readonly Color24 SaddleBrown = new Color24((byte)139, (byte)69, (byte)19);
		public static readonly Color24 Salmon = new Color24((byte)250, (byte)128, (byte)114);
		public static readonly Color24 SandyBrown = new Color24((byte)244, (byte)164, (byte)96);
		public static readonly Color24 SeaGreen = new Color24((byte)46, (byte)139, (byte)87);
		public static readonly Color24 Seashell = new Color24((byte)255, (byte)245, (byte)238);
		public static readonly Color24 Sienna = new Color24((byte)160, (byte)82, (byte)45);
		public static readonly Color24 Silver = new Color24((byte)192, (byte)192, (byte)192);
		public static readonly Color24 SkyBlue = new Color24((byte)135, (byte)206, (byte)235);
		public static readonly Color24 SlateBlue = new Color24((byte)106, (byte)90, (byte)205);
		public static readonly Color24 SlateGray = new Color24((byte)112, (byte)128, (byte)144);
		public static readonly Color24 Snow = new Color24((byte)255, (byte)250, (byte)250);
		public static readonly Color24 SpringGreen = new Color24((byte)0, (byte)255, (byte)127);
		public static readonly Color24 SteelBlue = new Color24((byte)70, (byte)130, (byte)180);
		public static readonly Color24 Tan = new Color24((byte)210, (byte)180, (byte)140);
		public static readonly Color24 Tea = new Color24((byte)0, (byte)128, (byte)128);
		public static readonly Color24 Thistle = new Color24((byte)216, (byte)191, (byte)216);
		public static readonly Color24 Tomato = new Color24((byte)255, (byte)99, (byte)71);
		public static readonly Color24 Turquoise = new Color24((byte)64, (byte)224, (byte)208);
		public static readonly Color24 Violet = new Color24((byte)238, (byte)130, (byte)238);
		public static readonly Color24 Wheat = new Color24((byte)245, (byte)222, (byte)179);
		public static readonly Color24 White = new Color24((byte)255, (byte)255, (byte)255);
		public static readonly Color24 WhiteSmoke = new Color24((byte)245, (byte)245, (byte)245);
		public static readonly Color24 Yellow = new Color24((byte)255, (byte)255, (byte)0);
		public static readonly Color24 YellowGreen = new Color24((byte)154, (byte)205, (byte)50);

		#pragma warning restore 1591

		#endregion

		#region Color names

		private static readonly (string Name, Color24 Color)[] _colorList = new (string Name, Color24 Color)[]
		{
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
		private static IReadOnlyDictionary<string, Color24>? _colorsByName;

		/// <summary>
		/// The list of colors above, turned into a dictionary keyed by color.
		/// </summary>
		private static IReadOnlyDictionary<Color24, string>? _namesByColor;

		private static IReadOnlyDictionary<string, Color24> MakeColorsByName()
		{
			// Fanciness to project the color list to a dictionary, without taking a dependency on Linq.
			Dictionary<string, Color24> colorsByName = new Dictionary<string, Color24>();
			foreach ((string Name, Color24 Color) pair in _colorList)
			{
				colorsByName.Add(pair.Name, pair.Color);
			}
			return colorsByName;
		}

		private static IReadOnlyDictionary<Color24, string> MakeNamesByColor()
		{
			// Fanciness to project the color list to a dictionary, without taking a dependency on Linq.
			Dictionary<Color24, string> namesByColor = new Dictionary<Color24, string>();
			foreach ((string Name, Color24 Color) pair in _colorList)
			{
				if (!namesByColor.ContainsKey(pair.Color))
					namesByColor[pair.Color] = pair.Name;
			}
			return namesByColor;
		}

		/// <summary>
		/// Retrieve the full list of defined colors, in their definition order.
		/// </summary>
		public static IReadOnlyList<(string Name, Color24 Color)> ColorList => _colorList;

		/// <summary>
		/// A dictionary that maps color values to their matching names, in lowercase.
		/// </summary>
		public static IReadOnlyDictionary<Color24, string> NamesByColor => _namesByColor ??= MakeNamesByColor();

		/// <summary>
		/// A dictionary that maps color names to their matching color values.
		/// </summary>
		public static IReadOnlyDictionary<string, Color24> ColorsByName => _colorsByName ??= MakeColorsByName();

		#endregion

		#region Color parsing

		/// <summary>
		/// Parse a 24-bit RGB color (8 bits per channel) in one of several commmon CSS-style formats:
		///    - "#RGB"
		///    - "#RRGGBB"
		///    - "rgb(123, 45, 67)"
		///    - "name"   (standard web color names)
		/// </summary>
		/// <param name="value">The color value to parse.</param>
		/// <returns>The resulting actual color.</returns>
		/// <exception cref="ArgumentException">Thrown if the color string cannot be parsed in one
		/// of the known formats.</exception>
		public static Color24 Parse(string value)
		{
			if (!TryParse(value, out Color24 color))
				throw new ArgumentException($"Invalid color value '{value}'.");
			return color;
		}

		/// <summary>
		/// Attempt to parse a 24-bit RGB color (8 bits per channel) in one of several commmon CSS-style formats:
		///    - "#RGB"
		///    - "#RRGGBB"
		///    - "rgb(123, 45, 67)"
		///    - "name"   (standard web color names)
		/// </summary>
		/// <param name="value">The color value to parse.</param>
		/// <param name="color">The resulting actual color; if the string cannot be parsed,
		/// this will be set to Color.Transparent.</param>
		/// <returns>True if the color could be parsed, false if it could not.</returns>
		public static bool TryParse(string value, out Color24 color)
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
				if (!TryLexRgb(input.Slice(3), out color))
					return false;
				return true;
			}

			if (input.Length > 20)
			{
				// All of the color names are 20 characters or less.
				color = Black;
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

			if (ColorsByName.TryGetValue(lowerName.ToString(), out Color24 c))
			{
				color = c;
				return true;
			}

			color = Black;
			return false;
		}

		/// <summary>
		/// Attempt to parse a 24-bit RGB color (8 bits per channel) in one of two commmon CSS-style formats:
		///    - "RGB"
		///    - "RRGGBB"
		/// </summary>
		/// <param name="hex">The color value to parse.</param>
		/// <param name="color">The resulting actual color; if the string cannot be parsed,
		/// this will be set to Color.Transparent.</param>
		/// <returns>True if the color could be parsed, false if it could not.</returns>
		public static bool TryParseHexColor(ReadOnlySpan<char> hex, out Color24 color)
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
					color = new Color24(r, g, b);
					return true;

				case 6:
					r = ParseHexPair(hex[0], hex[1]);
					g = ParseHexPair(hex[2], hex[3]);
					b = ParseHexPair(hex[4], hex[5]);
					color = new Color24(r, g, b);
					return true;

				default:
					color = Black;
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

		private static bool TryLexRgb(ReadOnlySpan<char> input, out Color24 color)
		{
			// Before entering this method, we assume that the 'rgb' letters have
			// been tested and matched.
			int ptr = 3;
			color = default;

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

			// Require a ')' next.
			if (!(ptr < input.Length && input[ptr++] == ')'))
				return false;

			SkipWhitespace(input, ref ptr);

			// Finally, we should be at the end of the string.
			if (ptr != input.Length)
				return false;

			// We got it, and the data parsed cleanly.  Return it!
			color = new Color24(red, green, blue);
			return true;
		}

		#endregion
	}
}
