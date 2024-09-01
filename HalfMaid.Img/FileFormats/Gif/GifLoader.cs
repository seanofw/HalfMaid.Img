using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// A loader for (most) GIF files.
	/// </summary>
	public class GifLoader : IImageLoader
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Gif;

		/// <inheritdoc />
		public string Title => "GIF";

		/// <inheritdoc />
		public string DefaultExtension => ".gif";

		internal static Encoding Latin1 { get; } = Encoding.GetEncoding("ISO-8859-1");

		/// <inheritdoc />
		public unsafe ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data)
		{
			if (data.Length < 16)
				return ImageCertainty.No;

			// Check for signature - 6 bytes.
			int sigMatch = 0;
			sigMatch += (data[0] == 'G' ? 1 : 0);
			sigMatch += (data[1] == 'I' ? 1 : 0);
			sigMatch += (data[2] == 'F' ? 1 : 0);
			sigMatch += (data[3] == '8' ? 1 : 0);
			sigMatch += (data[4] == '7' || data[4] == '9' ? 1 : 0);
			sigMatch += (data[5] == 'a' ? 1 : 0);

			// Make sure the image's size is 32767 pixels or less;
			// any larger than that probably indicates a bad image file,
			// since GIF never practically gets that large.
			sigMatch += ((data[7] & 0x80) == 0 ? 1 : 0);
			sigMatch += ((data[9] & 0x80) == 0 ? 1 : 0);

			return sigMatch >= 8 ? ImageCertainty.Yes
				: sigMatch >= 6 ? ImageCertainty.Probably
				: sigMatch >= 4 ? ImageCertainty.Maybe
				: ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageCertainty DoesNameMatch(string filename)
		{
			return filename.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
				? ImageCertainty.Yes : ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data)
		{
			if (data.Length <= 16)
				return null;

			ReadOnlySpan<GifHeader> header = MemoryMarshal.Cast<byte, GifHeader>(data);

			int width = header[0].Width.LE();
			int height = header[0].Height.LE();
			if (header[0].BitsPerPixel < 2 || header[0].BitsPerPixel > 8)
				return null;
			return new ImageFileMetadata(width, height, ImageFileColorFormat.Paletted8Bit);
		}

		/// <summary>
		/// Load the given image data as a full animated GIF.  If the data only represents
		/// a static GIF image, this will return an "animated" GIF with only one frame.
		/// </summary>
		/// <param name="data">The data to decode as a GIF image.</param>
		/// <returns>The full animated GIF data, with Image8 objects representing each frame.</returns>
		public GifImage? LoadAnimatedGif(ReadOnlySpan<byte> data)
			=> LoadGif(data, true);

		/// <summary>
		/// Load the given image data, either as the first frame, or as all frames of an
		/// animated GIF.
		/// </summary>
		/// <param name="data">The data to decode as a GIF image.</param>
		/// <param name="allFrames">Whether to load all frames (true) or just the first
		/// frame (false).  When loading just the first frame, this will avoid decoding any
		/// past the first frame, which is much faster.</param>
		/// <returns>The full GIF data, with Image8 objects representing each frame.</returns>
		private GifImage? LoadGif(ReadOnlySpan<byte> data, bool allFrames)
		{
			// Check that the header is sane.
			if (DoesDataMatch(data) <= ImageCertainty.Maybe)
				return null;

			// Read the data from the header.
			ReadOnlySpan<GifHeader> header = MemoryMarshal.Cast<byte, GifHeader>(data);
			int width = header[0].Width.LE();
			int height = header[0].Height.LE();
			GifHeaderFlags flags = header[0].Flags;
			if (header[0].BitsPerPixel < 1 || header[0].BitsPerPixel > 8)
				return null;

			// Skim forward past the header.
			data = data.Slice(13);

			// Read the global palette, if there is one.
			Color32[] palette;
			int bpp = (int)(flags & GifHeaderFlags.GlobalBits) + 1;
			if ((flags & GifHeaderFlags.GlobalPalette) != 0)
			{
				palette = ReadPalette(data, bpp);
				data = data.Slice(palette.Length * 3);
			}
			else
			{
				// No palette, so we just make a grayscale palette as the global palette.
				palette = MakeGrayscalePalette(bpp);
			}

			// Decode the sequence of GIF blocks, including all frames so that we
			// can actually represent a true animated GIF.
			GifMetadata gifMetadata = new GifMetadata();
			List<GifFrame> frames = DecodeGifBlocks(data, width, height, flags, palette, gifMetadata, allFrames);
			return new GifImage(width, height, palette, flags, frames,
				gifMetadata.Comment, gifMetadata.RepeatCount);
		}

		/// <inheritdoc />
		public ImageLoadResult? LoadImage(ReadOnlySpan<byte> data, PreferredImageType preferredImageType)
		{
			GifImage? gifImage = LoadGif(data, false);
			if (gifImage == null || gifImage.Frames.Count <= 0)
				return null;

			// Reextract the Image8, unwrapping it from the PureImage8.  This is only
			// safe because we know nothing else can return the data.
			PureImage8 pureImage8 = gifImage.Frames[0].Image;
			Image8 image8 = new Image8(pureImage8.Width, pureImage8.Height, pureImage8.Data, pureImage8.Palette);

			return new ImageLoadResult(ImageFileColorFormat.Paletted8Bit, gifImage.Size,
				image: image8,
				metadata: gifImage.Comment != null ? new Dictionary<string, object>
				{
					{ nameof(ImageMetadataKey.Comment), gifImage.Comment },
				} : null);
		}

		private static Color32[] ReadPalette(ReadOnlySpan<byte> data, int bpp)
		{
			Color32[] palette = new Color32[1 << bpp];
			int src = 0;
			for (int i = 0; i < palette.Length; i++)
			{
				palette[i] = new Color32(data[src], data[src + 1], data[src + 2]);
				src += 3;
			}
			return palette;
		}

		private static Color32[] MakeGrayscalePalette(int bpp)
		{
			Color32[] palette = new Color32[1 << bpp];

			if (bpp == 2)
			{
				palette[0] = Color32.Black;
				palette[1] = Color32.White;
			}
			else if (bpp == 3)
			{
				palette[0] = Color32.Black;
				palette[1] = new Color32(0x55, 0x55, 0x55);
				palette[2] = new Color32(0xAA, 0xAA, 0xAA);
				palette[3] = Color32.White;
			}
			else
			{
				for (int i = 0; i < palette.Length; i++)
				{
					byte g = (byte)((i << (8 - bpp)) | (i >> (bpp - 4)));
					palette[i] = new Color32(g, g, g);
				}
			}

			return palette;
		}

		private List<GifFrame> DecodeGifBlocks(ReadOnlySpan<byte> data,
			int width, int height, GifHeaderFlags flags, Color32[] palette, GifMetadata metadata,
			bool allFrames)
		{
			List<GifFrame> frames = new List<GifFrame>();

			GifControlBlockFlags controlBlockFlags = default;
			byte transparentColorIndex = default;
			ushort frameDelay = default;

			while (data.Length > 0)
			{
				byte blocktype = data[0];
				data = data.Slice(1);

				switch (blocktype)
				{
					case (byte)',':
						// An image block.
						ReadOnlySpan<GifImageBlockHeader> imageBlockHeader = MemoryMarshal.Cast<byte, GifImageBlockHeader>(data);

						GifImageBlockFlags localFlags = imageBlockHeader[0].Flags;
						int frameWidth = imageBlockHeader[0].Width.LE();
						int frameHeight = imageBlockHeader[0].Height.LE();
						int x = imageBlockHeader[0].X.LE();
						int y = imageBlockHeader[0].Y.LE();
						data = data.Slice(9);

						Color32[]? localPalette = null;
						if ((localFlags & GifImageBlockFlags.LocalPalette) != 0)
						{
							int localBpp = (int)(localFlags & GifImageBlockFlags.LocalBits) + 1;
							localPalette = ReadPalette(data, localBpp);
							data = data.Slice(localPalette.Length * 3);
						}

						byte codeBits = data[0];
						data = data.Slice(1);

						Image8 image = new Image8(frameWidth, frameHeight, (localPalette ?? palette).AsSpan());
						int offset = GifDecoder.DecodeImage(image.Data,
							frameWidth, frameHeight, data, imageBlockHeader[0].Flags, codeBits);
						data = data.Slice(offset + 1);

						frames.Add(new GifFrame(image, x, y, controlBlockFlags, transparentColorIndex, frameDelay,
							(localFlags & GifImageBlockFlags.LocalPalette) != 0));
						if (!allFrames)
							return frames;

						controlBlockFlags = default;
						transparentColorIndex = default;
						frameDelay = default;
						break;

					case (byte)'!':
						// An extension block.
						if (data.Length < 2)
							goto done;

						byte extType = data[0];
						data = data.Slice(1);

						switch (extType)
						{
							case 0xF9:
								// Graphic control extension.
								if (data.Length < 1)
									goto done;
								byte extSize = data[0];
								data = data.Slice(1);
								if (data.Length < extSize)
									goto done;
								if (extSize != 4)
									goto default;
								controlBlockFlags = (GifControlBlockFlags)data[0];
								frameDelay = (ushort)((ushort)data[1] | ((ushort)data[2] << 8));
								transparentColorIndex = data[3];
								data = data.Slice(4);

								// We ignore all other fields, but if this has a transparent
								// palette index, we mark that palette color as properly
								// transparent.  We only change the alpha value of that color
								// so that the original GIF palette color is recoverable if
								// desired.
								if ((controlBlockFlags & GifControlBlockFlags.Transparent) != 0
									&& transparentColorIndex < palette.Length)
								{
									Color32 color = palette[transparentColorIndex];
									palette[transparentColorIndex] = new Color32(color.R, color.G, color.B, (byte)0);
								}

								// Skip the block's contents, if any exist.
								int commentLen;
								do
								{
									if (data.Length <= 0)
										break;
									commentLen = data[0];
									data = data.Slice(1);
									if (commentLen <= 0)
										break;
									commentLen = Math.Min(commentLen, data.Length);
									data = data.Slice(commentLen);
								} while (commentLen > 0);
								break;

							case 0xFE:
								// Comment extension.  Read it into the metadata dictionary.
								StringBuilder stringBuilder = new StringBuilder();
								do
								{
									if (data.Length <= 0)
										break;
									commentLen = data[0];
									data = data.Slice(1);
									if (commentLen <= 0)
										break;
									commentLen = Math.Min(commentLen, data.Length);
									string partialComment;
									try
									{
#if NET5_0_OR_GREATER
										partialComment = Latin1.GetString(data.Slice(0, commentLen));
#else
										partialComment = Latin1.GetString(data.Slice(0, commentLen).ToArray());
#endif
									}
									catch
									{
										partialComment = string.Empty;
									}
									stringBuilder.Append(partialComment);
									data = data.Slice(commentLen);
								} while (commentLen > 0);
								metadata.Comment = stringBuilder.ToString();
								break;

							case 0xFF:
								// Netscape repeat-count block.
								if (data.Length < 17
									|| data[0] != 11
									|| data[1] != 'N' || data[2] != 'E' || data[3] != 'T'
									|| data[4] != 'S' || data[5] != 'C' || data[6] != 'A' || data[7] != 'P' || data[8] != 'E'
									|| data[9] != '2' || data[10] != '.' || data[11] != '0'
									|| data[12] != 3 || data[13] != 1
									|| data[16] != 0)
									goto default;   // Don't know what this is, so skip it.
								metadata.RepeatCount = data[14] | (data[15] << 8);
								data = data.Slice(17);
								break;

							default:
								// Application or unknown extension.  Just skip it.
								do
								{
									if (data.Length <= 0)
										break;
									commentLen = data[0];
									data = data.Slice(1);
									if (commentLen <= 0)
										break;
									commentLen = Math.Min(commentLen, data.Length);
									data = data.Slice(commentLen);
								} while (commentLen > 0);
								break;
						}
						break;

					case (byte)';':
						// End of data, successfully (or successfully enough).
						goto done;

					default:
						// Garbage character; skip it if we can.
						break;
				}
			}

		done:
			return frames;
		}
	}
}
