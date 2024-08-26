using System;

namespace HalfMaid.Img.FileFormats.Png
{
	/// <summary>
	/// This exception is raised when a PNG file cannot be decoded due to damaged or invalid data.
	/// </summary>
	public class PngDecodeException : Exception
	{
		/// <summary>
		/// Construct a new PNG-decode exception.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public PngDecodeException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Construct a new PNG-decode exception that wraps another exception.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The inner exception that this exception wraps.</param>
		public PngDecodeException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
