namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Possible chunk kinds, as found in the raw Aseprite metafile.
	/// </summary>
	public enum AsepriteChunkKind : ushort
	{
		#pragma warning disable CS1591

		Layer        = 0x2004,
		Cel          = 0x2005,
		CelExtra     = 0x2006,
		ColorProfile = 0x2007,
		Mask         = 0x2016,
		Path         = 0x2017,
		Tags         = 0x2018,
		Palette      = 0x2019,
		UserData     = 0x2020,
		Slices       = 0x2021, // Deprecated
		Slice        = 0x2022,
		Tileset      = 0x2023,

		#pragma warning restore CS1591
	}
}
