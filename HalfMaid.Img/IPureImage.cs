namespace HalfMaid.Img
{
	/// <summary>
	/// All mutable image types implement this interface.
	/// </summary>
	/// <remarks>
	/// Note that due to the lack of support for covariant return types in C# as of C# 10.0,
	/// this interface is empty:  Implementing all of the methods you'd expect this interface
	/// to have would require a duplicate method in every class, and we're not doing that.
	/// </remarks>
	public interface IPureImage : IImageBase
	{
	}

	/// <summary>
	/// All mutable image types implement this interface, as well as providing direct
	/// access to the raw data.
	/// </summary>
	public interface IPureImage<T> : IPureImage, IImageBase<T>
	{
	}
}
