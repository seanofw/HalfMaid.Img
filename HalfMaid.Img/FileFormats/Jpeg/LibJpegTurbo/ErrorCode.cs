namespace HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo
{
	/// <summary>
	/// The two kinds of errors that can be raised by TurboJpeg.
	/// </summary>
	internal enum ErrorCode
	{
		/// <summary>
		/// The error was non-fatal and recoverable, but the destination image may
		/// still be corrupt.
		/// </summary>
		Warning = 0,

		/// <summary>
		/// The error was fatal and non-recoverable.
		/// </summary>
		Fatal = 1,
	}
}
