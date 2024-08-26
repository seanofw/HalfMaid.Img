using System;
using System.Collections;
using System.Collections.Generic;
using HalfMaid.Img.Compression;
using HalfMaid.Img.FileFormats.Png.Chunks;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// This reads PNG chunks lazily, returning each chunk in the order in which
	/// they're found in the input.  Each chunk will have its CRC-32 validated but
	/// will otherwise be returned as just a 'type' and a byte array.
	/// </summary>
	public struct PngChunkReader : IEnumerable<IPngChunk>
	{
		private unsafe byte* _src;
		private int _length;

		private unsafe PngChunkReader(byte* src, int length)
		{
			_src = src;
			_length = length;
		}

		/// <summary>
		/// Get an enumerator that will yield one PNG chunk at a time.
		/// </summary>
		/// <returns>An enumerator that lazily decodes PNG chunks.</returns>
		public unsafe IEnumerator<IPngChunk> GetEnumerator()
			=> new PngChunkEnumerator(_src, _length);

		/// <summary>
		/// Get an enumerator that will yield one PNG chunk at a time.
		/// </summary>
		/// <returns>An enumerator that lazily decodes PNG chunks.</returns>
		unsafe IEnumerator IEnumerable.GetEnumerator()
			=> new PngChunkEnumerator(_src, _length);

		/// <summary>
		/// Construct a new PNG chunk reader.
		/// </summary>
		/// <param name="src">The data buffer to start reading.</param>
		/// <param name="length">The number of bytes in the data buffer.</param>
		/// <returns>An IEnumerable that yields decoded PNG chunks lazily.</returns>
		public static unsafe IEnumerable<IPngChunk> Read(byte* src, int length)
			=> new PngChunkReader(src, length);

		private class PngChunkEnumerator : IEnumerator<IPngChunk>
		{
			private unsafe byte* _start;
			private int _startLength;
			private unsafe byte* _src;
			private int _length;
			private IPngChunk? _current;

			public unsafe PngChunkEnumerator(byte* src, int length)
			{
				_src = _start = src;
				_length = _startLength = length;
				_current = default;
			}

			public unsafe void Reset()
			{
				_src = _start;
				_length = _startLength;
			}

			public void Dispose() { }

			public IPngChunk Current => _current!;

			object? IEnumerator.Current => Current;

			public unsafe bool MoveNext()
			{
				if (_current != null && _current.Type == "IEND")
				{
					_current = null;
					return false;
				}

				if (_src == _start)
				{
					_src += 8;
					_length -= 8;
				}

				if (_length < 8)
					return false;

				// Extract the chunk's length.
				int Length = (int)(((uint)_src[0] << 24)
					| ((uint)_src[1] << 16)
					| ((uint)_src[2] << 8)
					|  (uint)_src[3]);

				// Extract the chunk's type string (fast!) using the stack as temporary space.
				Span<char> typeChars = stackalloc char[4];
				typeChars[0] = (char)(ushort)_src[4];
				typeChars[1] = (char)(ushort)_src[5];
				typeChars[2] = (char)(ushort)_src[6];
				typeChars[3] = (char)(ushort)_src[7];
				string type = typeChars.ToString();

				if (Length > _length - 12)
					return false;

				// Extract the CRC-32.
				uint crc32 = ((uint)_src[8 + Length] << 24)
					| ((uint)_src[ 9 + Length] << 16)
					| ((uint)_src[10 + Length] << 8)
					|  (uint)_src[11 + Length];

				// Validate the CRC-32.
				uint expectedCrc32 = Checksums.Crc32(new ReadOnlySpan<byte>(_src + 4, Length + 4));

				if (crc32 != expectedCrc32)
					throw new PngDecodeException($"CRC-32 for '{type}' chunk is invalid.");

				// We have a valid chunk, so return it.
				ReadOnlySpan<byte> chunkBytes = new ReadOnlySpan<byte>(_src + 8, Length);
				_current = type switch
				{
					"IHDR" => new PngIhdrChunk(chunkBytes),
					"PLTE" => new PngPlteChunk(chunkBytes),
					"IEND" => new PngIEndChunk(chunkBytes),
					"IDAT" => new PngIDatChunk(chunkBytes.ToArray()),

					"tRNS" => new PngTrnsChunk(chunkBytes),
					"gAMA" => new PngGamaChunk(chunkBytes),
					"cHRM" => new PngChrmChunk(chunkBytes),
					"sRGB" => new PngSrgbChunk(chunkBytes),
					"iCCP" => new PngIccpChunk(chunkBytes),

					"pHYs" => new PngPhysChunk(chunkBytes),
					"tEXt" => new PngTextChunk(chunkBytes),
					"zTXt" => new PngZtxtChunk(chunkBytes),
					"iTXt" => new PngItxtChunk(chunkBytes),
					"tIME" => new PngTimeChunk(chunkBytes),

					_ => new RawPngChunk(type, chunkBytes)
				};

				// Move to the next chunk.
				_src += Length + 12;
				_length -= Length + 12;

				return true;
			}
		}
	}
}
