#if NETSTANDARD2_0

// This is a simple wrapper that provides a crude equivalent of MathF on
// flavors of .NET that do not already have it.
//
// This is distributed under the MIT license.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable SA1516

namespace OpenTK.Mathematics
{
    internal static class MathF
    {
        public const float PI = 3.141592653f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float x)
            => (float)Math.Abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Floor(float x)
            => (float)Math.Floor(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ceiling(float x)
            => (float)Math.Ceiling(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float x)
            => (float)Math.Sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float x)
            => (float)Math.Log(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float x, float y)
            => (float)Math.Log(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pow(float x, float y)
            => (float)Math.Pow(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float x)
            => (float)Math.Sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float x)
            => (float)Math.Cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float x)
            => (float)Math.Tan(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Asin(float x)
            => (float)Math.Asin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Acos(float x)
            => (float)Math.Acos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan(float x)
            => (float)Math.Atan(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2(float x, float y)
            => (float)Math.Atan2(x, y);
    }
}

#endif