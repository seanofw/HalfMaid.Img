using System;

namespace HalfMaid.Img.Dithering
{
	internal class Ordered4x4Ditherer : OrderedDitherBase
	{
		private static readonly byte[] _thresholdMap4x4 =
		{
			  0, 12,  3, 15,
			  8,  4, 11,  7,
			  2, 14,  1, 13,
			 10,  6,  9,  5,
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

					int amount = DeviseMixingPlan(color, mixingPlan, 16);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap4x4[((y & 3) << 2) | (x & 3)];
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

					int amount = DeviseMixingPlan(color, mixingPlan, 16);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap4x4[((y & 3) << 2) | (x & 3)];
					result[x, y] = (byte)(amount > threshold ? mixingPlan[1] : mixingPlan[0]);
				}
			}

			return result;
		}
	}
}
