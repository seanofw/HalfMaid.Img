using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP

namespace System
{
	/// <summary>
	/// Old .NET versions don't have MathF, so we provide a wrapper for float types
	/// built around the Math methods.  This is less efficient than MathF, but it's
	/// as good as older versions of .NET can do.
	/// </summary>
	internal static class MathF
	{
		public const float E = 2.7182818284590451f;
		public const float PI = 3.1415926535897931f;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Ceiling(float x)
			=> (float)Math.Ceiling(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Floor(float x)
			=> (float)Math.Floor(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Abs(float x)
			=> (float)Math.Abs(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Sign(float x)
			=> Math.Sign(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sin(float x)
			=> (float)Math.Sin(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sinh(float x)
			=> (float)Math.Sinh(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cos(float x)
			=> (float)Math.Cos(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cosh(float x)
			=> (float)Math.Cosh(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Tan(float x)
			=> (float)Math.Tan(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Tanh(float x)
			=> (float)Math.Tanh(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sqrt(float x)
			=> (float)Math.Sqrt(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Pow(float x, float y)
			=> (float)Math.Pow(x, y);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Asin(float x)
			=> (float)Math.Asin(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Acos(float x)
			=> (float)Math.Acos(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan(float x)
			=> (float)Math.Atan(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan2(float x, float y)
			=> (float)Math.Atan2(x, y);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Exp(float x)
			=> (float)Math.Exp(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log(float x, float y)
			=> (float)Math.Log(x, y);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log(float x)
			=> (float)Math.Log(x);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log10(float x)
			=> (float)Math.Log10(x);
	}
}

#endif