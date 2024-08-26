using System;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// A tag describes how to animate a single frame, and provides additional metadata.
	/// </summary>
	public class AsepriteTag
	{
		/// <summary>
		/// The frame that this tag describes.
		/// </summary>
		public AsepriteFrame Frame { get; }

		/// <summary>
		/// From frame?
		/// </summary>
		public short From { get; }

		/// <summary>
		/// To frame?
		/// </summary>
		public short To { get; }

		/// <summary>
		/// Which direction to animate this frame sequence.
		/// </summary>
		public AsepriteAniDir AniDir { get; }

		/// <summary>
		/// RGB values of the tag color [DEPRECATED].
		/// </summary>
		public Color32 Color { get; }

		/// <summary>
		/// Tag name.
		/// </summary>
		public string Name { get; }

		private AsepriteTag(AsepriteFrame frame, short from, short to,
			AsepriteAniDir aniDir, Color32 color, string name)
		{
			Frame = frame;
			From = from;
			To = to;
			AniDir = aniDir;
			Color = color;
			Name = name;
		}

		/// <summary>
		/// Read a tag from the raw Aseprite file bytes.
		/// </summary>
		/// <param name="frame">The frame this tag is associated with.</param>
		/// <param name="sourceData">The raw Aseprite file bytes.</param>
		/// <param name="tag">The resulting tag.</param>
		/// <returns>The raw Aseprite file pointer, moved forward past the tag chunk.</returns>
		public static ReadOnlySpan<byte> ReadTag(AsepriteFrame frame, ReadOnlySpan<byte> sourceData,
			out AsepriteTag tag)
		{
			short from = (short)(sourceData[0] | (sourceData[1] << 8));
			short to = (short)(sourceData[2] | (sourceData[3] << 8));
			AsepriteAniDir aniDir = (AsepriteAniDir)sourceData[4];
			if (aniDir > AsepriteAniDir.PingPong)
				aniDir = AsepriteAniDir.Forward;

			sourceData = sourceData.Slice(5 + 8);

			Color32 color = new Color32(sourceData[0], sourceData[1], sourceData[2], (byte)255);

			sourceData = sourceData.Slice(4);

			string name = AsepriteImage.ReadString(sourceData);

			sourceData = sourceData.Slice(2 + name.Length);

			tag = new AsepriteTag(frame, from, to, aniDir, color, name);

			return sourceData;
		}
	}
}