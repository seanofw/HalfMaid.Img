using System;

namespace HalfMaid.Img.Dithering
{
	internal abstract class OrderedDitherBase : IDitherer
	{
		protected Color32[] Palette { get; private set; } = null!;
		protected Color32[] GammaAdjustedPalette { get; private set; } = null!;

		protected Color32Searcher ColorSearcher = null!;

		protected double Gamma = 1.0;

		public void Setup(ReadOnlySpan<Color32> palette, bool useWeightedDistances = false)
		{
			if (palette.Length > 256)
				throw new ArgumentException("Palette cannot have more than 256 colors to use a variable ordered dither.", nameof(palette));

			Palette = palette.ToArray();

			GammaAdjustedPalette = new Color32[Palette.Length];
			for (int i = 0; i < Palette.Length; i++)
			{
				GammaAdjustedPalette[i] = Palette[i].Gamma(Gamma);
			}

			ColorSearcher = new Color32Searcher(GammaAdjustedPalette, (a, b) => a.WeightedDistance(b),
				(channel, a, b) => channel switch
				{
					ColorChannel.Red => ((a.R - b.R) * (a.R - b.R)) * (0.299 / (255 * 255)),
					ColorChannel.Green => ((a.G - b.G) * (a.G - b.G)) * (0.587 / (255 * 255)),
					ColorChannel.Blue => ((a.B - b.B) * (a.B - b.B)) * (0.299 / (255 * 255)),
					ColorChannel.Alpha => ((a.A - b.A) * (a.A - b.A)) * (1.000 / (255 * 255)),
					_ => throw new ArgumentException($"Invalid channel '{channel}'"),
				});
		}

		protected int DeviseMixingPlan(Color32 color, Span<int> mixingPlan, int thresholdLimit)
		{
			color = color.Gamma(Gamma);

			(Color32 closest, int index) = ColorSearcher.FindNearest(color);

			if (closest == color)
			{
				mixingPlan[0] = index;
				mixingPlan[1] = index;
				return thresholdLimit - 1;
			}

			int balanceR = color.R * 2 - closest.R;
			int balanceG = color.G * 2 - closest.G;
			int balanceB = color.B * 2 - closest.B;
			int balanceA = color.A * 2 - closest.A;

			int altIndex = FindClosestColor(index, balanceR, balanceG, balanceB);
			Color32 alternate = GammaAdjustedPalette[altIndex];

			double closestDistance = color.WeightedDistance(closest);
			double altDistance = color.WeightedDistance(alternate);

			double totalDistance = closestDistance + altDistance;
			double ratio = closestDistance / totalDistance;
			int iRatio = (int)(ratio * thresholdLimit + 0.5);

			mixingPlan[0] = index;
			mixingPlan[1] = altIndex;
			return iRatio;
		}

		private int FindClosestColor(int skip, int targetR, int targetG, int targetB)
		{
			int bestDist = int.MaxValue;
			int index = -1;

			for (int i = 0; i < GammaAdjustedPalette.Length; i++)
			{
				if (i == skip)
					continue;
				Color32 color = GammaAdjustedPalette[i];
				int dist = (color.R - targetR) * (color.R - targetR) * 299
					+ (color.G - targetG) * (color.G - targetG) * 587
					+ (color.B - targetB) * (color.B - targetB) * 114;
				if (dist < bestDist)
				{
					bestDist = dist;
					index = i;
				}
			}

			return index;
		}

		public abstract Image8 Dither(Image32 image);
	}
}
