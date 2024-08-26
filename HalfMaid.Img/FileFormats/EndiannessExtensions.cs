using System;
using System.Runtime.CompilerServices;

namespace HalfMaid.Img.FileFormats
{
	internal static class EndiannessExtensions
	{
		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short LE(this short value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// This is a no-op if the current CPU is big-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short BE(this short value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort LE(this ushort value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// This is a no-op if the current CPU is big-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort BE(this ushort value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LE(this int value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// This is a no-op if the current CPU is big-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BE(this int value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint LE(this uint value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// This is a no-op if the current CPU is big-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint BE(this uint value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long LE(this long value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// This is a no-op if the current CPU is big-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long BE(this long value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Conditionally swap between little-endian and the current native endian.
		/// This is a no-op if the current CPU is little-endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong LE(this ulong value)
			=> BitConverter.IsLittleEndian ? value : BSwap(value);

		/// <summary>
		/// Conditionally swap between big-endian and the current native endian.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong BE(this ulong value)
			=> BitConverter.IsLittleEndian ? BSwap(value) : value;

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort BSwap(this ushort value)
			=> (ushort)( (ushort)(value << 8)
			           | (ushort)(value >> 8) );

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short BSwap(this short value)
			=> (short)BSwap((ushort)value);

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint BSwap(this uint value)
			=> (uint)( (uint)((value & 0x000000FFU) << 24)
					 | (uint)((value & 0x0000FF00U) <<  8)
					 | (uint)((value & 0x00FF0000U) >>  8)
					 | (uint)((value & 0xFF000000U) >> 24) );

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BSwap(this int value)
			=> (int)BSwap((uint)value);

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong BSwap(this ulong value)
			=> (ulong)( (ulong)((value & 0x00000000000000FFUL) << 56)
					  | (ulong)((value & 0x000000000000FF00UL) << 40)
					  | (ulong)((value & 0x0000000000FF0000UL) << 24)
					  | (ulong)((value & 0x00000000FF000000UL) <<  8)
					  | (ulong)((value & 0x000000FF00000000UL) >>  8)
					  | (ulong)((value & 0x0000FF0000000000UL) >> 24)
					  | (ulong)((value & 0x00FF000000000000UL) >> 40)
					  | (ulong)((value & 0xFF00000000000000UL) >> 56) );

		/// <summary>
		/// Byte-swap the given value from big-endian to little-endian
		/// (or vice-versa).  Hopefully the JIT will recognize that this can
		/// be boiled down to a single machine instruction.
		/// </summary>
		/// <param name="value">The value to byte-swap.</param>
		/// <returns>The byte-swapped value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long BSwap(this long value)
			=> (long)BSwap((ulong)value);
	}
}
