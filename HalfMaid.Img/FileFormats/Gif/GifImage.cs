using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// A GIF image or animation.  This is an immutable data structure.
	/// </summary>
	public class GifImage
	{
		/// <summary>
		/// The width of this image, in pixels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// The height of this image, in pixels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// The global palette for this image (shared by all frames that want to share it).
		/// </summary>
		public IReadOnlyList<Color32> GlobalPalette { get; }

		/// <summary>
		/// Optional flags controlling how this image is stored.
		/// </summary>
		public GifHeaderFlags Flags { get; }

		/// <summary>
		/// The sequence of frames for this image/animation.  Images have one frame,
		/// animations have more than one.
		/// </summary>
		public IReadOnlyList<GifFrame> Frames { get; }

		/// <summary>
		/// An optional comment for this image.
		/// </summary>
		public string? Comment { get; }

		/// <summary>
		/// How many times to repeat the animation (if at all).
		/// </summary>
		public int? RepeatCount { get; }

		/// <summary>
		/// The pixel dimensions of this image, as a Vector2i.
		/// </summary>
		public Vector2i Size => new Vector2i(Width, Height);

		/// <summary>
		/// Construct a new GIF image.
		/// </summary>
		/// <param name="width">The width of this image, in pixels.</param>
		/// <param name="height">The height of this image, in pixels.</param>
		/// <param name="globalPalette">The global palette for this image (shared by all frames that want to share it).</param>
		/// <param name="flags">Optional flags controlling how this image is stored.</param>
		/// <param name="frames">The sequence of frames for this image/animation.  Images have one frame,
		/// animations have more than one.</param>
		/// <param name="comment">An optional comment for this image.</param>
		/// <param name="repeatCount">How many times to repeat the animation (if at all).</param>
		public GifImage(int width, int height, IEnumerable<Color32> globalPalette, GifHeaderFlags flags,
			IEnumerable<GifFrame> frames, string? comment, int? repeatCount)
		{
			Width = width;
			Height = height;
			GlobalPalette = globalPalette.ToArray();
			Flags = flags;
			Frames = frames.ToArray();
			Comment = comment;
			RepeatCount = repeatCount;
		}

		private GifImage(int width, int height, IReadOnlyList<Color32> globalPalette, GifHeaderFlags flags,
			IReadOnlyList<GifFrame> frames, string? comment, int? repeatCount, bool isInternal)
		{
			Width = width;
			Height = height;
			GlobalPalette = globalPalette;
			Flags = flags;
			Frames = frames;
			Comment = comment;
			RepeatCount = repeatCount;
		}

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithWidth(int width)
			=> new GifImage(width, Height, GlobalPalette, Flags, Frames, Comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithHeight(int height)
			=> new GifImage(Width, height, GlobalPalette, Flags, Frames, Comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithPalette(IEnumerable<Color32> globalPalette)
			=> new GifImage(Width, Height, globalPalette.ToArray(), Flags, Frames, Comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithFlags(GifHeaderFlags flags)
			=> new GifImage(Width, Height, GlobalPalette, flags, Frames, Comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithFrames(IReadOnlyList<GifFrame> frames)
			=> new GifImage(Width, Height, GlobalPalette, Flags, frames, Comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithComment(string? comment)
			=> new GifImage(Width, Height, GlobalPalette, Flags, Frames, comment, RepeatCount, isInternal: true);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifImage WithRepeatCount(int? repeatCount)
			=> new GifImage(Width, Height, GlobalPalette, Flags, Frames, Comment, repeatCount, isInternal: true);
	}
}