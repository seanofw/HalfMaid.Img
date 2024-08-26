
namespace HalfMaid.Img.FileFormats
{
	/// <summary>
	/// Keys for common well-known image metadata.  These types of metadata are directly
	/// provided by many of the image loaders, and supported by many of the image savers
	/// as well.
	/// </summary>
	public static class ImageMetadataKey
	{
		/// <summary>
		/// Pixels per meter horizontally in the image (supported by BMP, JPEG, PNG, possibly others).
		/// Type is double.
		/// </summary>
		public const string PixelsPerMeterX = nameof(PixelsPerMeterX);

		/// <summary>
		/// Pixels per meter vertically in the image (supported by BMP, JPEG, PNG, possibly others).
		/// Type is double.
		/// </summary>
		public const string PixelsPerMeterY = nameof(PixelsPerMeterY);

		/// <summary>
		/// Original number of bits per channel.
		/// Type is int.
		/// </summary>
		public const string BitsPerChannel = nameof(BitsPerChannel);

		/// <summary>
		/// Original number of color channels.
		/// Type is int.
		/// </summary>
		public const string NumChannels = nameof(NumChannels);

		/// <summary>
		/// Whether this was stored as a progressive JPEG.
		/// Type is bool.
		/// </summary>
		public const string JpegProgressive = nameof(JpegProgressive);

		/// <summary>
		/// Whether this was stored in a grayscale format.
		/// Type is bool.
		/// </summary>
		public const string Grayscale = nameof(Grayscale);

		/// <summary>
		/// The display gamma for this image, if known.
		/// Type is double.
		/// </summary>
		public const string Gamma = nameof(Gamma);

		/// <summary>
		/// Which palette index is considered "transparent."
		/// Type is int.
		/// </summary>
		public const string TransparentIndex = nameof(TransparentIndex);

		/// <summary>
		/// The sRGB rendering intent, as defined by the International Color Consortium.
		/// Type is an enumerated string, of one of the following four values:
		///     "Perceptual"
		///     "RelativeColorimetric"
		///     "Saturation"
		///     "AbsoluteColorimetric"
		/// </summary>
		public const string SrgbRenderingIntent = nameof(SrgbRenderingIntent);

		/// <summary>
		/// An embedded ICCP color profile.
		/// Type is byte[].
		/// </summary>
		public const string IccpProfile = nameof(IccpProfile);

		/// <summary>
		/// The name of the embedded ICCP color profile.
		/// Type is string.
		/// </summary>
		public const string IccpProfileName = nameof(IccpProfileName);

		// Various chromaticity color points.  Type is double for each value.
		#pragma warning disable CS1591
		public const string ChromaticityRedX = nameof(ChromaticityRedX);
		public const string ChromaticityRedY = nameof(ChromaticityRedY);
		public const string ChromaticityGreenX = nameof(ChromaticityGreenX);
		public const string ChromaticityGreenY = nameof(ChromaticityGreenY);
		public const string ChromaticityBlueX = nameof(ChromaticityBlueX);
		public const string ChromaticityBlueY = nameof(ChromaticityBlueY);
		public const string ChromaticityWhitePointX = nameof(ChromaticityWhitePointX);
		public const string ChromaticityWhitePointY = nameof(ChromaticityWhitePointY);
		#pragma warning restore CS1591

		/// <summary>
		/// The last-write time of the image, if embedded in the image itself.
		/// Type is DateTime.
		/// </summary>
		public const string Timestamp = nameof(Timestamp);

		// Common/standard text-based metadata, by name.  All of these are strings.
		// Custom text-based metadata may be included by prefixing its key with the special
		// '!' marker, which indicates custom text-based metadata of an unknown type.

		/// <summary>
		/// Title or caption for this image.
		/// </summary>
		public const string Title = nameof(Title);

		/// <summary>
		/// Name of image's creator.
		/// </summary>
		public const string Author = nameof(Author);

		/// <summary>
		/// Copyright notice.
		/// </summary>
		public const string Copyright = nameof(Copyright);

		/// <summary>
		/// Description of this image.
		/// </summary>
		public const string Description = nameof(Description);

		/// <summary>
		/// Miscellaneous comment.
		/// </summary>
		public const string Comment = nameof(Comment);

		/// <summary>
		/// Legal disclaimer.
		/// </summary>
		public const string Disclaimer = nameof(Disclaimer);

		/// <summary>
		/// Warning of the nature of the content of this image.  (violence, language, adult content, etc.)
		/// </summary>
		public const string Warning = nameof(Warning);

		/// <summary>
		/// Software used to create the image.
		/// </summary>
		public const string Software = nameof(Software);
	}
}
