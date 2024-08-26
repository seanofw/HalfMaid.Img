using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// GIF control block flags.
	/// </summary>
	[Flags]
	public enum GifControlBlockFlags : byte
	{
		/// <summary>
		/// The transparent color index is valid.
		/// </summary>
		Transparent = 0x01,

		/// <summary>
		/// Wait for a keypress before continuing
		/// </summary>
		WaitKey = 0x02,

		/// <summary>
		/// Method bits for removing the frame
		/// </summary>
		RemoveMask = 0x1C,

		/// <summary>
		/// Do nothing
		/// </summary>
		None = 0x00,

		/// <summary>
		/// Leave image where it is, subsequent frames draw over it
		/// </summary>
		LeaveImage = 0x04,

		/// <summary>
		/// Replace frame with a rectangle of background color
		/// </summary>
		ReplaceWithBgColor = 0x08,

		/// <summary>
		/// Revert to whatever was displayed before this frame
		/// </summary>
		RevertToPrevious = 0x10,
	};
}
