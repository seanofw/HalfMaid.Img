using System.Runtime.InteropServices;


#pragma warning disable CA1416

namespace HalfMaid.Img.Gdi
{
	internal static class Win32
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenClipboard(IntPtr hWndNewOwner);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EmptyClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetClipboardData(uint uFormat);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool IsClipboardFormatAvailable(uint format);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int CountClipboardFormats();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint EnumClipboardFormats(uint format);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

		[DllImport("Kernel32.dll")]
		[return: MarshalAs(UnmanagedType.SysInt)]
		public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

		[DllImport("Kernel32.dll")]
		[return: MarshalAs(UnmanagedType.SysInt)]
		public static extern IntPtr GlobalLock(IntPtr hMem);

		[DllImport("Kernel32.dll")]
		[return: MarshalAs(UnmanagedType.SysInt)]
		public static extern IntPtr GlobalSize(IntPtr hMem);

		[DllImport("Kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GlobalUnlock(IntPtr hMem);

		[DllImport("Kernel32.dll")]
		[return: MarshalAs(UnmanagedType.SysInt)]
		public static extern IntPtr GlobalFree(IntPtr hMem);
	}
}