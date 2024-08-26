using System;

namespace HalfMaid.Img.Compression
{
	/// <summary>
	/// Various checksum algorithms.
	/// </summary>
	public static class Checksums
    {
		private static uint[]? _crcTable;

		/// <summary>
		/// Calculate the Adler-32 checksum of the given data buffer.
		/// </summary>
		/// <param name="buffer">The buffer to checksum.</param>
		/// <param name="seed">The initial seed (defaults to 1, and may be used to
		/// "continue" a checksum).</param>
		/// <returns>A checksum of the data buffer, which can be used to detect errors in it.</returns>
		public static unsafe uint Adler32(ReadOnlySpan<byte> buffer, uint seed = 1)
		{
			int length = buffer.Length;

			uint a = seed & 0xFFFF;
			uint b = seed >> 16 & 0xFFFF;

			fixed (byte* bufferBase = buffer)
			{
				for (int i = 0; i < length;)
				{
					int m = Math.Min(length - i, 2654) + i;

					for (; i < m; i++)
					{
						a += bufferBase[i];
						b += a;
					}

					a = 15 * (a >> 16) + (a & 65535);
					b = 15 * (b >> 16) + (b & 65535);
				}
			}

			return b % 65521 << 16 | a % 65521;
		}

		/// <summary>
		/// Calculate a table of all 8-byte values for performing CRC-32 quickly.
		/// </summary>
		/// <returns>The table of CRC-32 values.</returns>
		private static uint[] MakeCrcTable()
		{
			uint[] crcTable = new uint[256];

			for (uint n = 0; n < 256; n++)
			{
				uint c = n;
				for (uint k = 0; k < 8; k++)
				{
					if ((c & 1) != 0)
						c = 0xEDB88320U ^ (c >> 1);
					else
						c >>= 1;
				}
				crcTable[n] = c;
			}

			return crcTable;
		}

		/// <summary>
		/// Update a running CRC-32 with the given data.
		/// </summary>
		/// <remarks>
		/// This automatically performs the required 1's complement operation before
		/// and after updating the CRC-32.
		/// </remarks>
		/// <param name="buffer">The buffer of data to include in the CRC-32.</param>
		/// <param name="crc">The CRC-32 to update (0 initially).</param>
		/// <returns>The calculated CRC-32 value.</returns>
		public static unsafe uint Crc32(ReadOnlySpan<byte> buffer, uint crc = 0)
		{
			uint c = crc ^ 0xFFFFFFFFU;

			_crcTable ??= MakeCrcTable();

			fixed (uint* crcTable = _crcTable)
			fixed (byte* data = buffer)
			{
				int length = buffer.Length;
				for (int n = 0; n < length; n++)
					c = crcTable[(c ^ data[n]) & 0xFF] ^ (c >> 8);
			}

			return c ^ 0xFFFFFFFFU;
		}
	}
}
