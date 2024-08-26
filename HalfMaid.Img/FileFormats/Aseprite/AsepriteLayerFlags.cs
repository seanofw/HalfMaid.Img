using System;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Flag bits for each layer of an Aseprite file.
	/// </summary>
	[Flags]
	public enum AsepriteLayerFlags
	{
		/// <summary>
		/// No flags.
		/// </summary>
		None = 0,

		/// <summary>
		/// This layer can be viewed by the user (i.e., it's readable).
		/// </summary>
		Visible = 1,

		/// <summary>
		/// This layer can be edited by the user (i.e., it's writable).
		/// </summary>
		Editable = 2,

		/// <summary>
		/// This layer cannot be moved.
		/// </summary>
		LockMove = 4,

		/// <summary>
		/// This layer's stack order cannot be changed.
		/// </summary>
		Background = 8,

		/// <summary>
		/// Prefer to link cels when the user copy them.
		/// </summary>
		Continuous = 16,

		/// <summary>
		/// Prefer to show this group layer collapsed.
		/// </summary>
		Collapsed = 32,

		/// <summary>
		/// This is a reference layer.
		/// </summary>
		Reference = 64,

		/// <summary>
		/// A mask for the flags persisted in the file (vs. flags that are purely in-memory).
		/// </summary>
		PersistentFlagsMask = 0xFFFF,

		/// <summary>
		/// Was visible in the alternative state (Alt+click)  (in-memory only).
		/// </summary>
		Internal_WasVisible = 0x10000,

		/// <summary>
		/// This is a true background layer:  It can be neither moved nor reordered.
		/// </summary>
		BackgroundLayerFlags = LockMove | Background,
	}
}
