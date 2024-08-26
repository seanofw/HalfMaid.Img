#pragma warning disable CA1416

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using HalfMaid.Img.FileFormats.Bmp;

namespace HalfMaid.Img.Gdi
{
	public static class ClipboardImage
	{
		private const int BitmapFileHeaderSize = 14;
		
		private const int GMEM_MOVEABLE = 2;

		private const uint CF_BITMAP = 2;
		private const uint CF_DIB = 8;
		private const uint CF_DIBV5 = 17;

		/// <summary>
		/// Copy the given image to the Windows clipboard as a DIB.
		/// </summary>
		/// <param name="image">The image to copy to the clipboard.</param>
		/// <param name="includeAlpha">Whether to include the alpha channel (32-bit BGRA image),
		/// or to export it as a 24-bit BGR image and discard the alpha channel.</param>
		public static void Copy(this PureImage32 image, bool includeAlpha = true)
			=> Write(image, includeAlpha);

		/// <summary>
		/// Copy the given image to the Windows clipboard as a DIB.
		/// </summary>
		/// <param name="image">The image to copy to the clipboard.</param>
		/// <param name="includeAlpha">Whether to include the alpha channel (32-bit BGRA image),
		/// or to export it as a 24-bit BGR image and discard the alpha channel.</param>
		public static void Copy(this Image32 image, bool includeAlpha = true)
			=> Write(image, includeAlpha);

		/// <summary>
		/// Paste to the given image from an image on the Windows clipboard.
		/// </summary>
		/// <param name="image">The image to replace with clipboard contents.</param>
		/// <returns>True if the image was successfully obtained from the clipboard, false otherwise.</returns>
		public static bool Paste(this Image32 image)
		{
			Image32? result = Read();
			if (result == null)
				return false;
			image.Replace(result);
			return true;
		}

		/// <summary>
		/// Paste to the given pure image from an image on the Windows clipboard.
		/// </summary>
		/// <param name="image">The image to replace with clipboard contents.</param>
		/// <returns>A pure image with the clipboard contents, or null if the clipboard could not be read.</returns>
		public static PureImage32? Paste(this PureImage32 image)
		{
			Image32? result = Read();
			if (result == null)
				return null;
			return result.Pure;
		}

		/// <summary>
		/// Write the given image to the clipboard.
		/// </summary>
		/// <param name="image">The image to write to the clipboard.</param>
		/// <param name="includeAlpha">Whether to include alpha values in the image.</param>
		/// <exception cref="Win32Exception">Thrown if the clipboard is not accessible or
		/// cannot be written to (is locked).</exception>
		public static void Write(Image32 image, bool includeAlpha = true)
			=> Write((PureImage32)image, includeAlpha);

		/// <summary>
		/// Write the given image to the clipboard.
		/// </summary>
		/// <param name="image">The image to write to the clipboard.</param>
		/// <param name="includeAlpha">Whether to include alpha values in the image.</param>
		/// <exception cref="Win32Exception">Thrown if the clipboard is not accessible or
		/// cannot be written to (is locked).</exception>
		public static void Write(PureImage32 image, bool includeAlpha = true)
		{
			bool clipboardIsOpen = false;

			try
			{
				if (!Win32.OpenClipboard(IntPtr.Zero))
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot open clipboard");
				clipboardIsOpen = true;
				if (!Win32.EmptyClipboard())
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot remove existing contents from clipboard.");

				if (includeAlpha)
				{
					// Include both transparent and non-transparent versions, if transparency
					// is supposed to be included.  This will allow clipboard readers that only
					// understand non-alpha images to still use the pasted content.
					SetClipboardDataToImage(image.SaveFile(ImageFormat.Bmp,
						new BmpSaveOptions { IncludeAlpha = true }), true);
				}

				SetClipboardDataToImage(image.SaveFile(ImageFormat.Bmp,
					new BmpSaveOptions { IncludeAlpha = false }), false);
			}
			finally
			{
				if (clipboardIsOpen)
				{
					Win32.CloseClipboard();
				}
			}
		}

		private static void SetClipboardDataToImage(byte[] bitmapData, bool hasAlpha)
		{
			IntPtr allocHandle = IntPtr.Zero;
			IntPtr lockPtr = IntPtr.Zero;

			try
			{
				allocHandle = Win32.GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(bitmapData.Length - BitmapFileHeaderSize));
				if (allocHandle == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot allocate memory for bitmap on clipboard.");

				unsafe
				{
					lockPtr = Win32.GlobalLock(allocHandle);
					if (lockPtr == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot lock memory for bitmap on clipboard.");
					Span<byte> destBuffer = new Span<byte>((void*)lockPtr, bitmapData.Length - BitmapFileHeaderSize);
					bitmapData.AsSpan().Slice(BitmapFileHeaderSize).CopyTo(destBuffer);
					Win32.GlobalUnlock(lockPtr);
				}

				if (Win32.SetClipboardData(hasAlpha ? CF_DIBV5 : CF_DIB, allocHandle) == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot set clipboard data.");
			}
			finally
			{
				if (lockPtr != IntPtr.Zero)
					Win32.GlobalUnlock(lockPtr);
				if (allocHandle != IntPtr.Zero)
					Win32.GlobalFree(allocHandle);
			}
		}

		/// <summary>
		/// Determine if the clipboard contains a data format that can be converted
		/// to an image.
		/// </summary>
		/// <returns>True if the clipboard contains an image, false if it does not.</returns>
		public static bool Exists()
		{
			return Win32.IsClipboardFormatAvailable(CF_DIB)
				|| Win32.IsClipboardFormatAvailable(CF_DIBV5)
				|| Win32.IsClipboardFormatAvailable(CF_BITMAP);
		}

		/// <summary>
		/// Read the image from the clipboard.
		/// </summary>
		/// <returns>The image on the clipboard, if the clipboard contains an image, or
		/// null if the clipboard is accessible but does not contain an image.</returns>
		/// <exception cref="Win32Exception">Thrown if the clipboard is not accessible or
		/// cannot be read from (is locked).</exception>
		public static Image32? Read()
		{
			bool clipboardIsOpen = false;

			try
			{
				if (!Win32.OpenClipboard(IntPtr.Zero))
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot open clipboard");
				clipboardIsOpen = true;

				IntPtr handle;
				Image32? image =
					  (handle = Win32.GetClipboardData(CF_DIBV5)) != IntPtr.Zero
						? GetClipboardDataAsImage_Dib(handle)
					: (handle = Win32.GetClipboardData(CF_DIB)) != IntPtr.Zero
						? GetClipboardDataAsImage_Dib(handle)
					: (handle = Win32.GetClipboardData(CF_BITMAP)) != IntPtr.Zero
						? GetClipboardDataAsImage_HBitmap(handle)
					: null;
				return image;
			}
			finally
			{
				if (clipboardIsOpen)
				{
					Win32.CloseClipboard();
				}
			}
		}

		private static Image32? GetClipboardDataAsImage_Dib(IntPtr handle)
		{
			IntPtr lockPtr = IntPtr.Zero;
			IntPtr size;
			try
			{
				unsafe
				{
					lockPtr = Win32.GlobalLock(handle);
					if (lockPtr == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot lock memory for bitmap on clipboard.");
					size = Win32.GlobalSize(handle);
					Span<byte> srcBuffer = new Span<byte>((void*)lockPtr, (int)size);
					Image32? result = Image32.LoadFile(srcBuffer, imageFormat: ImageFormat.Bmp);
					return result;
				}
			}
			finally
			{
				if (lockPtr != IntPtr.Zero)
					Win32.GlobalUnlock(handle);
			}
		}

		private static Image32? GetClipboardDataAsImage_HBitmap(IntPtr handle)
			=> Bitmap.FromHbitmap(handle).ToImage32();
	}
}