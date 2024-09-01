using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HalfMaid.Img
{
	/// <summary>
	/// Resampling is hard.  This class knows how to take an RGBA image and
	/// resample it using a variety of different resampling algorithms from a
	/// simple box filter all the way up to fancy things like Mitchell and
	/// Lanczos filters.
	/// </summary>
	internal static class ImageResampler
	{
		/// <summary>
		/// Perform resampling using the chosen mode.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.  This overwrites all
		/// pixels in the given destination image with new data.
		/// </summary>
		/// <param name="src">The source image to be resampled.</param>
		/// <param name="dest">The destination image; the source will be resampled to exactly fit it.</param>
		/// <param name="mode">The mode (sampling function) to use.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static unsafe void ResampleTo(Image32 src, Image32 dest, ResampleMode mode)
		{
			// Resample by one of a variety of different algorithms.  We use
			// the general methodology suggested by Dale Schumacher (see the book
			// Graphics Gems III).  The basic idea is that we set up a sampling
			// kernel across each line of data and determine how much of each
			// source sample factors into each destination sample; this will vary
			// depending on the source sample's position.  This is a very general
			// solution, allowing a variety of curves to be used to describe how
			// individual pixels get included.  However, for simple box (nearest)
			// filtering and triangular (linear) filtering, this solution is not as
			// efficient as hand-coded solutions, but then you didn't really want
			// to use those crappy filtering methods anyway, did you?

			if (dest.Width <= 0 || dest.Height <= 0)
				return;

			const int SentinelValue = unchecked((int)0x8BADF00D);

			ContribList* contribX = null;
			Contrib* contribDataX = null;
			ContribList* contribY = null;
			Contrib* contribDataY = null;
			Color32* temp = null;

			try
			{
				(Func<float, float> resampleFunc, float support) = _resampleFuncs[(int)mode];

				// Compute the spans (the maximum number of contributing source
				// pixels per destination pixel).
				float spanX = dest.Width < src.Width ? support * src.Width / dest.Width : support;
				float spanY = dest.Height < src.Height ? support * src.Height / dest.Height : support;

				// Allocate intermediate buffers.
				int maxHorzContribs = (int)(spanX * 2 + 3) * dest.Width;
				int maxVertContribs = (int)(spanY * 2 + 3) * dest.Height;
				contribDataX = (Contrib*)Marshal.AllocHGlobal((maxHorzContribs + 1) * sizeof(Contrib));
				contribDataY = (Contrib*)Marshal.AllocHGlobal((maxVertContribs + 1) * sizeof(Contrib));
				contribX = (ContribList*)Marshal.AllocHGlobal((dest.Width + 1) * sizeof(ContribList));
				contribY = (ContribList*)Marshal.AllocHGlobal((dest.Height + 1) * sizeof(ContribList));

				// Add some sentinels so we can be sure we haven't overrun our memory buffers.
				contribX[dest.Width].Contrib = null;
				contribX[dest.Width].NumContributions = SentinelValue;
				contribY[dest.Height].Contrib = null;
				contribY[dest.Height].NumContributions = SentinelValue;
				contribDataX[maxHorzContribs].Weight = 0;
				contribDataX[maxHorzContribs].Pixel = SentinelValue;
				contribDataY[maxVertContribs].Weight = 0;
				contribDataY[maxVertContribs].Pixel = SentinelValue;

				int tw = dest.Width, th = src.Height;
				temp = (Color32*)Marshal.AllocHGlobal(tw * th * sizeof(Color32));

				// Now it's time to do the setup.  We precalculate the contributions
				// of each pixel in the horizontal scans and vertical scans and
				// use a method similar to that of Schumacher to do it.  We can
				// precalculate this because we only need to know the relative
				// sizes of each image and the resampling method; this data has
				// nothing to do with the current pixel values.

				// Set up the contributions in each scan-line.
				SetupContributions(contribX, contribDataX, dest.Width,
					src.Width, resampleFunc, support, (ResampleMode)((int)mode >> 8));

				// Set up the contributions in each column.
				SetupContributions(contribY, contribDataY, dest.Height,
					src.Height, resampleFunc, support, mode);

				// Now that we have the contributions figured out, the rest of this
				// is (mostly) easy:  Iterate across the destination image, and
				// sum the source pixels that contribute to each destination pixel.
				// Do this once into tempimage to resolve the horizontal part of the
				// resampling; and do it again into dest to resolve the vertical.

				// Horizontal resample first.
				fixed (Color32* srcBase = src.Data)
				{
					Color32* destStart = temp;
					Color32* srcPtr = srcBase;

					for (int row = 0; row < th; row++)
					{
						Color32* destPtr = destStart;

						for (int col = 0; col < tw; col++)
						{
							// Build up the correct fractional color.
							int r = 0, g = 0, b = 0, a = 0;
							int len = contribX[col].NumContributions;
							Debug.Assert(len <= 256);
							Contrib* contribs = contribX[col].Contrib;
							for (int i = 0; i < len; i++)
							{
								int index = contribs[i].Pixel;
								Color32 color = srcPtr[index];
								int weight = contribs[i].IntWeight;
								r += color.R * weight;
								g += color.G * weight;
								b += color.B * weight;
								a += color.A * weight;
							}

							// Scale and clamp the result to [0, 255].
							byte br = ClampValue(r);
							byte bg = ClampValue(g);
							byte bb = ClampValue(b);
							byte ba = ClampValue(a);

							*destPtr++ = new Color32(br, bg, bb, ba);
						}

						// Move to the next row and do it again.
						destStart += tw;
						srcPtr += src.Width;
					}
				}

				// Okay, the horizontal resampling is done, and the horizontally-
				// resampled image is correctly stored in temp.  Now we need to do
				// the vertical resampling.
				fixed (Color32* destBase = dest.Data)
				{
					Color32* destStart = destBase;
					Color32* srcPtr = temp;
					int dw = dest.Width, dh = dest.Height;

					for (int row = 0; row < dh; row++)
					{
						Color32* destPtr = destStart;

						for (int col = 0; col < dw; col++)
						{
							// Build up the correct fractional color.
							int r = 0, g = 0, b = 0, a = 0;
							int len = contribY[row].NumContributions;
							Debug.Assert(len <= 256);
							Contrib* contribs = contribY[row].Contrib;
							for (int i = 0; i < len; i++)
							{
								int index = contribs[i].Pixel * tw + col;
								Color32 color = srcPtr[index];
								int weight = contribs[i].IntWeight;
								r += color.R * weight;
								g += color.G * weight;
								b += color.B * weight;
								a += color.A * weight;
							}

							// Scale and clamp the result to [0, 255].
							byte br = ClampValue(r);
							byte bg = ClampValue(g);
							byte bb = ClampValue(b);
							byte ba = ClampValue(a);

							*destPtr++ = new Color32(br, bg, bb, ba);
						}

						// Go to the next row and do it again.
						destStart += dw;
					}
				}

				// Critical safety check.
				if (contribX[dest.Width].NumContributions != SentinelValue
					|| contribY[dest.Height].NumContributions != SentinelValue
					|| contribDataX[maxHorzContribs].Pixel != SentinelValue
					|| contribDataY[maxVertContribs].Pixel != SentinelValue)
					throw new InvalidOperationException("Fatal error: Internal buffer overrun!");
			}
			finally
			{
				if (temp != null)
					Marshal.FreeHGlobal((IntPtr)temp);
				if (contribX != null)
					Marshal.FreeHGlobal((IntPtr)contribX);
				if (contribY != null)
					Marshal.FreeHGlobal((IntPtr)contribY);
				if (contribDataX != null)
					Marshal.FreeHGlobal((IntPtr)contribDataX);
				if (contribDataY != null)
					Marshal.FreeHGlobal((IntPtr)contribDataY);
			}
		}

		/// <summary>
		/// Perform resampling using the chosen mode.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.  This overwrites all
		/// pixels in the given destination image with new data.
		/// </summary>
		/// <param name="src">The source image to be resampled.</param>
		/// <param name="dest">The destination image; the source will be resampled to exactly fit it.</param>
		/// <param name="mode">The mode (sampling function) to use.</param>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static unsafe void ResampleTo(Image24 src, Image24 dest, ResampleMode mode)
		{
			// Resample by one of a variety of different algorithms.  We use
			// the general methodology suggested by Dale Schumacher (see the book
			// Graphics Gems III).  The basic idea is that we set up a sampling
			// kernel across each line of data and determine how much of each
			// source sample factors into each destination sample; this will vary
			// depending on the source sample's position.  This is a very general
			// solution, allowing a variety of curves to be used to describe how
			// individual pixels get included.  However, for simple box (nearest)
			// filtering and triangular (linear) filtering, this solution is not as
			// efficient as hand-coded solutions, but then you didn't really want
			// to use those crappy filtering methods anyway, did you?

			if (dest.Width <= 0 || dest.Height <= 0)
				return;

			const int SentinelValue = unchecked((int)0x8BADF00D);

			ContribList* contribX = null;
			Contrib* contribDataX = null;
			ContribList* contribY = null;
			Contrib* contribDataY = null;
			Color24* temp = null;

			try
			{
				(Func<float, float> resampleFunc, float support) = _resampleFuncs[(int)mode];

				// Compute the spans (the maximum number of contributing source
				// pixels per destination pixel).
				float spanX = dest.Width < src.Width ? support * src.Width / dest.Width : support;
				float spanY = dest.Height < src.Height ? support * src.Height / dest.Height : support;

				// Allocate intermediate buffers.
				int maxHorzContribs = (int)(spanX * 2 + 3) * dest.Width;
				int maxVertContribs = (int)(spanY * 2 + 3) * dest.Height;
				contribDataX = (Contrib*)Marshal.AllocHGlobal((maxHorzContribs + 1) * sizeof(Contrib));
				contribDataY = (Contrib*)Marshal.AllocHGlobal((maxVertContribs + 1) * sizeof(Contrib));
				contribX = (ContribList*)Marshal.AllocHGlobal((dest.Width + 1) * sizeof(ContribList));
				contribY = (ContribList*)Marshal.AllocHGlobal((dest.Height + 1) * sizeof(ContribList));

				// Add some sentinels so we can be sure we haven't overrun our memory buffers.
				contribX[dest.Width].Contrib = null;
				contribX[dest.Width].NumContributions = SentinelValue;
				contribY[dest.Height].Contrib = null;
				contribY[dest.Height].NumContributions = SentinelValue;
				contribDataX[maxHorzContribs].Weight = 0;
				contribDataX[maxHorzContribs].Pixel = SentinelValue;
				contribDataY[maxVertContribs].Weight = 0;
				contribDataY[maxVertContribs].Pixel = SentinelValue;

				int tw = dest.Width, th = src.Height;
				temp = (Color24*)Marshal.AllocHGlobal(tw * th * sizeof(Color24));

				// Now it's time to do the setup.  We precalculate the contributions
				// of each pixel in the horizontal scans and vertical scans and
				// use a method similar to that of Schumacher to do it.  We can
				// precalculate this because we only need to know the relative
				// sizes of each image and the resampling method; this data has
				// nothing to do with the current pixel values.

				// Set up the contributions in each scan-line.
				SetupContributions(contribX, contribDataX, dest.Width,
					src.Width, resampleFunc, support, (ResampleMode)((int)mode >> 8));

				// Set up the contributions in each column.
				SetupContributions(contribY, contribDataY, dest.Height,
					src.Height, resampleFunc, support, mode);

				// Now that we have the contributions figured out, the rest of this
				// is (mostly) easy:  Iterate across the destination image, and
				// sum the source pixels that contribute to each destination pixel.
				// Do this once into tempimage to resolve the horizontal part of the
				// resampling; and do it again into dest to resolve the vertical.

				// Horizontal resample first.
				fixed (Color24* srcBase = src.Data)
				{
					Color24* destStart = temp;
					Color24* srcPtr = srcBase;

					for (int row = 0; row < th; row++)
					{
						Color24* destPtr = destStart;

						for (int col = 0; col < tw; col++)
						{
							// Build up the correct fractional color.
							int r = 0, g = 0, b = 0;
							int len = contribX[col].NumContributions;
							Debug.Assert(len <= 256);
							Contrib* contribs = contribX[col].Contrib;
							for (int i = 0; i < len; i++)
							{
								int index = contribs[i].Pixel;
								Color24 color = srcPtr[index];
								int weight = contribs[i].IntWeight;
								r += color.R * weight;
								g += color.G * weight;
								b += color.B * weight;
							}

							// Scale and clamp the result to [0, 255].
							byte br = ClampValue(r);
							byte bg = ClampValue(g);
							byte bb = ClampValue(b);

							*destPtr++ = new Color24(br, bg, bb);
						}

						// Move to the next row and do it again.
						destStart += tw;
						srcPtr += src.Width;
					}
				}

				// Okay, the horizontal resampling is done, and the horizontally-
				// resampled image is correctly stored in temp.  Now we need to do
				// the vertical resampling.
				fixed (Color24* destBase = dest.Data)
				{
					Color24* destStart = destBase;
					Color24* srcPtr = temp;
					int dw = dest.Width, dh = dest.Height;

					for (int row = 0; row < dh; row++)
					{
						Color24* destPtr = destStart;

						for (int col = 0; col < dw; col++)
						{
							// Build up the correct fractional color.
							int r = 0, g = 0, b = 0;
							int len = contribY[row].NumContributions;
							Debug.Assert(len <= 256);
							Contrib* contribs = contribY[row].Contrib;
							for (int i = 0; i < len; i++)
							{
								int index = contribs[i].Pixel * tw + col;
								Color24 color = srcPtr[index];
								int weight = contribs[i].IntWeight;
								r += color.R * weight;
								g += color.G * weight;
								b += color.B * weight;
							}

							// Scale and clamp the result to [0, 255].
							byte br = ClampValue(r);
							byte bg = ClampValue(g);
							byte bb = ClampValue(b);

							*destPtr++ = new Color24(br, bg, bb);
						}

						// Go to the next row and do it again.
						destStart += dw;
					}
				}

				// Critical safety check.
				if (contribX[dest.Width].NumContributions != SentinelValue
					|| contribY[dest.Height].NumContributions != SentinelValue
					|| contribDataX[maxHorzContribs].Pixel != SentinelValue
					|| contribDataY[maxVertContribs].Pixel != SentinelValue)
					throw new InvalidOperationException("Fatal error: Internal buffer overrun!");
			}
			finally
			{
				if (temp != null)
					Marshal.FreeHGlobal((IntPtr)temp);
				if (contribX != null)
					Marshal.FreeHGlobal((IntPtr)contribX);
				if (contribY != null)
					Marshal.FreeHGlobal((IntPtr)contribY);
				if (contribDataX != null)
					Marshal.FreeHGlobal((IntPtr)contribDataX);
				if (contribDataY != null)
					Marshal.FreeHGlobal((IntPtr)contribDataY);
			}
		}

		// A set of Contrib structures jointly describe which pixels will be
		// joined to form the resulting pixel, and how much of each.  For
		// example, with triangular (linear) filtering, when we're exactly
		// doubling an image, the resulting pixel at (1,0) will be exactly 0.5
		// of the value of the source pixel at (0,0) and exactly 0.5 of the
		// value of the source pixel at (1,0).  Thus there will be two Contrib
		// structures to describe that pixel relative to that scan-line; one
		// containing (0,0.5) and the other containing (1,0.5).
		private struct Contrib
		{
			public int Pixel;              // Source pixel to use
			public float Weight;           // Weight to give it
			public int IntWeight;          // That weight converted to a 16.16 fixed-point int
		};

		// We are really resampling the image twice:  Once horizontally, and
		// once vertically.  However, unlike Schumacher's original algorithm,
		// we do not actually do it in two separate stages.  Instead, we use
		// two arrays of Contrib structures to describe the horizontal and
		// vertical contributions to each pixel, and crunch out the resulting
		// image all at once.

		private unsafe struct ContribList
		{
			public Contrib* Contrib;
			public int NumContributions;
		};

		// Set up the pixel contribution factors for a given row/column.
		private static unsafe void SetupContributions(ContribList* contrib,
			Contrib* contribData, int destSize, int srcSize,
			Func<float, float> resampleFunc, float support, ResampleMode edgeMode)
		{
			// Determine the inverse scaling factor.
			float scale = (float)srcSize / destSize;

			if (destSize > srcSize)
			{
				// We're scaling up (interpolating), so we can actually use
				// the resampling function we were provided (they really make
				// very little sense when scaling down).

				SetupContributionsForScalingUp(contrib, contribData, destSize, srcSize,
					edgeMode, scale, resampleFunc, support);
			}
			else if (resampleFunc != Box)
			{
				// We're scaling down, and they want something nicer than
				// just dropping pixels.  The Schumacher algorithms for this
				// are terrible; they may work with some kinds of signals,
				// but they don't do very well with images (for example,
				// the "filter.c" program that he provides produces near-
				// garbage when asked to scale down images using triangular
				// filtering).  We could make the various sampling functions
				// work correctly but then we'd have to also include the
				// discrete integral of each function, and the whole thing
				// would be hoary and complex (and not significantly more
				// accurate than what we're going to do here).  So instead,
				// we fall back on a bilinear filtering solution that seems
				// to produce very good results (and is compatible with the
				// methods everybody else seems to be using).

				SetupContributionsForScalingDown(contrib, contribData, destSize, srcSize,
					edgeMode, scale);
			}
			else
			{
				// We're scaling down, and this is a straightforward
				// decimation-by-dropping-pixels, so the contributions
				// for each pixel are not hard to figure out: the
				// contributing source pixel is simply whichever one is
				// at the center.

				SetupContributionsForBoxMode(contrib, contribData, destSize, scale);
			}
		}

#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private static unsafe void SetupContributionsForScalingDown(ContribList* contrib, Contrib* contribData,
			int destSize, int srcSize, ResampleMode edgeMode, float scale)
		{
			int i;
			for (i = 0; i < destSize; i++)
			{
				int contribOffset = (int)(scale * 2 + 3) * i;
				contrib[i].Contrib = contribData + contribOffset;

				float min = i * scale;
				float max = min + scale;
				int top = (int)min;
				int bottom = (int)MathF.Ceiling(max);
				float totalWeight = 0.0f;

				// Calculate which source pixels will contribute to this
				// destination pixel.
				int j, k = 0;
				for (j = top; j < bottom; j++)
				{
					int n = CalculateSourcePixelCoordinate(srcSize, edgeMode, j);

					// Each pixel is weighted solely based on its position
					// relative to the center.
					float weight = j == top ? 1.0f - (min - top)
						: j == bottom - 1 ? 1.0f - (bottom - max)
						: 1.0f;
					if (weight != 0.0f)
					{
						contribData[contribOffset + k].Pixel = n;
						contribData[contribOffset + k++].Weight = weight;
						totalWeight += weight;
					}
				}

				// Reduce the weights to the range of [0,1].
				float ooTotalWeight = 1.0f / totalWeight;
				for (j = 0; j < k; j++)
				{
					contribData[contribOffset + j].Weight *= ooTotalWeight;
					contribData[contribOffset + j].IntWeight = (int)(contribData[contribOffset + j].Weight * 65536.0f);
				}

				contrib[i].NumContributions = k;
			}
		}

#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private static unsafe void SetupContributionsForScalingUp(ContribList* contrib, Contrib* contribData,
			int destSize, int srcSize, ResampleMode edgeMode, float scale,
			Func<float, float> resampleFunc, float support)
		{
			int i;
			for (i = 0; i < destSize; i++)
			{
				int contribOffset = (int)(support * 2 + 3) * i;
				contrib[i].Contrib = contribData + contribOffset;

				float center = i * scale;
				int top = (int)(center - support);
				int bottom = (int)MathF.Ceiling(center + support);

				// Calculate which source pixels will contribute to this
				// destination pixel.
				int j, k = 0;
				for (j = top; j <= bottom; j++)
				{
					int n = CalculateSourcePixelCoordinate(srcSize, edgeMode, j);

					float weight = resampleFunc(j - center);
					if (weight != 0.0f)
					{
						contribData[contribOffset + k].Pixel = n;
						contribData[contribOffset + k].Weight = weight;
						contribData[contribOffset + k].IntWeight = (int)(contribData[contribOffset + k].Weight * 65536.0f);
						k++;
					}
				}

				contrib[i].NumContributions = k;
			}
		}

#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static int CalculateSourcePixelCoordinate(int srcSize, ResampleMode edgeMode, int x)
			=> x < 0 ? ((edgeMode & ResampleMode.TopMode) == ResampleMode.TopWrap ? x + srcSize : -x)
				: x >= srcSize ? ((edgeMode & ResampleMode.BottomMode) == ResampleMode.BottomWrap ? x - srcSize : srcSize * 2 - x - 1)
				: x;

#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		private static unsafe void SetupContributionsForBoxMode(ContribList* contrib, Contrib* contribData,
			int destSize, float scale)
		{
			int i;
			for (i = 0; i < destSize; i++)
			{
				contrib[i].Contrib = contribData + i;
				contrib[i].NumContributions = 1;
				contribData[i].Pixel = (int)(i * scale + 0.5f);
				contribData[i].Weight = 1.0f;
				contribData[i].IntWeight = 65536;
			}
		}

		/// <summary>
		/// Demote a value from [0.0, 1.0] to integer, rounding correctly, and clamp it to
		/// the range of [0, 255] (saturating).
		/// </summary>
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static byte ClampValue(int value)
		{
			value = (value + 32768) >> 16;                          // Round to nearest integer.
			if ((value & 0xFFFFFF00) == 0)
				return (byte)value;                                 // No clamping needed.
			else
				return (byte)Math.Min(Math.Max(value, 0), 255);     // Clamp to [0, 255].
		}

		///////////////////////////////////////////////////////////////////////
		//  Filtering functions.
		//    Each filtering function is a 1-dimensional function f(x) that
		//    satisfies the following four properties:
		//      1.  At 0, the function's value is +1.
		//      2.  At -inf and +inf, the function's value is 0.
		//      3.  The discrete integral of the function over [-inf, +inf]
		//           is equal to 1.
		//      4.  On [-inf, -1] and [+1, +inf], f(x) is approximately 0.
		//    Other than that, they're all different.  These functions below
		//    are the most common ones used, because (A) they are efficient
		//    to compute and (B) they seem to achieve good results.

		// The standard box (pulse, Fourier, 1st-order B-spline) function.
		// Also known as the "nearest-neighbor" function.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Box(float x)
			=> x >= -0.5f && x < 0.5f ? 1.0f
				: 0.0f;

		// The standard triangle (linear, Bartlett, 2nd-order B-spline)
		// function.  Also known as "bilinear filtering".
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Triangle(float x)
		{
			x = MathF.Abs(x);

			return x >= 1.0f ? 0.0f
				: 1.0f - x;
		}

		// The Hermite curve, a simple cubic function that looks a little
		// nicer than the triangle filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Hermite(float x)
		{
			x = MathF.Abs(x);

			return x >= 1.0f ? 0.0f
				: (x * 2.0f - 3.0f) * x * x + 1.0f;
		}

		// The Bell function (3rd-order, or quadratic B-spline).
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Bell(float x)
		{
			x = Math.Abs(x);

			if (x < 0.5f)
				return 0.75f - x * x;
			if (x >= 1.5f)
				return 0.0f;
			x -= 1.5f;
			return (x * x) * 0.5f;
		}

		// The 4th-order (cubic) B-spline.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float BSpline(float x)
		{
			x = MathF.Abs(x);

			if (x < 1.0f)
			{
				float x2 = x * x;
				return 0.5f * x2 * x - x2 + (2.0f / 3.0f);
			}
			else if (x < 2.0f)
			{
				x = 2.0f - x;
				return (1.0f / 6.0f) * x * x * x;
			}
			else return 0.0f;
		}

		// The two-parameter cubic function proposed by
		// Mitchell & Netravali (see SIGGRAPH 88).
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Mitchell(float x)
		{
			const float B = (1.0f / 3.0f);
			const float C = (1.0f / 3.0f);

			x = MathF.Abs(x);

			if (x < 1.0f)
			{
				float x2 = x * x;
				x = (((12.0f - 9.0f * B - 6.0f * C) * (x * x2))
				   + ((-18.0f + 12.0f * B + 6.0f * C) * x2)
				   + (6.0f - 2 * B));
				return x * (1.0f / 6.0f);
			}
			else if (x < 2.0f)
			{
				float x2 = x * x;
				x = (((-1.0f * B - 6.0f * C) * (x * x2))
				   + ((6.0f * B + 30.0f * C) * x2)
				   + ((-12.0f * B - 48.0f * C) * x)
				   + (8.0f * B + 24 * C));
				return x * (1.0f / 6.0f);
			}
			else return 0.0f;
		}

		// The standard sinc() function, defined as sinc(x) = sin(x * pi) / (x * pi).
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Sinc(float x)
		{
			float nx = MathF.PI * x;
			return MathF.Sin(nx) / nx;
		}

		// A three-lobed Lanczos filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Lanczos3(float x)
		{
			x = MathF.Abs(x);
			return x >= 3.0f ? 0.0f
				: x == 0.0f ? 1.0f
				: Sinc(x) * Sinc(x * (1.0f / 3.0f));
		}

		// A five-lobed Lanczos filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Lanczos5(float x)
		{
			x = MathF.Abs(x);
			return x >= 5.0f ? 0.0f
				: x == 0.0f ? 1.0f
				: Sinc(x) * Sinc(x * (1.0f / 5.0f));
		}

		// A seven-lobed Lanczos filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Lanczos7(float x)
		{
			x = MathF.Abs(x);
			return x >= 7.0f ? 0.0f
				: x == 0.0f ? 1.0f
				: Sinc(x) * Sinc(x * (1.0f / 7.0f));
		}

		// A nine-lobed Lanczos filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Lanczos9(float x)
		{
			x = MathF.Abs(x);
			return x >= 9.0f ? 0.0f
				: x == 0.0f ? 1.0f
				: Sinc(x) * Sinc(x * (1.0f / 9.0f));
		}

		// An eleven-lobed Lanczos filter.
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		[Pure]
		private static float Lanczos11(float x)
		{
			x = MathF.Abs(x);
			return x >= 11.0f ? 0.0f
				: x == 0.0f ? 1.0f
				: Sinc(x) * Sinc(x * (1.0f / 11.0f));
		}

		/// <summary>
		/// This table contains the complete list of filtering functions,
		/// as well as the required support for each function.  The support
		/// value is the width of the source data around 0 where the function's
		/// values are significant.  For finite functions, like Box and Triangle,
		/// this support value is exact; for infinite functions, like Lanczos,
		/// this value simply defines a window of the "most significant" data
		/// points.  This is in an order that must match that of the
		/// ImageResampleMode enum.
		/// </summary>
		private static readonly (Func<float, float>, float)[] _resampleFuncs = {
			(Box, 0.5f),
			(Triangle, 1.0f),
			(Hermite, 1.0f),
			(Bell, 1.5f),
			(BSpline, 2.0f),
			(Mitchell, 2.0f),
			(Lanczos3, 3.0f),
			(Lanczos5, 5.0f),
			(Lanczos7, 7.0f),
			(Lanczos9, 9.0f),
			(Lanczos11, 11.0f),
		};
	}
}
