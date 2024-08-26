namespace HalfMaid.Img.FileFormats.Targa
{
	internal enum TargaImageType : byte
	{
		None = 0,
		Paletted = 1,
		Truecolor = 2,
		Grayscale = 3,

		Rle = 8,
		PalettedRle = Paletted | Rle,
		TruecolorRle = Truecolor | Rle,
		GrayscaleRle = Grayscale | Rle,
	}
}
