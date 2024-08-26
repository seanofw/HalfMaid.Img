using System;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// OutputWriter collects raw byte output in an endian-safe fashion.  It is
	/// a little like MemoryStream, but unidirectional, faster, and endian-safe.
	/// This implements IDisposable, as it stores its data outside the GC heap
	/// for efficiency reasons, so you *must* Dispose it when you are done with it.
	/// </summary>
	public unsafe sealed class OutputWriter : IDisposable
	{
		private byte* _data;
		private int _capacity;
		private int _length = 0;

		/// <summary>
		/// The current length of the bytes in the output buffer.
		/// </summary>
		public int Length => _length;

		/// <summary>
		/// Direct access to the data in the output buffer, temporarily valid until
		/// the next invocation of any other OutputWriter method.  Do not hang onto
		/// this reference; it can and will change.
		/// </summary>
		public ReadOnlySpan<byte> Data
			=> _data != null
				? new Span<byte>(_data, _length)
				: throw new InvalidOperationException("OutputWriter is disposed.");

		/// <summary>
		/// Construct a new, empty OutputWriter.
		/// </summary>
		public OutputWriter()
		{
			_data = (byte*)Marshal.AllocHGlobal(_capacity = 1024);
			Zero(_data, _capacity);
			_length = 0;
		}

		/// <summary>
		/// Destroy an existing OutputWriter.
		/// </summary>
		~OutputWriter()
		{
			Dispose(false);
		}

		/// <summary>
		/// Dispose of this OutputWriter and its data.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool isDisposing)
		{
			Marshal.FreeHGlobal((IntPtr)_data);
			_data = null;
		}

		/// <summary>
		/// Finish writing to this OutputWriter, and retrieve the entire output as
		/// a byte[] array.
		/// </summary>
		/// <returns>The completed output.</returns>
		public byte[] Finish()
		{
			if (_data == null)
				throw new InvalidOperationException("Cannot finish a disposed output writer.");

			return new Span<byte>(_data, _length).ToArray();
		}

		/// <summary>
		/// Fast-write data to the OutputWriter by preallocating space for data, and
		/// then directly writing it via a pointer.
		/// </summary>
		/// <param name="count">The number of bytes that are to be written.</param>
		/// <returns>A pointer to the location to write exactly that many bytes to,
		/// valid until the next invocation of any OutputWriter method.</returns>
		public byte* FastWrite(int count)
		{
			if (_length + count > _capacity)
				Grow(count);
			byte* ptr = _data + _length;
			_length += count;
			return ptr;
		}

		/// <summary>
		/// Write a given set of bytes to the output.
		/// </summary>
		/// <param name="s">The set of bytes to write.</param>
		public void Write(ReadOnlySpan<byte> s)
		{
			if (_length + s.Length > _capacity)
				Grow(s.Length);
			fixed (byte* src = s)
			{
				Buffer.MemoryCopy(src, _data + _length, _capacity - _length, s.Length);
			}
			_length += s.Length;
		}

		/// <summary>
		/// Overwrite a byte at a specific location in the output.  The location
		/// must already have been written to once by another method.
		/// </summary>
		/// <param name="index">The location to write to, which must be between
		/// 0 and Length-1 (inclusive).</param>
		/// <param name="b">The byte value to write.</param>
		public void WriteByteAt(int index, byte b)
		{
			if (index < 0 || index >= _length)
				throw new ArgumentOutOfRangeException(nameof(index));
			_data[index] = b;
		}

		/// <summary>
		/// Write a byte to the end of the output.  This is the least-efficient
		/// way to use an OutputWriter, but is supported because many use cases
		/// call for it.
		/// </summary>
		/// <param name="b">The byte to write.</param>
		public void WriteByte(byte b)
		{
			if (_length + 1 > _capacity)
				Grow(1);
			_data[_length] = b;
			_length++;
		}

		/// <summary>
		/// Write a byte to the end of the output.  This is the least-efficient
		/// way to use an OutputWriter, but is supported because many use cases
		/// call for it.
		/// </summary>
		/// <param name="b">The byte to write.</param>
		public void Write(byte b)
		{
			if (_length + 1 > _capacity)
				Grow(1);
			_data[_length] = b;
			_length++;
		}

		/// <summary>
		/// Write a short value to the end of the output, little-endian (always).
		/// </summary>
		/// <param name="s">The short value to write.</param>
		public void WriteLE(short s)
		{
			if (_length + 2 > _capacity)
				Grow(2);
			*(short*)(_data + _length) = s.LE();
			_length += 2;
		}

		/// <summary>
		/// Write a short value to the end of the output, big-endian (always).
		/// </summary>
		/// <param name="s">The short value to write.</param>
		public void WriteBE(short s)
		{
			if (_length + 2 > _capacity)
				Grow(2);
			*(short*)(_data + _length) = s.BE();
			_length += 2;
		}

		/// <summary>
		/// Write a ushort value to the end of the output, little-endian (always).
		/// </summary>
		/// <param name="u">The ushort value to write.</param>
		public void WriteLE(ushort u)
		{
			if (_length + 2 > _capacity)
				Grow(2);
			*(ushort*)(_data + _length) = u.LE();
			_length += 2;
		}

		/// <summary>
		/// Write a ushort value to the end of the output, big-endian (always).
		/// </summary>
		/// <param name="u">The ushort value to write.</param>
		public void WriteBE(ushort u)
		{
			if (_length + 2 > _capacity)
				Grow(2);
			*(ushort*)(_data + _length) = u.BE();
			_length += 2;
		}

		/// <summary>
		/// Write an int value to the end of the output, little-endian (always).
		/// </summary>
		/// <param name="i">The int value to write.</param>
		public void WriteLE(int i)
		{
			if (_length + 4 > _capacity)
				Grow(4);
			*(int*)(_data + _length) = i.LE();
			_length += 4;
		}

		/// <summary>
		/// Write an int value to the end of the output, big-endian (always).
		/// </summary>
		/// <param name="i">The int value to write.</param>
		public void WriteBE(int i)
		{
			if (_length + 4 > _capacity)
				Grow(4);
			*(int*)(_data + _length) = i.BE();
			_length += 4;
		}

		/// <summary>
		/// Write a uint value to the end of the output, little-endian (always).
		/// </summary>
		/// <param name="u">The uint value to write.</param>
		public void WriteLE(uint u)
		{
			if (_length + 4 > _capacity)
				Grow(4);
			*(uint*)(_data + _length) = u.LE();
			_length += 4;
		}

		/// <summary>
		/// Write a uint value to the end of the output, big-endian (always).
		/// </summary>
		/// <param name="u">The uint value to write.</param>
		public void WriteBE(uint u)
		{
			if (_length + 4 > _capacity)
				Grow(4);
			*(uint*)(_data + _length) = u.BE();
			_length += 4;
		}

		private void Grow(int count)
		{
			if (_length + count <= _capacity)
				return;

			int newCapacity = _capacity * 2;
			while (_length + count > newCapacity)
				newCapacity *= 2;

			byte* newData = (byte*)Marshal.AllocHGlobal(newCapacity);
			Buffer.MemoryCopy(_data, newData, newCapacity, _length);
			Zero(newData + _length, newCapacity - _length);

			Marshal.FreeHGlobal((IntPtr)_data);

			_data = newData;
			_capacity = newCapacity;
		}

		private void Zero(byte* buffer, int length)
		{
			if (length <= 0)
				return;

#if NETCOREAPP
			System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned(buffer, 0, (uint)length);
#else
			for (int i = 0; i < length; i++)
				*buffer++ = 0;
#endif
		}
	}
}
