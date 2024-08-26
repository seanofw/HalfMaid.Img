namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A dithering routine that uses Stucki error-diffused dithering.
	/// </summary>
	internal class StuckiDitherer : DitherAlgorithmBase
	{
		private static readonly DitherEntry[] _stucki = new[]
		{
			new DitherEntry(+1,  0, 8),
			new DitherEntry(+2,  0, 4),
			new DitherEntry(-2, +1, 2),
			new DitherEntry(-1, +1, 4),
			new DitherEntry( 0, +1, 8),
			new DitherEntry(+1, +1, 4),
			new DitherEntry(+2, +1, 2),
			new DitherEntry(-2, +2, 1),
			new DitherEntry(-1, +2, 2),
			new DitherEntry( 0, +2, 4),
			new DitherEntry(+1, +2, 2),
			new DitherEntry(+2, +2, 1),
		};
		private const int StuckiDivisor = 42;

		public override Image8 Dither(Image32 image)
			=> DitherWithDivisor(image, _stucki, StuckiDivisor);
	}
}