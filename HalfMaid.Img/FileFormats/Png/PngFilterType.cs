namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// Available filter types for standard PNG filtering.
	/// </summary>
	public enum PngFilterType : byte
	{
		/// <summary>
		/// This scan line does not use filtering and consists only of raw bytes.
		/// </summary>
		None = 0,

		/// <summary>
		/// This scan line stores the difference between successive pixels horizontally.
		/// </summary>
		Sub = 1,

		/// <summary>
		/// This scan line stores the difference between successive pixels vertically.
		/// </summary>
		Up = 2,

		/// <summary>
		/// This scan line stores the difference between the average of the pixel above
		/// and the pixel to the left.
		/// </summary>
		Average = 3,

		/// <summary>
		/// This scan line uses the "Paeth predictor" to estimate probabilities for each pixel.
		/// </summary>
		Paeth = 4,
	}
}
