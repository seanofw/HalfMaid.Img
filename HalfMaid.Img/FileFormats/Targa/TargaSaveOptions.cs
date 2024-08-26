
namespace HalfMaid.Img.FileFormats.Targa
{
	/// <summary>
	/// Options for saving images as Targa files.
	/// </summary>
	public class TargaSaveOptions : IFileSaveOptions
	{
		/// <summary>
		/// Whether to include the alpha channel when saving an Image32.
		/// </summary>
		public bool IncludeAlpha { get; set; }
	}
}
