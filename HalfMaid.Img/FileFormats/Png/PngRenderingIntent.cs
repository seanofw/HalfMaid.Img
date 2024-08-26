namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// sRGB rendering intents, per the PNG standard.
	/// </summary>
	public enum PngRenderingIntent : byte
	{
		#pragma warning disable CS1591

		Perceptual = 0,
		RelativeColorimetric = 1,
		Saturation = 2,
		AbsoluteColorimetric = 3,

		#pragma warning restore CS1591
	}
}
