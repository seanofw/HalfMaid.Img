using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace HalfMaid.Img
{
	/// <summary>
	/// A color quantizer based on Heckbert's median-cut algorithm.
	/// </summary>
	internal static class MedianCutQuantizer24
	{
		private const double Gamma = 2.2;
		private const double OoGamma = 1.0 / Gamma;

		/// <summary>
		/// A single bucket in the median-cut algorithm.  This has its ranges
		/// precomputed so that it can be directly inserted into the priority
		/// queue as soon as it has been created.  This is a small enough object
		/// that it's actually just a little struct, only 12 bytes long.
		/// </summary>
		private readonly struct QuantizeBucket
		{
			private class SortByRed : IComparer<(Color24 Color, int Count)>
			{
				public static readonly SortByRed Instance = new SortByRed();
				public int Compare((Color24 Color, int Count) a, (Color24 Color, int Count) b)
					=> a.Color.R - b.Color.R;
			}

			private class SortByGreen : IComparer<(Color24 Color, int Count)>
			{
				public static readonly SortByGreen Instance = new SortByGreen();
				public int Compare((Color24 Color, int Count) a, (Color24 Color, int Count) b)
					=> a.Color.G - b.Color.G;
			}

			private class SortByBlue : IComparer<(Color24 Color, int Count)>
			{
				public static readonly SortByBlue Instance = new SortByBlue();
				public int Compare((Color24 Color, int Count) a, (Color24 Color, int Count) b)
					=> a.Color.B - b.Color.B;
			}

			public readonly int Start;
			public readonly int Length;
			public readonly int RRange;
			public readonly int GRange;
			public readonly int BRange;

			public int MaxRange => Math.Max(Math.Max(RRange, GRange), BRange);

			public QuantizeBucket(int start, int length, (Color24 Color, int Count)[] histogram)
			{
				Start = start;
				Length = length;

				int minR = int.MaxValue, maxR = int.MinValue;
				int minG = int.MaxValue, maxG = int.MinValue;
				int minB = int.MaxValue, maxB = int.MinValue;

				for (int i = 0; i < length; i++)
				{
					Color24 color = histogram[start + i].Color;
					minR = Math.Min(minR, color.R);
					maxR = Math.Max(maxR, color.R);
					minG = Math.Min(minG, color.G);
					maxG = Math.Max(maxG, color.G);
					minB = Math.Min(minB, color.B);
					maxB = Math.Max(maxB, color.B);
				}

				RRange = (byte)(maxR - minR);
				GRange = (byte)(maxG - minG);
				BRange = (byte)(maxB - minB);
			}

			public IComparer<(Color24 Color, int Count)> GetSortChannel()
			{
				IComparer<(Color24 Color, int Count)> comparer = SortByRed.Instance;
				int widestRange = RRange;

				if (GRange > widestRange)
				{
					widestRange = GRange;
					comparer = SortByGreen.Instance;
				}

				if (BRange > widestRange)
					comparer = SortByBlue.Instance;

				return comparer;
			}

			public int PartitionMaxRangeAtMedian((Color24 Color, int Count)[] histogram)
			{
				// Find the channel that represents the widest range.
				IComparer<(Color24, int)> comparer = GetSortChannel();

				// Sort that bucket's colors by the chosen color channel.
				ArraySortHelper<(Color24, int)>.IntrospectiveSort(
					histogram.AsSpan().Slice(Start, Length),
					comparer);

				// Sum to find the number of pixels in this bucket.
				int totalPixels = 0;
				for (int i = 0; i < Length; i++)
					totalPixels += histogram[Start + i].Count;

				// Linear search to find the median.
				int pixelCount = 0;
				int halfPixels = totalPixels / 2;
				int median = 0;
				while ((pixelCount += histogram[Start + median].Count) < halfPixels && median < Length)
					median++;
				return median;
			}

			public override string ToString()
				=> $"Start: {Start}, Length: {Length}, RRange: {RRange}, GRange: {GRange}, BRange: {BRange}, MaxRange: {MaxRange}";
		}
		/// <summary>
		/// Generate a quantized palette of this image with at most the given number
		/// of colors in it.  This uses Heckbert's medium cut.
		/// </summary>
		/// <param name="histogram">The full histogram of the image, which may be
		/// unordered, but which must include full counts of all colors.</param>
		/// <param name="numColors">The maximum number of colors to emit in the
		/// quantized palette.</param>
		/// <param name="useOriginalColors">Whether to use only the original colors
		/// or to use other nearby colors for a better palette.</param>
		/// <returns>The palette.</returns>
		[Pure]
		public static Color24[] Quantize((Color24 Color, int Count)[] histogram,
			int numColors, bool useOriginalColors)
		{
			if (numColors < 2 || numColors > 256)
				throw new ArgumentException("Number of colors in the quantized palette must be in the range of 2 to 256.");

			if (histogram.Length < numColors)
			{
				// We can just use the image's colors directly, as they're within the
				// desired number of colors already.
				Color24[] colors = new Color24[histogram.Length];
				for (int i = 0; i < histogram.Length; i++)
					colors[i] = histogram[i].Color;
				return colors;
			}

			// Set up the initial bucket.
			PriorityQueue<QuantizeBucket, int> pq = new PriorityQueue<QuantizeBucket, int>();
			QuantizeBucket initialBucket = new QuantizeBucket(0, histogram.Length, histogram);
			pq.Enqueue(initialBucket, -initialBucket.MaxRange);

			// Repeatedly partition the worst bucket until we have numColors buckets total.
			while (pq.Count < numColors)
			{
				// The head of the priority queue is the bucket with the widest range in
				// one of its channels.
				QuantizeBucket widestBucket = pq.Peek();

				// If every bucket has uniform colors or every bucket has only one color,
				// then there's nothing left to do, and we've found the entire set of colors.
				if (widestBucket.MaxRange == 0)
					break;

				pq.Dequeue();

				// Partition the bucket into two new buckets around the median.
				int median = widestBucket.PartitionMaxRangeAtMedian(histogram) + 1;
				median = Math.Max(Math.Min(median, widestBucket.Length - 1), 1);
				QuantizeBucket bucketA = new QuantizeBucket(widestBucket.Start, median, histogram);
				pq.Enqueue(bucketA, -bucketA.MaxRange);
				if (median < histogram.Length)
				{
					QuantizeBucket bucketB = new QuantizeBucket(widestBucket.Start + median, widestBucket.Length - median, histogram);
					pq.Enqueue(bucketB, -bucketB.MaxRange);
				}
			}

			// We now have all of the buckets in the priority queue, so now we
			// just need to turn them into a good palette.
			HashSet<Color24> addedColors = new HashSet<Color24>();
			List<Color24> palette = new List<Color24>(pq.Count);
			while (pq.TryDequeue(out QuantizeBucket bucket, out var _))
			{
				Color24 c;
				if (useOriginalColors)
				{
					// Add an existing color to the palette.
					if (bucket.Length > 1)
					{
						int median = bucket.PartitionMaxRangeAtMedian(histogram);
						c = histogram[bucket.Start + median].Color;
					}
					else c = histogram[bucket.Start].Color;
				}
				else
				{
					// Find the mean of this bucket's colors.
					double sumRed = 0, sumGreen = 0, sumBlue = 0;
					for (int i = bucket.Start, end = bucket.Start + bucket.Length; i < end; i++)
					{
						c = histogram[i].Color;
						sumRed += Math.Pow(c.Rd, Gamma);
						sumGreen += Math.Pow(c.Gd, Gamma);
						sumBlue += Math.Pow(c.Bd, Gamma);
					}
					if (bucket.Length > 0)
					{
						sumRed /= bucket.Length;
						sumGreen /= bucket.Length;
						sumBlue /= bucket.Length;
					}
					c = new Color24(Math.Pow(sumRed, OoGamma),
						Math.Pow(sumGreen, OoGamma),
						Math.Pow(sumBlue, OoGamma));
				}
				if (addedColors.Add(c))
					palette.Add(c);
			}

			// Sort the resulting palette into hue buckets and then by brightness, so it's
			// at least ordered in a way that seems vaguely sensible.  After all the rest of
			// the work we've done above, this isn't much of an extra cost, and it makes for
			// much nicer results than the default "random order."
			palette.Sort((a, b) =>
			{
				(double aH, double aS, _) = a.ToHsb();
				(double bH, double bS, _) = b.ToHsb();
				int aHue = (int)(aH * 6);
				int bHue = (int)(bH * 6);
				if (aHue != bHue)
					return aHue - bHue;
				int aSat = (int)(aS * 6);
				int bSat = (int)(bS * 6);
				if (aSat != bSat)
					return aSat - bSat;
				return (a.R * 299 + a.G * 587 + a.B * 114)
				     - (b.R * 299 + b.G * 587 + b.B * 114);
			});

			return palette.ToArray();
		}
	}
}
