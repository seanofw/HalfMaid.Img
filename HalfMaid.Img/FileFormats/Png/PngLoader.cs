using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HalfMaid.Img.Compression;
using HalfMaid.Img.FileFormats.Png.Chunks;
using OpenTK.Mathematics;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// This loader knows how to load and decode PNG files.
	/// </summary>
	public class PngLoader : IImageLoader
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Png;

		/// <inheritdoc />
		public string Title => "PNG";

		/// <inheritdoc />
		public string DefaultExtension => ".png";

		internal static Encoding Latin1 { get; } = Encoding.GetEncoding("ISO-8859-1");

		/// <inheritdoc />
		public ImageCertainty DoesDataMatch(ReadOnlySpan<byte> data)
		{
			if (data.Length < 41)			// PNG Header + IHDR chunk + IEND chunk
				return ImageCertainty.No;

			int quality = 0;

			quality += (data[0] == 0x89 ? 1 : 0);
			quality += (data[1] == 'P' ? 1 : 0);
			quality += (data[2] == 'N' ? 1 : 0);
			quality += (data[3] == 'G' ? 1 : 0);
			quality += (data[4] == 0x0D ? 1 : 0);
			quality += (data[5] == 0x0A ? 1 : 0);
			quality += (data[6] == 0x1A ? 1 : 0);
			quality += (data[7] == 0x0A ? 1 : 0);

			quality += (data[8] == 0 ? 1 : 0);
			quality += (data[9] == 0 ? 1 : 0);
			quality += (data[10] == 0 ? 1 : 0);
			quality += (data[11] == 13 ? 1 : 0);

			quality += (data[12] == 'I' ? 1 : 0);
			quality += (data[13] == 'H' ? 1 : 0);
			quality += (data[14] == 'D' ? 1 : 0);
			quality += (data[15] == 'R' ? 1 : 0);

			if (quality >= 16) return ImageCertainty.Yes;
			else if (quality >= 13) return ImageCertainty.Probably;
			else if (quality >= 10) return ImageCertainty.Maybe;
			else return ImageCertainty.No;
		}

		/// <inheritdoc />
		public ImageCertainty DoesNameMatch(string filename)
		{
			return filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
				? ImageCertainty.Yes : ImageCertainty.No;
		}

		/// <inheritdoc />
		public unsafe ImageFileMetadata? GetMetadata(ReadOnlySpan<byte> data)
		{
			if (DoesDataMatch(data) == ImageCertainty.No)
				return null;

			fixed (byte* pngBase = data)
			{
				foreach (RawPngChunk pngChunk in PngChunkReader.Read(pngBase, data.Length))
				{
					if (pngChunk.Type == "IHDR")
					{
						ReadOnlySpan<PngIhdr> ihdr = MemoryMarshal.Cast<byte, PngIhdr>(pngChunk.Data);

						int width = (int)(ihdr[0].Width.BE());
						int height = (int)(ihdr[0].Height.BE());

						ImageFileColorFormat colorFormat =
							  ihdr[0].ColorType == 0 && ihdr[0].BitDepth == 1 ? ImageFileColorFormat.BlackAndWhite1Bit
							: ihdr[0].ColorType == 0 && ihdr[0].BitDepth <= 8 ? ImageFileColorFormat.Gray8Bit
							: ihdr[0].ColorType == 0 ? ImageFileColorFormat.Gray16Bit
							: ihdr[0].ColorType == 2 && ihdr[0].BitDepth <= 8 ? ImageFileColorFormat.Rgb24Bit
							: ihdr[0].ColorType == 2 ? ImageFileColorFormat.Rgb48Bit
							: ihdr[0].ColorType == 3 && ihdr[0].BitDepth <= 4 ? ImageFileColorFormat.Paletted4Bit
							: ihdr[0].ColorType == 3 ? ImageFileColorFormat.Paletted8Bit
							: ihdr[0].ColorType == 4 && ihdr[0].BitDepth <= 8 ? ImageFileColorFormat.GrayAlpha8Bit
							: ihdr[0].ColorType == 4 ? ImageFileColorFormat.GrayAlpha16Bit
							: ihdr[0].ColorType == 6 && ihdr[0].BitDepth <= 8 ? ImageFileColorFormat.Rgba32Bit
							: ihdr[0].ColorType == 6 ? ImageFileColorFormat.Rgba64Bit
							: ImageFileColorFormat.Unknown;

						if (colorFormat == ImageFileColorFormat.Unknown)
							return null;

						return new ImageFileMetadata(width, height, colorFormat);
					}
					else return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Load the PNG chunks directly.  This decomposes the file into its
		/// constituent chunks, and it validates their CRC-32s, but it does not
		/// attempt to interpret any of the data *in* the chunks, leaving that
		/// to the caller.  This provides a good way to extract "unusual" chunks
		/// from a PNG file that would otherwise be discarded.
		/// </summary>
		/// <param name="data">The raw bytes of the PNG file.</param>
		/// <returns>A list of the chunks in the file, in order.</returns>
		public unsafe List<IPngChunk>? LoadChunks(ReadOnlySpan<byte> data)
		{
			if (DoesDataMatch(data) == ImageCertainty.No)
				return null;

			fixed (byte* dataBase = data)
			{
				List<IPngChunk> chunks = PngChunkReader.Read(dataBase, data.Length).ToList();
				return chunks;
			}
		}

		/// <inheritdoc />
		public ImageLoadResult? LoadImage(ReadOnlySpan<byte> data, PreferredImageType preferredImageType)
		{
			// First, decode all the chunks in the PNG stream.
			List<IPngChunk>? pngChunks = LoadChunks(data);
			if (pngChunks == null)
				return null;

			// Get the header, which contains the image dimensions, bit depth, and color type.
			PngIhdrChunk? header = pngChunks.FirstOrDefault(c => c is PngIhdrChunk) as PngIhdrChunk;
			if (header == null)
				return null;

			// Disallow unknown color types.
			if ((int)header.ColorType > 7 || (int)header.ColorType == 1 || (int)header.ColorType == 5)
				return null;

			// Disallow non-power-of-two bit depths.
			if (!(header.BitDepth == 1 || header.BitDepth == 2 || header.BitDepth == 4
				|| header.BitDepth == 8 || header.BitDepth == 16))
				return null;

			// Paletted images can only have 1, 2, 4, or 8 bits per pixel.
			if (header.ColorType == PngColorType.Paletted && header.BitDepth > 8)
				return null;

			// Most formats except grayscale and paletted only allow 8 or 16 bits per pixel.
			if (header.ColorType != PngColorType.Paletted
				&& header.ColorType != PngColorType.Grayscale
				&& header.BitDepth < 8)
				return null;

			// All PNG images use the same set of filter algorithms.
			if (header.FilterMethod != PngFilterMethod.Filtered)
				return null;

			// If this has a palette, retrieve it, and apply transparency to it.
			Color32[]? palette = (pngChunks.FirstOrDefault(c => c is PngPlteChunk) as PngPlteChunk)?.Colors;
			PngTrnsChunk? trans = pngChunks.FirstOrDefault(c => c is PngTrnsChunk) as PngTrnsChunk;
			if (trans != null)
			{
				if (header.ColorType == PngColorType.Paletted && palette != null)
				{
					int count = Math.Min(trans.Alphas.Length, palette.Length);
					for (int i = 0; i < count; i++)
					{
						Color32 c = palette[i];
						palette[i] = new Color32(c.R, c.G, c.B, trans.Alphas[i]);
					}
				}
				else
				{
					// We don't support the weird use case of a single tRNS value for
					// grayscale or RGB images, which requires a final pass over the fully-
					// decoded image data to set the alpha values of specific pixels to 0.
					// It's arguable how useful that feature of PNG is anyway, and it's
					// doubtful that anyone ever actually used it outside of a test case >_>
				}
			}

			// PNG is rich with metadata, assuming anybody included it in the file.
			// Return any of the known chunks back to the caller as metadata.
			Dictionary<string, object> metadata = DecodeMetadata(header, pngChunks);

			// We don't attempt to stream the image data; we just read all of the IDATs
			// into a big buffer and then decompress it at once.  First, count up how
			// big the buffer will need to be.  This will avoid copies and reallocations,
			// at a cost of passing over the chunk data twice.
			int compressedImageDataLength = 0;
			foreach (IPngChunk chunk in pngChunks)
			{
				if (chunk is PngIDatChunk pngIDatChunk)
					compressedImageDataLength += pngIDatChunk.CompressedData.Length;
			}

			// Copy all the IDATs into the big buffer.
			byte[] compressedImageData = new byte[compressedImageDataLength];
			int offset = 0;
			foreach (IPngChunk chunk in pngChunks)
			{
				if (chunk is PngIDatChunk pngIDatChunk)
				{
					pngIDatChunk.CompressedData.AsSpan().CopyTo(compressedImageData.AsSpan().Slice(offset));
					offset += pngIDatChunk.CompressedData.Length;
				}
			}

			// Reinflate the compressed image data.
			byte[] rawImageData = Zlib.Inflate(compressedImageData);

			// Construct the image object that will hold the resulting image.
			Image32? image32 = null;
			Image24? image24 = null;
			Image8? image8 = null;
			ImageFileColorFormat colorFormat = ImageFileColorFormat.Unknown;
			Vector2i size = default;
			switch (header.ColorType)
			{
				case PngColorType.Grayscale:
					image8 = new Image8(header.Width, header.Height,
						  header.BitDepth == 1 ? Palettes.BlackAndWhite
						: header.BitDepth == 2 ? Palettes.Grayscale4A
						: header.BitDepth == 4 ? Palettes.Grayscale16A
						: Palettes.Grayscale256);
					colorFormat = header.BitDepth == 1 ? ImageFileColorFormat.BlackAndWhite1Bit
						: header.BitDepth <= 8 ? ImageFileColorFormat.Gray8Bit
						: ImageFileColorFormat.Gray16Bit;
					size = image8.Size;
					break;

				case PngColorType.Rgb:
					colorFormat = header.BitDepth <= 8 ? ImageFileColorFormat.Rgb24Bit
						: ImageFileColorFormat.Rgb48Bit;
					if (preferredImageType == PreferredImageType.Image32)
					{
						image32 = new Image32(header.Width, header.Height);
						size = image32.Size;
					}
					else
					{
						image24 = new Image24(header.Width, header.Height);
						size = image24.Size;
					}
					break;

				case PngColorType.Paletted:
					image8 = new Image8(header.Width, header.Height, (IEnumerable<Color32>?)palette);
					colorFormat = header.BitDepth <= 4 ? ImageFileColorFormat.Paletted4Bit
						: ImageFileColorFormat.Paletted8Bit;
					size = image8.Size;
					break;

				case PngColorType.GrayscaleAlpha:
					image32 = new Image32(header.Width, header.Height);
					colorFormat = header.BitDepth <= 8 ? ImageFileColorFormat.GrayAlpha8Bit
						: ImageFileColorFormat.GrayAlpha16Bit;
					size = image32.Size;
					break;

				case PngColorType.Rgba:
					colorFormat = header.BitDepth <= 8 ? ImageFileColorFormat.Rgba32Bit
						: ImageFileColorFormat.Rgba64Bit;
					if (preferredImageType == PreferredImageType.Image24)
					{
						image24 = new Image24(header.Width, header.Height);
						size = image24.Size;
					}
					else
					{
						image32 = new Image32(header.Width, header.Height);
						size = image32.Size;
					}
					break;

				default:
					throw new PngDecodeException($"Unsupported color type: {(int)header.ColorType}");
			}


			if (header.InterlaceMethod != PngInterlaceMethod.None)
			{
				// This image is stored interlaced.  We need to make 7 passes over the data, decoding
				// 7 successive images of smaller sizes into the data buffers at the correct positions.
				if (!DecodeInterlaced(image32!, image24!, image8!, rawImageData, header))
					return null;
			}
			else
			{
				// Decode the one-and-only data pass.
				int interlaceOffset = 0;
				if (DecodeOnePass(header.FilterMethod, rawImageData, header.Width, header.Height,
					image32!, image24!, image8!, header.ColorType, header.BitDepth, interlaceOffset, 1, header.Width) < 0)
					return null;
			}

			return new ImageLoadResult(colorFormat, size,
				(image32 as IImage) ?? (image24 as IImage) ?? (image8 as IImage), metadata);
		}

		private Dictionary<string, object> DecodeMetadata(PngIhdrChunk header,
			IEnumerable<IPngChunk> pngChunks)
		{
			Dictionary<string, object> metadata = new Dictionary<string, object>();

			foreach (IPngChunk pngChunk in pngChunks)
			{
				if (pngChunk is PngGamaChunk gama)
				{
					metadata.Add(nameof(ImageMetadataKey.Gamma), gama.Gamma);
				}
				else if (pngChunk is PngChrmChunk chrm)
				{
					metadata.Add(nameof(ImageMetadataKey.ChromaticityRedX), chrm.RedX);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityRedY), chrm.RedY);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityGreenX), chrm.GreenX);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityGreenY), chrm.GreenY);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityBlueX), chrm.BlueX);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityBlueY), chrm.BlueY);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityWhitePointX), chrm.WhitePointX);
					metadata.Add(nameof(ImageMetadataKey.ChromaticityWhitePointY), chrm.WhitePointY);
				}
				else if (pngChunk is PngSrgbChunk srgb)
				{
					metadata.Add(nameof(ImageMetadataKey.SrgbRenderingIntent), srgb.RenderingIntent.ToString());
				}
				else if (pngChunk is PngIccpChunk iccp)
				{
					metadata.Add(nameof(ImageMetadataKey.IccpProfile), iccp.Profile);
					metadata.Add(nameof(ImageMetadataKey.IccpProfileName), iccp.ProfileName);
				}
				else if (pngChunk is IPngTextChunk text)
				{
					metadata.Add(text.Keyword.ToLowerInvariant() switch
					{
						"title" => "Title",
						"author" => "Author",
						"copyright" => "Copyright",
						"description" => "Description",
						"comment" => "Comment",
						"disclaimer" => "Disclaimer",
						"warning" => "Warning",
						"software" => "Software",
						_ => "." + text.Keyword
					}, text.Text);
				}
				else if (pngChunk is PngPhysChunk phys)
				{
					metadata.Add(nameof(ImageMetadataKey.PixelsPerMeterX), phys.XAxis);
					metadata.Add(nameof(ImageMetadataKey.PixelsPerMeterY), phys.YAxis);
				}
				else if (pngChunk is PngTimeChunk time)
				{
					metadata.Add(nameof(ImageMetadataKey.Timestamp), time.DateTime);
				}
			}

			return metadata;
		}

		private readonly struct PngInterlacingInfo
		{
			public int XOffset => _xOffset;
			private readonly sbyte _xOffset;

			public int YOffset => _yOffset;
			private readonly sbyte _yOffset;

			public int ColSpan => 1 << _widthShift;
			public int WidthShift => _widthShift;
			private readonly sbyte _widthShift;

			public int RowSpan => 1 << _heightShift;
			public int HeightShift => _heightShift;
			private readonly sbyte _heightShift;

			public PngInterlacingInfo(int xOffset, int yOffset, int widthShift, int heightShift)
			{
				_xOffset = (sbyte)xOffset;
				_yOffset = (sbyte)yOffset;
				_widthShift = (sbyte)widthShift;
				_heightShift = (sbyte)heightShift;
			}
		}

		private static readonly PngInterlacingInfo[] _passes =
		{
			new PngInterlacingInfo(xOffset: 0, yOffset: 0, widthShift: 3, heightShift: 3),
			new PngInterlacingInfo(xOffset: 4, yOffset: 0, widthShift: 3, heightShift: 3),
			new PngInterlacingInfo(xOffset: 0, yOffset: 4, widthShift: 2, heightShift: 3),
			new PngInterlacingInfo(xOffset: 2, yOffset: 0, widthShift: 2, heightShift: 2),
			new PngInterlacingInfo(xOffset: 0, yOffset: 2, widthShift: 1, heightShift: 2),
			new PngInterlacingInfo(xOffset: 1, yOffset: 0, widthShift: 1, heightShift: 1),
			new PngInterlacingInfo(xOffset: 0, yOffset: 1, widthShift: 0, heightShift: 1),
		};

		private bool DecodeInterlaced(Image32 image32, Image24 image24, Image8 image8, Span<byte> rawImageData,
			PngIhdrChunk header)
		{
			for (int pass = 0; pass < 7; pass++)
			{
				PngInterlacingInfo passInfo = _passes[pass];

				if (passInfo.XOffset >= header.Width || passInfo.YOffset >= header.Height)
					continue;   // Empty-pass corner case, explicitly called out in the PNG spec.

				int width = (header.Width + (passInfo.ColSpan - 1) - passInfo.XOffset) >> passInfo.WidthShift;
				int height = (header.Height + (passInfo.RowSpan - 1) - passInfo.YOffset) >> passInfo.HeightShift;

				int interlaceOffset = passInfo.XOffset + passInfo.YOffset * header.Width;

				// Decode the pixels associated with this pass into the correct
				// positions in the image data.
				int bytesConsumed = DecodeOnePass(header.FilterMethod, rawImageData, width, height,
					image32, image24, image8, header.ColorType, header.BitDepth, interlaceOffset,
					passInfo.ColSpan, passInfo.RowSpan * header.Width);
				if (bytesConsumed < 0)
					return false;

				rawImageData = rawImageData.Slice(bytesConsumed);
			}

			return true;
		}

		private int DecodeOnePass(PngFilterMethod filterMethod, Span<byte> rawImageData, int width, int height,
			Image32 image32, Image24 image24, Image8 image8, PngColorType colorType, int bitDepth, int interlaceOffset, int colSpan, int rowSpan)
		{
			// Unapply the image filter to the raw byte data, if there is an image filter applied.
			// The result of this will be a new byte array that is completely unfiltered (raw image pixels).
			if (filterMethod != PngFilterMethod.Filtered)
				throw new PngDecodeException($"PNG filter method not supported: {(int)filterMethod}");

			int result = PngFiltering.Unfilter(rawImageData, width, height, colorType, bitDepth);

			switch (colorType)
			{
				case PngColorType.Grayscale:
					return LoadPng8Bit(image8.Data.AsSpan().Slice(interlaceOffset),
						width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;

				case PngColorType.Rgb:
					if (image32 != null)
						return LoadPngRgbAsImage32(image32!.Data.AsSpan().Slice(interlaceOffset),
							width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;
					else
						return LoadPngRgb(image24!.Data.AsSpan().Slice(interlaceOffset),
							width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;

				case PngColorType.Paletted:
					return LoadPng8Bit(image8!.Data.AsSpan().Slice(interlaceOffset),
						width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;

				case PngColorType.GrayscaleAlpha:
					return LoadPngGrayscaleAlpha(image32!.Data.AsSpan().Slice(interlaceOffset),
						width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;

				case PngColorType.Rgba:
					if (image24 != null)
						return LoadPngRgbaAsImage24(image24!.Data.AsSpan().Slice(interlaceOffset),
							width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;
					else
						return LoadPngRgba(image32!.Data.AsSpan().Slice(interlaceOffset),
							width, height, rawImageData, bitDepth, colSpan, rowSpan) >= 0 ? result : -1;

				default:
					throw new PngDecodeException($"Unsupported PNG color type: {(int)colorType}");
			}
		}

		/// <summary>
		/// Load an RGB-form PNG image, either 48 bits or 24 bits, into the given color buffer.
		/// </summary>
		/// <param name="data">The color buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPngRgb(Span<Color24> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 8)
			{
				// 3 bytes per pixel, in order of R,G,B.
				if (rawImageData.Length < width * height * 3)
					return -1;
				fixed (Color24* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color24* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color24* dest = destRow;
						for (int x = 0; x < width; x++, src += 3, dest += colStep)
							*dest = new Color24(src[0], src[1], src[2]);
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 16)
			{
				// 6 bytes per pixel, in order of R,R,G,G,B,B.
				if (rawImageData.Length < width * height * 6)
					return -1;
				fixed (Color24* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color24* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color24* dest = destRow;
						for (int x = 0; x < width; x++, src += 6, dest += colStep)
							*dest = new Color24(src[0], src[2], src[4]);
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}

		/// <summary>
		/// Load an RGB-form PNG image, either 48 bits or 24 bits, into the given color buffer,
		/// adding a default opaque alpha channel.
		/// </summary>
		/// <param name="data">The color buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPngRgbAsImage32(Span<Color32> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 8)
			{
				// 3 bytes per pixel, in order of R,G,B.
				if (rawImageData.Length < width * height * 3)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 3, dest += colStep)
							*dest = new Color32(src[0], src[1], src[2]);
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 16)
			{
				// 6 bytes per pixel, in order of R,R,G,G,B,B.
				if (rawImageData.Length < width * height * 6)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 6, dest += colStep)
							*dest = new Color32(src[0], src[2], src[4]);
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}

		/// <summary>
		/// Load an RGBA-form PNG image, either 64 bits or 32 bits, into the given color buffer.
		/// </summary>
		/// <param name="data">The color buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPngRgba(Span<Color32> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 8)
			{
				// 4 bytes per pixel, in order of R,G,B,A.
				if (rawImageData.Length < width * height * 4)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 4, dest += colStep)
							*dest = new Color32(src[0], src[1], src[2], src[3]);
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 16)
			{
				// 8 bytes per pixel, in order of R,R,G,G,B,B,A,A.
				if (rawImageData.Length < width * height * 8)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 8, dest += colStep)
							*dest = new Color32(src[0], src[2], src[4], src[6]);
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}

		/// <summary>
		/// Load an RGBA-form PNG image, either 64 bits or 32 bits, into the given
		/// 24-bit color buffer, discarding the alpha channel.
		/// </summary>
		/// <param name="data">The color buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPngRgbaAsImage24(Span<Color24> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 8)
			{
				// 4 bytes per pixel, in order of R,G,B,A.
				if (rawImageData.Length < width * height * 4)
					return -1;
				fixed (Color24* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color24* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color24* dest = destRow;
						for (int x = 0; x < width; x++, src += 4, dest += colStep)
							*dest = new Color24(src[0], src[1], src[2]);
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 16)
			{
				// 8 bytes per pixel, in order of R,R,G,G,B,B,A,A.
				if (rawImageData.Length < width * height * 8)
					return -1;
				fixed (Color24* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color24* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color24* dest = destRow;
						for (int x = 0; x < width; x++, src += 8, dest += colStep)
							*dest = new Color24(src[0], src[2], src[4]);
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}

		/// <summary>
		/// Load a paletted PNG image, between 1 and 8 bits per source pixel, into the given
		/// destination 8-bit pixel buffer.
		/// </summary>
		/// <param name="data">The destination pixel buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPng8Bit(Span<byte> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 16)
			{
				if (rawImageData.Length < width * height * 2)
					return -1;
				fixed (byte* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					byte* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						byte* dest = destRow;
						for (int x = 0; x < width; x++, dest += colStep, src += 2)
							*dest = *src;
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 8)
			{
				if (rawImageData.Length < width * height)
					return -1;
				fixed (byte* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					byte* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						byte* dest = destRow;
						for (int x = 0; x < width; x++, dest += colStep)
							*dest = *src++;
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 1)
			{
				// Expand each packed set of pixels to individual byte values.
				if (rawImageData.Length * 8 < width * height)
					return -1;
				fixed (byte* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					byte* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						byte* dest = destRow;
						for (int x = 0; x < width; x++, dest += colStep)
						{
							int srcIndex = x >> 3;
							int bitOffset = (7 - x) & 7;
							*dest = (byte)(((uint)src[srcIndex] >> bitOffset) & 1);
						}
						src += (width + 7) >> 3;
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 2)
			{
				if (rawImageData.Length * 4 < width * height)
					return -1;
				fixed (byte* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					byte* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						byte* dest = destRow;
						for (int x = 0; x < width; x++, dest += colStep)
						{
							int srcIndex = x >> 2;
							int bitOffset = ((3 - x) & 3) << 1;
							*dest = (byte)(((uint)src[srcIndex] >> bitOffset) & 3);

						}
						src += (width + 3) >> 2;
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 4)
			{
				if (rawImageData.Length * 2 < width * height)
					return -1;
				fixed (byte* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					byte* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						byte* dest = destRow;
						for (int x = 0; x < width; x++, dest += colStep)
						{
							int srcIndex = x >> 1;
							int bitOffset = ((1 - x) & 1) << 2;
							*dest = (byte)(((uint)src[srcIndex] >> bitOffset) & 15);

						}
						src += (width + 1) >> 1;
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}

		/// <summary>
		/// Load an GrayAlpha-form PNG image, either 32 bits or 16 bits, into the given
		/// color buffer, promoting/demoting the source data to 32-bit RGBA.
		/// </summary>
		/// <param name="data">The color buffer to write to.</param>
		/// <param name="width">The width of the image to decode.</param>
		/// <param name="height">The height of the image to decode.</param>
		/// <param name="rawImageData">The raw image data to read from.</param>
		/// <param name="bitDepth">The PNG bit depth value.</param>
		/// <param name="colStep">How many pixels to move forward for each column.</param>
		/// <param name="rowStep">How many pixels to move down for each row.</param>
		/// <returns>The number of bytes consumed in the provided rawImageData (-1 means failure).</returns>
		private static unsafe int LoadPngGrayscaleAlpha(Span<Color32> data, int width, int height,
			ReadOnlySpan<byte> rawImageData, int bitDepth, int colStep, int rowStep)
		{
			if (bitDepth == 8)
			{
				// 2 bytes per pixel, in order of G,A.
				if (rawImageData.Length < width * height * 2)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 2, dest += colStep)
							*dest = new Color32(src[0], src[0], src[0], src[1]);
					}
					return (int)(src - srcBase);
				}
			}
			else if (bitDepth == 16)
			{
				// 4 bytes per pixel, in order of G,G,A,A.
				if (rawImageData.Length < width * height * 4)
					return -1;
				fixed (Color32* destBase = data)
				fixed (byte* srcBase = rawImageData)
				{
					Color32* destRow = destBase;
					byte* src = srcBase;
					for (int y = 0; y < height; y++, destRow += rowStep)
					{
						Color32* dest = destRow;
						for (int x = 0; x < width; x++, src += 4, dest += colStep)
							*dest = new Color32(src[0], src[0], src[0], src[2]);
					}
					return (int)(src - srcBase);
				}
			}
			else return -1;
		}
	}
}
