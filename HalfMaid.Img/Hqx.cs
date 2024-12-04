//---------------------------------------------------------------------------
//
// This is a port of Clément Bœsch's excellent clean-room implementation
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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using HalfMaid.Img.FileFormats;

namespace HalfMaid.Img
{
	/// <summary>
	/// A port of the Hqx pixel-art upsampling algorithm to C#.
	/// </summary>
	public sealed class Hqx : IDisposable
	{
		/// <summary>
		/// A lookup table from RGB colors to YUV colors.  This is 64 megabytes long
		/// so that we have a pure lookup table and don't need to do anything fancy
		/// to convert RGB to YUV:  It's a trade of memory for speed, and it's kept
		/// off-heap so as not to burden the GC (but in exchange, this class implements
		/// IDisposable).
		/// </summary>
		private static unsafe uint* _rgb2yuv;
		private static int _initCount;
		private static object _initLock = new object();
		private int _isDisposed;

		/// <summary>
		/// Construct a new instance of the Hqx pixel-art upsampling algorithm.
		/// This requires setup time, so prefer to keep an instance around if you
		/// intend to use it more than once.
		/// </summary>
		public Hqx()
		{
			unsafe
			{
				lock (_initLock)
				{
					if (_initCount++ == 0)
					{
						_rgb2yuv = MakeRgbToYuvTable();
					}
				}
			}
		}

		/// <summary>
		/// Destroy an instance of the Hqx class, releasing all of its
		/// (not-small) resources, if this is the last instance of it.
		/// </summary>
		~Hqx()
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
		///  Destroy an instance of the Hqx class, releasing all of its
		/// (not-small) resources, if this is the last instance of it.
		/// </summary>
		/// <param name="isDisposing">Whether this is being disposed by a call
		/// to Dispose() or by a call to the finalizer.</param>
		private void Dispose(bool isDisposing)
		{
			if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				unsafe
				{
					lock (_initLock)
					{
						if (_initCount-- == 0)
						{
							Marshal.FreeHGlobal((IntPtr)_rgb2yuv);
							_rgb2yuv = null;
						}
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
		/// Convert the RGB color c to YUV via the provided 'r2y' table, fast.
		/// </summary>
		/// <param name="c">The RGB color to locate, packed as a 32-bit integer.</param>
		/// <returns>The equivalent YUV color.</returns>
		[MethodImpl(Optimized)]
		private unsafe uint Rgb2Yuv(uint c)
			=> _rgb2yuv[c & 0xFFFFFF];

		/// <summary>
		/// Calculate the absolute difference between signed integers a and b:  abs(a - b).
		/// </summary>
		[MethodImpl(Optimized)]
		private static int AbsDiff(uint a, uint b)
			=> Math.Abs((int)a - (int)b);

		/// <summary>
		/// Determine whether the two provided YUV colors exceed the color-difference
		/// thresholds allowed by HQX.
		/// </summary>
		/// <param name="yuv1">The first color to compare.</param>
		/// <param name="yuv2">The second color to compare.</param>
		/// <param name="aMask">The mask to use for alpha values.</param>
		/// <returns>True if they are sufficiently similar as to be considered the same
		/// by HQX, or false if they are sufficiently different that they should be
		/// considered an edge between two color regions.</returns>
		[MethodImpl(Optimized)]
		private static bool YuvDiff(uint yuv1, uint yuv2, uint aMask)
			=> AbsDiff(yuv1 & 0xFF0000, yuv2 & 0xFF0000) > (48 << 16) ||
			   AbsDiff(yuv1 & 0x00FF00, yuv2 & 0x00FF00) > ( 7 <<  8) ||
			   AbsDiff(yuv1 & 0x0000FF, yuv2 & 0x0000FF) > ( 6 <<  0) ||
			   AbsDiff(yuv1 &    aMask, yuv2 &    aMask) > (32 << 16);

		/// <summary>
		/// Calculate (c1*w1 + c2*w2) >> s, where c1, c2, w1, and w2 represent packed
		/// color components or scaling factors.  All four color components are calculated
		/// in parallel.
		/// </summary>
		[MethodImpl(Optimized)]
		private static uint Interp2Px(uint c1, int w1, uint c2, int w2, int s)
			=> (((((c1 & 0xff00ff00) >> 8) * (uint)w1 + ((c2 & 0xff00ff00) >> 8) * (uint)w2) << (8 - s)) & 0xff00ff00) |
			   (((((c1 & 0x00ff00ff)     ) * (uint)w1 + ((c2 & 0x00ff00ff)     ) * (uint)w2) >>      s ) & 0x00ff00ff);

		/// <summary>
		/// Calculate (c1*w1 + c2*w2 + c2*w3) >> s, where c1, c2, c3, w1, w2, and w3
		/// represent packed color components or scaling factors.  All four color components
		/// are calculated in parallel.
		/// </summary>
		[MethodImpl(Optimized)]
		private static uint Interp3Px(uint c1, int w1, uint c2, int w2, uint c3, int w3, int s)
			=> (((((c1 & 0xff00ff00) >> 8) * (uint)w1 + ((c2 & 0xff00ff00) >> 8) * (uint)w2 + ((c3 & 0xff00ff00) >> 8) * (uint)w3) << (8 - s)) & 0xff00ff00) |
			   (((((c1 & 0x00ff00ff)     ) * (uint)w1 + ((c2 & 0x00ff00ff)     ) * (uint)w2 + ((c3 & 0x00ff00ff)     ) * (uint)w3) >>      s ) & 0x00ff00ff);

		/// <summary>
		/// m is the mask of diff with the center pixel that matters in the pattern, and
		/// r is the expected result (bit set to 1 if there is difference with the
		/// center, 0 otherwise)
		/// </summary>
		[MethodImpl(Optimized)]
		private static bool P(int ks, int m, int r)
			=> (ks & m) == r;

		/// <summary>
		/// Adjust bit indexes 012345678 to become 01235678: The mask shouldn't contain the
		/// (meaningless) difference between the center/current pixel and itself.
		/// </summary>
		/// <param name="z">A number from 0 to 8, inclusive.</param>
		/// <returns>The given number, if it was from 0 to 3; one less than that number
		/// if it was greater than 4.  The meaning of '4' is (formally) undefined.</returns>
		[MethodImpl(Optimized)]
		private static uint Drop4(uint z)
			=> z > 4 ? z - 1 : z;

		/// <summary>
		/// Shuffle the input mask: move bit n (4-adjusted) to position stored in p{n}.
		/// </summary>
		[MethodImpl(Optimized)]
		private static int Shuffle(int x, int rot, int n, int pn)
			=> (int)(((uint)x >> (int)(rot != 0 ? 7 - Drop4((uint)n) : Drop4((uint)n)) & 1) << (int)Drop4((uint)pn));

		/// <summary>
		/// Check if there is YUV difference between 2 pixels.
		/// </summary>
		[MethodImpl(Optimized)]
		private bool WDiff(uint c1, uint c2, uint aMask)
			=> YuvDiff(Rgb2Yuv(c1), Rgb2Yuv(c2), aMask);

		/// <summary>
		/// Assuming p0..p8 is mapped to pixels 0..8, this function interpolates the
		/// top-left pixel in the total of the 2x2 pixels to interpolate. The function
		/// is also used for the 3 other pixels.
		/// </summary>
		[MethodImpl(Optimized)]
		private unsafe uint Hq2xInterp1x1(int k, uint* w,
			int p0, int p1, int p2,
			int p3, int p4, int p5,
			int p6, int p7, int p8,
			uint aMask)
		{
			int ks = Shuffle(k, 0, 0, p0) | Shuffle(k, 0, 1, p1) | Shuffle(k, 0, 2, p2)
				   | Shuffle(k, 0, 3, p3) |           0          | Shuffle(k, 0, 5, p5)
				   | Shuffle(k, 0, 6, p6) | Shuffle(k, 0, 7, p7) | Shuffle(k, 0, 8, p8);

			uint w0 = w[p0], w1 = w[p1],
				 w3 = w[p3], w4 = w[p4], w5 = w[p5],
							 w7 = w[p7];

			if ((P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13)) && WDiff(w1, w5, aMask))
				return Interp2Px(w4, 3, w3, 1, 2);
			if ((P(ks, 0xdb, 0x49) || P(ks, 0xef, 0x6d)) && WDiff(w7, w3, aMask))
				return Interp2Px(w4, 3, w1, 1, 2);
			if ((P(ks, 0x0b, 0x0b) || P(ks, 0xfe, 0x4a) || P(ks, 0xfe, 0x1a)) && WDiff(w3, w1, aMask))
				return w4;
			if ((  P(ks, 0x6f, 0x2a) || P(ks, 0x5b, 0x0a) || P(ks, 0xbf, 0x3a) || P(ks, 0xdf, 0x5a)
				|| P(ks, 0x9f, 0x8a) || P(ks, 0xcf, 0x8a) || P(ks, 0xef, 0x4e) || P(ks, 0x3f, 0x0e)
				|| P(ks, 0xfb, 0x5a) || P(ks, 0xbb, 0x8a) || P(ks, 0x7f, 0x5a) || P(ks, 0xaf, 0x8a)
				|| P(ks, 0xeb, 0x8a)) && WDiff(w3, w1, aMask))
				return Interp2Px(w4, 3, w0, 1, 2);
			if (P(ks, 0x0b, 0x08))
				return Interp3Px(w4, 2, w0, 1, w1, 1, 2);
			if (P(ks, 0x0b, 0x02))
				return Interp3Px(w4, 2, w0, 1, w3, 1, 2);
			if (P(ks, 0x2f, 0x2f))
				return Interp3Px(w4, 14, w3, 1, w1, 1, 4);
			if (P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13))
				return Interp3Px(w4, 5, w1, 2, w3, 1, 3);
			if (P(ks, 0xdb, 0x49) || P(ks, 0xef, 0x6d))
				return Interp3Px(w4, 5, w3, 2, w1, 1, 3);
			if (P(ks, 0x1b, 0x03) || P(ks, 0x4f, 0x43) || P(ks, 0x8b, 0x83) || P(ks, 0x6b, 0x43))
				return Interp2Px(w4, 3, w3, 1, 2);
			if (P(ks, 0x4b, 0x09) || P(ks, 0x8b, 0x89) || P(ks, 0x1f, 0x19) || P(ks, 0x3b, 0x19))
				return Interp2Px(w4, 3, w1, 1, 2);
			if (P(ks, 0x7e, 0x2a) || P(ks, 0xef, 0xab) || P(ks, 0xbf, 0x8f) || P(ks, 0x7e, 0x0e))
				return Interp3Px(w4, 2, w3, 3, w1, 3, 3);
			if (   P(ks, 0xfb, 0x6a) || P(ks, 0x6f, 0x6e) || P(ks, 0x3f, 0x3e) || P(ks, 0xfb, 0xfa)
				|| P(ks, 0xdf, 0xde) || P(ks, 0xdf, 0x1e))
				return Interp2Px(w4, 3, w0, 1, 2);
			if (   P(ks, 0x0a, 0x00) || P(ks, 0x4f, 0x4b) || P(ks, 0x9f, 0x1b) || P(ks, 0x2f, 0x0b)
				|| P(ks, 0xbe, 0x0a) || P(ks, 0xee, 0x0a) || P(ks, 0x7e, 0x0a) || P(ks, 0xeb, 0x4b)
				|| P(ks, 0x3b, 0x1b))
				return Interp3Px(w4, 2, w3, 1, w1, 1, 2);
			return Interp3Px(w4, 6, w3, 1, w1, 1, 3);
		}

		/// <summary>
		/// Assuming p0..p8 is mapped to pixels 0..8, this function interpolates the
		/// top-left and top-center pixel in the total of the 3x3 pixels to
		/// interpolates. The function is also used for the 3 other couples of pixels
		/// defining the outline. The center pixel is not defined through this function,
		/// since it's just the same as the original value.
		/// </summary>
		[MethodImpl(Optimized)]
		private unsafe void Hq3xInterp2x1(uint* dst, int dst_linesize,
			int k, uint* w,
			int pos00, int pos01,
			int p0, int p1, int p2,
			int p3, int p4, int p5,
			int p6, int p7, int p8,
			int rotate, uint aMask)
		{
			int ks = Shuffle(k, rotate, 0, p0) | Shuffle(k, rotate, 1, p1) | Shuffle(k, rotate, 2, p2)
				   | Shuffle(k, rotate, 3, p3) |              0            | Shuffle(k, rotate, 5, p5)
				   | Shuffle(k, rotate, 6, p6) | Shuffle(k, rotate, 7, p7) | Shuffle(k, rotate, 8, p8);

			uint w0 = w[p0], w1 = w[p1],
				 w3 = w[p3], w4 = w[p4], w5 = w[p5],
							 w7 = w[p7];

			uint* dst00 = &dst[dst_linesize * (pos00 >> 1) + (pos00 & 1)];
			uint* dst01 = &dst[dst_linesize * (pos01 >> 1) + (pos01 & 1)];

			if ((P(ks, 0xdb, 0x49) || P(ks, 0xef, 0x6d)) && WDiff(w7, w3, aMask))
				*dst00 = Interp2Px(w4, 3, w1, 1, 2);
			else if ((P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13)) && WDiff(w1, w5, aMask))
				*dst00 = Interp2Px(w4, 3, w3, 1, 2);
			else if ((P(ks, 0x0b, 0x0b) || P(ks, 0xfe, 0x4a) || P(ks, 0xfe, 0x1a)) && WDiff(w3, w1, aMask))
				*dst00 = w4;
			else if ((P(ks, 0x6f, 0x2a) || P(ks, 0x5b, 0x0a) || P(ks, 0xbf, 0x3a) || P(ks, 0xdf, 0x5a) ||
					  P(ks, 0x9f, 0x8a) || P(ks, 0xcf, 0x8a) || P(ks, 0xef, 0x4e) || P(ks, 0x3f, 0x0e) ||
					  P(ks, 0xfb, 0x5a) || P(ks, 0xbb, 0x8a) || P(ks, 0x7f, 0x5a) || P(ks, 0xaf, 0x8a) ||
					  P(ks, 0xeb, 0x8a)) && WDiff(w3, w1, aMask))
				*dst00 = Interp2Px(w4, 3, w0, 1, 2);
			else if (P(ks, 0x4b, 0x09) || P(ks, 0x8b, 0x89) || P(ks, 0x1f, 0x19) || P(ks, 0x3b, 0x19))
				*dst00 = Interp2Px(w4, 3, w1, 1, 2);
			else if (P(ks, 0x1b, 0x03) || P(ks, 0x4f, 0x43) || P(ks, 0x8b, 0x83) || P(ks, 0x6b, 0x43))
				*dst00 = Interp2Px(w4, 3, w3, 1, 2);
			else if (P(ks, 0x7e, 0x2a) || P(ks, 0xef, 0xab) || P(ks, 0xbf, 0x8f) || P(ks, 0x7e, 0x0e))
				*dst00 = Interp2Px(w3, 1, w1, 1, 1);
			else if (P(ks, 0x4f, 0x4b) || P(ks, 0x9f, 0x1b) || P(ks, 0x2f, 0x0b) || P(ks, 0xbe, 0x0a) ||
					 P(ks, 0xee, 0x0a) || P(ks, 0x7e, 0x0a) || P(ks, 0xeb, 0x4b) || P(ks, 0x3b, 0x1b))
				*dst00 = Interp3Px(w4, 2, w3, 7, w1, 7, 4);
			else if (P(ks, 0x0b, 0x08) || P(ks, 0xf9, 0x68) || P(ks, 0xf3, 0x62) || P(ks, 0x6d, 0x6c) ||
					 P(ks, 0x67, 0x66) || P(ks, 0x3d, 0x3c) || P(ks, 0x37, 0x36) || P(ks, 0xf9, 0xf8) ||
					 P(ks, 0xdd, 0xdc) || P(ks, 0xf3, 0xf2) || P(ks, 0xd7, 0xd6) || P(ks, 0xdd, 0x1c) ||
					 P(ks, 0xd7, 0x16) || P(ks, 0x0b, 0x02))
				*dst00 = Interp2Px(w4, 3, w0, 1, 2);
			else
				*dst00 = Interp3Px(w4, 2, w3, 1, w1, 1, 2);

			if ((P(ks, 0xfe, 0xde) || P(ks, 0x9e, 0x16) || P(ks, 0xda, 0x12) || P(ks, 0x17, 0x16) ||
				 P(ks, 0x5b, 0x12) || P(ks, 0xbb, 0x12)) && WDiff(w1, w5, aMask))
				*dst01 = w4;
			else if ((P(ks, 0x0f, 0x0b) || P(ks, 0x5e, 0x0a) || P(ks, 0xfb, 0x7b) || P(ks, 0x3b, 0x0b) ||
					  P(ks, 0xbe, 0x0a) || P(ks, 0x7a, 0x0a)) && WDiff(w3, w1, aMask))
				*dst01 = w4;
			else if (P(ks, 0xbf, 0x8f) || P(ks, 0x7e, 0x0e) || P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13))
				*dst01 = Interp2Px(w1, 3, w4, 1, 2);
			else if (P(ks, 0x02, 0x00) || P(ks, 0x7c, 0x28) || P(ks, 0xed, 0xa9) || P(ks, 0xf5, 0xb4) ||
					 P(ks, 0xd9, 0x90))
				*dst01 = Interp2Px(w4, 3, w1, 1, 2);
			else if (P(ks, 0x4f, 0x4b) || P(ks, 0xfb, 0x7b) || P(ks, 0xfe, 0x7e) || P(ks, 0x9f, 0x1b) ||
					 P(ks, 0x2f, 0x0b) || P(ks, 0xbe, 0x0a) || P(ks, 0x7e, 0x0a) || P(ks, 0xfb, 0x4b) ||
					 P(ks, 0xfb, 0xdb) || P(ks, 0xfe, 0xde) || P(ks, 0xfe, 0x56) || P(ks, 0x57, 0x56) ||
					 P(ks, 0x97, 0x16) || P(ks, 0x3f, 0x1e) || P(ks, 0xdb, 0x12) || P(ks, 0xbb, 0x12))
				*dst01 = Interp2Px(w4, 7, w1, 1, 3);
			else
				*dst01 = w4;
		}

		/// <summary>
		/// Assuming p0..p8 is mapped to pixels 0..8, this function interpolates the
		/// top-left block of 2x2 pixels in the total of the 4x4 pixels(or 4 blocks) to
		/// interpolates.The function is also used for the 3 other blocks of 2x2
		/// pixels.
		/// </summary>
		[MethodImpl(Optimized)]
		private unsafe void Hq4xInterp2x2(uint* dst, int dst_linesize,
			int k, uint* w,
			int pos00, int pos01,
			int pos10, int pos11,
			int p0, int p1, int p2,
			int p3, int p4, int p5,
			int p6, int p7, int p8,
			uint aMask)
		{
			int ks = Shuffle(k, 0, 0, p0) | Shuffle(k, 0, 1, p1) | Shuffle(k, 0, 2, p2)
				   | Shuffle(k, 0, 3, p3) |           0          | Shuffle(k, 0, 5, p5)
				   | Shuffle(k, 0, 6, p6) | Shuffle(k, 0, 7, p7) | Shuffle(k, 0, 8, p8);

			uint w0 = w[p0], w1 = w[p1],
				 w3 = w[p3], w4 = w[p4], w5 = w[p5],
							 w7 = w[p7];

			uint *dst00 = &dst[dst_linesize*(pos00>>1) + (pos00&1)];
			uint *dst01 = &dst[dst_linesize*(pos01>>1) + (pos01&1)];
			uint *dst10 = &dst[dst_linesize*(pos10>>1) + (pos10&1)];
			uint *dst11 = &dst[dst_linesize*(pos11>>1) + (pos11&1)];

			bool cond00 = (P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13)) && WDiff(w1, w5, aMask);
			bool cond01 = (P(ks, 0xdb, 0x49) || P(ks, 0xef, 0x6d)) && WDiff(w7, w3, aMask);
			bool cond02 = (P(ks, 0x6f, 0x2a) || P(ks, 0x5b, 0x0a) || P(ks, 0xbf, 0x3a) ||
						   P(ks, 0xdf, 0x5a) || P(ks, 0x9f, 0x8a) || P(ks, 0xcf, 0x8a) ||
						   P(ks, 0xef, 0x4e) || P(ks, 0x3f, 0x0e) || P(ks, 0xfb, 0x5a) ||
						   P(ks, 0xbb, 0x8a) || P(ks, 0x7f, 0x5a) || P(ks, 0xaf, 0x8a) ||
						   P(ks, 0xeb, 0x8a)) && WDiff(w3, w1, aMask);
			bool cond03 = P(ks, 0xdb, 0x49) || P(ks, 0xef, 0x6d);
			bool cond04 = P(ks, 0xbf, 0x37) || P(ks, 0xdb, 0x13);
			bool cond05 = P(ks, 0x1b, 0x03) || P(ks, 0x4f, 0x43) || P(ks, 0x8b, 0x83) ||
						  P(ks, 0x6b, 0x43);
			bool cond06 = P(ks, 0x4b, 0x09) || P(ks, 0x8b, 0x89) || P(ks, 0x1f, 0x19) ||
						  P(ks, 0x3b, 0x19);
			bool cond07 = P(ks, 0x0b, 0x08) || P(ks, 0xf9, 0x68) || P(ks, 0xf3, 0x62) ||
						  P(ks, 0x6d, 0x6c) || P(ks, 0x67, 0x66) || P(ks, 0x3d, 0x3c) ||
						  P(ks, 0x37, 0x36) || P(ks, 0xf9, 0xf8) || P(ks, 0xdd, 0xdc) ||
						  P(ks, 0xf3, 0xf2) || P(ks, 0xd7, 0xd6) || P(ks, 0xdd, 0x1c) ||
						  P(ks, 0xd7, 0x16) || P(ks, 0x0b, 0x02);
			bool cond08 = (P(ks, 0x0f, 0x0b) || P(ks, 0x2b, 0x0b) || P(ks, 0xfe, 0x4a) ||
						   P(ks, 0xfe, 0x1a)) && WDiff(w3, w1, aMask);
			bool cond09 = P(ks, 0x2f, 0x2f);
			bool cond10 = P(ks, 0x0a, 0x00);
			bool cond11 = P(ks, 0x0b, 0x09);
			bool cond12 = P(ks, 0x7e, 0x2a) || P(ks, 0xef, 0xab);
			bool cond13 = P(ks, 0xbf, 0x8f) || P(ks, 0x7e, 0x0e);
			bool cond14 = P(ks, 0x4f, 0x4b) || P(ks, 0x9f, 0x1b) || P(ks, 0x2f, 0x0b) ||
						  P(ks, 0xbe, 0x0a) || P(ks, 0xee, 0x0a) || P(ks, 0x7e, 0x0a) ||
						  P(ks, 0xeb, 0x4b) || P(ks, 0x3b, 0x1b);
			bool cond15 = P(ks, 0x0b, 0x03);

			if (cond00)
				*dst00 = Interp2Px(w4, 5, w3, 3, 3);
			else if (cond01)
				*dst00 = Interp2Px(w4, 5, w1, 3, 3);
			else if ((P(ks, 0x0b, 0x0b) || P(ks, 0xfe, 0x4a) || P(ks, 0xfe, 0x1a)) && WDiff(w3, w1, aMask))
				*dst00 = w4;
			else if (cond02)
				*dst00 = Interp2Px(w4, 5, w0, 3, 3);
			else if (cond03)
				*dst00 = Interp2Px(w4, 3, w3, 1, 2);
			else if (cond04)
				*dst00 = Interp2Px(w4, 3, w1, 1, 2);
			else if (cond05)
				*dst00 = Interp2Px(w4, 5, w3, 3, 3);
			else if (cond06)
				*dst00 = Interp2Px(w4, 5, w1, 3, 3);
			else if (P(ks, 0x0f, 0x0b) || P(ks, 0x5e, 0x0a) || P(ks, 0x2b, 0x0b) || P(ks, 0xbe, 0x0a) ||
					 P(ks, 0x7a, 0x0a) || P(ks, 0xee, 0x0a))
				*dst00 = Interp2Px(w1, 1, w3, 1, 1);
			else if (cond07)
				*dst00 = Interp2Px(w4, 5, w0, 3, 3);
			else
				*dst00 = Interp3Px(w4, 2, w1, 1, w3, 1, 2);

			if (cond00)
				*dst01 = Interp2Px(w4, 7, w3, 1, 3);
			else if (cond08)
				*dst01 = w4;
			else if (cond02)
				*dst01 = Interp2Px(w4, 3, w0, 1, 2);
			else if (cond09)
				*dst01 = w4;
			else if (cond10)
				*dst01 = Interp3Px(w4, 5, w1, 2, w3, 1, 3);
			else if (P(ks, 0x0b, 0x08))
				*dst01 = Interp3Px(w4, 5, w1, 2, w0, 1, 3);
			else if (cond11)
				*dst01 = Interp2Px(w4, 5, w1, 3, 3);
			else if (cond04)
				*dst01 = Interp2Px(w1, 3, w4, 1, 2);
			else if (cond12)
				*dst01 = Interp3Px(w1, 2, w4, 1, w3, 1, 2);
			else if (cond13)
				*dst01 = Interp2Px(w1, 5, w3, 3, 3);
			else if (cond05)
				*dst01 = Interp2Px(w4, 7, w3, 1, 3);
			else if (P(ks, 0xf3, 0x62) || P(ks, 0x67, 0x66) || P(ks, 0x37, 0x36) || P(ks, 0xf3, 0xf2) ||
					 P(ks, 0xd7, 0xd6) || P(ks, 0xd7, 0x16) || P(ks, 0x0b, 0x02))
				*dst01 = Interp2Px(w4, 3, w0, 1, 2);
			else if (cond14)
				*dst01 = Interp2Px(w1, 1, w4, 1, 1);
			else
				*dst01 = Interp2Px(w4, 3, w1, 1, 2);

			if (cond01)
				*dst10 = Interp2Px(w4, 7, w1, 1, 3);
			else if (cond08)
				*dst10 = w4;
			else if (cond02)
				*dst10 = Interp2Px(w4, 3, w0, 1, 2);
			else if (cond09)
				*dst10 = w4;
			else if (cond10)
				*dst10 = Interp3Px(w4, 5, w3, 2, w1, 1, 3);
			else if (P(ks, 0x0b, 0x02))
				*dst10 = Interp3Px(w4, 5, w3, 2, w0, 1, 3);
			else if (cond15)
				*dst10 = Interp2Px(w4, 5, w3, 3, 3);
			else if (cond03)
				*dst10 = Interp2Px(w3, 3, w4, 1, 2);
			else if (cond13)
				*dst10 = Interp3Px(w3, 2, w4, 1, w1, 1, 2);
			else if (cond12)
				*dst10 = Interp2Px(w3, 5, w1, 3, 3);
			else if (cond06)
				*dst10 = Interp2Px(w4, 7, w1, 1, 3);
			else if (P(ks, 0x0b, 0x08) || P(ks, 0xf9, 0x68) || P(ks, 0x6d, 0x6c) || P(ks, 0x3d, 0x3c) ||
					 P(ks, 0xf9, 0xf8) || P(ks, 0xdd, 0xdc) || P(ks, 0xdd, 0x1c))
				*dst10 = Interp2Px(w4, 3, w0, 1, 2);
			else if (cond14)
				*dst10 = Interp2Px(w3, 1, w4, 1, 1);
			else
				*dst10 = Interp2Px(w4, 3, w3, 1, 2);

			if ((P(ks, 0x7f, 0x2b) || P(ks, 0xef, 0xab) || P(ks, 0xbf, 0x8f) || P(ks, 0x7f, 0x0f)) &&
				 WDiff(w3, w1, aMask))
				*dst11 = w4;
			else if (cond02)
				*dst11 = Interp2Px(w4, 7, w0, 1, 3);
			else if (cond15)
				*dst11 = Interp2Px(w4, 7, w3, 1, 3);
			else if (cond11)
				*dst11 = Interp2Px(w4, 7, w1, 1, 3);
			else if (P(ks, 0x0a, 0x00) || P(ks, 0x7e, 0x2a) || P(ks, 0xef, 0xab) || P(ks, 0xbf, 0x8f) ||
					 P(ks, 0x7e, 0x0e))
				*dst11 = Interp3Px(w4, 6, w3, 1, w1, 1, 3);
			else if (cond07)
				*dst11 = Interp2Px(w4, 7, w0, 1, 3);
			else
				*dst11 = w4;
		}

		/// <summary>
		/// Scale the given image to 2x its original size using the Hq2x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <returns>A new image that contains the original pixels, scaled by 2x.</returns>
		public Image24 Scale2x(PureImage24 sourceImage)
			=> Scale(sourceImage, 2);

		/// <summary>
		/// Scale the given image to 3x its original size using the Hq3x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <returns>A new image that contains the original pixels, scaled by 3x.</returns>
		public Image24 Scale3x(PureImage24 sourceImage)
			=> Scale(sourceImage, 3);

		/// <summary>
		/// Scale the given image to 4x its original size using the Hq4x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <returns>A new image that contains the original pixels, scaled by 4x.</returns>
		public Image24 Scale4x(PureImage24 sourceImage)
			=> Scale(sourceImage, 4);

		/// <summary>
		/// Scale the given image to 2x its original size using the Hq2x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <param name="includeAlpha">Whether to consider alpha differences when comparing
		/// colors (true) or to ignore the alpha channel (false).</param>
		/// <returns>A new image that contains the original pixels, scaled by 2x.</returns>
		public Image32 Scale2x(PureImage32 sourceImage, bool includeAlpha = true)
			=> Scale(sourceImage, 2, includeAlpha);

		/// <summary>
		/// Scale the given image to 3x its original size using the Hq3x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <param name="includeAlpha">Whether to consider alpha differences when comparing
		/// colors (true) or to ignore the alpha channel (false).</param>
		/// <returns>A new image that contains the original pixels, scaled by 3x.</returns>
		public Image32 Scale3x(PureImage32 sourceImage, bool includeAlpha = true)
			=> Scale(sourceImage, 3, includeAlpha);

		/// <summary>
		/// Scale the given image to 4x its original size using the Hq4x algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <param name="includeAlpha">Whether to consider alpha differences when comparing
		/// colors (true) or to ignore the alpha channel (false).</param>
		/// <returns>A new image that contains the original pixels, scaled by 4x.</returns>
		public Image32 Scale4x(PureImage32 sourceImage, bool includeAlpha = true)
			=> Scale(sourceImage, 4, includeAlpha);

		/// <summary>
		/// Scale the given image to 2x, 3x, or 4x its original size using
		/// the Hqx algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <param name="n">The scaling factor, which must be 2, 3, or 4.</param>
		/// <param name="includeAlpha">Whether to consider alpha differences when comparing
		/// colors (true) or to ignore the alpha channel (false).</param>
		/// <returns>A new image that contains the original pixels, scaled by the given amount.</returns>
		/// <exception cref="ArgumentException">Thrown if the scaling factor is not 2, 3, or 4.</exception>
		private Image32 Scale(PureImage32 sourceImage, int n, bool includeAlpha)
		{
			Image32 destImage = new Image32(sourceImage.Size * n);

			int srcHeight = sourceImage.Height;
			int srcWidth = sourceImage.Width;
			int destWidth = destImage.Width;

			uint aMask = includeAlpha ? 0xFF000000U : 0;

			unsafe
			{
				uint* w = stackalloc uint[9];

				fixed (Color32* src = sourceImage.Data)
				fixed (Color32* dst = destImage.Data)
				{
					for (int y = 0; y < srcHeight; y++)
					{
						uint* src32 = (uint*)src + y * srcWidth;
						uint* dst32 = (uint*)dst + y * destWidth * n;

						int prevline = y > 0             ? -srcWidth : 0;
						int nextline = y < srcHeight - 1 ?  srcWidth : 0;

						for (int x = 0; x < srcWidth; x++)
						{
							int prevcol = x > 0            ? -1 : 0;
							int nextcol = x < srcWidth - 1 ?  1 : 0;

							w[0] = src32[prevcol + prevline];
							w[1] = src32[          prevline];
							w[2] = src32[nextcol + prevline];
							w[3] = src32[prevcol           ];
							w[4] = src32[      0           ];
							w[5] = src32[nextcol           ];
							w[6] = src32[prevcol + nextline];
							w[7] = src32[          nextline];
							w[8] = src32[nextcol + nextline];

							if (!BitConverter.IsLittleEndian)
							{
								w[0] = w[0].BSwap();
								w[1] = w[1].BSwap();
								w[2] = w[2].BSwap();
								w[3] = w[3].BSwap();
								w[4] = w[4].BSwap();
								w[5] = w[5].BSwap();
								w[6] = w[6].BSwap();
								w[7] = w[7].BSwap();
								w[8] = w[8].BSwap();
							}

							uint yuv1 = Rgb2Yuv(w[4]);

							int pattern = (w[4] != w[0] && YuvDiff(yuv1, Rgb2Yuv(w[0]), aMask) ? 1 << 0 : 0)
										| (w[4] != w[1] && YuvDiff(yuv1, Rgb2Yuv(w[1]), aMask) ? 1 << 1 : 0)
										| (w[4] != w[2] && YuvDiff(yuv1, Rgb2Yuv(w[2]), aMask) ? 1 << 2 : 0)
										| (w[4] != w[3] && YuvDiff(yuv1, Rgb2Yuv(w[3]), aMask) ? 1 << 3 : 0)
										| (w[4] != w[5] && YuvDiff(yuv1, Rgb2Yuv(w[5]), aMask) ? 1 << 4 : 0)
										| (w[4] != w[6] && YuvDiff(yuv1, Rgb2Yuv(w[6]), aMask) ? 1 << 5 : 0)
										| (w[4] != w[7] && YuvDiff(yuv1, Rgb2Yuv(w[7]), aMask) ? 1 << 6 : 0)
										| (w[4] != w[8] && YuvDiff(yuv1, Rgb2Yuv(w[8]), aMask) ? 1 << 7 : 0);

							if (n == 2)
							{
								dst32[destWidth * 0 + 0] = Hq2xInterp1x1(pattern, w, 0, 1, 2, 3, 4, 5, 6, 7, 8, aMask);  // 00
								dst32[destWidth * 0 + 1] = Hq2xInterp1x1(pattern, w, 2, 1, 0, 5, 4, 3, 8, 7, 6, aMask);  // 01 (vert mirrored)
								dst32[destWidth * 1 + 0] = Hq2xInterp1x1(pattern, w, 6, 7, 8, 3, 4, 5, 0, 1, 2, aMask);  // 10 (horiz mirrored)
								dst32[destWidth * 1 + 1] = Hq2xInterp1x1(pattern, w, 8, 7, 6, 5, 4, 3, 2, 1, 0, aMask);  // 11 (center mirrored)
							}
							else if (n == 3)
							{
								Hq3xInterp2x1(dst32                    , destWidth, pattern, w, 0, 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, aMask);  // 00 01
								Hq3xInterp2x1(dst32 + 1                , destWidth, pattern, w, 1, 3, 2, 5, 8, 1, 4, 7, 0, 3, 6, 1, aMask);  // 02 12 (rotated to the right)
								Hq3xInterp2x1(dst32 + 1 * destWidth    , destWidth, pattern, w, 2, 0, 6, 3, 0, 7, 4, 1, 8, 5, 2, 1, aMask);  // 20 10 (rotated to the left)
								Hq3xInterp2x1(dst32 + 1 * destWidth + 1, destWidth, pattern, w, 3, 2, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, aMask);  // 22 21 (center mirrored)
								dst32[destWidth + 1] = w[4];                                                                                 // 11
							}
							else if (n == 4)
							{
								Hq4xInterp2x2(dst32                    , destWidth, pattern, w, 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 8, aMask);  // 00 01 10 11
								Hq4xInterp2x2(dst32 + 2                , destWidth, pattern, w, 1, 0, 3, 2, 2, 1, 0, 5, 4, 3, 8, 7, 6, aMask);  // 02 03 12 13 (vert mirrored)
								Hq4xInterp2x2(dst32 + 2 * destWidth    , destWidth, pattern, w, 2, 3, 0, 1, 6, 7, 8, 3, 4, 5, 0, 1, 2, aMask);  // 20 21 30 31 (horiz mirrored)
								Hq4xInterp2x2(dst32 + 2 * destWidth + 2, destWidth, pattern, w, 3, 2, 1, 0, 8, 7, 6, 5, 4, 3, 2, 1, 0, aMask);  // 22 23 32 33 (center mirrored)
							}
							else
							{
								throw new ArgumentException("Hqx can only scale 2x, 3x, or 4x the original size.", nameof(n));
							}

							src32++;
							dst32 += n;
						}
					}
				}
			}

			return destImage;
		}

		/// <summary>
		/// Scale the given image to 2x, 3x, or 4x its original size using
		/// the Hqx algorithm.
		/// </summary>
		/// <param name="sourceImage">The source image.</param>
		/// <param name="n">The scaling factor, which must be 2, 3, or 4.</param>
		/// <returns>A new image that contains the original pixels, scaled by the given amount.</returns>
		/// <exception cref="ArgumentException">Thrown if the scaling factor is not 2, 3, or 4.</exception>
		private Image24 Scale(PureImage24 sourceImage, int n)
		{
			Image24 destImage = new Image24(sourceImage.Size * n);

			int srcHeight = sourceImage.Height;
			int srcWidth = sourceImage.Width;
			int destWidth = destImage.Width;

			unsafe
			{
				uint* w = stackalloc uint[9];
				uint* tmp = stackalloc uint[16];

				fixed (Color24* srcBase = sourceImage.Data)
				fixed (Color24* dstBase = destImage.Data)
				{
					for (int y = 0; y < srcHeight; y++)
					{
						Color24* src = srcBase + y * srcWidth;
						Color24* dst = dstBase + y * destWidth * n;

						int prevline = y > 0             ? -srcWidth : 0;
						int nextline = y < srcHeight - 1 ?  srcWidth : 0;

						for (int x = 0; x < srcWidth; x++)
						{
							int prevcol = x > 0            ? -1 : 0;
							int nextcol = x < srcWidth - 1 ?  1 : 0;

							w[0] = ToUint(src[prevcol + prevline]);
							w[1] = ToUint(src[          prevline]);
							w[2] = ToUint(src[nextcol + prevline]);
							w[3] = ToUint(src[prevcol           ]);
							w[4] = ToUint(src[      0           ]);
							w[5] = ToUint(src[nextcol           ]);
							w[6] = ToUint(src[prevcol + nextline]);
							w[7] = ToUint(src[          nextline]);
							w[8] = ToUint(src[nextcol + nextline]);

							uint yuv1 = Rgb2Yuv(w[4]);

							int pattern = (w[4] != w[0] && YuvDiff(yuv1, Rgb2Yuv(w[0]), 0) ? 1 << 0 : 0)
										| (w[4] != w[1] && YuvDiff(yuv1, Rgb2Yuv(w[1]), 0) ? 1 << 1 : 0)
										| (w[4] != w[2] && YuvDiff(yuv1, Rgb2Yuv(w[2]), 0) ? 1 << 2 : 0)
										| (w[4] != w[3] && YuvDiff(yuv1, Rgb2Yuv(w[3]), 0) ? 1 << 3 : 0)
										| (w[4] != w[5] && YuvDiff(yuv1, Rgb2Yuv(w[5]), 0) ? 1 << 4 : 0)
										| (w[4] != w[6] && YuvDiff(yuv1, Rgb2Yuv(w[6]), 0) ? 1 << 5 : 0)
										| (w[4] != w[7] && YuvDiff(yuv1, Rgb2Yuv(w[7]), 0) ? 1 << 6 : 0)
										| (w[4] != w[8] && YuvDiff(yuv1, Rgb2Yuv(w[8]), 0) ? 1 << 7 : 0);

							if (n == 2)
							{
								dst[destWidth * 0 + 0] = FromUint(Hq2xInterp1x1(pattern, w, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0));  // 00
								dst[destWidth * 0 + 1] = FromUint(Hq2xInterp1x1(pattern, w, 2, 1, 0, 5, 4, 3, 8, 7, 6, 0));  // 01 (vert mirrored)
								dst[destWidth * 1 + 0] = FromUint(Hq2xInterp1x1(pattern, w, 6, 7, 8, 3, 4, 5, 0, 1, 2, 0));  // 10 (horiz mirrored)
								dst[destWidth * 1 + 1] = FromUint(Hq2xInterp1x1(pattern, w, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0));  // 11 (center mirrored)
							}
							else if (n == 3)
							{
								Hq3xInterp2x1(tmp            , 4, pattern, w, 0, 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 0);  // 00 01
								Hq3xInterp2x1(tmp + 1        , 4, pattern, w, 1, 3, 2, 5, 8, 1, 4, 7, 0, 3, 6, 1, 0);  // 02 12 (rotated to the right)
								Hq3xInterp2x1(tmp + 1 * 4    , 4, pattern, w, 2, 0, 6, 3, 0, 7, 4, 1, 8, 5, 2, 1, 0);  // 20 10 (rotated to the left)
								Hq3xInterp2x1(tmp + 1 * 4 + 1, 4, pattern, w, 3, 2, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 0);  // 22 21 (center mirrored)
								tmp[4 + 1] = w[4];                                                                     // 11
								dst[destWidth * 0 + 0] = FromUint(tmp[   0]);
								dst[destWidth * 0 + 1] = FromUint(tmp[   1]);
								dst[destWidth * 0 + 2] = FromUint(tmp[   2]);
								dst[destWidth * 1 + 0] = FromUint(tmp[ 4+0]);
								dst[destWidth * 1 + 1] = FromUint(tmp[ 4+1]);
								dst[destWidth * 1 + 2] = FromUint(tmp[ 4+2]);
								dst[destWidth * 2 + 0] = FromUint(tmp[ 8+0]);
								dst[destWidth * 2 + 1] = FromUint(tmp[ 8+1]);
								dst[destWidth * 2 + 2] = FromUint(tmp[ 8+2]);
							}
							else if (n == 4)
							{
								Hq4xInterp2x2(tmp            , 4, pattern, w, 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0);  // 00 01 10 11
								Hq4xInterp2x2(tmp + 2        , 4, pattern, w, 1, 0, 3, 2, 2, 1, 0, 5, 4, 3, 8, 7, 6, 0);  // 02 03 12 13 (vert mirrored)
								Hq4xInterp2x2(tmp + 2 * 4    , 4, pattern, w, 2, 3, 0, 1, 6, 7, 8, 3, 4, 5, 0, 1, 2, 0);  // 20 21 30 31 (horiz mirrored)
								Hq4xInterp2x2(tmp + 2 * 4 + 2, 4, pattern, w, 3, 2, 1, 0, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0);  // 22 23 32 33 (center mirrored)
								dst[destWidth * 0 + 0] = FromUint(tmp[   0]);
								dst[destWidth * 0 + 1] = FromUint(tmp[   1]);
								dst[destWidth * 0 + 2] = FromUint(tmp[   2]);
								dst[destWidth * 0 + 3] = FromUint(tmp[   3]);
								dst[destWidth * 1 + 0] = FromUint(tmp[ 4+0]);
								dst[destWidth * 1 + 1] = FromUint(tmp[ 4+1]);
								dst[destWidth * 1 + 2] = FromUint(tmp[ 4+2]);
								dst[destWidth * 1 + 3] = FromUint(tmp[ 4+3]);
								dst[destWidth * 2 + 0] = FromUint(tmp[ 8+0]);
								dst[destWidth * 2 + 1] = FromUint(tmp[ 8+1]);
								dst[destWidth * 2 + 2] = FromUint(tmp[ 8+2]);
								dst[destWidth * 2 + 3] = FromUint(tmp[ 8+3]);
								dst[destWidth * 3 + 0] = FromUint(tmp[12+0]);
								dst[destWidth * 3 + 1] = FromUint(tmp[12+1]);
								dst[destWidth * 3 + 2] = FromUint(tmp[12+2]);
								dst[destWidth * 3 + 3] = FromUint(tmp[12+3]);
							}
							else
							{
								throw new ArgumentException("Hqx can only scale 2x, 3x, or 4x the original size.", nameof(n));
							}

							src++;
							dst += n;
						}
					}
				}
			}

			return destImage;
		}

		[MethodImpl(Optimized)]
		private static uint ToUint(Color24 c)
			=> (uint)c.R | ((uint)c.G << 8) | ((uint)c.B << 16);

		[MethodImpl(Optimized)]
		private static Color24 FromUint(uint c)
			=> new Color24((byte)c, (byte)(c >> 8), (byte)(c >> 16));

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
