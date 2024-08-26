using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	internal static class GifEncoder
	{
		private ref struct ImageEncoder
		{
			private const int LargestCode = 4095;

			private const int HashTableSize = 8191;

			public readonly OutputWriter Output;

			public readonly byte[] OutputBuffer;

			public readonly short[] PrevCode;
			public readonly short[] CurrentCode;
			public readonly short[] NextCode;

			public int BitOffset;
			public int CodeSize;

			public int ClearCode;
			public int EofCode;
			public int FreeCode;

			public int MaxCode;

			private int ByteOffset;
			private int BitsLeft;

			public ImageEncoder(OutputWriter output, int numBits)
			{
				Output = output;

				OutputBuffer = new byte[256 + 3];		// Can never get bigger than this.

				PrevCode = new short[HashTableSize];
				CurrentCode = new short[HashTableSize];
				NextCode = new short[HashTableSize];

				BitOffset = 0;
				ByteOffset = 0;
				BitsLeft = 0;

				CodeSize = 0;

				ClearCode = 0;
				EofCode = 0;
				FreeCode = 0;

				MaxCode = 0;

				Init(numBits);
			}

			public void Init(int numBits)
			{
				CodeSize = numBits + 1;

				ClearCode = (1 << numBits);
				EofCode = ClearCode + 1;
				FreeCode = ClearCode + 2;

				MaxCode = (1 << CodeSize);

				for (int i = 0; i < HashTableSize; i++)
					CurrentCode[i] = 0;
			}

			public void WriteCode(int code)
			{
				ByteOffset = BitOffset >> 3;
				BitsLeft = BitOffset & 7;

				if (ByteOffset >= 254)
				{
					Flush(ByteOffset);
					OutputBuffer[0] = OutputBuffer[ByteOffset];
					BitOffset = BitsLeft;
					ByteOffset = 0;
				}

				if (BitsLeft > 0)
				{
					int temp = (code << BitsLeft) | OutputBuffer[ByteOffset];
					OutputBuffer[ByteOffset    ] = (byte) temp       ;
					OutputBuffer[ByteOffset + 1] = (byte)(temp >>  8);
					OutputBuffer[ByteOffset + 2] = (byte)(temp >> 16);
				}
				else
				{
					OutputBuffer[ByteOffset    ] = (byte) code       ;
					OutputBuffer[ByteOffset + 1] = (byte)(code >>  8);
				}

				BitOffset += CodeSize;
			}

			public readonly void Flush(int position)
			{
				Output.WriteByte((byte)position);
				Output.Write(OutputBuffer.AsSpan(0, position));
			}

			public void Encode(ReadOnlySpan<byte> imageData, int numBits)
			{
				WriteCode(ClearCode);

				int ptr = 0;
				int suffixByte = imageData[ptr++];
				int prefixCode = suffixByte;

				while (ptr < imageData.Length)
				{
					suffixByte = imageData[ptr++];

					int hashIndex = (prefixCode ^ (suffixByte << 5)) % HashTableSize;
					int slot = 1;

					while (true)
					{
						if (CurrentCode[hashIndex] == 0)
						{
							WriteCode(prefixCode);
							slot = FreeCode;

							if (FreeCode <= LargestCode)
							{
								PrevCode[hashIndex] = (short)prefixCode;
								NextCode[hashIndex] = (short)suffixByte;
								CurrentCode[hashIndex] = (short)FreeCode;
								FreeCode++;
							}

							if (slot == MaxCode)
							{
								if (CodeSize < 12)
								{
									CodeSize++;
									MaxCode <<= 1;
								}
								else
								{
									WriteCode(ClearCode);
									Init(numBits);
								}
							}

							prefixCode = suffixByte;
							break;
						}

						if (PrevCode[hashIndex] == prefixCode && NextCode[hashIndex] == suffixByte)
						{
							prefixCode = CurrentCode[hashIndex];
							break;
						}

						hashIndex += slot;
						slot += 2;

						if (hashIndex >= HashTableSize)
							hashIndex -= HashTableSize;
					}
				}

				WriteCode(prefixCode);
				WriteCode(EofCode);

				if (BitOffset > 0)
					Flush((BitOffset + 7) >> 3);
				Flush(0);
			}
		}

		public static void EncodeImage(OutputWriter output, ReadOnlySpan<byte> imageData, int numBits)
		{
			if (numBits >= 9)
				throw new ArgumentException("Cannot encode GIF with 9-bit pixels or higher.");
			numBits = Math.Max(numBits, 2);

			ImageEncoder encoder = new ImageEncoder(output, numBits);
			output.WriteByte((byte)numBits);
			encoder.Encode(imageData, numBits);
		}
	}
}
