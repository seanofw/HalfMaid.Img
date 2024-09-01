using System;

namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// An image-loader class, which knows how to decode images of a given type.
	/// </summary>
	public interface IImageLoader
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
		/// Attempt to load file metadata from this image data.  The caller
		/// must supply the first 64 KB of the image data, but is not obligated
		/// to supply more than 64 KB.
		/// </summary>
		/// <param name="data">The raw bytes of the incoming file.</param>
		/// <returns>The metadata, if this was a match, or null if the data couldn't be decoded.</returns>
		ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data);

		/// <summary>
		/// Load the data as whatever kind of image it is.
		/// </summary>
		/// <param name="data">The file data.</param>
		/// <param name="preferredImageType">The type of image that the caller would
		/// prefer to receive, if the loader is able to do so.  The caller is still
		/// obligated to accept *any* returned image, but the loader can use this
		/// flag to optimize its loading and provide an image that avoids a possible
		/// later transformation of it.</param>
		/// <returns>The resulting Image object, or null if the data couldn't be decoded.</returns>
		ImageLoadResult? LoadImage(ReadOnlySpan<byte> data, PreferredImageType preferredImageType);

		/// <summary>
		/// Test the given filename to see if it suggests an image of this type.  Some
		/// filenames are definitive ("*.jpeg" or "*.gif"), but others can be fuzzy
		/// ("*.dat") so this will answer its best guess.
		/// </summary>
		/// <param name="filename">The filename to test.</param>
		/// <returns>How certain this loader is that the image is of the given type.</returns>
		ImageCertainty DoesNameMatch(string filename);

		/// <summary>
		/// Test the given image data to see if it suggests an image of this type.
		/// The caller must supply the first 16 KB of the image data, but is not
		/// obligated to provide *more* than 16 KB.
		/// </summary>
		/// <param name="data">The data to test.</param>
		/// <returns>How certain this loader is that the image is of the given type.</returns>
		ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data);
	}
}
