using System.Collections.Generic;
using OpenTK.Mathematics;

namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// The result of attempting to load an image file, which should be either
	/// a 24/32-bit RGBA image or an 8-bit image with a palette.  Only one of
	/// 'Image' or 'Image8' will be populated here; the other image type will
	/// be set to null.
	/// </summary>
	public class ImageLoadResult
	{
		/// <summary>
		/// The original color format on disk that this image used.
		/// </summary>
		public ImageFileColorFormat ColorFormat { get; }

		/// <summary>
		/// The dimensions of this image, in pixels.
		/// </summary>
		public Vector2i Size { get; }

		/// <summary>
		/// The actual decoded image.
		/// </summary>
		public IImage? Image { get; }

		/// <summary>
		/// Optional metadata for the image, which varies depending on the file format.
		/// </summary>
		public IReadOnlyDictionary<string, object> Metadata { get; }

		/// <summary>
		/// Construct an image-load result object.
		/// </summary>
		/// <param name="colorFormat">The format of the file that was loaded.</param>
		/// <param name="size">The pixel size of the image that was loaded.</param>
		/// <param name="image">The decoded image.</param>
		/// <param name="metadata">Optional metadata for the image, which varies depending on the file format.</param>
		public ImageLoadResult(ImageFileColorFormat colorFormat, Vector2i size,
			IImage? image = null, IReadOnlyDictionary<string, object>? metadata = null)
		{
			ColorFormat = colorFormat;
			Size = size;
			Image = image;
			Metadata = metadata ?? new Dictionary<string, object>();
		}
	}
}
