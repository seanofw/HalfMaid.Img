using System;
using System.IO.Compression;
using System.IO;

namespace HalfMaid.Img.Compression
{
	/// <summary>
	/// Support code for zlib-style deflate/inflate.  .NET includes a good
	/// implementation of "raw" deflate/inflate, but it didn't support the
	/// zlib format prior to .NET 6, so this provides zlib compatibility
	/// all the way back to the .NET Framework 4.x era.
	/// </summary>
	public static class Zlib
    {
		/// <summary>
		/// There are only 32 valid zlib header values.  This table is indexed as CINFO
		/// (low 3 bits) and FLEVEL (next 2 bits).
		/// </summary>
		private static readonly byte[] _zlibChecksums = new byte[]
		{
			0x1D, 0x19, 0x15, 0x11, 0x0D, 0x09, 0x05, 0x01,
			0x5B, 0x57, 0x53, 0x4F, 0x4B, 0x47, 0x43, 0x5E,
			0x99, 0x95, 0x91, 0x8D, 0x89, 0x85, 0x81, 0x9C,
			0xD7, 0xD3, 0xCF, 0xCB, 0xC7, 0xC3, 0xDE, 0xDA,
		};

		/// <summary>
		/// Deflate the given uncompressed data using zlib-style compression.
		/// </summary>
		/// <remarks>
		/// ZLibStream is included only in .NET 6 and newer, so we include this as a way
		/// to be backward-compatible all the way back to the .NET Framework 4.x era.
		/// </remarks>
		/// <param name="uncompressedData">The source data to compress.</param>
		/// <param name="level">How heavily to compress the data vs. how fast to compress the data.</param>
		/// <returns>The source data compressed into a new byte array.</returns>
		public unsafe static byte[] Deflate(ReadOnlySpan<byte> uncompressedData, CompressionLevel? level = null)
        {
            fixed (byte* srcBase = uncompressedData)
            {
				MemoryStream outputStream = new MemoryStream(uncompressedData.Length);

				const int cinfo = 7;
				int flevel =
					 (level == CompressionLevel.Optimal ? 2
					: level == CompressionLevel.Fastest ? 1
					: level == CompressionLevel.NoCompression ? 0
#if NET6_0_OR_GREATER
					: level == CompressionLevel.SmallestSize ? 3
#endif
					: 2);

				outputStream.WriteByte((cinfo << 4) | 8);
				outputStream.WriteByte(_zlibChecksums[(flevel << 3) | cinfo]);

				using UnmanagedMemoryStream inputStream = new UnmanagedMemoryStream(srcBase, uncompressedData.Length);
				using (DeflateStream deflateStream = level.HasValue
					? new DeflateStream(outputStream, level.Value, leaveOpen: true)
					: new DeflateStream(outputStream, CompressionMode.Compress, leaveOpen: true))
				{
					inputStream.CopyTo(deflateStream);
				}

				uint adler32 = Checksums.Adler32(uncompressedData);

				outputStream.WriteByte((byte)((adler32 >> 24) & 0xFF));
				outputStream.WriteByte((byte)((adler32 >> 16) & 0xFF));
				outputStream.WriteByte((byte)((adler32 >> 8) & 0xFF));
				outputStream.WriteByte((byte)(adler32 & 0xFF));

				byte[] compressedData = outputStream.ToArray();
				return compressedData;
			}
		}

		/// <summary>
		/// Inflate the given zlib-style compressed data.
		/// </summary>
		/// <remarks>
		/// ZLibStream is included only in .NET 6 and newer, so we include this as a way
		/// to be backward-compatible all the way back to the .NET Framework 4.x era.
		/// </remarks>
		/// <param name="compressedData">The source data to decompress.</param>
		/// <returns>The source data decompressed into a new byte array.</returns>
		public unsafe static byte[] Inflate(ReadOnlySpan<byte> compressedData)
        {
            fixed (byte* srcBase = compressedData)
            {
				MemoryStream outputStream = new MemoryStream(compressedData.Length);

                const int ZLibHeaderSize = 2;
                const int ZLibTrailerSize = 4;

                if ((srcBase[0] & 0xF) != 0x8)
                    throw new InvalidDataException("Zlib header contains an unknown/unsupported compression method.");
				if ((srcBase[1] & 0x20) != 0)
					throw new InvalidDataException("Zlib header indicates that decompression requires a custom preset dictionary.");

				int flevel = (srcBase[1] >> 6) & 3;
				int cinfo = (srcBase[0] >> 4) & 7;
				byte expectedChecksum = _zlibChecksums[(flevel << 3) | cinfo];
				if (srcBase[1] != expectedChecksum)
					throw new InvalidDataException($"Zlib header has a checksum value of {srcBase[1] & 0x1F} but should have a checksum value of {expectedChecksum & 0x1F}.");

				using UnmanagedMemoryStream inputStream = new UnmanagedMemoryStream(srcBase + ZLibHeaderSize,
                    compressedData.Length - ZLibHeaderSize - ZLibTrailerSize);
                using DeflateStream deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
                deflateStream.CopyTo(outputStream);
                byte[] uncompressedData = outputStream.ToArray();

                uint actualAdler32 = srcBase[compressedData.Length - 1]
                    | (uint)srcBase[compressedData.Length - 2] << 8
                    | (uint)srcBase[compressedData.Length - 3] << 16
                    | (uint)srcBase[compressedData.Length - 4] << 24;
                uint expectedAdler32 = Checksums.Adler32(uncompressedData);

                if (actualAdler32 != expectedAdler32)
                    throw new InvalidDataException("Adler32 checksum indicates Zlib-compressed data is damaged.");

                return uncompressedData;
            }
        }
    }
}
