using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// The handful of common fields and methods required by all image types.
	/// </summary>
	public interface IImage
	{
		/// <summary>
		/// The width of the image, in pixels.
		/// </summary>
		int Width { get; }

		/// <summary>
		/// The height of the image, in pixels.
		/// </summary>
		int Height { get; }

		/// <summary>
		/// The size of this image, represented as a 2D vector.
		/// </summary>
		Vector2i Size { get; }

		/// <summary>
		/// Extract out a copy of the raw data in this image, as bytes.  This is
		/// slower than pinning the raw data and then casting it to a byte pointer, but
		/// it is considerably safer.
		/// </summary>
		byte[] GetBytes();

		/// <summary>
		/// Make a perfect duplicate of this image and return it.
		/// </summary>
		/// <returns>The newly-cloned image.</returns>
		IImage Clone();

		/// <summary>
		/// Every image type that is not 32-bit RGBA must provide a method to convert
		/// it to 32-bit RGBA.  For 32-bit RGBA images, this behaves the same as Clone().
		/// </summary>
		/// <returns>A copy of the image data, promoted or demoted to a 32-bit RGBA image
		/// as necessary.</returns>
		Image32 ToImage32();

		/// <summary>
		/// Every image type that is not 24-bit RGB must provide a method to convert
		/// it to 24-bit RGB.  For 24-bit RGB images, this behaves the same as Clone().
		/// </summary>
		/// <returns>A copy of the image data, promoted or demoted to a 24-bit RGB image
		/// as necessary.</returns>
		Image24 ToImage24();
	}

	/// <summary>
	/// Every image type in this library shares this same basic pattern:  It's an
	/// simple array of structs, with a width and a height, top-to-bottom left-to-right,
	/// with no padding or other weirdness.
	/// </summary>
	/// <typeparam name="T">The type of each pixel in the image.</typeparam>
	/// <remarks>
	/// <para>Every image type that implements this interface provides several strong
	/// guarantees about the underlying data representation, which are the following:</para>
	/// <list>
	///   <item>The data is stored in RAM, not in VRAM.</item>
	///   <item>The data is directly user-program-accessible at all times, both for reading
	///      and for writing.</item>
	///   <item>The data may be pinned by the user program using ordinary `fixed` statements
	///      to obtain raw pointers to it.</item>
	///   <item>The data is stored as a contiguous array of pixels from the start of the
	///      image to the end of the image, with no gaps in the data, not even a single
	///      byte.</item>
	///   <item>The contiguous array of pixels is in scan-line order, from the top of the
	///      image to the bottom of the image, with each scan-line's pixels ordered
	///      from left to right.</item>
	///   <item>Each C# class type stores exactly one kind of image data:  `Image`
	///      represents images as 32-bit RGBA, `Image8` represents them as 8-bit paletted
	///      images, `ImageCmyk` represents them as 32-bit CMYK, and so on.  You don't
	///      have to guess what the pixel format is or write complex switch statements:
	///      The C# type always tells you what the pixel format is.</item>
	///   <item>Each pixel is always represented in "natural" form, which is to say that:
	///       <list>
	///       <item>For 8-bit single-channel images, each pixel is a single byte.</item>
	///       <item>For 16-bit single-channel images, each pixel is a single ushort,
	///          in host-endian order.</item>
	///       <item>For RGB images, each pixel's values are always in the order of R,
	///          then G, then B.</item>
	///       <item>For RGBA images, each pixel's values are always in the order of R,
	///          then G, then B, then A.</item>
	///       <item>For CMYK, YIQ, L*a*b, and other color formats, the pixels values are
	///          again in the order of C, M, Y, and K; or Y, I, and Q; or L, a, and b;
	///          and so on.</item>
	///       </list>
	///   </item>
	/// </list>
	/// <para>In other words, you can use `fixed` and pointers to read and write the image
	/// data; you don't have to guess whether channels are in ARGB or BGRA or some
	/// other strange order; every image is always top-to-bottom, left-to-right; and
	/// there are no random extra padding bytes or other quirks that would complicate
	/// writing straightforward algorithms against the data.</para>
	/// 
	/// <para>These strong constraints limit performance, but they are not an accident:
	/// They allow for good interoperability with existing software and graphics systems
	/// like OpenGL and Direct3D, they provide predictable usage and GC behavior, and they
	/// allow the user program to treat every image as a big frame buffer that it can read
	/// from and write to at will.</para>
	/// 
	/// <para>For single-channel images, {T} is always `byte` or `ushort` or `float` or
	/// some other primitive type.  For multi-channel images, {T} is always a struct
	/// type with an explicit [StructLayout] defined such that the raw-memory
	/// representation is always predictable.</para>
	/// 
	/// <para>Note that multi-byte values like `ushort` are always represented in host-native
	/// endianness --- whichever endian is native to the computer running this software.
	/// Conversion routines are included to ensure that image file formats are read and
	/// written correctly regardless of the computer's endianness; but if you access,
	/// say, a `ushort[]` array as `byte*`, be prepared for the fact that the bytes will
	/// be in different orders on some computers.</para>
	/// </remarks>
	public interface IImage<T> : IImage
	{
		/// <summary>
		/// Obtain direct access to the underlying array of color data, which you
		/// can then manipulate directly, or even pin and manipulate using pointers.
		/// </summary>
		/// <returns>The underlying array of color data.</returns>
		T[] Data { get; }

		/// <summary>
		/// Access the pixels using "easy" 2D array-brackets.  This is safe, easy, reliable,
		/// and slower than almost every other way of accessing the pixels, but it's very useful
		/// for simple cases.  Reading a pixel outside the image will return `default(T)`, and
		/// writing a pixel outside the image is a no-op.
		/// </summary>
		/// <param name="x">The X coordinate of the pixel to read or write.</param>
		/// <param name="y">The Y coordinate of the pixel to read or write.</param>
		/// <returns>The color at that pixel.</returns>
		T this[int x, int y] { get; }
	}
}
