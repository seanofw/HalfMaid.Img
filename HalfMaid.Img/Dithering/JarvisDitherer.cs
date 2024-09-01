namespace HalfMaid.Img.Dithering
{
	/// <summary>
	/// A dithering routine that uses Jarvis error-diffused dithering.
	/// </summary>
	internal class JarvisDitherer : DitherAlgorithmBase
	{
		private static readonly DitherEntry[] _jarvis = new[]
		{
			new DitherEntry(+1,  0, 7),
			new DitherEntry(+2,  0, 5),
			new DitherEntry(-2, +1, 3),
			new DitherEntry(-1, +1, 5),
			new DitherEntry( 0, +1, 7),
			new DitherEntry(+1, +1, 5),
			new DitherEntry(+2, +1, 3),
			new DitherEntry(-2, +2, 1),
			new DitherEntry(-1, +2, 3),
			new DitherEntry( 0, +2, 5),
			new DitherEntry(+1, +2, 3),
			new DitherEntry(+2, +2, 1),
		};
		private const int JarvisDivisor = 48;

		public override Image8 Dither(Image32 image)
			=> DitherWithDivisor(image, _jarvis, JarvisDivisor);
		public override Image8 Dither(Image24 image)
			=> DitherWithDivisor(image, _jarvis, JarvisDivisor);
	}
}