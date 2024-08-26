namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Different ways layers can be blended together when rendering an Aseprite image.
	/// </summary>
	public enum AsepriteBlendMode : ushort
	{
		#pragma warning disable CS1591

		Normal = 0,
		Multiply = 1,
		Screen = 2,
		Overlay = 3,
		Darken = 4,
		Lighten = 5,
		ColorDodge = 6,
		ColorBurn = 7,
		HardLight = 8,
		SoftLight = 9,
		Difference = 10,
		Exclusion = 11,
		Hue = 12,
		Saturation = 13,
		Color = 14,
		Luminosity = 15,
		Addition = 16,
		Subtract = 17,
		Divide = 18,

		#pragma warning restore CS1591
	}
}
