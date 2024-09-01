using System;

namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A simple ditherer that just remaps each pixel to the nearest neighbor
	/// in the palette.  Fast, but often pretty inaccurate.
	/// </summary>
	internal class NearestNeighborDitherer : DitherAlgorithmBase
	{
		public override Image8 Dither(Image32 image)
		{
			Image8 image8 = new Image8(image.Size, Palette.AsSpan());

			for (int i = 0, end = image.Width * image.Height; i < end; i++)
			{
				Color32 c = image.Data[i];
				(_, int bestIndex) = ColorSearcher.FindNearest(c);
				image8.Data[i] = (byte)bestIndex;
			}

			return image8;
		}

		public override Image8 Dither(Image24 image)
		{
			Image8 image8 = new Image8(image.Size, Palette.AsSpan());

			for (int i = 0, end = image.Width * image.Height; i < end; i++)
			{
				Color24 c = image.Data[i];
				(_, int bestIndex) = ColorSearcher.FindNearest(c);
				image8.Data[i] = (byte)bestIndex;
			}

			return image8;
		}
	}
}
