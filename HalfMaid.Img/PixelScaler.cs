
namespace HalfMaid.Img
{
	/// <summary>
	/// Different types of supported pixel-scaling algorithms.
	/// </summary>
	public enum PixelScaler
	{
		/// <summary>
		/// No specific pixel-scaling algorithm.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Use simple nearest-neighbor upscaling (valid for 2x, 3x, and 4x).
		/// </summary>
		NearestNeighbor,

		/// <summary>
		/// The HQX pixel-scaling algorithm (valid for 2x, 3x, and 4x).
		/// </summary>
		Hqx,
	}
}
