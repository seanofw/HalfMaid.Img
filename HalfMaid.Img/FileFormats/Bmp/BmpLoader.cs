using System;
using System.Collections.Generic;

namespace HalfMaid.Img.FileFormats.Bmp
{
	/// <summary>
	/// A loader for (most) Windows BMP files.  This can load between most and all
	/// major BMP formats of 32 bits or less, except for the obscure BMP RLE format.
	/// </summary>
	public class BmpLoader : IImageLoader
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Bmp;

		/// <inheritdoc />
		public string Title => "Windows Bitmap";

		/// <inheritdoc />
		public string DefaultExtension => ".bmp";

		/// <inheritdoc />
		public unsafe ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data)
		{
			fixed (byte* dataBase = data)
			{
				BitmapInfoHeader* infoHeader;
				if (data[0] == 'B' && data[1] == 'M'
					&& data.Length >= sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader))
					infoHeader = (BitmapInfoHeader*)(dataBase + sizeof(BitmapFileHeader));
				else if (data.Length >= sizeof(BitmapInfoHeader))
					infoHeader = (BitmapInfoHeader*)dataBase;
				else
					return ImageCertainty.No;

				int quality = 0;
				quality += (infoHeader->biSize.LE() == sizeof(BitmapInfoHeader)
					|| infoHeader->biSize.LE() == 108
					|| infoHeader->biSize.LE() == 124) ? 1 : 0;
				quality += (infoHeader->biPlanes.LE() == 1) ? 1 : 0;
				quality += (infoHeader->biClrUsed.LE() <= 256) ? 1 : 0;
				quality += (infoHeader->biClrImportant.LE() <= 256) ? 1 : 0;
				quality += (infoHeader->biClrImportant.LE() <= infoHeader->biClrUsed.LE()) ? 1 : 0;
				quality += (infoHeader->biBitCount.LE() == 1
					|| infoHeader->biBitCount.LE() == 4
					|| infoHeader->biBitCount.LE() == 8
					|| infoHeader->biBitCount.LE() == 24
					|| infoHeader->biBitCount.LE() == 32) ? 4 : 0;

				if (quality == 9) return ImageCertainty.Yes;
				else if (quality >= 6) return ImageCertainty.Probably;
				else if (quality != 0) return ImageCertainty.Maybe;
				else return ImageCertainty.No;
			}
		}

		/// <inheritdoc />
		public ImageCertainty DoesNameMatch(string filename)
		{
			return filename.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
				? ImageCertainty.Yes : ImageCertainty.No;
		}

		/// <inheritdoc />
		public unsafe ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data)
		{
			fixed (byte* dataBase = data)
			{
				BitmapInfoHeader* infoHeader;
				if (data[0] == 'B' && data[1] == 'M'
					&& data.Length >= sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader))
					infoHeader = (BitmapInfoHeader*)(dataBase + sizeof(BitmapFileHeader));
				else if (data.Length >= sizeof(BitmapInfoHeader))
					infoHeader = (BitmapInfoHeader*)dataBase;
				else
					return null;

				int width = infoHeader->biWidth.LE();
				int height = infoHeader->biHeight.LE();
				ImageFileColorFormat colorFormat =
					(infoHeader->biBitCount.LE() == 1 ? ImageFileColorFormat.BlackAndWhite1Bit
					: infoHeader->biBitCount.LE() == 4 ? ImageFileColorFormat.Paletted4Bit
					: infoHeader->biBitCount.LE() == 8 ? ImageFileColorFormat.Paletted8Bit
					: infoHeader->biBitCount.LE() == 24 ? ImageFileColorFormat.Rgb24Bit
					: infoHeader->biBitCount.LE() == 32 ? ImageFileColorFormat.Rgba32Bit
					: ImageFileColorFormat.Unknown);

				return colorFormat != ImageFileColorFormat.Unknown
					? new ImageFileMetadata(width, height, colorFormat)
					: null;
			}
		}

		/// <inheritdoc />
		public unsafe ImageLoadResult? LoadImage(ReadOnlySpan<byte> data, PreferredImageType preferredImageType)
		{
			fixed (byte* dataBase = data)
			{
				BitmapInfoHeader infoHeader = default;
				int length = data.Length;
				byte* palData = null;
				byte* imgData = null;

				if (data[0] == 'B' && data[1] == 'M'
					&& data.Length >= sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader))
				{
					// We have a BitmapFileHeader at the start.
					BitmapFileHeader* fileHeader;
					fileHeader = (BitmapFileHeader*)dataBase;
					infoHeader = *(BitmapInfoHeader*)(dataBase + sizeof(BitmapFileHeader));
					palData = dataBase + sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader);
					imgData = dataBase + fileHeader->bfOffBits.LE();
					length = data.Length - fileHeader->bfOffBits.LE();
				}
				else if (data.Length >= sizeof(BitmapInfoHeader))
				{
					// No BitmapFileHeader, so presumably there's a BitmapInfoHeader.
					infoHeader = *(BitmapInfoHeader*)dataBase;

					// Find the image data.  Sadly, the BMP header is (maybe)
					// missing, so we have to guess at its location based on
					// what we know about its contents.
					int numColors = infoHeader.biClrUsed.LE();
					if (numColors == 0)
						numColors = 1 << infoHeader.biBitCount.LE();
					int biSize = infoHeader.biSize.LE();
					if (biSize <= 0)
						return null;
					imgData = dataBase + biSize;
					palData = dataBase + biSize;
					length -= biSize;

					if (infoHeader.biBitCount.LE() <= 8)
					{
						length -= numColors * 4;
						imgData += numColors * 4;
					}
					else if (infoHeader.biCompression.LE() == 3
						|| infoHeader.biCompression.LE() == 6)
					{
						palData = dataBase + sizeof(BitmapInfoHeader);
					}
				}
				else length = 0;

				// If this image just can't be processed, give up now.
				if (length <= 0 || infoHeader.biWidth.LE() <= 0 || infoHeader.biHeight.LE() <= 0)
					return null;

				// We don't support BMP RLE compression or any of the weirder
				// BMP extensions that probably shouldn't exist.
				if (infoHeader.biCompression.LE() != 0
					&& infoHeader.biCompression.LE() != 3
					&& infoHeader.biCompression.LE() != 6)
					return null;

				// Optional metadata, which for BMP is only the pixel density.
				Dictionary<string, object> metadata = new Dictionary<string, object>
				{
					{ ImageMetadataKey.PixelsPerMeterX, (double)infoHeader.biXPelsPerMeter },
					{ ImageMetadataKey.PixelsPerMeterY, (double)infoHeader.biYPelsPerMeter },
				};

				// Windows bitmaps come in several different bit depths, so we have
				// custom decoders for each of them.
				if (infoHeader.biBitCount.LE() <= 8)
				{
					// Paletted mode(s).
					return DecodeInPalettedModes(infoHeader, imgData, palData, length, metadata);
				}
				else if (infoHeader.biCompression.LE() == 3
					|| infoHeader.biCompression.LE() == 6)
				{
					// Bitfield mode.  There's no palette.  Instead, we have bit-shifting
					// fun in various modes.
					return DecodeByBitfields(infoHeader, imgData, palData, length, metadata, preferredImageType);
				}
				else
				{
					// Simple uncompressed truecolor.
					return DecodeTruecolor(infoHeader, imgData, length, metadata, preferredImageType);
				}
			}
		}

		private unsafe ImageLoadResult? DecodeInPalettedModes(BitmapInfoHeader infoHeader,
			byte* imgData, byte* palData, int length, Dictionary<string, object> metadata)
		{
			// We don't support any form of compression.
			if (infoHeader.biCompression.LE() != 0)
				return null;

			Image8 result = new Image8(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
			Color32[]? palette = ExtractPalette(palData, (int)(imgData - palData),
				infoHeader.biClrUsed.LE());
			if (palette != null)
				result.ReplacePalette(palette.AsSpan());
			ImageFileColorFormat colorFormat;

			if (infoHeader.biBitCount.LE() == 1)
			{
				// 1-bit, possibly with a palette; we expand it to an 8-bit paletted image.
				colorFormat = ImageFileColorFormat.BlackAndWhite1Bit;
				metadata[ImageMetadataKey.NumChannels] = 1;
				metadata[ImageMetadataKey.BitsPerChannel] = 1;
				if (!Decode1Bit(result, imgData, length))
					return null;
			}
			else if (infoHeader.biBitCount.LE() == 4)
			{
				// 4-bit, with a palette; we expand it to an 8-bit paletted image.
				colorFormat = ImageFileColorFormat.Paletted4Bit;
				metadata[ImageMetadataKey.NumChannels] = 1;
				metadata[ImageMetadataKey.BitsPerChannel] = 4;
				if (!Decode4Bit(result, imgData, length))
					return null;
			}
			else if (infoHeader.biBitCount.LE() == 8)
			{
				// 8-bit, with a palette; and we decode it as an 8-bit paletted image.
				colorFormat = ImageFileColorFormat.Paletted8Bit;
				metadata[ImageMetadataKey.NumChannels] = 1;
				metadata[ImageMetadataKey.BitsPerChannel] = 8;
				if (!Decode8Bit(result, imgData, length))
					return null;
			}
			else return null;

			return new ImageLoadResult(colorFormat, result.Size,
				result, metadata: metadata);
		}

		private static unsafe ImageLoadResult? DecodeByBitfields(in BitmapInfoHeader infoHeader,
			byte* imgData, byte* palData, int length, Dictionary<string, object> metadata,
			PreferredImageType preferredImageType)
		{
			if ((imgData - palData) < 16)
				return null;

			IImage result;
			ImageFileColorFormat colorFormat;

			// Get the bitmasks that describe where the colors can be found.
			uint rbits = ((uint*)palData)[0];
			uint gbits = ((uint*)palData)[1];
			uint bbits = ((uint*)palData)[2];
			uint abits = ((uint*)palData)[3];

			// Calculate the actual shift positions and sizes for the masks.
			int rshift = rbits.CountTrailingZeros();
			int gshift = gbits.CountTrailingZeros();
			int bshift = bbits.CountTrailingZeros();
			int ashift = abits.CountTrailingZeros();

			rbits >>= rshift;
			gbits >>= gshift;
			bbits >>= bshift;
			abits >>= ashift;

			int rsize = 32 - rbits.CountLeadingZeros();
			int gsize = 32 - gbits.CountLeadingZeros();
			int bsize = 32 - bbits.CountLeadingZeros();
			int asize = 32 - abits.CountLeadingZeros();

			// We don't support any sizes that aren't 8 bits long or that
			// are located in the middle of a word somewhere.
			if (rsize != 8 || gsize != 8 || bsize != 8
				|| ((rshift | gshift | bshift | ashift) & 7) != 0)
				return null;

			// Turn the masks into simple byte offsets.
			int* offsets = stackalloc int[4];
			offsets[0] = rshift >> 3;
			offsets[1] = gshift >> 3;
			offsets[2] = bshift >> 3;
			offsets[3] = ashift >> 3;

			// We don't support 16-bit image files, because they were almost never
			// used in the real world.
			if (infoHeader.biBitCount.LE() == 24)
			{
				colorFormat = ImageFileColorFormat.Rgb24Bit;
				metadata[ImageMetadataKey.NumChannels] = 3;
				metadata[ImageMetadataKey.BitsPerChannel] = 8;
				if (preferredImageType == PreferredImageType.Image32)
				{
					Image32 image = new Image32(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode24BitTo32BitMasked(image, imgData, length, offsets))
						return null;
					result = image;
				}
				else
				{
					Image24 image = new Image24(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode24BitMasked(image, imgData, length, offsets))
						return null;
					result = image;
				}
			}
			else if (infoHeader.biBitCount.LE() == 32)
			{
				if (asize != 8)
					return null;

				colorFormat = ImageFileColorFormat.Rgba32Bit;
				metadata[ImageMetadataKey.NumChannels] = 4;
				metadata[ImageMetadataKey.BitsPerChannel] = 8;
				if (preferredImageType == PreferredImageType.Image24)
				{
					Image24 image = new Image24(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode32BitTo24BitMasked(image, imgData, length, offsets))
						return null;
					result = image;
				}
				else
				{
					Image32 image = new Image32(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode32BitMasked(image, imgData, length, offsets))
						return null;
					result = image;
				}
			}
			else return null;

			return new ImageLoadResult(colorFormat, result.Size,
				image: result, metadata: metadata);
		}

		private static unsafe ImageLoadResult? DecodeTruecolor(in BitmapInfoHeader infoHeader,
			byte* imgData, int length, Dictionary<string, object> metadata,
			PreferredImageType preferredImageType)
		{
			IImage result;
			ImageFileColorFormat colorFormat;

			if (infoHeader.biBitCount.LE() == 24)
			{
				// 24-bit BGR; we decode it as 24-bit RGB.
				colorFormat = ImageFileColorFormat.Rgb24Bit;
				metadata[ImageMetadataKey.NumChannels] = 3;
				metadata[ImageMetadataKey.BitsPerChannel] = 8;
				if (preferredImageType == PreferredImageType.Image32)
				{
					Image32 image = new Image32(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode24BitTo32Bit(image, imgData, length))
						return null;
					result = image;
				}
				else
				{
					Image24 image = new Image24(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode24Bit(image, imgData, length))
						return null;
					result = image;
				}
			}
			else if (infoHeader.biBitCount.LE() == 32)
			{
				// 32-bit BGRA; we decode it as 32-bit RGBA.
				colorFormat = ImageFileColorFormat.Rgba32Bit;
				metadata[ImageMetadataKey.NumChannels] = 4;
				metadata[ImageMetadataKey.BitsPerChannel] = 8;
				if (preferredImageType == PreferredImageType.Image24)
				{
					Image24 image = new Image24(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode32BitTo24Bit(image, imgData, length))
						return null;
					result = image;
				}
				else
				{
					Image32 image = new Image32(infoHeader.biWidth.LE(), infoHeader.biHeight.LE());
					if (!Decode32Bit(image, imgData, length))
						return null;
					result = image;
				}
			}
			else return null;

			return new ImageLoadResult(colorFormat, result.Size,
				image: result, metadata: metadata);
		}

		/// <summary>
		/// Extract out the palette from the BMP file at the given pointer.
		/// The palette is stored as BGR0, which is both backward and has a useless
		/// zero in it, so we have to do some munging to get the data to a sane order.
		/// </summary>
		/// <param name="palData">The source of the palette data.</param>
		/// <param name="maxLength">The maximum number of bytes in the palette data,
		/// to avoid buffer overruns.</param>
		/// <param name="numColors">The number of colors to extract.</param>
		/// <returns>The palette, or null if it couldn't be read.</returns>
		private static unsafe Color32[]? ExtractPalette(byte* palData, int maxLength, int numColors)
		{
			if (numColors * 4 > maxLength)
				return null;

			Color32[] palette = new Color32[numColors];
			for (int i = 0; i < numColors; i++)
			{
				palette[i] = new Color32(palData[2], palData[1], palData[0]);
				palData += 4;
			}

			return palette;
		}

		#region Low bit counts (1-8 bits)

		private static unsafe bool Decode1Bit(Image8 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes, so there are shenanigans here.
			int rowStride = ((result.Width + 31) & ~31) >> 3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (byte* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					byte* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x += 8)
					{
						// Calculate how many bits come from the next byte.
						int bitCount = Math.Min(8, result.Width - x);

						// Read the next byte from the source.
						byte b = *src++;

						// The bits are stored high-bit first, low-bit last, so
						// we pull them out from top to bottom.
						for (int bit = bitCount - 1; bit >= 0; bit++)
							*dest++ = (byte)((b >> bit) & 1);
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode4Bit(Image8 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes, so there are shenanigans here.
			int rowStride = ((result.Width + 7) & ~7) >> 1;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (byte* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					byte* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x += 2)
					{
						// Read the next byte from the source.
						byte b = *src++;

						// Decompose the nybbles.
						*dest++ = (byte)(b >> 4);
						*dest++ = (byte)(b & 0xF);
					}

					// Handle a last trailing nybble if there is one.
					if ((result.Width & 1) != 0)
					{
						byte b = *src++;
						*dest++ = (byte)(b >> 4);
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode8Bit(Image8 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes.
			int rowStride = (result.Width + 3) & ~3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (byte* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					byte* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
						*dest++ = *src++;

					imgData += rowStride;
				}
			}

			return true;
		}

		#endregion

		#region 24-bit and 32-bit truecolor

		private static unsafe bool Decode24Bit(Image24 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes.
			int rowStride = (result.Width * 3 + 3) & ~3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (Color24* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color24* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						// The colors are stored in BGR order, so we have to flip them.
						*dest++ = new Color24(src[2], src[1], src[0]);
						src += 3;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode24BitTo32Bit(Image32 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes.
			int rowStride = (result.Width * 3 + 3) & ~3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (Color32* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color32* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						// The colors are stored in BGR order, so we have to flip them.
						*dest++ = new Color32(src[2], src[1], src[0]);
						src += 3;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode32Bit(Image32 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.
			int rowStride = result.Width * 4;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (Color32* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color32* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						// The colors are stored in BGRA order, so we have to flip them.
						*dest++ = new Color32(src[2], src[1], src[0], src[3]);
						src += 4;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode32BitTo24Bit(Image24 result, byte* imgData, int length)
		{
			// Calculate the number of bytes per source row.
			int rowStride = result.Width * 4;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			fixed (Color24* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color24* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						// The colors are stored in BGRA order, so we have to flip them.
						*dest++ = new Color24(src[2], src[1], src[0]);
						src += 4;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		#endregion

		#region 24-bit and 32-bit bitmask-based formats

		private static unsafe bool Decode24BitMasked(Image24 result, byte* imgData, int length, int* offsets)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes.
			int rowStride = (result.Width * 3 + 3) & ~3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			int r = offsets[0];
			int g = offsets[1];
			int b = offsets[2];

			fixed (Color24* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color24* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						*dest++ = new Color24(src[r], src[g], src[b]);
						src += 3;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode24BitTo32BitMasked(Image32 result, byte* imgData, int length, int* offsets)
		{
			// Calculate the number of bytes per source row.  Each row must
			// pad to an even 4 bytes.
			int rowStride = (result.Width * 3 + 3) & ~3;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			int r = offsets[0];
			int g = offsets[1];
			int b = offsets[2];

			fixed (Color32* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color32* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						*dest++ = new Color32(src[r], src[g], src[b]);
						src += 3;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode32BitMasked(Image32 result, byte* imgData, int length, int* offsets)
		{
			// Calculate the number of bytes per source row.
			int rowStride = result.Width * 4;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			int r = offsets[0];
			int g = offsets[1];
			int b = offsets[2];
			int a = offsets[3];

			fixed (Color32* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color32* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						*dest++ = new Color32(src[r], src[g], src[b], src[a]);
						src += 4;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		private static unsafe bool Decode32BitTo24BitMasked(Image24 result, byte* imgData, int length, int* offsets)
		{
			// Calculate the number of bytes per source row.
			int rowStride = result.Width * 4;

			// Make sure there's enough source data.
			if (length < rowStride * result.Height)
				return false;

			int r = offsets[0];
			int g = offsets[1];
			int b = offsets[2];

			fixed (Color24* destBase = result.Data)
			{
				// Windows bitmaps are stored upside-down, because of old OS/2 decisions :(
				for (int y = result.Height - 1; y >= 0; y--)
				{
					byte* src = imgData;
					Color24* dest = destBase + y * result.Width;

					for (int x = 0; x < result.Width; x++)
					{
						*dest++ = new Color24(src[r], src[g], src[b]);
						src += 4;
					}

					imgData += rowStride;
				}
			}

			return true;
		}

		#endregion
	}
}
