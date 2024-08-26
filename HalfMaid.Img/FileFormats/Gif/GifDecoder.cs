using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// This class knows how to decode a GIF image (or sequence of GIF images).
	/// </summary>
	internal static class GifDecoder
	{
		public const int None = -1;

		private static int[] _interlaceIncrements = { 8, 8, 4, 2, 0 };  // Interlace increments
		private static int[] _interlaceStarts = { 0, 4, 2, 1, 0 };      // Interlace start offsets

		/// <summary>
		/// Unpack a single LZW-compressed image.  This does not perform any allocations;
		/// however, it *does* require 16K of stack space.
		/// </summary>
		/// <remarks>
		/// To avoid memory-safety attacks, this uses exclusively managed code:  No pointers,
		/// no 'unsafe' code.  This comes at a slight performance cost, but provides strong
		/// guarantees that a malicious input file cannot cause trouble.  Thanks to modern C#
		/// features like Span{T}, this is only slightly slower than an unmanaged equivalent
		/// would be, but it's completely safe in the face of malicious inputs.
		/// </remarks>
		/// <param name="destBuffer">The buffer to write the image to.  This must be width*height
		/// bytes large.</param>
		/// <param name="width">The width of the image (in bytes, not pixels).</param>
		/// <param name="height">The height of the image (in pixels).</param>
		/// <param name="srcBuffer">The compressed source buffer to read the image from.</param>
		/// <param name="blockFlags">Flags that control how this block was stored/compressed.</param>
		/// <param name="bitsPerPixel">The number of bits per pixel in the image (must be between 2 and 8).</param>
		/// <returns>The offset within the source data where the reading stopped.</returns>
		public static int DecodeImage(Span<byte> destBuffer, int width, int height,
			ReadOnlySpan<byte> srcBuffer, GifImageBlockFlags blockFlags, int bitsPerPixel)
		{
			int bits2;			// 1 << bitsPerPixel
			int codeSize;		// Current code size in bits
			int codeSize2;		// 1 << codeSize
			int mask;			// (1 << codeSize) - 1
			int nextCode;		// Next available table entry
			int oldToken;		// Last symbol decoded
			int oldCode;		// Code read before this one
			int blockSize;		// Bytes in next block
			int pass = 0;		// Pass number for interlaced pictures

			int blockSrc;		// Pointer to current byte in input block
			int blockEnd;		// Pointer past last byte in input block
			int bitQueue = 0;	// Holds the incoming queue of data bits
			int bitQueueSize = 0;	// The number of bits in the bit queue

			int line = 0;		// Current line we're writing
			int lineBuffer;		// Where to write in the current line
			int lineEnd;		// Where to stop writing the current line

			int src = 0;		// Primary pointer into the srcBuffer.

			Span<byte> firstCodeStack = stackalloc byte[4096];  // Stack for first codes
			Span<byte> lastCodeStack = stackalloc byte[4096];   // Stack for previous code
			Span<short> codeStack = stackalloc short[4096];     // Stack for links

			//---------------------------------------------------------------------

			if (bitsPerPixel < 2 || bitsPerPixel > 8)
				throw new GifDecodeException("Bits per pixel must be between 2 and 8 for GIF images");

			// Set up the decoder for the initial bits-per-pixel size.
			bits2 = 1 << bitsPerPixel;
			nextCode = bits2 + 2;
			codeSize = bitsPerPixel + 1;
			codeSize2 = 1 << codeSize;
			mask = codeSize2 - 1;
			oldCode = oldToken = None;

			// Aim the output pointers at the target data buffer.
			lineBuffer = 0;
			lineEnd = lineBuffer + width;

			// There's no initial code block.
			blockSrc = blockEnd = 0;

			// Loop until we run out of data or until we determine that the
			// input is damaged.
			while (true)
			{
				// Fill up the bit queue until we have at least 'codesize' bits,
				// and anywhere from 0 to 7 additional bits beyond that.
				while (bitQueueSize < codeSize)
				{
					if (++blockSrc >= blockEnd)
					{
						// Input block is empty, so find the next one.
						if (src >= srcBuffer.Length
							|| (blockSize = srcBuffer[src++]) == 0
							|| src + blockSize > srcBuffer.Length)
							throw new GifDecodeException("Unexpectedly reached EOI in input");

						blockSrc = src;
						blockEnd = blockSrc + blockSize;
						src += blockSize;
					}
					bitQueue |= srcBuffer[blockSrc] << bitQueueSize;
					bitQueueSize += 8;
				}

				// Read the current code from the head of the queue.
				int code = bitQueue & mask;
				int originalCode = code;
				bitQueue >>= codeSize;
				bitQueueSize -= codeSize;

				// Did we get the EOI code?
				if (code == bits2 + 1)
					return src;

				// Make sure this is a valid code.
				if (code > nextCode)
					throw new GifDecodeException("Illegal code found in compressed LZW data");

				// Did we get the "clear code"?
				if (code == bits2)
				{
					nextCode = bits2 + 2;
					codeSize = bitsPerPixel + 1;
					codeSize2 = 1 << codeSize;
					mask = codeSize2 - 1;
					oldToken = oldCode = None;
					continue;
				}

				// Collect codes on the stack.  If this is an individual
				// code, the stack will get only one element; if this is a
				// string, the stack will get the whole string, in reverse
				// order.
				int stackPtr = 0;
				if (code == nextCode)
				{
					if (oldCode == None)
						throw new GifDecodeException("Illegal start code found in compressed LZW data");
					firstCodeStack[stackPtr++] = (byte)oldToken;
					code = oldCode;
				}
				while (code >= bits2)
				{
					firstCodeStack[stackPtr++] = lastCodeStack[code];
					code = codeStack[code];
				}

				// Pop the stack to the output, reversing the order of its
				// codes to make them correct.
				oldToken = code;
				while (true)
				{
					destBuffer[lineBuffer++] = (byte)code;
					if (lineBuffer >= lineEnd)
					{
						// End of the current line, so move to the next.
						if ((blockFlags & GifImageBlockFlags.Interlaced) == 0)
							line++;
						else
						{
							// The lines come in a funny order when decoding interlaced images.
							line += _interlaceIncrements[pass];
							while (line >= height)
							{
								if (pass >= 4)
									throw new GifDecodeException("Bad interlacing found in compressed LZW data");
								line = _interlaceStarts[++pass];
							}
						}

						// Move to the next line in the output.
						lineBuffer = line * width;
						lineEnd = lineBuffer + width;
					}

					if (stackPtr > 0)
						code = firstCodeStack[--stackPtr];
					else
						break;
				}

				// Update the links to include the new code.
				if (nextCode < 4096 && oldCode != None)
				{
					codeStack[nextCode] = (short)oldCode;
					lastCodeStack[nextCode] = (byte)oldToken;
					if (++nextCode >= codeSize2 && codeSize < 12)
					{
						codeSize++;
						codeSize2 = 1 << codeSize;
						mask = codeSize2 - 1;
					}
				}
				oldCode = originalCode;
			}
		}
	}
}
