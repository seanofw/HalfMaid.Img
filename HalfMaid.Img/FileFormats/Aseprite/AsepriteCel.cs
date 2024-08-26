using System;
using System.Diagnostics;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// A single cel.  This is a rectangle of pixels, the lowest-level construct
	/// in an Aseprite file:  Frames contain layers; layers contain cels.
	/// </summary>
	[DebuggerDisplay("Cel: {Kind} color, {Width}x{Height} at ({X},{Y})")]
	public class AsepriteCel
	{
		/// <summary>
		/// The frame this cel belongs to.
		/// </summary>
		public AsepriteFrame Frame { get; }

		/// <summary>
		/// The layer this cel belongs to.
		/// </summary>
		public AsepriteImageLayer Layer { get; internal set; }

		/// <summary>
		/// The Z-index of the layer on which this cel is found.  0 is the
		/// farthest from the viewer, and other layers stack on top of it.
		/// </summary>
		public ushort LayerIndex { get; }

		/// <summary>
		/// The leftmost coordinate where this cel is rendered, in pixels, relative to the canvas.
		/// </summary>
		public short X { get; }

		/// <summary>
		/// The topmost coordinate where this cel is rendered, in pixels, relative to the canvas.
		/// </summary>
		public short Y { get; }

		/// <summary>
		/// The width of this cel, in pixels.
		/// </summary>
		public short Width { get; }

		/// <summary>
		/// The height of this cel, in pixels.
		/// </summary>
		public short Height { get; }

		/// <summary>
		/// How opaque this cel is, from 255 (opaque) to 0 (fully transparent).
		/// </summary>
		public byte Opacity { get; }

		/// <summary>
		/// What kind of compression this cel data uses.
		/// </summary>
		public AsepriteCelKind Kind { get; }

		/// <summary>
		/// The cel's image, if it's an 8-bit paletted sprite.
		/// </summary>
		public Image8? Image8 { get; }

		/// <summary>
		/// The cel's image, cropped/resized to fit the sprite's canvas.
		/// </summary>
		public Image8? CanvasImage8 => Image8 != null ? (_canvasImage8 ??= GetClippedImage8(Image8.Palette)) : null;
		private Image8? _canvasImage8;

		/// <summary>
		/// The cel's image, if it's a 32-bit RGBA sprite.
		/// </summary>
		public Image32? Image32 { get; }

		/// <summary>
		/// The cel's image, cropped/resized to fit the sprite's canvas.
		/// </summary>
		public Image32? CanvasImage32 => Image32 != null ? (_canvasImage32 ??= GetClippedImage()) : null;
		private Image32? _canvasImage32;

		/// <summary>
		/// The cel's target frame, if this is a link cel.
		/// </summary>
		public int LinkFrame { get; }

		/// <summary>
		/// Construct a new Aseprite cel from raw source data in the Aseprite file.
		/// </summary>
		/// <param name="frame">The frame this cel belongs to.</param>
		/// <param name="header">The Aseprite file header, which describes the canvas dimensions.</param>
		/// <param name="colorMode">What color mode the canvas uses (RGB, paletted, etc.).</param>
		/// <param name="sourceData">The raw source bytes from the Aseprite file for this cel.</param>
		/// <param name="palette">The shared color palette.</param>
		public AsepriteCel(AsepriteFrame frame, in AsepriteHeader header, AsepriteColorMode colorMode,
			ReadOnlySpan<byte> sourceData, ReadOnlySpan<Color32> palette)
		{
			Frame = frame;

			LayerIndex = (ushort)(sourceData[0] | (sourceData[1] << 8));
			X = (short)(sourceData[2] | (sourceData[3] << 8));
			Y = (short)(sourceData[4] | (sourceData[5] << 8));
			Opacity = sourceData[6];
			Kind = (AsepriteCelKind)(sourceData[7] | (sourceData[8] << 8));

			Layer = null!;

			sourceData = sourceData.Slice(16);

			int imageSize = header.Width * header.Height;
			int bytesPerPixel = header.Depth / 8;

			switch (Kind)
			{
				case AsepriteCelKind.Raw:
					Width = (short)(sourceData[0] | (sourceData[1] << 8));
					Height = (short)(sourceData[2] | (sourceData[3] << 8));
					sourceData = sourceData.Slice(4);

					ReadOnlySpan<byte> rawData = sourceData.Slice(0, imageSize * bytesPerPixel);

					switch (colorMode)
					{
						case AsepriteColorMode.Rgb:
							Image32 = new Image32(Width, Height, rawData);
							break;

						case AsepriteColorMode.Grayscale:
							Image8 = new Image8(Width, Height, Palettes.Grayscale256);
							Demote16BitTo8Bit(Image8.Data, rawData);
							break;

						case AsepriteColorMode.Indexed:
							Image8 = new Image8(Width, Height, rawData, palette);
							break;
					}
					break;

				case AsepriteCelKind.Compressed:
					Width = (short)(sourceData[0] | (sourceData[1] << 8));
					Height = (short)(sourceData[2] | (sourceData[3] << 8));
					sourceData = sourceData.Slice(4);

					rawData = Compression.Zlib.Inflate(sourceData);

					switch (colorMode)
					{
						case AsepriteColorMode.Rgb:
							Image32 = new Image32(Width, Height, rawData);
							break;

						case AsepriteColorMode.Grayscale:
							Image8 = new Image8(Width, Height, Palettes.Grayscale256);
							Demote16BitTo8Bit(Image8.Data, rawData);
							break;

						case AsepriteColorMode.Indexed:
							Image8 = new Image8(Width, Height, rawData);
							break;
					}
					break;

				case AsepriteCelKind.Link:
					LinkFrame = (ushort)(sourceData[0] | (sourceData[1] << 8));
					break;

				default:
					throw new InvalidOperationException($"Unknown cel kind {(int)Kind}.");
			}
		}

		/// <summary>
		/// We don't have a 16-bit image type, so if presented with a 16-bit grayscale image,
		/// demote it to an 8-bit grayscale image instead.
		/// </summary>
		/// <param name="data">The 8-bit destination buffer for the image data.</param>
		/// <param name="rawData">The raw 16-bit source data.</param>
		private unsafe void Demote16BitTo8Bit(byte[] data, ReadOnlySpan<byte> rawData)
		{
			int length = Math.Min(data.Length, rawData.Length);

			fixed (byte* destBase = data)
			fixed (byte* srcBase = rawData)
			{
				ushort* src = (ushort*)srcBase;
				byte* dest = destBase;
				while (length-- != 0)
					*dest++ = (byte)(((uint)*src++ + 128) >> 8);
			}
		}

		/// <summary>
		/// Because this cel can be positioned arbitrarily relative to the sprite's
		/// canvas, we need a way to extract its *apparent* pixels.  So this generates
		/// a derived image that only contains the visible pixels of this on the canvas.
		/// 
		/// This version applies to 32-bit RGBA images; any cel data outside the canvas
		/// will be removed, and any unpainted pixels will be Transparent.
		/// </summary>
		/// <returns>An image exactly the size of the sprite's canvas, with the correct
		/// visible pixels for this cel.</returns>
		public Image32? GetClippedImage()
		{
			// If this isn't 32-bit RGBA, abort.
			if (Image32 == null)
				return null;

			// If this is already canvas-sized, there's nothing to do.
			if (X == 0 && Y == 0 && Width == Frame.Image.Width && Height == Frame.Image.Height)
				return Image32;

			// It's offsetted or misshapen, so we need to blit it onto a new
			// transparent canvas.
			Image32 image = new Image32(Frame.Image.Width, Frame.Image.Height);
			image.Blit(Image32, 0, 0, X, Y, Width, Height, BlitFlags.Copy);
			return image;
		}

		/// <summary>
		/// Because this cel can be positioned arbitrarily relative to the sprite's
		/// canvas, we need a way to extract its *apparent* pixels.  So this generates
		/// a derived image that only contains the visible pixels of this on the canvas.
		/// 
		/// This version applies to 8-bit paletted images; any cel data outside the
		/// canvas will be removed, and any unpainted pixels will be color 0.
		/// </summary>
		/// <returns>An image exactly the size of the sprite's canvas, with the correct
		/// visible pixels for this cel.</returns>
		public Image8? GetClippedImage8(ReadOnlySpan<Color32> palette)
		{
			// If this isn't 8-bit paletted, abort.
			if (Image8 == null)
				return null;

			// If this is already canvas-sized, there's nothing to do.
			if (X == 0 && Y == 0 && Width == Frame.Image.Width && Height == Frame.Image.Height)
				return Image8;

			// It's offsetted or misshapen, so we need to blit it onto a new
			// transparent canvas.
			Image8 image = new Image8(Frame.Image.Width, Frame.Image.Height, palette);
			image.Blit(Image8, 0, 0, X, Y, Width, Height, BlitFlags.Copy);
			return image;
		}
	}
}