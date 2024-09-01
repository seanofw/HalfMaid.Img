using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HalfMaid.Img.Compression;
using HalfMaid.Img.FileFormats.Png.Chunks;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// This class knows how to save images as PNG files.
	/// </summary>
	public class PngSaver : IImageSaver
	{
		/// <inheritdoc />
		public ImageFormat Format => ImageFormat.Png;

		/// <inheritdoc />
		public string Title => "PNG";

		/// <inheritdoc />
		public string DefaultExtension => ".png";

		private static readonly PngSaveOptions _defaultOptions = new PngSaveOptions();

		private static readonly IReadOnlyDictionary<string, object> _emptyDictionary
			= new Dictionary<string, object>();

		private static readonly byte[] _pngSignature = new byte[]
		{
			137, 80, 78, 71, 13, 10, 26, 10
		};


		private static readonly HashSet<string> _textChunkMetadataKeys = new HashSet<string>
		{
			ImageMetadataKey.Title,
			ImageMetadataKey.Author,
			ImageMetadataKey.Copyright,
			ImageMetadataKey.Description,
			ImageMetadataKey.Comment,
			ImageMetadataKey.Disclaimer,
			ImageMetadataKey.Warning,
			ImageMetadataKey.Software,
		};

		/// <summary>
		/// Save a 32-bit RGBA image as a PNG file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  PNG
		/// supports most of the standard image-metadata keys natively, and you may also
		/// include custom text metadata by starting a key with a '.' character.</param>
		/// <param name="fileSaveOptions">Save options.  If provided, this should be a
		/// PngSaveOptions object.</param>
		/// <returns>The resulting PNG file, as a byte array.</returns>
		public byte[] SaveImage(Image32 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			PngSaveOptions options = fileSaveOptions as PngSaveOptions ?? _defaultOptions;

			List<IPngChunk> chunks = new List<IPngChunk>();

			// Write the IHDR.
			chunks.Add(new PngIhdrChunk(image.Width, image.Height, 8,
				options.IncludeAlpha ? PngColorType.Rgba : PngColorType.Rgb,
				PngCompressionMethod.Deflate, PngFilterMethod.Filtered, PngInterlaceMethod.None));

			// Write any required metadata just before the IDAT chunks start.
			chunks.AddRange(WriteMetadata(imageMetadata ?? _emptyDictionary));

			// Write any required metadata just before the IDAT chunks start.
			chunks.AddRange(WriteMetadata(imageMetadata ?? _emptyDictionary));

			// In paletted modes, there's often no value in using filtering,
			// but we're obligated to emit a filter byte anyway for every scan line.
			// And in grayscale mode, there can be as much value in filtering
			// as in RGB modes.  So we might as well use the same code path for
			// both and apply filtering for everything, because it may be beneficial.
			ReadOnlySpan<byte> rawData = options.IncludeAlpha
				? MemoryMarshal.Cast<Color32, byte>(image.Data.AsSpan())
				: MemoryMarshal.Cast<Color24, byte>(image.ToImage24().Data.AsSpan());
			byte[] filteredData = PngFiltering.FilterWholeImage(rawData, image.Width, image.Height,
				options.IncludeAlpha ? 4 : 3, options.FilterType);

			// Compress the filtered data into one or more IDAT chunks.
			chunks.AddRange(CompressFilteredDataToIDatChunks(filteredData, options));

			// Write the IEND.
			chunks.Add(new PngIEndChunk());

			// Finally, serialize the chunks to raw bytes.
			return ConvertChunksToBytes(chunks);
		}

		/// <summary>
		/// Save a 24-bit RGB image as a PNG file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  PNG
		/// supports most of the standard image-metadata keys natively, and you may also
		/// include custom text metadata by starting a key with a '.' character.</param>
		/// <param name="fileSaveOptions">Save options.  If provided, this should be a
		/// PngSaveOptions object.</param>
		/// <returns>The resulting PNG file, as a byte array.</returns>
		public byte[] SaveImage(Image24 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			PngSaveOptions options = fileSaveOptions as PngSaveOptions ?? _defaultOptions;

			List<IPngChunk> chunks = new List<IPngChunk>();

			// Write the IHDR.
			chunks.Add(new PngIhdrChunk(image.Width, image.Height, 8, PngColorType.Rgb,
				PngCompressionMethod.Deflate, PngFilterMethod.Filtered, PngInterlaceMethod.None));

			// Write any required metadata just before the IDAT chunks start.
			chunks.AddRange(WriteMetadata(imageMetadata ?? _emptyDictionary));

			// Write any required metadata just before the IDAT chunks start.
			chunks.AddRange(WriteMetadata(imageMetadata ?? _emptyDictionary));

			// In paletted modes, there's often no value in using filtering,
			// but we're obligated to emit a filter byte anyway for every scan line.
			// And in grayscale mode, there can be as much value in filtering
			// as in RGB modes.  So we might as well use the same code path for
			// both and apply filtering for everything, because it may be beneficial.
			ReadOnlySpan<byte> rawData = MemoryMarshal.Cast<Color24, byte>(image.Data.AsSpan());
			byte[] filteredData = PngFiltering.FilterWholeImage(rawData, image.Width, image.Height,
				3, options.FilterType);

			// Compress the filtered data into one or more IDAT chunks.
			chunks.AddRange(CompressFilteredDataToIDatChunks(filteredData, options));

			// Write the IEND.
			chunks.Add(new PngIEndChunk());

			// Finally, serialize the chunks to raw bytes.
			return ConvertChunksToBytes(chunks);
		}

		/// <summary>
		/// Save an 8-bit paletted image as a PNG file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="imageMetadata">Optional metadata to include with the image.  PNG
		/// supports most of the standard image-metadata keys natively, and you may also
		/// include custom text metadata by starting a key with a '.' character.</param>
		/// <param name="fileSaveOptions">Save options.  If provided, this should be a
		/// PngSaveOptions object.</param>
		/// <returns>The resulting PNG file, as a byte array.</returns>
		public byte[] SaveImage(Image8 image,
			IReadOnlyDictionary<string, object>? imageMetadata = null,
			IFileSaveOptions? fileSaveOptions = null)
		{
			PngSaveOptions options = fileSaveOptions as PngSaveOptions ?? _defaultOptions;

			List<IPngChunk> chunks = new List<IPngChunk>();

			bool isGrayscale = image.IsGrayscale256();

			// Write the IHDR.
			chunks.Add(new PngIhdrChunk(image.Width, image.Height, 8,
				isGrayscale ? PngColorType.Grayscale : PngColorType.Paletted,
				PngCompressionMethod.Deflate, PngFilterMethod.Filtered, PngInterlaceMethod.None));

			if (!isGrayscale)
			{
				// This needs a PLTE chunk, since it's not grayscale, and possibly a tRNS chunk too.
				chunks.AddRange(WritePaletteAndTransparencyChunks(image.Palette));
			}

			// Write any required metadata just before the IDAT chunks start.
			chunks.AddRange(WriteMetadata(imageMetadata ?? _emptyDictionary));

			// In paletted modes, there's often no value in using filtering,
			// but we're obligated to emit a filter byte anyway for every scan line.
			// And in grayscale mode, there can be as much value in filtering
			// as in RGB modes.  So we might as well use the same code path for
			// both and apply filtering for everything, because it may be beneficial.
			byte[] filteredData = PngFiltering.FilterWholeImage(image.Data, image.Width, image.Height, 1,
				options.FilterType);

			// Compress the filtered data into one or more IDAT chunks.
			chunks.AddRange(CompressFilteredDataToIDatChunks(filteredData, options));

			// Write the IEND.
			chunks.Add(new PngIEndChunk());

			// Finally, serialize the chunks to raw bytes.
			return ConvertChunksToBytes(chunks);
		}

		private static List<PngIDatChunk> CompressFilteredDataToIDatChunks(ReadOnlySpan<byte> filteredData,
			PngSaveOptions options)
		{
			List<PngIDatChunk> chunks = new List<PngIDatChunk>();

			// Compress the resulting filtered data, zlib-style.
			byte[] compressedData = Zlib.Deflate(filteredData, options.CompressionLevel);

			// Break the compressed data into 16 Kb(-ish) IDAT chunks to be
			// nicer to other PNG decoders.  It's arguable how much of a difference
			// this makes in the modern era, but keeping the sizes smaller does
			// ensure maximum compatibility even with really low-end equipment.
			for (int i = 0; i < compressedData.Length; i += 16384)
			{
				int chunkLength = Math.Min(16384, compressedData.Length - i);
				chunks.Add(new PngIDatChunk(compressedData.AsSpan().Slice(i, chunkLength).ToArray()));
			}

			return chunks;
		}

		private static IReadOnlyCollection<IPngChunk> WriteMetadata(IReadOnlyDictionary<string, object> metadata)
		{
			List<IPngChunk> chunks = new List<IPngChunk>();

			// Add the pHYS chunk, if "PixelsPerMeterX" and "...Y" are provided in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.PixelsPerMeterX, out object? ppmXObject)
				&& metadata.TryGetValue(ImageMetadataKey.PixelsPerMeterY, out object? ppmYObject))
			{
				double ppmX = Convert.ToDouble(ppmXObject);
				double ppmY = Convert.ToDouble(ppmYObject);
				chunks.Add(new PngPhysChunk((uint)(int)(ppmX + 0.5), (uint)(int)(ppmY + 0.5),
					PngPhysUnits.PerMeter));
			}

			// Add the cHRM chunk, if ALL of the various chromatiticy values are provided
			// in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.ChromaticityWhitePointX, out object? whitePointX)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityWhitePointY, out object? whitePointY)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityRedX, out object? redX)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityRedY, out object? redY)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityGreenX, out object? greenX)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityGreenY, out object? greenY)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityBlueX, out object? blueX)
				&& metadata.TryGetValue(ImageMetadataKey.ChromaticityBlueY, out object? blueY))
			{
				chunks.Add(new PngChrmChunk(
					Convert.ToDouble(whitePointX), Convert.ToDouble(whitePointY),
					Convert.ToDouble(redX), Convert.ToDouble(redY),
					Convert.ToDouble(greenX), Convert.ToDouble(greenY),
					Convert.ToDouble(blueX), Convert.ToDouble(blueY)));
			}

			// Add the sRGB chunk, if "SrgbRenderingIntent" is provided in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.SrgbRenderingIntent, out object? renderingIntentObject))
			{
				if (!(renderingIntentObject is PngRenderingIntent renderingIntent))
				{
					string str = renderingIntentObject?.ToString() ?? string.Empty;
					renderingIntent = Enum.TryParse(str, out PngRenderingIntent ri) ? ri : default;
				}
				chunks.Add(new PngSrgbChunk(renderingIntent));
			}

			// Add the gAMA chunk, if "Gamma" is provided in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.Gamma, out object? gammaObject))
			{
				double gamma = Convert.ToDouble(gammaObject);
				chunks.Add(new PngGamaChunk(gamma));
			}

			// Add the iCCP chunk, if the ICCP fields are provided in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.IccpProfileName, out object? iccpProfileName)
				&& metadata.TryGetValue(ImageMetadataKey.IccpProfile, out object? iccpProfile)
				&& iccpProfileName is string profileName
				&& iccpProfile is byte[] profileBytes)
			{
				chunks.Add(new PngIccpChunk(profileName, profileBytes));
			}

			// Add the tIMe chunk, if a timestamp is provided in the metadata.
			if (metadata.TryGetValue(ImageMetadataKey.Timestamp, out object? timestampObject)
				&& timestampObject is DateTime dateTime)
			{
				chunks.Add(new PngTimeChunk(dateTime));
			}

			// Add various text chunks, if they're included in the metadata.  We switch
			// between tEXt/zTXt/iTXt as necessary for optimal compression.
			HashSet<string> usedKeys = new HashSet<string>();
			foreach (KeyValuePair<string, object> pair in metadata)
			{
				if (!_textChunkMetadataKeys.Contains(pair.Key)
					&& !pair.Key.StartsWith("."))
					continue;

				string key = pair.Key.StartsWith(".") ? pair.Key.Substring(1) : pair.Key;

				if (!usedKeys.Add(key))
					continue;

				string value = pair.Value?.ToString() ?? string.Empty;
				if (!IsEntirelyLatin1(value))
					chunks.Add(new PngItxtChunk(key, string.Empty, string.Empty, value));
				else
					chunks.Add(value.Length <= 10
						? new PngTextChunk(key, value) : new PngZtxtChunk(key, value));
			}

			return chunks;
		}

		/// <summary>
		/// Determine if the given string uses only characters in ISO Latin-1.
		/// </summary>
		private static bool IsEntirelyLatin1(string str)
		{
			foreach (char ch in str)
				if (ch > 255)
					return false;
			return true;
		}

		/// <summary>
		/// For paletted images, write the PLTE and tRNS chunks, as necessary.
		/// </summary>
		/// <param name="palette">The palette.</param>
		/// <returns>A collection of the resulting PLTE and tRNS chunks, as needed.</returns>
		private static IReadOnlyCollection<IPngChunk> WritePaletteAndTransparencyChunks(Color32[]? palette)
		{
			List<IPngChunk> chunks = new List<IPngChunk>();

			// Add the palette, if this has a palette.
			if (palette == null || palette.Length == 0)
				return chunks;
			chunks.Add(new PngPlteChunk(palette));

			// Decide if this has any transparent or translucent colors in the palette.
			if (!palette.Any(c => c.A != 255))
				return chunks;

			// This needs a tRNS chunk to represent the alpha/transparency data.  So
			// find out how much data the tRNS chunk actually requires.
			int end = palette.Length;
			while (end > 0 && palette[end - 1].A == 255)
				end--;

			// Copy the alpha values into an array just large enough for the
			// critical data.
			byte[] alphaBytes = new byte[end];
			for (int i = 0; i < end; i++)
				alphaBytes[i] = palette[i].A;

			// Write the tRNS chunk.
			chunks.Add(new PngTrnsChunk(alphaBytes));
			return chunks;
		}

		/// <summary>
		/// Convert the given sequence of PNG chunks to a finished PNG file.  The first
		/// chunk must be an IHDR chunk, and the last must be IEND, as per the PNG spec.
		/// This method gives you a lot of control in emitting more complicated files
		/// than SaveImage() can, since those construct IPngChunk sequences and then
		/// invoke this.
		/// </summary>
		/// <param name="chunks">The chunks to write.</param>
		/// <returns>The finished chunks.</returns>
		public unsafe byte[] ConvertChunksToBytes(IEnumerable<IPngChunk> chunks)
		{
			using OutputWriter outputWriter = new OutputWriter();

			outputWriter.Write(_pngSignature);

			bool isFirst = true;
			IPngChunk? lastChunk = null;
			foreach (IPngChunk chunk in chunks)
			{
				if (isFirst && !(chunk is PngIhdrChunk))
					throw new ArgumentException("First PNG chunk must be an IHDR chunk.");

				int chunkStart = outputWriter.Length;

				// Write a placeholder for the chunk length.
				outputWriter.WriteBE(0);

				// Write the chunk's type (name).
				outputWriter.WriteByte((byte)chunk.Type[0]);
				outputWriter.WriteByte((byte)chunk.Type[1]);
				outputWriter.WriteByte((byte)chunk.Type[2]);
				outputWriter.WriteByte((byte)chunk.Type[3]);

				// Write the chunk's data, if any.
				int dataStart = outputWriter.Length;
				chunk.WriteData(outputWriter);
				int dataEnd = outputWriter.Length;

				int chunkEnd = outputWriter.Length;

				// Calculate the CRC-32 of the written chunk.
				uint crc32 = Checksums.Crc32(outputWriter.Data.Slice(chunkStart + 4, chunkEnd - (chunkStart + 4)));
				outputWriter.WriteBE(crc32);

				// Go back and write the CRC-32 of the chunk.
				int chunkLength = dataEnd - dataStart;
				outputWriter.WriteByteAt(chunkStart    , (byte)((chunkLength >> 24) & 0xFF));
				outputWriter.WriteByteAt(chunkStart + 1, (byte)((chunkLength >> 16) & 0xFF));
				outputWriter.WriteByteAt(chunkStart + 2, (byte)((chunkLength >>  8) & 0xFF));
				outputWriter.WriteByteAt(chunkStart + 3, (byte)( chunkLength        & 0xFF));

				isFirst = false;
				lastChunk = chunk;
			}
			if (!(lastChunk is PngIEndChunk))
				throw new ArgumentException("Last PNG chunk must be an IEND chunk.");

			return outputWriter.Finish();
		}
	}
}
