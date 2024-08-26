namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Animation directions for a sequence of frames. 
	/// </summary>
	public enum AsepriteAniDir : byte
	{
		/// <summary>
		/// Run the frames forward from start to end.
		/// </summary>
		Forward = 0,

		/// <summary>
		/// Run the frames backward from end to start.
		/// </summary>
		Reverse = 1,

		/// <summary>
		/// Run forward, then backward, and repeat.
		/// </summary>
		PingPong = 2,
	}
}