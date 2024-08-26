namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// What kind of layer this is:  Either an individual image, or a group of
	/// other layers (forming a hierarchy).
	/// </summary>
	public enum AsepriteLayerKind : ushort
	{
		/// <summary>
		/// An individual image.
		/// </summary>
		Image = 0,

		/// <summary>
		/// A group of other layers.
		/// </summary>
		Group = 1,
	}
}
