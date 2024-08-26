using System;

namespace HalfMaid.Img.FileFormats.Gif
{
	/// <summary>
	/// This exception is raised if a GIF image cannot be decoded.
	/// </summary>
	public class GifDecodeException : Exception
	{
		/// <summary>
		/// Construct a new GIF-decode exception.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public GifDecodeException(string message)
			: base(message)
		{
		}
	}
}
