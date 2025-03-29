using OpenTK.Mathematics;

namespace HalfMaid.Img.Fonts
{
	/// <summary>
	/// A glyph that has been fully positioned (shaped) and is ready to be
	/// displayed at a given location.
	/// </summary>
	public readonly struct PositionedGlyph
	{
		/// <summary>
		/// The glyph to render.
		/// </summary>
		public Glyph Glyph { get; }

		/// <summary>
		/// Where to render this glyph (absolute position).
		/// </summary>
		public Vector2 Position { get; }

		/// <summary>
		/// Create a ShapedGlyph, which is simply a FontGlyph at a known
		/// position, with how far to advance to the next one.
		/// </summary>
		/// <param name="glyph">The glyph to render.</param>
		/// <param name="position">Where to render this glyph (absolute position).</param>
		public PositionedGlyph(Glyph glyph, Vector2 position)
		{
			Glyph = glyph;
			Position = position;
		}

		/// <summary>
		/// Convert this positioned glyph to a string, mostly for debugging purposes.
		/// </summary>
		/// <returns>The positioned glyph, as a string.</returns>
		public override string ToString()
			=> $"{Glyph} position:{Position}";
	}
}

