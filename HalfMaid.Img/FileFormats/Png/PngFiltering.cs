using System;
using System.Runtime.CompilerServices;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// This class knows how to apply and unapply PNG filter method 0 (a.k.a., "PNG filtering")
	/// to a given dataset.
	/// </summary>
	internal static class PngFiltering
	{
		/// <summary>
		/// This skims the raw image data, applying the best filters it can to each line, emitting
		/// a new buffer of filtered data where each line is prefixed by a filter byte.
		/// </summary>
		/// <param name="data">The raw source data to filter.</param>
		/// <param name="width">The width of the source data, in pixels.</param>
		/// <param name="height">The height of the source data, in pixels.</param>
		/// <param name="bytesPerPixel">How many bytes there are for each pixel (always an
		/// integral number of bytes).</param>
		/// <param name="filterOverride">An optional override to force the filter for each
		/// line instead of calculating the best filter for each line.</param>
		/// <returns>A new array of size (width*height*bytesPerPixel + height) with filtered data in it.</returns>
		public static byte[] FilterWholeImage(ReadOnlySpan<byte> data,
			int width, int height, int bytesPerPixel, PngFilterType? filterOverride)
		{
			int srcSpan = width * bytesPerPixel;

			int destSpan = width * bytesPerPixel + 1;
			byte[] result = new byte[destSpan * height];

			// Working space.  It's important that this start out zeroed.  Each row of
			// this buffer will hold the results of a different kind of filtering, and
			// the last row will always be zero.
			byte[] temp = new byte[destSpan * (_filtersToTest.Length + 1)];
			ReadOnlySpan<byte> emptyRow = temp.AsSpan().Slice(destSpan * _filtersToTest.Length);

			for (int y = 0; y < height; y++)
			{
				Span<byte> resultRow = result.AsSpan().Slice(y * destSpan, destSpan);
				ReadOnlySpan<byte> sourceRow = data.Slice(y * srcSpan, srcSpan);
				ReadOnlySpan<byte> priorRow = y > 0 ? data.Slice((y - 1) * srcSpan, srcSpan) : emptyRow;

				if (filterOverride.HasValue)
					ApplyFilter(filterOverride.Value, int.MaxValue, resultRow, sourceRow, priorRow, width, bytesPerPixel);
				else
					ApplyBestFilter(resultRow, temp, sourceRow, priorRow, width, bytesPerPixel);
			}

			return result;
		}

		/// <summary>
		/// Possible image filters, in order of balancing computational effort
		/// to perform the filter against expected compression quality.
		/// </summary>
		private static readonly PngFilterType[] _filtersToTest = new PngFilterType[]
		{
			PngFilterType.Sub,
			PngFilterType.Up,
			PngFilterType.None,
			PngFilterType.Average,
			PngFilterType.Paeth,
		};

		/// <summary>
		/// Attempt to find the best filter type for the given row of data that
		/// will result in the best compression for that row.
		/// </summary>
		/// <param name="resultRow">The output buffer, for applying the filter into.</param>
		/// <param name="temp">A temporary buffer, big enough to hold all possible filterings for this row.</param>
		/// <param name="sourceRow">The original source row of bytes.</param>
		/// <param name="priorRow">The prior row (the row above the source row) of bytes.</param>
		/// <param name="width">The width of this row, in pixels.</param>
		/// <param name="bytesPerPixel">How many bytes there are for each pixel (always an
		/// integral number of bytes).</param>
		/// <returns>The best filter type to use for this row.</returns>
		private static PngFilterType ApplyBestFilter(Span<byte> resultRow, Span<byte> temp,
			ReadOnlySpan<byte> sourceRow, ReadOnlySpan<byte> priorRow, int width, int bytesPerPixel)
		{
			int bestScore = int.MaxValue;
			PngFilterType bestFilterType = PngFilterType.None;

			int rspan = width * bytesPerPixel + 1;

			foreach (PngFilterType filterType in _filtersToTest)
			{
				int score = ApplyFilter(filterType, bestScore,
					temp.Slice((int)filterType * rspan, rspan), sourceRow, priorRow, width, bytesPerPixel);
				if (score < bestScore)
				{
					bestScore = score;
					bestFilterType = filterType;
				}
			}

			// Copy the winning filtered row.
			temp.Slice((int)bestFilterType * rspan, rspan).CopyTo(resultRow);

			return bestFilterType;
		}

		/// <summary>
		/// Filter the given source row (and prior source row) using the given filter
		/// technique, and write the result (including the filter type itself) into the
		/// result row.
		/// </summary>
		/// <param name="filterType"></param>
		/// <param name="cutoff">A cutoff value above which the absolute-distance-from-zero
		/// heuristic is considered "bad" and this filter operation should be aborted.</param>
		/// <param name="resultRow">The output buffer.  The first byte will be a copy of
		/// the filter type, and all subsequent bytes will be filtered data from the
		/// sourceRow/priorRow.</param>
		/// <param name="sourceRow">The original source row of bytes.</param>
		/// <param name="priorRow">The prior row (the row above the source row) of bytes.</param>
		/// <param name="width">The width of this row, in pixels.</param>
		/// <param name="bytesPerPixel">How many bytes there are for each pixel (always an
		/// integral number of bytes).</param>
		/// <returns>The sum of the absolute-difference-from-zero for each of the filtered bytes.</returns>
		private static int ApplyFilter(PngFilterType filterType, int cutoff,
			Span<byte> resultRow, ReadOnlySpan<byte> sourceRow, ReadOnlySpan<byte> priorRow,
			int width, int bytesPerPixel)
		{
			byte v;
			int src = 0;

			int dest = 0;
			resultRow[dest++] = (byte)filterType;

			int bytesPerScanLine = width * bytesPerPixel;

			int sum = 0;

			switch (filterType)
			{
				default:
					throw new PngDecodeException($"Invalid PNG filter type: {(int)filterType}");

				case PngFilterType.None:
					for (int i = 0; i < bytesPerScanLine; i++, src++, dest++)
					{
						resultRow[dest] = v = sourceRow[src];
						sum += Math.Abs((int)(sbyte)v);
						if (sum >= cutoff)
							return sum;
					}
					break;

				case PngFilterType.Sub:
					for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
					{
						resultRow[dest] = v = sourceRow[src];
						sum += Math.Abs((int)(sbyte)v);
						if (sum >= cutoff)
							return sum;
					}
					for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
					{
						for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
						{
							resultRow[dest] = v = (byte)(sourceRow[src] - sourceRow[src - bytesPerPixel]);
							sum += Math.Abs((int)(sbyte)v);
							if (sum >= cutoff)
								return sum;
						}
					}
					break;

				case PngFilterType.Up:
					for (int i = 0; i < bytesPerScanLine; i += bytesPerPixel)
					{
						for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
						{
							resultRow[dest] = v = (byte)(sourceRow[src] - priorRow[src]);
							sum += Math.Abs((int)(sbyte)v);
							if (sum >= cutoff)
								return sum;
						}
					}
					break;

				case PngFilterType.Average:
					for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
					{
						resultRow[dest] = v = (byte)(
							sourceRow[src] - (priorRow[src] >> 1));
						sum += Math.Abs((int)(sbyte)v);
						if (sum >= cutoff)
							return sum;
					}
					for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
					{
						for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
						{
							resultRow[dest] = v = (byte)(
								sourceRow[src] - ((sourceRow[src - bytesPerPixel] + priorRow[src]) >> 1));
							sum += Math.Abs((int)(sbyte)v);
							if (sum >= cutoff)
								return sum;
						}
					}
					break;

				case PngFilterType.Paeth:
					for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
					{
						resultRow[dest] = v = (byte)(
							sourceRow[src] - PaethPredictor(0, priorRow[src], 0));
						sum += Math.Abs((int)(sbyte)v);
						if (sum >= cutoff)
							return sum;
					}
					for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
					{
						for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
						{
							resultRow[dest] = v = (byte)(
								sourceRow[src] - PaethPredictor(sourceRow[src - bytesPerPixel],
									priorRow[src],
									priorRow[src - bytesPerPixel]));
							sum += Math.Abs((int)(sbyte)v);
							if (sum >= cutoff)
								return sum;
						}
					}
					break;
			}

			return sum;
		}

		/// <summary>
		/// This unapplies whatever filter(s) may be used on each scan line of the image, in place.
		/// </summary>
		/// <param name="data">The image data, which is filtered when initially called,
		/// and which is unfiltered raw data upon return.</param>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="colorType">What kind of color format this image uses.</param>
		/// <param name="bitDepth">How many bits there are per color component per pixel.</param>
		/// <returns>The number of source bytes consumed by the unfiltering operation.</returns>
		public static int Unfilter(Span<byte> data, int width, int height,
			PngColorType colorType, int bitDepth)
		{
			uint colorTypeMultiplier = colorType switch
			{
				PngColorType.Grayscale => 1,
				PngColorType.Rgb => 3,
				PngColorType.Paletted => 1,
				PngColorType.GrayscaleAlpha => 2,
				PngColorType.Rgba => 4,
				_ => 0
			};
			uint bitDepthRoundedUp = ((uint)bitDepth + 7) >> 3;

			int bytesPerScanLine = (int)((uint)width * colorTypeMultiplier * bitDepth + 7) >> 3;
			int bytesPerPixel = (int)(bitDepthRoundedUp * colorTypeMultiplier);

			int src = 0, dest = 0;
			for (int y = 0; y < height; y++)
			{
				PngFilterType filterType = (PngFilterType)data[src++];
				switch (filterType)
				{
					default:
						throw new PngDecodeException($"Invalid PNG filter type: {(int)filterType}");

					case PngFilterType.None:
						for (int i = 0; i < bytesPerScanLine; i++, src++, dest++)
							data[dest] = data[src];
						break;

					case PngFilterType.Sub:
						for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
							data[dest] = data[src];
						for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
						{
							for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
								data[dest] = (byte)(data[src] + data[dest - bytesPerPixel]);
						}
						break;

					case PngFilterType.Up:
						if (y == 0)
						{
							for (int i = 0; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = data[src];
							}
						}
						else
						{
							for (int i = 0; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = (byte)(data[src] + data[dest - bytesPerScanLine]);
							}
						}
						break;

					case PngFilterType.Average:
						if (y == 0)
						{
							for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
								data[dest] = data[src];
							for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = (byte)(
										data[src] + (data[dest - bytesPerPixel] >> 1));
							}
						}
						else
						{
							for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
								data[dest] = (byte)(
									data[src] + (data[dest - bytesPerScanLine] >> 1));
							for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = (byte)(
										data[src] + ((
											  data[dest - bytesPerPixel]
											+ data[dest - bytesPerScanLine]) >> 1));
							}
						}
						break;

					case PngFilterType.Paeth:
						if (y == 0)
						{
							for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
								data[dest] = (byte)(
									data[src] + PaethPredictor(0, 0, 0));
							for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = (byte)(
										data[src] + PaethPredictor(data[dest - bytesPerPixel], 0, 0));
							}
						}
						else
						{
							for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
								data[dest] = (byte)(
									data[src] + PaethPredictor(0, data[dest - bytesPerScanLine], 0));
							for (int i = bytesPerPixel; i < bytesPerScanLine; i += bytesPerPixel)
							{
								for (int p = 0; p < bytesPerPixel; p++, src++, dest++)
									data[dest] = (byte)(
										data[src] + PaethPredictor(data[dest - bytesPerPixel],
											data[dest - bytesPerScanLine],
											data[dest - bytesPerScanLine - bytesPerPixel]));
							}
						}
						break;
				}
			}

			return src;
		}

		/// <summary>
		/// The Paeth predictor, exactly as described in the PNG documentation.
		/// </summary>
		/// <param name="a">The byte to the left.</param>
		/// <param name="b">The byte above.</param>
		/// <param name="c">The byte to the upper-left.</param>
		/// <returns>A prediction value that describes what this byte likely will be.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte PaethPredictor(byte a, byte b, byte c)
		{
			int p = a + b - c;

			int pa = Math.Abs(p - a);
			int pb = Math.Abs(p - b);
			int pc = Math.Abs(p - c);

			return pa <= pb && pa <= pc ? a
				: pb <= pc ? b
				: c;
		}
	}
}
