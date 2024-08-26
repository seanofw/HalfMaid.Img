using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo
{
	internal static class Tj3
	{
		#region DLL imports

		public const string DllFilename = "turbojpeg";

		[DllImport(DllFilename, EntryPoint = "tj3Init")]
		public static extern IntPtr Init(InitType initType);

		[DllImport(DllFilename, EntryPoint = "tj3Destroy")]
		public static extern void Destroy(IntPtr tjHandle);

		[DllImport(DllFilename, EntryPoint = "tj3GetErrorCode")]
		public static extern ErrorCode GetErrorCode(IntPtr tjHandle);

		[DllImport(DllFilename, EntryPoint = "tj3GetErrorStr")]
		private static extern IntPtr GetErrorStrInternal(IntPtr tjHandle);

		[DllImport(DllFilename, EntryPoint = "tj3Get")]
		public static extern int Get(IntPtr tjHandle, Param param);

		[DllImport(DllFilename, EntryPoint = "tj3Set")]
		private static extern int SetInternal(IntPtr tjHandle, Param param, int value);

		[DllImport(DllFilename, EntryPoint = "tj3Free")]
		private static extern void FreeInternal(IntPtr buffer);

		[DllImport(DllFilename, EntryPoint = "tj3Alloc")]
		public static extern IntPtr Alloc(IntPtr size);

		[DllImport(DllFilename, EntryPoint = "tj3DecompressHeader")]
		private static extern int DecompressHeader(IntPtr tjHandle, IntPtr jpegBuf, IntPtr jpegSize);

		[DllImport(DllFilename, EntryPoint = "tj3Compress8")]
		private static extern int Compress8(IntPtr tjHandle, IntPtr srcBuf, int width, int pitch, int height,
			PixelFormat pixelFormat, out IntPtr jpegBuf, out IntPtr jpegSize);

		[DllImport(DllFilename, EntryPoint = "tj3Decompress8")]
		private static extern int Decompress8(IntPtr tjHandle, IntPtr jpegBuf, IntPtr jpegSize,
			IntPtr dstBuf, int pitch, PixelFormat pixelFormat);

		#endregion

		#region C#-friendly wrappers

		private static readonly int[] _samplesPerPixel = new int[]
		{
			3, 3,		// RGB and BGR
			4, 4, 4, 4,	// X* and *X
			1,			// Gray
			4, 4, 4, 4, // A* and *A
			4,			// CMYK
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetErrorStr(IntPtr tjHandle)
			=> Marshal.PtrToStringAnsi(GetErrorStrInternal(tjHandle))!;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Set(IntPtr tjHandle, Param param, int value)
			=> SetInternal(tjHandle, param, value) == 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Free(void* buffer)
			=> FreeInternal((IntPtr)buffer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void* Alloc(int size)
			=> (void*)Alloc((IntPtr)size);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool DecompressHeader(IntPtr tjHandle, void *jpegBuf, int jpegSize)
			=> DecompressHeader(tjHandle, (IntPtr)jpegBuf, (IntPtr)jpegSize) == 0;

		public static void DecompressHeader(IntPtr tjHandle, ReadOnlySpan<byte> src)
		{
			unsafe
			{
				fixed (byte* srcPtr = src)
				{
					if (!DecompressHeader(tjHandle, srcPtr, src.Length))
						throw new InvalidDataException("Error reading JPEG header: " + GetErrorStr(tjHandle));
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool Compress8(IntPtr tjHandle, void* srcBuf, int width, int pitch, int height,
			PixelFormat pixelFormat, out void* jpegBuf, out int jpegSize)
		{
			int result = Compress8(tjHandle, (IntPtr)srcBuf, width, pitch, height, pixelFormat,
				out IntPtr jpegBufRaw, out IntPtr jpegSizeRaw);

			jpegBuf = (void*)jpegBufRaw;
			jpegSize = (int)jpegSizeRaw;

			return result == 0;
		}

		public static byte[] Compress8(IntPtr tjHandle, ReadOnlySpan<byte> src, int width, int pitch, int height,
			PixelFormat pixelFormat)
		{
			if (pixelFormat < PixelFormat.Rgb || pixelFormat > PixelFormat.Cmyk)
				throw new ArgumentException("Legal pixel format required.");
			if (pitch < 0)
				throw new ArgumentOutOfRangeException(nameof(pitch));
			if (width <= 0 || width >= 65536)
				throw new ArgumentOutOfRangeException(nameof(width));
			if (height <= 0 || height >= 65536)
				throw new ArgumentOutOfRangeException(nameof(height));

			if (pitch == 0)
				pitch = width * _samplesPerPixel[(int)pixelFormat];

			if ((long)width * _samplesPerPixel[(int)pixelFormat] > pitch)
				throw new ArgumentException($"Pitch of size {pitch} is not big enough for width {width} x {pixelFormat}.");
			if ((long)pitch * height > src.Length)
				throw new ArgumentException($"Source byte array of size {src.Length} is too small for an image of size {width}x{height} with a pitch of {pitch}.");

			unsafe
			{
				void* jpegBuf = null;
				int jpegSize;

				fixed (byte* srcPtr = src)
				{
					if (!Compress8(tjHandle, srcPtr, width, pitch, height, pixelFormat, out jpegBuf, out jpegSize))
					{
						if (jpegBuf != null)
							Free(jpegBuf);
						throw new InvalidDataException("Error compressing to JPEG: " + GetErrorStr(tjHandle));
					}
				}

				byte[] result = new byte[jpegSize];
				fixed (byte* resultPtr = result)
				{
					Buffer.MemoryCopy(jpegBuf, resultPtr, result.Length, jpegSize);
				}

				return result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool Decompress8(IntPtr tjHandle, void* jpegBuf, int jpegSize, void* dstBuf,
			int pitch, PixelFormat pixelFormat)
			=> Decompress8(tjHandle, (IntPtr)jpegBuf, (IntPtr)jpegSize, (IntPtr)dstBuf,
				pitch, pixelFormat) == 0;

		public static bool Decompress8(IntPtr tjHandle, ReadOnlySpan<byte> src, Span<byte> dest,
			int pitch, PixelFormat pixelFormat)
		{
			if (pixelFormat < PixelFormat.Rgb || pixelFormat > PixelFormat.Cmyk)
				throw new ArgumentException("Legal pixel format required.");
			if (pitch <= 0)
				throw new ArgumentOutOfRangeException(nameof(pitch));

			Set(tjHandle, Param.MaxPixels, dest.Length);

			unsafe
			{
				fixed (byte* destPtr = dest)
				fixed (byte* srcPtr = src)
				{
					return Decompress8(tjHandle, srcPtr, src.Length, destPtr, pitch, pixelFormat);
				}
			}
		}

		public static byte[] Decompress8(IntPtr tjHandle, ReadOnlySpan<byte> src, PixelFormat pixelFormat)
		{
			if (pixelFormat < PixelFormat.Rgb || pixelFormat > PixelFormat.Cmyk)
				throw new ArgumentException("Legal pixel format required.");

			DecompressHeader(tjHandle, src);

			int width = Get(tjHandle, Param.JpegWidth);
			int height = Get(tjHandle, Param.JpegHeight);
			if (width <= 0 || width >= 65536
				|| height <= 0 || height >= 65536)
				throw new InvalidDataException("Source JPEG data is damaged.");
			int samplesPerPixel = _samplesPerPixel[(int)pixelFormat];
			int pitch = samplesPerPixel * width;

			byte[] dest = new byte[pitch * height];
			unsafe
			{
				fixed (byte* destPtr = dest)
				fixed (byte* srcPtr = src)
				{
					if (!Decompress8(tjHandle, srcPtr, src.Length, destPtr, 0, pixelFormat))
						throw new InvalidDataException("Error decompressing JPEG data: " + GetErrorStr(tjHandle));
				}
			}

			return dest;
		}

		#endregion
	}
}
