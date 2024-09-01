
namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// This enum is passed to the IImageLoader.LoadImage() method to ask it (if possible)
	/// to load the image as the given type.  LoadImage() is not obligated to do so (the
	/// caller is obligated to transform the image if it's not appropriate); but this
	/// flag can be used to optimize the loading process if the loader supports it.
	/// </summary>
	public enum PreferredImageType
	{
		/// <summary>
		/// The caller does not care what type is returned, and will deal with whatever
		/// it is given.
		/// </summary>
		None = 0,

		/// <summary>
		/// The caller would prefer an 8-bit paletted image.
		/// </summary>
		Image8,

		/// <summary>
		/// The caller would prefer a 24-bit RGB image.
		/// </summary>
		Image24,

		/// <summary>
		/// The caller would prefer a 32-bit RGBA image.
		/// </summary>
		Image32,
	}
}
