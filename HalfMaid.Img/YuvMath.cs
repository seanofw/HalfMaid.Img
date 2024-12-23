//---------------------------------------------------------------------------
//
// This is part of a port of Clément Bœsch's excellent clean-room implementation
// of HQX, which he describes here:  https://blog.pkh.me/p/19-butchering-hqx-scaling-filters.html
// This is implemented as pure, managed C# code, with no dependencies on
// external C libraries.  It uses pointers for speed, but it is still
// otherwise C#.  The original copyright/license that Bœsch included is
// provided below.  This C# version, like the rest of the HalfMaid.Img library,
// is covered under the terms of the MIT open-source license (which is the
// same license that Bœsch used on his FFmpeg C port).
//
// Share and enjoy.
//
//---------------------------------------------------------------------------
//
// Copyright (c) 2014 Clément Bœsch
//
// This file is part of FFmpeg.
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted, provided that the above
// copyright notice and this permission notice appear in all copies.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
// ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
// ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
// OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//
//---------------------------------------------------------------------------
//
// @file
// hqx magnification filters (hq2x, hq3x, hq4x)
//
// Originally designed by Maxim Stephin.
//
// @see http://en.wikipedia.org/wiki/Hqx
// @see http://web.archive.org/web/20131114143602/http://www.hiend3d.com/hq3x.html
// @see http://blog.pkh.me/p/19-butchering-hqx-scaling-filters.html
//
//---------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace HalfMaid.Img
{
	/// <summary>
	/// This class performs high-speed conversion from RGB to YUV by using
	/// a precomputed lookup table.  It is used as the basis of several other
	/// algorithms, but you can instantiate it independently if you need to.
	/// For performance reasons, the actual data table itself (64 megabytes'
	/// worth) is kept off-heap.
	/// </summary>
	public class YuvMath : IDisposable
	{
		/// <summary>
		/// A lookup table from RGB colors to YUV colors.  This is 64 megabytes long
		/// so that we have a pure lookup table and don't need to do anything fancy
		/// to convert RGB to YUV:  It's a trade of memory for speed, and it's kept
		/// off-heap so as not to burden the GC (but in exchange, this class has to
		/// implement IDisposable to clean up after itself, and instantiation can take
		/// a moment because of how long it takes to build the table).<br />
		/// <br />
		/// Note that we keep this around as long as there's an Hqx class instance:
		/// They share the table, so a simple way to avoid construction overhead is
		/// to create an instance when your program fires up and then never discard it.
		/// </summary>
		private static unsafe uint* _rgb2yuv;

		private static int _initCount;
		private int _isDisposed;

		/// <summary>
		/// Construct a new instance of the YuvLookup table.
		/// This can require setup time if it's the first instance, so prefer to keep
		/// an instance around if you intend to use it more than once.
		/// </summary>
		/// <remarks>
		/// Note that if multiple instances of YuvLookup are constructed on parallel
		/// threads, only the first instance will construct the required data tables,
		/// and other
		/// parallel instances will NOT work correctly until the first has finished
		/// initializing.  The safest way to avoid this problem is just to construct
		/// a single instance of YuvLookup and then share it.
		/// </remarks>
		public YuvMath()
		{
			if (Interlocked.Increment(ref _initCount) == 1)
			{
				unsafe
				{
					_rgb2yuv = MakeRgbToYuvTable();
				}
			}
		}

		/// <summary>
		/// Destroy an instance of the YuvLookup class, releasing all of its
		/// (not-small) resources, if this is the last instance of it.
		/// </summary>
		~YuvMath()
		{
			Dispose(false);
		}

		/// <summary>
		/// Destroy an instance of the Hqx class, releasing all of its
		/// (not-small) resources, if this is the last instance of it.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///  Destroy an instance of the YuvLookup class, releasing all of its
		/// (not-small) resources, if this is the last instance of it.
		/// </summary>
		/// <param name="isDisposing">Whether this is being disposed by a call
		/// to Dispose() or by a call to the finalizer.</param>
		private void Dispose(bool isDisposing)
		{
			if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				if (Interlocked.Decrement(ref _initCount) == 0)
				{
					unsafe
					{
						Marshal.FreeHGlobal((IntPtr)_rgb2yuv);
						_rgb2yuv = null;
					}
				}
			}
		}

		/// <summary>
		/// Because many of the methods below are equivalent to C macros, they must
		/// have maximum inlining and optimization from the JIT, so we turn on as
		/// much as possible here throughout.
		/// </summary>
#if NETCOREAPP
		private const MethodImplOptions Optimized =
			MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
		private const MethodImplOptions Optimized = MethodImplOptions.AggressiveInlining;
#endif

		/// <summary>
		/// Calculate the absolute difference between signed integers a and b:  abs(a - b).
		/// </summary>
		[MethodImpl(Optimized)]
		public static int AbsDiff(uint a, uint b)
			=> Math.Abs((int)a - (int)b);

		/// <summary>
		/// Determine whether the two provided YUV colors exceed the color-difference
		/// thresholds allowed by HQX.  This is used by both the HQX and xBR algorithms.
		/// </summary>
		/// <param name="yuv1">The first color to compare.</param>
		/// <param name="yuv2">The second color to compare.</param>
		/// <returns>True if they are sufficiently similar as to be considered the same
		/// by HQX, or false if they are sufficiently different that they should be
		/// considered an edge between two color regions.</returns>
		[MethodImpl(Optimized)]
		public static bool DiffNoAlpha(uint yuv1, uint yuv2)
			=> AbsDiff(yuv1 & 0xFF0000, yuv2 & 0xFF0000) > (48 << 16) ||
			   AbsDiff(yuv1 & 0x00FF00, yuv2 & 0x00FF00) > ( 7 <<  8) ||
			   AbsDiff(yuv1 & 0x0000FF, yuv2 & 0x0000FF) > ( 6 <<  0);

		/// <summary>
		/// Determine whether the two provided YUV colors exceed the color-difference
		/// thresholds allowed by HQX.  This is used by both the HQX and xBR algorithms.
		/// </summary>
		/// <param name="yuv1">The first color to compare.</param>
		/// <param name="yuv2">The second color to compare.</param>
		/// <returns>True if they are sufficiently similar as to be considered the same
		/// by HQX, or false if they are sufficiently different that they should be
		/// considered an edge between two color regions.</returns>
		[MethodImpl(Optimized)]
		public static bool DiffWithAlpha(uint yuv1, uint yuv2)
			=> AbsDiff(yuv1 & 0xFF0000, yuv2 & 0xFF0000) > (48 << 16) ||
			   AbsDiff(yuv1 & 0x00FF00, yuv2 & 0x00FF00) > (7 << 8) ||
			   AbsDiff(yuv1 & 0x0000FF, yuv2 & 0x0000FF) > (6 << 0) ||
			   AbsDiff((yuv1 >> 24) & 0xFF, (yuv2 >> 24) & 0xFF) > 32;

		/// <summary>
		/// Determine whether the two provided YUV colors exceed the color-difference
		/// thresholds allowed by HQX.  This is used by both the HQX and xBR algorithms.
		/// </summary>
		/// <param name="yuv1">The first color to compare.</param>
		/// <param name="yuv2">The second color to compare.</param>
		/// <param name="includeAlpha">Whether to consider the alpha channel when comparing.</param>
		/// <returns>True if they are sufficiently similar as to be considered the same
		/// by HQX, or false if they are sufficiently different that they should be
		/// considered an edge between two color regions.</returns>
		[MethodImpl(Optimized)]
		public static bool Diff(uint yuv1, uint yuv2, bool includeAlpha)
			=> includeAlpha ? DiffWithAlpha(yuv1, yuv2) : DiffNoAlpha(yuv1, yuv2);

		/// <summary>
		/// Calculate (c1*w1 + c2*w2) >> s, where c1, c2, w1, and w2 represent packed
		/// color components or scaling factors.  All four color components are calculated
		/// in parallel.
		/// </summary>
		[MethodImpl(Optimized)]
		public static uint Interp2Px(uint c1, int w1, uint c2, int w2, int s)
			=> (((((c1 & 0xff00ff00) >> 8) * (uint)w1 + ((c2 & 0xff00ff00) >> 8) * (uint)w2) << (8 - s)) & 0xff00ff00) |
			   (((((c1 & 0x00ff00ff)     ) * (uint)w1 + ((c2 & 0x00ff00ff)     ) * (uint)w2) >>      s ) & 0x00ff00ff);

		/// <summary>
		/// Calculate (c1*w1 + c2*w2 + c2*w3) >> s, where c1, c2, c3, w1, w2, and w3
		/// represent packed color components or scaling factors.  All four color components
		/// are calculated in parallel.
		/// </summary>
		[MethodImpl(Optimized)]
		public static uint Interp3Px(uint c1, int w1, uint c2, int w2, uint c3, int w3, int s)
			=> (((((c1 & 0xff00ff00) >> 8) * (uint)w1 + ((c2 & 0xff00ff00) >> 8) * (uint)w2 + ((c3 & 0xff00ff00) >> 8) * (uint)w3) << (8 - s)) & 0xff00ff00) |
			   (((((c1 & 0x00ff00ff)     ) * (uint)w1 + ((c2 & 0x00ff00ff)     ) * (uint)w2 + ((c3 & 0x00ff00ff)     ) * (uint)w3) >>      s ) & 0x00ff00ff);

		/// <summary>
		/// Convert the RGB color c to YUV via the provided '_rgb2yuv' table, fast.<br />
		/// <br />
		/// This is static for performance reasons, but it is only valid to call it
		/// while at least one instance of YuvLookup exists:  If no such instance exists,
		/// this method will crash with a NullReferenceException.
		/// </summary>
		/// <param name="c">The RGB color to locate, packed as a 32-bit integer.
		/// When converting an RGB tuple to the lookup index, the red component should
		/// be the lowest bits 0-7; the green component should be the middle bits 8-15;
		/// and the blue component should be the highest bits, 16-23.</param>
		/// <returns>The equivalent YUV color.  The Y (brightness) component will be
		/// the highest bits, 16-23; the U component will be the middle bits 8-15; and
		/// the V component will be the lowest bits, 0-7.</returns>
		[MethodImpl(Optimized)]
		public static unsafe uint Rgb2Yuv(uint c)
			=> _rgb2yuv[c & 0xFFFFFF];

		/// <summary>
		/// Construct the YUV lookup table itself.
		/// </summary>
		/// <returns>A pointer to a table with 16777216 entries, one for each possible
		/// RGB color, whose values are the YUV equivalent of that color (visually
		/// weighted).</returns>
		[MethodImpl(Optimized)]
		private static unsafe uint* MakeRgbToYuvTable()
		{
			uint* rgb2yuv = (uint*)Marshal.AllocHGlobal(sizeof(uint) * (1 << 24));

			for (int bg = -255; bg < 256; bg++)
			{
				for (int rg = -255; rg < 256; rg++)
				{
					uint u = (uint)((-169 * rg + 500 * bg) / 1000) + 128;
					uint v = (uint)((500 * rg - 81 * bg) / 1000) + 128;
					int startg = Math.Max(-bg, Math.Max(-rg, 0));
					int endg = Math.Min(255 - bg, Math.Min(255 - rg, 255));
					uint y = (uint)((299 * rg + 1000 * startg + 114 * bg) / 1000);
					uint c = (uint)(bg + rg * (1 << 16) + 0x010101 * startg);
					for (int g = startg; g <= endg; g++)
					{
						rgb2yuv[c] = ((y++) << 16) + (u << 8) + v;
						c += 0x010101;
					}
				}
			}

			return rgb2yuv;
		}
	}
}
