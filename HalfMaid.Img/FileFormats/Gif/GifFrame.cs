using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// A single frame of a GIF animation.  This is an immutable data structure.
	/// </summary>
	public class GifFrame
	{
		/// <summary>
		/// The image that should be displayed for this frame.
		/// </summary>
		public PureImage8 Image { get; }

		/// <summary>
		/// The horizontal coordinate where this frame's image should be rendered, in pixels
		/// relative to the upper left of the overall image.
		/// </summary>
		public int X { get; }

		/// <summary>
		/// The vertical coordinate where this frame's image should be rendered, in pixels
		/// relative to the upper left of the overall image.
		/// </summary>
		public int Y { get; }

		/// <summary>
		/// The width of this frame, in pixels.
		/// </summary>
		public int Width => Image.Width;

		/// <summary>
		/// The height of this frame, in pixels.
		/// </summary>
		public int Height => Image.Height;

		/// <summary>
		/// Whether this frame has a local palette (true), or shares the global palette (false).
		/// </summary>
		public bool HasLocalPalette { get; }

		/// <summary>
		/// Optional flags controlling the frame rendering.
		/// </summary>
		public GifControlBlockFlags Flags { get; }

		/// <summary>
		/// The palette index for this frame's transparent color (if enabled, per the flags).
		/// </summary>
		public byte TransparentColorIndex { get; }

		/// <summary>
		/// How long to display this frame, in 1/100ths of a second.
		/// </summary>
		public ushort FrameDelay { get; }

		/// <summary>
		/// How long to display this frame, as a TimeSpan.
		/// </summary>
		public TimeSpan Duration => new TimeSpan(0, 0, 0, 0, FrameDelay * 10);

		/// <summary>
		/// Construct a new frame.
		/// </summary>
		/// <param name="image">The image that should be displayed for this frame.</param>
		/// <param name="x">The horizontal coordinate where this frame's image should be rendered, in pixels
		/// relative to the upper left of the overall image.</param>
		/// <param name="y">The vertical coordinate where this frame's image should be rendered, in pixels
		/// relative to the upper left of the overall image.</param>
		/// <param name="flags">Optional flags controlling the frame rendering.</param>
		/// <param name="transparentColorIndex">The palette index for this frame's transparent color (if enabled, per the flags).</param>
		/// <param name="frameDelay">How long to display this frame, in 1/100ths of a second.</param>
		/// <param name="hasLocalPalette">Whether this frame has a local palette (true), or shares the global palette (false).</param>
		public GifFrame(PureImage8 image, int x, int y,
			GifControlBlockFlags flags, int transparentColorIndex, int frameDelay, bool hasLocalPalette)
		{
			Image = image;
			X = x;
			Y = y;
			Flags = flags;
			TransparentColorIndex = (byte)Math.Min(Math.Max(transparentColorIndex, 0), 255);
			FrameDelay = (ushort)Math.Min(Math.Max(frameDelay, 0), ushort.MaxValue);
			HasLocalPalette = hasLocalPalette;
		}

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithImage(PureImage8 image)
			=> new GifFrame(image, X, Y, Flags, TransparentColorIndex, FrameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithX(int x)
			=> new GifFrame(Image, x, Y, Flags, TransparentColorIndex, FrameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithY(int y)
			=> new GifFrame(Image, X, y, Flags, TransparentColorIndex, FrameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithFlags(GifControlBlockFlags flags)
			=> new GifFrame(Image, X, Y, flags, TransparentColorIndex, FrameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithTransparentColorIndex(int transparentColorIndex)
			=> new GifFrame(Image, X, Y, Flags, transparentColorIndex, FrameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithFrameDelay(int frameDelay)
			=> new GifFrame(Image, X, Y, Flags, TransparentColorIndex, frameDelay, HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithDuration(TimeSpan duration)
			=> new GifFrame(Image, X, Y, Flags, TransparentColorIndex, (int)(duration.TotalMilliseconds * 0.1 + 0.5), HasLocalPalette);

		/// <summary>
		/// Copy this object, replacing one property.
		/// </summary>
		public GifFrame WithHasLocalPalette(bool hasLocalPalette)
			=> new GifFrame(Image, X, Y, Flags, TransparentColorIndex, FrameDelay, hasLocalPalette);

		/// <summary>
		/// Convert this frame to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"frame size {Width},{Height} at {X},{Y} for {FrameDelay * 10} msec then {Flags & GifControlBlockFlags.RemoveMask}";
	}
}
