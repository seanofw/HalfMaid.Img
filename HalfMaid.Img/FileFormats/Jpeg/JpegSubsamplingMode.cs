namespace HalfMaid.Img.FileFormats.Jpeg
{
	/// <summary>
	/// Supported subsampling modes for JPEG.  Most JPEGs don't use anything other
	/// than 4:4:4 mode, but you can specify others if you know that they are more
	/// appropriate for your use case.
	/// </summary>
	public enum JpegSubsamplingMode
	{
		/// <summary>
		/// 4:4:4 chrominance subsampling (no chrominance subsampling).  The JPEG or
		/// YUV image will contain one chrominance component for every pixel in the
		/// source image.
		/// </summary>
		Samp444 = 0,

		/// <summary>
		/// 4:2:2 chrominance subsampling.  The JPEG or YUV image will contain one
		/// chrominance component for every 2x1 block of pixels in the source image.
		/// </summary>
		Samp422 = 1,

		/// <summary>
		/// 4:2:0 chrominance subsampling.  The JPEG or YUV image will contain one
		/// chrominance component for every 2x2 block of pixels in the source image.
		/// </summary>
		Samp420 = 2,

		/// <summary>
		/// Grayscale.  The JPEG or YUV image will contain no chrominance components.
		/// </summary>
		Gray = 3,

		/// <summary>
		/// 4:4:0 chrominance subsampling.  The JPEG or YUV image will contain one
		/// chrominance component for every 1x2 block of pixels in the source image.
		/// </summary>
		Samp440 = 4,

		/// <summary>
		/// 4:1:1 chrominance subsampling.  The JPEG or YUV image will contain one
		/// chrominance component for every 4x1 block of pixels in the source image.
		/// JPEG images compressed with 4:1:1 subsampling will be almost exactly the
		/// same size as those compressed with 4:2:0 subsampling, and in the
		/// aggregate, both subsampling methods produce approximately the same
		/// perceptual quality.  However, 4:1:1 is better able to reproduce sharp
		/// horizontal features.
		/// </summary>
		Samp411 = 5,

		/// <summary>
		/// 4:4:1 chrominance subsampling.  The JPEG or YUV image will contain one
		/// chrominance component for every 1x4 block of pixels in the source image.
		/// JPEG images compressed with 4:4:1 subsampling will be almost exactly the
		/// same size as those compressed with 4:2:0 subsampling, and in the
		/// aggregate, both subsampling methods produce approximately the same
		/// perceptual quality.  However, 4:4:1 is better able to reproduce sharp
		/// vertical features.
		/// </summary>
		Samp441 = 6,

		/// <summary>
		/// Unknown subsampling.  The JPEG image uses an unusual type of chrominance
		/// subsampling.  Such images can be decompressed into packed-pixel images,
		/// but they cannot be
		/// - decompressed into planar YUV images,
		/// - losslessly transformed if #TJXOPT_CROP is specified, or
		/// - partially decompressed using a cropping region.
		/// </summary>
		Unknown = -1,
	}
}
