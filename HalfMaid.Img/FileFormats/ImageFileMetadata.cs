using System;

namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// Metadata resulting from parsing an image file's header.
	/// </summary>
	public readonly struct ImageFileMetadata : IEquatable<ImageFileMetadata>
	{
		/// <summary>
		/// The width of the image, in pixels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// The (approximate) color format of the image, as close as it
		/// can be represented by this enumeration.
		/// </summary>
		public ImageFileColorFormat ColorFormat { get; }

		/// <summary>
		/// Construct a new ImageFileMetadata object.
		/// </summary>
		/// <param name="width">The width of the image, in pixels.</param>
		/// <param name="height">The height of the image, in pixels.</param>
		/// <param name="colorFormat">The (approximate) color format of the image,
		/// as close as it can be represented by this enumeration.</param>
		public ImageFileMetadata(int width, int height, ImageFileColorFormat colorFormat)
		{
			Width = width;
			Height = height;
			ColorFormat = colorFormat;
		}

		/// <summary>
		/// Compare this metadata against another object for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if they are equal (identical), false if they are different.</returns>
		public override bool Equals(object? obj)
			=> obj is ImageFileMetadata other && Equals(other);

		/// <summary>
		/// Compare this metadata against another metadata object for equality.
		/// </summary>
		/// <param name="other">The other object to compare against.</param>
		/// <returns>True if they are equal (identical), false if they are different.</returns>
		public bool Equals(ImageFileMetadata other)
			=> Width == other.Width && Height == other.Height && ColorFormat == other.ColorFormat;

		/// <summary>
		/// Generate a hash code suitable for using this as a dictionary key.
		/// </summary>
		/// <returns>A hash code for keying against this object.</returns>
		public override int GetHashCode()
			=> unchecked((((Width * 65599) + Height) * 65599) + (int)ColorFormat);

		/// <summary>
		/// Compare two metadata objects against each other for equality.
		/// </summary>
		/// <param name="a">The first object to compare.</param>
		/// <param name="b">The other object to compare against.</param>
		/// <returns>True if they are equal (identical), false if they are different.</returns>
		public static bool operator ==(ImageFileMetadata a, ImageFileMetadata b)
			=> a.Equals(b);

		/// <summary>
		/// Compare two metadata objects against each other for equality.
		/// </summary>
		/// <param name="a">The first object to compare.</param>
		/// <param name="b">The other object to compare against.</param>
		/// <returns>False if they are equal (identical), true if they are different.</returns>
		public static bool operator !=(ImageFileMetadata a, ImageFileMetadata b)
			=> !a.Equals(b);

		/// <summary>
		/// Convert this metadata info to a string, largely for debugging purposes.
		/// </summary>
		/// <returns>A string representation of this metadata.</returns>
		public override string ToString()
			=> $"{Width}x{Height} {ColorFormat}";
	}
}
