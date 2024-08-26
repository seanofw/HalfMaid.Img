namespace HalfMaid.Img
{
	/// <summary>
	/// Different ways to resize/resample an image to cover a container.
	/// </summary>
	public enum FitMode
	{
		/// <summary>
		/// Don't change the image to fit the container.
		/// </summary>
		None = 0,

		/// <summary>
		/// Stretch or shrink the image along either dimension to exactly
		/// fill the container, which may affect the image's aspect ratio.
		/// </summary>
		Stretch,

		/// <summary>
		/// Fit the image within the container such that none of the image is
		/// cut off by the container and the image's aspect ratio is preserved.
		/// This may require "black bars" around the image to completely fill
		/// the container.
		/// </summary>
		Fit,

		/// <summary>
		/// Fill the container with the image, such that the image's aspect
		/// ratio is preserved but the container is entirely covered, possibly
		/// cutting off parts of the image to fill the container.
		/// </summary>
		Fill,
	}
}