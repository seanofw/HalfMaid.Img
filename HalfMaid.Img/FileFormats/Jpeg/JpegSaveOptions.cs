namespace HalfMaid.Img.FileFormats.Jpeg
{
	/// <summary>
	/// Options that can control how a JPEG image is saved.  Reasonable defaults are
	/// applied, but you can override the saving behavior as needed.
	/// </summary>
	public class JpegSaveOptions : IFileSaveOptions
	{
		/// <summary>
		/// Quality level of the compressed JPEG image, from 1 to 100.  Default is 85 if unset.
		/// </summary>
		public int? Quality { get; set; }

		/// <summary>
		/// Whether to prefer the fast DCT (save compression time) or a more accurate DCT (improved
		/// image quality).  If unset, default is to prefer the accurate DCT.
		/// </summary>
		public bool? FastDCT { get; set; }

		/// <summary>
		/// Optimized baseline entropy coding [lossy compression only].  Optimized baseline entropy
		/// coding will improve compression slightly (generally 5% or less), but it will reduce
		/// compression performance considerably.  Default is not to optimize if unset.
		/// </summary>
		public bool? Optimize { get; set; }

		/// <summary>
		/// Enable progressive encoding.  Default if unset is to use baseline entropy encoding.
		/// </summary>
		public bool? Progressive { get; set; }

		/// <summary>
		/// Enable arithmetic coding, which can improve compression at a substantial cost in both
		/// compression and decompression time.  Default if unset is to use Huffman encoding.
		/// </summary>
		public bool? Arithmetic { get; set; }

		/// <summary>
		/// Chrominance subsampling level.  If unset, uses default 4:4:4 subsampling.
		/// </summary>
		public JpegSubsamplingMode? SubsamplingMode { get; set; }
	}
}
