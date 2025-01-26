using System;
using System.Runtime.CompilerServices;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// This little struct exists to promote strings up to full 32-bit UCS-4,
	/// quickly, by decoding surrogate pairs.  It will yield UCS-4 values until
	/// it reaches the end of the string, and then will yield -1 forever after.
	/// </summary>
	public ref struct StringAsUnicode
	{
#if NETCOREAPP
		private const MethodImplOptions Optimized =
			MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
		private const MethodImplOptions Optimized = MethodImplOptions.AggressiveInlining;
#endif

		/// <summary>
		/// The text being decoded, as a span.
		/// </summary>
		public readonly ReadOnlySpan<char> Text;

		/// <summary>
		/// The current character read position within the source text.
		/// This is settable, but you should always be careful not to set
		/// it to within the middle of a surrogate pair by accident.
		/// </summary>
		public int Position
		{
			[MethodImpl(Optimized)]
			get => _ptr;

			[MethodImpl(Optimized)]
			set => _ptr = value;
		}

		private int _ptr = 0;

		/// <summary>
		/// The last Unicode code point read from the text.  This will be -1 if
		/// no code point has been read yet.  Note that this is *not* updated when
		/// the Position is changed.
		/// </summary>
		public int LastChar => _ch;
		private int _ch = -1;

		/// <summary>
		/// Wrap a string with a Unicode-converter struct so that it
		/// can be efficiently decoded.
		/// </summary>
		/// <param name="text">The text to wrap.</param>
		[MethodImpl(Optimized)]
		public StringAsUnicode(ReadOnlySpan<char> text)
			=> Text = text;

		/// <summary>
		/// Reset to the start of the text (including resetting the last
		/// code point read).
		/// </summary>
		[MethodImpl(Optimized)]
		public void Reset()
		{
			_ptr = 0;
			_ch = -1;
		}

		/// <summary>
		/// Read the next Unicode code point from the text, advancing
		/// the read position by one or two positions, depending on what
		/// was read.
		/// </summary>
		/// <returns>The next Unicode code point that was read, or -1 if
		/// there is no input remaining.</returns>
		[MethodImpl(Optimized)]
		public int Next()
			=> _ptr >= Text.Length
					? -1
				: (_ch = Text[_ptr++]) < 0xD800 || _ch >= 0xDC00
					? _ch
				: HandleSurrogatePairs(_ch);

		/// <summary>
		/// When a value in the range of 0xD800 to 0xDBFF is read, consume
		/// a subsequent value in the range of 0xDC00 to 0xE000 and then derive
		/// the resulting Unicode code point from both the high and low surrogates.<br />
		/// <br />
		/// This is factored out of Next() as this is far less likely to be invoked
		/// by most normal text, while Next() is a small, efficient code path that
		/// can be directly inlined into the caller.
		/// </summary>
		/// <param name="ch">The previous UTF-16 value that was read, which must be
		/// in the range of 0xD800 to 0xDBFF (inclusive).</param>
		/// <returns>The resulting Unicode code point.  If an invalid surrogate pair
		/// is found, the provided low surrogate will be returned instead.</returns>
		private int HandleSurrogatePairs(int ch)
		{
			int hi = ch & 0x3FF;
			int lo = _ptr < Text.Length ? Text[_ptr] : 0;
			if (lo >= 0xDC00 && lo < 0xE000)
			{
				lo &= 0x3FF;
				_ptr++;
				return (_ch = (hi << 10) | lo);
			}
			else return _ch;
		}
	}
}
