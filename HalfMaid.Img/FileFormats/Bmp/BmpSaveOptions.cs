
namespace HalfMaid.Img.FileFormats.Bmp
{
	/// <summary>
	/// Save options for BMP files.
	/// </summary>
	public class BmpSaveOptions : IFileSaveOptions
	{
		/// <summary>
		/// Whether to include the alpha channel when saving an Image32.  The default
		/// behavior is to discard the alpha channel, as many programs that support
		/// BMP can't handle a 32-bit BMP file.
		/// </summary>
		public bool IncludeAlpha { get; set; }
	}
}
