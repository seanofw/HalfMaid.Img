
namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// Basic text alignments, supported by the text-drawing routines.
	/// </summary>
	public enum TextAlignment
	{
		/// <summary>
		/// Default alignment.  (Usually this is equivalent to Left | Top.)
		/// </summary>
		Default = 0,

		/// <summary>
		/// Horizontal-alignment mask.
		/// </summary>
		HorzMask = (0xFF << 0),

		/// <summary>
		/// Align the left edge of the text to the left edge of the containing rectangle.
		/// </summary>
		Left = (1 << 0),

		/// <summary>
		/// Align the right edge of the text to the right edge of the containing rectangle.
		/// </summary>
		Right = (2 << 0),

		/// <summary>
		/// Align the horizontal center of the text to the horizontal center of the containing rectangle.
		/// </summary>
		HorzCenter = (3 << 0),

		/// <summary>
		/// Vertical-alignment mask.
		/// </summary>
		VertMask = (0xFF << 8),

		/// <summary>
		/// Align the top edge of the text to the top edge of the containing rectangle.
		/// </summary>
		Top = (1 << 8),

		/// <summary>
		/// Align the bottom edge of the text to the bottom edge of the containing rectangle.
		/// </summary>
		Bottom = (2 << 8),

		/// <summary>
		/// Align the vertical center of the text to the vertical center of the containing rectangle.
		/// </summary>
		VertCenter = (3 << 8),

		/// <summary>
		/// Align the baseline of the top line of text to the top edge of the containing rectangle.
		/// </summary>
		Baseline = (4 << 8),

		/// <summary>
		/// Fully center the text within the given box.
		/// </summary>
		Center = HorzCenter | VertCenter,

		/// <summary>
		/// Top left corner.
		/// </summary>
		TopLeft = Top | Left,

		/// <summary>
		/// Top right corner.
		/// </summary>
		TopRight = Top | Right,

		/// <summary>
		/// Bottom left corner.
		/// </summary>
		BottomLeft = Bottom | Left,

		/// <summary>
		/// Bottom right corner.
		/// </summary>
		BottomRight = Bottom | Right,
	}
}
