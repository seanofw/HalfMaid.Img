using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// This class knows how to save a GIF image or a GIF animation.
	/// </summary>
	public class GifSaver : IImageSaver
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Gif;

		/// <inheritdoc />
		public string Title => "GIF";

		/// <inheritdoc />
		public string DefaultExtension => ".gif";

		/// <summary>
		/// Save a truecolor image.  GIF does not support truecolor, so this method only throws a NotSupportedException.
		/// </summary>
		byte[] IImageSaver.SaveImage(Image32 image, IReadOnlyDictionary<string, object>? imageMetadata,
			IFileSaveOptions? fileSaveOptions)
		{
			throw new NotSupportedException("GIF does not support writing truecolor images. Convert this image to a paletted Image8.");
		}

		/// <summary>
		/// Save an 8-bit paletted image as a GIF.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  Only the
		/// Comment property of this is taken, if provided.</param>
		/// <param name="fileSaveOptions">Save options.  This is ignored for GIF.</param>
		/// <returns>The resulting GIF file, as a byte array.</returns>
		public byte[] SaveImage(Image8 image, IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			// Turn the image into a single frame of a GIF "animation."
			GifFrame[] frames = new[]
			{
				new GifFrame(image, 0, 0, GifControlBlockFlags.None, 0, 0, false)
			};

			// Extract the comment, if provided.
			string? comment =
				imageMetadata != null
				&& imageMetadata.TryGetValue(ImageMetadataKey.Comment, out object? commentObject)
				&& commentObject is string c
				? c : null;

			// Save this using the shared animated-GIF core.
			return SaveGif(new GifImage(image.Width, image.Height, image.Palette,
				default, frames, comment, null), false);
		}

		/// <summary>
		/// Save this image or animation as a GIF file.
		/// </summary>
		/// <param name="gifImage">The image or animation to save, set up as a
		/// GifImage data structure.</param>
		/// <param name="isAnimatedOrTransparent">Whether this image uses the GIF89a format (true)
		/// or the GIF87a format (false).</param>
		/// <returns>The resulting GIF file, as a byte array.</returns>
		public byte[] SaveGif(GifImage gifImage, bool isAnimatedOrTransparent)
		{
			OutputWriter output = new OutputWriter();

			bool usesTransparency = gifImage.GlobalPalette.Any(p => p.A == 0)
				|| gifImage.Frames.Any(f => f.Image.Palette.Any(p => p.A == 0));
			bool hasGlobalPalette = gifImage.GlobalPalette != null && gifImage.GlobalPalette.Count > 0;

			int numBitsMinusOne = hasGlobalPalette ? CalculateFlagsFromColors(gifImage.GlobalPalette!.Count) : 0;

			// Construct the GIF header.
			WriteGifHeader(output, gifImage, isAnimatedOrTransparent, hasGlobalPalette, numBitsMinusOne);

			// Write the global palette.
			if (hasGlobalPalette)
			{
				IReadOnlyList<Color32> palette = gifImage.GlobalPalette!;
				int numColors = 1 << (numBitsMinusOne + 1);
				WritePalette(output, palette, numColors);
			}

			// Write the comment as extension blocks.
			if (!string.IsNullOrEmpty(gifImage.Comment))
				WriteComment(output, gifImage.Comment!);

			// Write the frames as image blocks.
			foreach (GifFrame frame in gifImage.Frames)
			{
				if (isAnimatedOrTransparent)
					WriteGifGraphicControlBlock(output, frame);
				WriteGifFrame(output, frame);
			}

			// Add the end-of-image marker.
			output.WriteByte((byte)';');

			return output.Finish();
		}

		private void WriteGifGraphicControlBlock(OutputWriter output, GifFrame frame)
		{
			output.WriteByte((byte)'!');
			output.WriteByte((byte)0xF9);
			output.WriteByte((byte)4);

			output.WriteByte((byte)frame.Flags);
			output.WriteByte((byte)(frame.FrameDelay & 0xFF));
			output.WriteByte((byte)((frame.FrameDelay >> 8) & 0xFF));
			output.WriteByte((byte)frame.TransparentColorIndex);
		}

		private void WriteGifFrame(OutputWriter output, GifFrame frame)
		{
			output.WriteByte((byte)',');

			Span<GifImageBlockHeader> header = stackalloc GifImageBlockHeader[1];

			header[0].X = ((short)frame.X).LE();
			header[0].Y = ((short)frame.Y).LE();
			header[0].Width = ((ushort)frame.Width).LE();
			header[0].Height = ((ushort)frame.Height).LE();

			int numBitsMinusOne = CalculateFlagsFromColors(frame.Image.Palette.Length);

			header[0].Flags = frame.HasLocalPalette
				? GifImageBlockFlags.LocalPalette | (GifImageBlockFlags)numBitsMinusOne
				: default;

			if (frame.HasLocalPalette)
				WritePalette(output, frame.Image.Palette, 1 << (numBitsMinusOne + 1));

			ReadOnlySpan<byte> headerBytes = MemoryMarshal.Cast<GifImageBlockHeader, byte>(header);
			output.Write(headerBytes);

			GifEncoder.EncodeImage(output, frame.Image.Data, numBitsMinusOne + 1);
		}

		/// <summary>
		/// Write the given comment as a GIF extension block.
		/// </summary>
		/// <param name="output">The output stream to write to.</param>
		/// <param name="comment">The comment to write.  This will be written as
		/// ISO 8859-1 (Latin-1) for backward compatibility.</param>
		private void WriteComment(OutputWriter output, string comment)
		{
			byte[] bytes = GifLoader.Latin1.GetBytes(comment);

			output.WriteByte((byte)'!');
			output.WriteByte(0xFE);

			for (int ptr = 0; ptr < bytes.Length; ptr += 255)
			{
				int end = Math.Min(ptr + 255, comment.Length);
				int length = end - ptr;
				output.WriteByte((byte)length);
				output.Write(bytes.AsSpan(ptr, length));
			}

			output.WriteByte(0);
		}

		/// <summary>
		/// Calculate the GIF 3-bit representation of the palette size from the given
		/// actual number of palette colors.
		/// </summary>
		/// <param name="numColors">The number of palette colors.</param>
		/// <returns>The 3-bit representation GIF uses to describe this palette size.</returns>
		private static int CalculateFlagsFromColors(int numColors)
		{
			if (numColors < 0 || numColors > 256)
				throw new NotSupportedException("GIF supports from 0 to 256 colors.");

			return
				( numColors > 128 ? 7
				: numColors >  64 ? 6
				: numColors >  32 ? 5
				: numColors >  16 ? 4
				: numColors >   8 ? 3
				: numColors >   4 ? 2
				: numColors >   2 ? 1
				: numColors >   1 ? 0
				:               0);
		}

		/// <summary>
		/// Write a GIF header to the output stream.
		/// </summary>
		/// <param name="output">The output stream.</param>
		/// <param name="gifImage">The GIF image to write a header for.</param>
		/// <param name="isAnimated">True if this is supposed to be an animated GIF (this flag
		/// chooses between the 'GIF89a' and 'GIF87a' signatures).</param>
		/// <param name="hasGlobalPalette">Whether this image has a global palette (for real).</param>
		/// <param name="colorFlags">The 3-bit representation of the palette size.</param>
		private static void WriteGifHeader(OutputWriter output,
			GifImage gifImage, bool isAnimated, bool hasGlobalPalette, int colorFlags)
		{
			Span<GifHeader> header = stackalloc GifHeader[1];

			unsafe
			{
				header[0].Signature[0] = (byte)'G';
				header[0].Signature[1] = (byte)'I';
				header[0].Signature[2] = (byte)'F';
				header[0].Version[0] = (byte)'8';
				header[0].Version[1] = isAnimated ? (byte)'9' : (byte)'7';
				header[0].Version[2] = (byte)'a';
			}
			header[0].Width = ((ushort)gifImage.Width).LE();
			header[0].Height = ((ushort)gifImage.Height).LE();
			header[0].Flags = hasGlobalPalette
				? GifHeaderFlags.GlobalPalette | (GifHeaderFlags)colorFlags
				: default;
			header[0].BgColor = 0;
			header[0].AspectRatio = 0;

			// Write the GIF header first.
			ReadOnlySpan<byte> headerBytes = MemoryMarshal.Cast<GifHeader, byte>(header);
			output.Write(headerBytes);
		}

		/// <summary>
		/// Write the given palette to the GIF output stream.
		/// </summary>
		/// <param name="output">The output stream.</param>
		/// <param name="palette">The palette to write.</param>
		/// <param name="numColors">The number of colors that the palette should include.
		/// This must be a power of two, and must be greater than or equal to palette.Count.</param>
		private static void WritePalette(OutputWriter output, IReadOnlyList<Color32> palette, int numColors)
		{
			byte[] buffer = new byte[numColors * 3];

			for (int i = 0; i < numColors; i++)
			{
				if (i < palette.Count)
				{
					buffer[i * 3] = palette[i].R;
					buffer[i * 3 + 1] = palette[i].G;
					buffer[i * 3 + 2] = palette[i].B;
				}
			}

			output.Write(buffer.AsSpan(0, buffer.Length));
		}
	}
}
