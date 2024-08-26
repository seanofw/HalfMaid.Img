namespace HalfMaid.Img
{
	/// <summary>
	/// Possible image color channels that can be extracted or worked with independently.
	/// </summary>
	public enum ColorChannel : byte
	{
		/// <summary>
		/// No specific color channel.
		/// </summary>
		None = 0,

		// These indices *must* match the order in which the fields of
		// the Color32 and Color24 structs are declared.

		/// <summary>
		/// The red color channel.
		/// </summary>
		Red = 1,

		/// <summary>
		/// The green color channel.
		/// </summary>
		Green = 2,

		/// <summary>
		/// The blue color channel.
		/// </summary>
		Blue = 3,

		/// <summary>
		/// The alpha color channel (32-bit RGBA colors only).
		/// </summary>
		Alpha = 4,
	}
}
