using HalfMaid.Img;

namespace ImgTest
{
	public class PaletteResult
	{
		public readonly Color32[]? StandardPalette;

		public readonly int NumColors;
		public readonly bool AdaptivePalette;
		public readonly bool IncludeAlpha;
		public readonly bool UseVisualWeighting;

		public readonly DitherMode? DitherMode;

		public PaletteResult(Color32[]? standardPalette,
			int numColors, bool adaptivePalette, bool includeAlpha, bool useVisualWeighting,
			DitherMode? ditherMode)
		{
			StandardPalette = standardPalette;
			NumColors = numColors;
			AdaptivePalette = adaptivePalette;
			IncludeAlpha = includeAlpha;
			UseVisualWeighting = useVisualWeighting;
			DitherMode = ditherMode;
		}
	}
}
