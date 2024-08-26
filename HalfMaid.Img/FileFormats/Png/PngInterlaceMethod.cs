namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// Possible different kinds of interlace methods that the PNG image can use.
	/// </summary>
	public enum PngInterlaceMethod : byte
	{
		/// <summary>
		/// No interlacing.
		/// </summary>
		None = 0,

		/// <summary>
		/// Adam7-style interlacing.
		/// </summary>
		Adam7 = 1,
	}
}
