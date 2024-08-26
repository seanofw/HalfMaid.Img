using System;

namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// Base class for supporting various different dithering algorithms.
	/// </summary>
	internal abstract class DitherAlgorithmBase : IDitherer
	{
		#region Nested classes

        /// <summary>
        /// A single entry in an error-diffusion dither matrix.
        /// </summary>
		protected readonly struct DitherEntry
		{
			public readonly int DX;
			public readonly int DY;
			public readonly int Amount;

			public DitherEntry(int dx, int dy, int amount)
			{
				DX = dx;
				DY = dy;
				Amount = amount;
			}

			public override string ToString()
				=> $"{DX},{DY}: {Amount}";
		}

		#endregion

		#region Properties

		/// <summary>
		/// A k-d tree that can be used for efficiently locating colors.  Most
		/// dither algorithms need this as a basis, so we include it as setup.
		/// </summary>
		protected Color32Searcher ColorSearcher { get; private set; } = null!;

		/// <summary>
		/// The assigned palette.
		/// </summary>
		protected Color32[] Palette { get; private set; } = null!;

		/// <summary>
		/// Whether to use linear distances (which is usually faster) or to prefer
		/// some kind of psychovisual weighting (slower, but more accurate).
		/// </summary>
		protected bool UseWeightedDistances { get; private set; }

		#endregion

		#region Primary API

		/// <summary>
		/// Set up the ditherer to use the given color palette as its target.
		/// </summary>
		/// <param name="palette">The palette to use.</param>
		/// <param name="useWeightedDistances">Whether to use linear distances (which is
		/// usually faster) or to prefer some kind of psychovisual weighting (slower, but
		/// more accurate).</param>
		public virtual void Setup(ReadOnlySpan<Color32> palette, bool useWeightedDistances = false)
		{
			Palette = palette.ToArray();
			UseWeightedDistances = useWeightedDistances;
			ColorSearcher = new Color32Searcher(Palette, includeAlpha: false);
		}

		/// <summary>
		/// Perform dithering to 256 colors using the current color palette.
		/// </summary>
		/// <param name="image">The truecolor image to dither to the assigned palette.</param>
		/// <returns>A remapped or dithered image that uses only 8 bits per pixel and
		/// the currently-assigned color palette.</returns>
		public abstract Image8 Dither(Image32 image);

		#endregion

		#region Reusable dither mechanics

		/// <summary>
		/// Dither the given image to the given 8-bit palette using the given
		/// error diffusion model, which uses a bit-shift as a divisor.
		/// </summary>
		/// <param name="image">The image to convert to 8-bit mode.</param>
		/// <param name="ditherMatrix">The dither matrix to use for error diffusion.</param>
		/// <param name="shift">The number of bits to shift by to normalize each error value.</param>
		/// <returns>The color-reduced image.</returns>
		protected Image8 DitherWithShift(Image32 image, DitherEntry[] ditherMatrix, int shift)
		{
			Image32 copy = image.Clone();
			Color32[] data = copy.Data;

			Image8 image8 = new Image8(image.Size, Palette.AsSpan());

			for (int y = 0, yEnd = image.Height; y < yEnd; y++)
			{
				int yOffset = image.Width * y;
				for (int x = 0, xEnd = image.Width; x < xEnd; x++)
				{
					int index = yOffset + x;

					// Find the best match.
					Color32 c = data[index];
					(_, int bestIndex) = ColorSearcher.FindNearest(c);
					image8.Data[index] = (byte)bestIndex;

					// Calculate the error.
					Color32 pc = Palette[bestIndex];
					int dr = c.R - pc.R;
					int dg = c.G - pc.G;
					int db = c.B - pc.B;
					int da = c.A - pc.A;

					// Distribute the error.
					foreach (DitherEntry ditherEntry in ditherMatrix)
					{
						int nx = x + ditherEntry.DX;
						int ny = y + ditherEntry.DY;
						if (ny < yEnd && nx > 0 && nx < xEnd)
						{
							int a = ditherEntry.Amount;
							Color32 nc = data[index + ditherEntry.DX + ditherEntry.DY * image.Width];
							int nr = nc.R + (dr * a >> shift);
							int ng = nc.G + (dg * a >> shift);
							int nb = nc.B + (db * a >> shift);
							int na = nc.A + (da * a >> shift);
							data[index + ditherEntry.DX + ditherEntry.DY * image.Width] = new Color32(nr, ng, nb, na);
						}
					}
				}
			}

			return image8;
		}

		/// <summary>
		/// Dither the given image to the given 8-bit palette using the given
		/// error diffusion model, which uses a bit-shift as a divisor.
		/// </summary>
		/// <param name="image">The image to convert to 8-bit mode.</param>
		/// <param name="ditherMatrix">The dither matrix to use for error diffusion.</param>
		/// <param name="divisor">The divisor to use to normalize each error value.</param>
		/// <returns>The color-reduced image.</returns>
		protected Image8 DitherWithDivisor(Image32 image, DitherEntry[] ditherMatrix, int divisor)
		{
			Image32 copy = image.Clone();
			Color32[] data = copy.Data;

			Image8 image8 = new Image8(image.Size, Palette.AsSpan());

			for (int y = 0, yEnd = image.Height; y < yEnd; y++)
			{
				int yOffset = image.Width * y;
				for (int x = 0, xEnd = image.Width; x < xEnd; x++)
				{
					int index = yOffset + x;

					// Find the best match.
					Color32 c = data[index];
					(_, int bestIndex) = ColorSearcher.FindNearest(c);
					image8.Data[index] = (byte)bestIndex;

					// Calculate the error.
					Color32 pc = Palette[bestIndex];
					int dr = c.R - pc.R;
					int dg = c.G - pc.G;
					int db = c.B - pc.B;
					int da = c.A - pc.A;

					// Distribute the error.
					foreach (DitherEntry ditherEntry in ditherMatrix)
					{
						int nx = x + ditherEntry.DX;
						int ny = y + ditherEntry.DY;
						if (ny < yEnd && nx > 0 && nx < xEnd)
						{
							int a = ditherEntry.Amount;
							Color32 nc = data[index + ditherEntry.DX + ditherEntry.DY * image.Width];
							int nr = nc.R + dr * a / divisor;
							int ng = nc.G + dg * a / divisor;
							int nb = nc.B + db * a / divisor;
							int na = nc.A + da * a / divisor;
							data[index + ditherEntry.DX + ditherEntry.DY * image.Width] = new Color32(nr, ng, nb, na);
						}
					}
				}
			}

			return image8;
		}

		#endregion
	}
}