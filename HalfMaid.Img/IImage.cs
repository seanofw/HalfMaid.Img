using System;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// All mutable image types implement these methods.
	/// </summary>
	public interface IImage : IImageBase
	{
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
	}

	/// <summary>
	/// All mutable image types implement these methods, as well as providing direct
	/// access to the raw data.
	/// </summary>
	public interface IImage<T> : IImage, IImageBase<T>
	{
	}
}
