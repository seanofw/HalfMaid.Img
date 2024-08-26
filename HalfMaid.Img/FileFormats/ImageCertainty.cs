
namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// How certain a file loader is that it's found a valid image of a given type.
	/// </summary>
	public enum ImageCertainty
	{
		/// <summary>
		/// Definitely not this kind of image; try another format.
		/// </summary>
		No = 0,

		/// <summary>
		/// Might be this kind of image; it's worth trying to decode it only if
		/// another format doesn't match.
		/// </summary>
		Maybe = 1,

		/// <summary>
		/// Probably is this kind of image.  Unless another format is a definite "Yes,"
		/// this is the type to decode.
		/// </summary>
		Probably = 2,

		/// <summary>
		/// Definitely is this kind of image; don't bother looking at any other types.
		/// </summary>
		Yes = 3,
	}
}
