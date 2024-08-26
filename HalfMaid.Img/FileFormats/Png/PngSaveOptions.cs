using System.IO.Compression;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// Options to control how a PNG file is written.
	/// </summary>
	public class PngSaveOptions : IFileSaveOptions
	{
		/// <summary>
		/// The level of Deflate compression to use.  Since we use System.IO.Compression,
		/// the compression levels available are the same ones it offers.
		/// </summary>
		public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

		/// <summary>
		/// Whether to include alpha when given a 32-bit Image32 to save, or whether to
		/// save it only as a 24-bit representation (i.e., this selects between RGB and
		/// RGBA color types).  If given an Image8, this will control whether the palette
		/// is saved as 24-bit or as 32-bit data (i.e., this controls whether a tRNS chunk
		/// is written for the palette).
		/// </summary>
		public bool IncludeAlpha { get; set; } = false;

		/// <summary>
		/// An optional override to control the filter type.  By default, the saver
		/// will use heuristics to decide which filter type to use on each line, but
		/// if you know that a specific filter type will work best on this image, you
		/// can override the filter type here.
		/// </summary>
		public PngFilterType? FilterType { get; set; } = null;
	}
}
