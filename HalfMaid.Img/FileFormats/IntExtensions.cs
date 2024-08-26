
namespace HalfMaid.Img.FileFormats
{
	internal static class IntExtensions
	{
		/// <summary>
		/// Count up how many zeros there are in the given integer above
		/// the highest 1 bit.
		/// </summary>
		/// <param name="value">The integer to test.</param>
		/// <returns>The number of zeros found (32 if the value is entirely zeros).</returns>
		public static int CountLeadingZeros(this int value)
			=> CountLeadingZeros((uint)value);

		/// <summary>
		/// Count up how many zeros there are in the given integer above
		/// the highest 1 bit.
		/// </summary>
		/// <param name="value">The integer to test.</param>
		/// <returns>The number of zeros found (32 if the value is entirely zeros).</returns>
		public static int CountLeadingZeros(this uint value)
		{
			if (value == 0)
				return 32;

			int count = 0;

			if ((value & 0xFFFF0000) == 0)
			{
				value <<= 16;
				count += 16;
			}
			if ((value & 0xFF000000) == 0)
			{
				value <<= 8;
				count += 8;
			}
			if ((value & 0xF0000000) == 0)
			{
				value <<= 4;
				count += 4;
			}
			if ((value & 0xC0000000) == 0)
			{
				value <<= 2;
				count += 2;
			}
			if ((value & 0x80000000) == 0)
			{
				value <<= 1;
				count += 1;
			}

			return count;
		}

		/// <summary>
		/// Count up how many zeros there are in the given integer below
		/// the lowest 1 bit.
		/// </summary>
		/// <param name="value">The integer to test.</param>
		/// <returns>The number of zeros found (32 if the value is entirely zeros).</returns>
		public static int CountTrailingZeros(this int value)
			=> CountTrailingZeros((uint)value);

		/// <summary>
		/// Count up how many zeros there are in the given integer below
		/// the lowest 1 bit.
		/// </summary>
		/// <param name="value">The integer to test.</param>
		/// <returns>The number of zeros found (32 if the value is entirely zeros).</returns>
		public static int CountTrailingZeros(this uint value)
		{
			if (value == 0)
				return 32;

			int count = 0;

			if ((value & 0x0000FFFF) == 0)
			{
				value >>= 16;
				count += 16;
			}
			if ((value & 0x000000FF) == 0)
			{
				value >>= 8;
				count += 8;
			}
			if ((value & 0x0000000F) == 0)
			{
				value >>= 4;
				count += 4;
			}
			if ((value & 0x00000003) == 0)
			{
				value >>= 2;
				count += 2;
			}
			if ((value & 0x00000001) == 0)
			{
				value >>= 1;
				count += 1;
			}

			return count;
		}
	}
}
