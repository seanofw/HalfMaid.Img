using System;
using System.Collections.Generic;

namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// An image-saver class, which knows how to create image files of a given type.
	/// </summary>
	public interface IImageSaver
	{
		/// <summary>
		/// The canonical image format this supports.
		/// </summary>
		ImageFormat Format { get; }

		/// <summary>
		/// A human-readable title for this file format, like "Windows Bitmap".
		/// </summary>
		string Title { get; }

		/// <summary>
		/// The default file extension to use, with the initial '.', like ".png".
		/// </summary>
		string DefaultExtension { get; }

		/// <summary>
		/// Save the given 24/32-bit RGBA image as a file of the given type.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">Options specific to the chosen file format.</param>
		/// <returns>The image converted to a file format of the given type.</returns>
		/// <exception cref="NotSupportedException">Thrown if this file format does
		/// not support RGB or RGBA images.</exception>
		byte[] SaveImage(Image32 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null);

		/// <summary>
		/// Save the given 8-bit image as a file of the given type.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to embed in the image, where supported.</param>
		/// <param name="fileSaveOptions">Options specific to the chosen file format.</param>
		/// <returns>The image converted to a file format of the given type.</returns>
		/// <exception cref="NotSupportedException">Thrown if this file format does
		/// not support 8-bit images.</exception>
		byte[] SaveImage(Image8 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null);
	}
}
