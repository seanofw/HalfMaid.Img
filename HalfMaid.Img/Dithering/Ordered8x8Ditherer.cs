using System;

namespace HalfMaid.Img.Dithering
{
	internal class Ordered8x8Ditherer : OrderedDitherBase
	{
		private static readonly byte[] _thresholdMap8x8 =
		{
			 0, 48, 12, 60,  3, 51, 15, 63,
			32, 16, 44, 28, 35, 19, 47, 31,
			 8, 56,  4, 52, 11, 59,  7, 55,
			40, 24, 36, 20, 43, 27, 39, 23,
			 2, 50, 14, 62,  1, 49, 13, 61,
			34, 18, 46, 30, 33, 17, 45, 29,
			10, 58,  6, 54,  9, 57,  5, 53,
			42, 26, 38, 22, 41, 25, 37, 21,
		};

		public override Image8 Dither(Image32 image)
		{
			Image8 result = new Image8(image.Size, Palette.AsSpan());

			Span<int> mixingPlan = stackalloc int[2];

			for (int y = 0; y < image.Height; y++)
			{
				for (int x = 0; x < image.Width; x++)
				{
					Color32 color = image[x, y];

					int amount = DeviseMixingPlan(color, mixingPlan, 64);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap8x8[((y & 7) << 3) | (x & 7)];
					result[x, y] = (byte)(amount > threshold ? mixingPlan[1] : mixingPlan[0]);
				}
			}

			return result;
		}

		public override Image8 Dither(Image24 image)
		{
			Image8 result = new Image8(image.Size, Palette.AsSpan());

			Span<int> mixingPlan = stackalloc int[2];

			for (int y = 0; y < image.Height; y++)
			{
				for (int x = 0; x < image.Width; x++)
				{
					Color24 color = image[x, y];

					int amount = DeviseMixingPlan(color, mixingPlan, 64);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap8x8[((y & 7) << 3) | (x & 7)];
					result[x, y] = (byte)(amount > threshold ? mixingPlan[1] : mixingPlan[0]);
				}
			}

			return result;
		}
	}
}
