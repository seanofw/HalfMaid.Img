namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A dithering routine that uses Bill Atkinson's error-diffused dithering.
	/// </summary>
	internal class AtkinsonDitherer : DitherAlgorithmBase
	{
		private static readonly DitherEntry[] _atkinson = new[]
		{
			new DitherEntry(+1,  0, 1),
			new DitherEntry(+2,  0, 1),
			new DitherEntry(-1, +1, 1),
			new DitherEntry( 0, +1, 1),
			new DitherEntry(+1, +1, 1),
			new DitherEntry( 0, +2, 1),
		};
		private const int AtkinsonDivisor = 8;
		private const int AtkinsonShift = 3;

		public override Image8 Dither(Image32 image)
			=> DitherWithShift(image, _atkinson, AtkinsonShift);
	}
}