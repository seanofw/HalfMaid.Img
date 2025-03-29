using System;
using System.Diagnostics.Contracts;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// All mutable image types implement these methods.
	/// </summary>
	public interface IImage : IImageBase
	{
		/// <summary>
		/// Access to the pure API for this image, which is (mostly) the
		/// same as the regular API, but all operations on the image result
		/// in a new image instance.
		/// </summary>
		[Pure]
		IPureImage Pure { get; }

		/// <summary>
		/// Perform resizing to fit the given container size using the chosen fitting
		/// mode, in-place.  Fast, but can be really, really inaccurate.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		void ResizeToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling, in-place.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="imageSize">The new size of the image.</param>
		void Resize(Vector2i imageSize);

		/// <summary>
		/// Resize the image using nearest-neighbor sampling, in-place.  Fast, but can
		/// be really, really inaccurate.
		/// </summary>
		/// <param name="newWidth">The new width of the image.</param>
		/// <param name="newHeight">The new height of the image.</param>
		void Resize(int newWidth, int newHeight);

		/// <summary>
		/// Perform resampling to fit the given container size using the chosen fitting
		/// mode and resampling mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="containerSize">The container size.</param>
		/// <param name="fitMode">How to fit the image to the container.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		void ResampleToFit(Vector2i containerSize, FitMode fitMode = FitMode.Fit, ResampleMode mode = ResampleMode.BSpline);

		/// <summary>
		/// Perform resampling using the chosen mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="width">The new image width.  If omitted/null, this will be determined
		/// automatically from the given height.</param>
		/// <param name="height">The new image height.  If omitted/null, this will be determined
		/// automatically from the given width.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		void Resample(int? width = null, int? height = null, ResampleMode mode = ResampleMode.BSpline);

		/// <summary>
		/// Perform resampling using the chosen mode, in-place.  This is slower than nearest-neighbor
		/// resampling, but it can produce much higher-fidelity results.
		/// </summary>
		/// <param name="newSize">The new image size.</param>
		/// <param name="mode">The mode (sampling function) to use.  If omitted/null, this will
		/// be taken as a simple cubic B-spline.</param>
		/// <exception cref="ArgumentException">Raised if the new image size is illegal.</exception>
		void Resample(Vector2i newSize, ResampleMode mode = ResampleMode.BSpline);

		/// <summary>
		/// Vertically flip the image, in-place.
		/// </summary>
		void FlipVert();

		/// <summary>
		/// Horizontally flip the image, in-place.
		/// </summary>
		void FlipHorz();

		/// <summary>
		/// Rotate the image 90 degrees clockwise, in-place.
		/// </summary>
		void Rotate90();

		/// <summary>
		/// Rotate the image 90 degrees counterclockwise, in-place.
		/// </summary>
		void Rotate90CCW();

		/// <summary>
		/// Rotate the image 180 degrees, in-place.
		/// </summary>
		void Rotate180();

		/// <summary>
		/// Crop this image to the given rectangle.
		/// </summary>
		void Crop(int x, int y, int width, int height);

		/// <summary>
		/// Copy from src image rectangle to dest image rectangle, in-place.  This will by default clip
		/// the provided coordinates to perform a safe blit (all pixels outside an image
		/// will be ignored).
		/// </summary>
		/// <param name="srcImage">The source image to copy from.  Each pixel will be promoted to 32-bit RGBA
		/// by adding an alpha channel whose values are entirely 255.</param>
		/// <param name="srcX">The X coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="srcY">The Y coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="destX">The X coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="destY">The Y coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="width">The width of the rectangle of pixels to copy.</param>
		/// <param name="height">The height of the rectangle of pixels to copy.</param>
		/// <param name="blitFlags">Flags controlling how the copy is performed.</param>
		/// <param name="color">The color to use for color-blit modes.</param>
		void Blit(IImage srcImage, int srcX, int srcY, int destX, int destY, int width, int height,
			BlitFlags blitFlags = default, Color32 color = default);
	}

	/// <summary>
	/// All mutable image types implement these methods, as well as providing direct
	/// access to the raw data.
	/// </summary>
	public interface IImage<T> : IImage, IImageBase<T>
	{
	}
}
