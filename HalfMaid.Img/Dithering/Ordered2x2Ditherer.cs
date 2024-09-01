using System;

namespace HalfMaid.Img.Dithering
{
	internal class Ordered2x2Ditherer : OrderedDitherBase
	{
		private static readonly byte[] _thresholdMap2x2 =
		{
			 0, 3,
			 2, 1,
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

					int amount = DeviseMixingPlan(color, mixingPlan, 4);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap2x2[((y & 1) << 1) | (x & 1)];
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

					int amount = DeviseMixingPlan(color, mixingPlan, 4);

					// Generate the mixing pattern of the two colors chosen.
					int threshold = _thresholdMap2x2[((y & 1) << 1) | (x & 1)];
					result[x, y] = (byte)(amount > threshold ? mixingPlan[1] : mixingPlan[0]);
				}
			}

			return result;
		}
	}
}
