namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A dithering routine that uses Burkes error-diffused dithering.
	/// </summary>
	internal class BurkesDitherer : DitherAlgorithmBase
	{
		private static readonly DitherEntry[] _burkes = new[]
		{
			new DitherEntry(+1,  0, 8),
			new DitherEntry(+2,  0, 4),
			new DitherEntry(-2, +1, 2),
			new DitherEntry(-1, +1, 4),
			new DitherEntry( 0, +1, 8),
			new DitherEntry(+1, +1, 4),
			new DitherEntry(+2, +1, 2),
		};
		private const int BurkesDivisor = 32;
		private const int BurkesShift = 5;

		public override Image8 Dither(Image32 image)
			=> DitherWithShift(image, _burkes, BurkesShift);
		public override Image8 Dither(Image24 image)
			=> DitherWithShift(image, _burkes, BurkesShift);
	}
}