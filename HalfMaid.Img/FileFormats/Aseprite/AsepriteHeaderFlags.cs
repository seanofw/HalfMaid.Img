using System;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Optional flags in the Aseprite header.
	/// </summary>
	[Flags]
	public enum AsepriteHeaderFlags : uint
	{
		/// <summary>
		/// Opacity/transparency is being used in some of the layers.
		/// </summary>
		Opacity = 1,
	}
}
