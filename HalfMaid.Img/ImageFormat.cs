using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HalfMaid.Img
{
	/// <summary>
	/// Supported image formats, among others, represented as canonical names.
	/// 
	/// This struct is really just a strongly-typed wrapper around a string, but
	/// it helps to ensure that when code references an image format, it's
	/// usually referencing a well-known string.
	/// </summary>
	public readonly struct ImageFormat : IEquatable<ImageFormat>
	{
		/// <summary>
		/// Image format names must conform to C-style naming conventions:  They
		/// are identifiers, not arbitrary strings.
		/// </summary>
		private static readonly Regex _nameRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

		/// <summary>
		/// No specific image format.
		/// </summary>
		public static readonly ImageFormat None = default;

		/// <summary>
		/// Windows Bitmap.
		/// </summary>
		public static ImageFormat Bmp { get; } = new ImageFormat(nameof(Bmp));

		/// <summary>
		/// Truevision Targa.
		/// </summary>
		public static ImageFormat Targa { get; } = new ImageFormat(nameof(Targa));

		/// <summary>
		/// JPEG.
		/// </summary>
		public static ImageFormat Jpeg { get; } = new ImageFormat(nameof(Jpeg));

		/// <summary>
		/// PNG.
		/// </summary>
		public static ImageFormat Png { get; } = new ImageFormat(nameof(Png));

		/// <summary>
		/// Compuserve GIF.
		/// </summary>
		public static ImageFormat Gif { get; } = new ImageFormat(nameof(Gif));

		/// <summary>
		/// The name for this image format.
		/// </summary>
		public readonly string? Name { get; }

		/// <summary>
		/// Construct a new image format with the given name.
		/// </summary>
		/// <param name="name">The canonical (computer-friendly, not human-friendly)
		/// name of the image format.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImageFormat(string name)
			=> Name = !string.IsNullOrEmpty(name) && _nameRegex.IsMatch(name)
				? name
				: throw new ArgumentException("Image format name must be a valid identifier.", nameof(name));

		/// <summary>
		/// Compare this image format against another object for equality.
		/// </summary>
		/// <param name="obj">The other object to compare against.</param>
		/// <returns>True if they are exactly equal, false if they are different.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
			=> obj is ImageFormat other && Equals(other);

		/// <summary>
		/// Compare this image format against another image format for equality.
		/// </summary>
		/// <param name="other">The other image format to compare against.</param>
		/// <returns>True if they are exactly equal, false if they are different.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ImageFormat other)
			=> Name == other.Name;

		/// <summary>
		/// Get a hash code suitable for using this image format as a dictionary key
		/// or in a hash table.
		/// </summary>
		/// <returns>A hash code for this image format.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => Name?.GetHashCode() ?? 0;

		/// <summary>
		/// Compare one image format against another image format for equality.
		/// </summary>
		/// <param name="a">The first image format to compare.</param>
		/// <param name="b">The other image format to compare against.</param>
		/// <returns>True if they are exactly equal, false if they are different.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ImageFormat a, ImageFormat b)
			=> a.Name == b.Name;

		/// <summary>
		/// Compare one image format against another image format for equality.
		/// </summary>
		/// <param name="a">The first image format to compare.</param>
		/// <param name="b">The other image format to compare against.</param>
		/// <returns>False if they are exactly equal, true if they are different.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ImageFormat a, ImageFormat b)
			=> a.Name != b.Name;

		/// <summary>
		/// Convert this image format to a printable string, which merely extracts
		/// its embedded string.
		/// </summary>
		/// <returns>The image format, as a name.</returns>
		public override string ToString()
			=> Name ?? string.Empty;
	}
}