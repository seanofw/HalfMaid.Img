namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A dithering routine that uses Floyd-Steinberg error-diffused dithering.
	/// </summary>
	internal class FloydSteinbergDitherer : DitherAlgorithmBase
	{
		private static readonly DitherEntry[] _floydSteinberg = new[]
		{
			new DitherEntry(+1,  0, 7),
			new DitherEntry(-1, +1, 3),
			new DitherEntry( 0, +1, 5),
			new DitherEntry(+1, +1, 1),
		};
		private const int FloydSteinbergDivisor = 16;
		private const int FloydSteinbergShift = 4;

		public override Image8 Dither(Image32 image)
			=> DitherWithShift(image, _floydSteinberg, FloydSteinbergShift);
		public override Image8 Dither(Image24 image)
			=> DitherWithShift(image, _floydSteinberg, FloydSteinbergShift);
	}
}