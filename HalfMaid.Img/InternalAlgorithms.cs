using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace HalfMaid.Img
{
	/// <summary>
	/// Internal mechanics shared by all of the different image types, primarily for
	/// blitting and cropping.
	/// </summary>
	internal static class InternalAlgorithms
	{
		/// <summary>
		/// Clip a blit to be within the given bounds of the image.
		/// </summary>
		/// <param name="destImageSize">The size of the destination image we're blitting to.</param>
		/// <param name="srcImageSize">The size of the source image we're blitting from.</param>
		/// <param name="srcX">The source X coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="srcY">The source Y coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="destX">The destination X coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="destY">The destination Y coordinate of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="width">The width of the blit, which will be updated to within bounds of both images.</param>
		/// <param name="height">The height of the blit, which will be updated to within bounds of both images.</param>
		/// <returns>True if the blit can proceed, or false if the blit should be aborted due to illegal/unusable values.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static bool ClipBlit(Vector2i destImageSize, Vector2i srcImageSize, ref int srcX, ref int srcY,
			ref int destX, ref int destY, ref int width, ref int height)
		{
			if (width <= 0 || height <= 0
				|| srcX >= srcImageSize.X || srcY >= srcImageSize.Y
				|| destX >= destImageSize.X || destY >= destImageSize.Y)
				return false;

			if (srcX < 0)
			{
				width += srcX;
				destX -= srcX;
				srcX = 0;
			}
			if (srcY < 0)
			{
				height += srcY;
				destY -= srcY;
				srcY = 0;
			}
			if (destX < 0)
			{
				width += destX;
				srcX -= destX;
				destX = 0;
			}
			if (destY < 0)
			{
				height += destY;
				srcY -= destY;
				destY = 0;
			}

			if (width > srcImageSize.X - srcX)
				width = srcImageSize.X - srcX;
			if (height > srcImageSize.Y - srcY)
				height = srcImageSize.Y - srcY;
			if (width > destImageSize.X - destX)
				width = destImageSize.X - destX;
			if (height > destImageSize.Y - destY)
				height = destImageSize.Y - destY;

			return width > 0 && height > 0;
		}

		/// <summary>
		/// Clip the given drawing rectangle to be within the image.
		/// </summary>
		/// <param name="imageSize">The size of the destination image we're drawing on.</param>
		/// <param name="x">The left coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="y">The top coordinate of the rectangle, which will be updated to be within the image.</param>
		/// <param name="width">The width of the rectangle, which will be updated to be within the image.</param>
		/// <param name="height">The height of the rectangle, which will be updated to be within the image.</param>
		/// <returns>True if the drawing may proceed, or false if the rectangle is invalid/unusable.</returns>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static bool ClipRect(Vector2i imageSize, ref int x, ref int y, ref int width, ref int height)
		{
			if (imageSize.X <= 0 || height <= 0 || x >= imageSize.X || y >= imageSize.Y)
				return false;

			if (x < 0)
			{
				width += x;
				x = 0;
			}
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (width > imageSize.X - x)
				width = imageSize.X - x;
			if (height > imageSize.Y - y)
				height = imageSize.Y - y;

			return width > 0 && height > 0;
		}

		/// <summary>
		/// Recalculate rendering pointers such that a resulting blit operation
		/// will appear flipped vertically.
		/// </summary>
		/// <typeparam name="T">The type of the pointer(s) to adjust.</typeparam>
		/// <param name="imageSize">The size of the image to blit to/from.</param>
		/// <param name="copySize">The size of the blit area itself.</param>
		/// <param name="ptr">The current (unflipped) source/destination pointer.</param>
		/// <param name="step">The current (unflipped) horizontal step amount.</param>
		/// <param name="skip">The current (unflipped) vertical skip amount.</param>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static unsafe void BlitFlipVertInternal<T>(Vector2i imageSize, Vector2i copySize,
			ref T* ptr, ref int step, ref int skip)
			where T : unmanaged
		{
			ptr += imageSize.X * (copySize.Y - 1);
			skip = -copySize.X - imageSize.X;
		}

		/// <summary>
		/// Recalculate rendering pointers such that a resulting blit operation
		/// will appear flipped horizontally.
		/// </summary>
		/// <typeparam name="T">The type of the pointer(s) to adjust.</typeparam>
		/// <param name="imageSize">The size of the image to blit to/from.</param>
		/// <param name="copySize">The size of the blit area itself.</param>
		/// <param name="ptr">The current (unflipped) source/destination pointer.</param>
		/// <param name="step">The current (unflipped) horizontal step amount.</param>
		/// <param name="skip">The current (unflipped) vertical skip amount.</param>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static unsafe void BlitFlipHorzInternal<T>(Vector2i imageSize, Vector2i copySize,
			ref T* ptr, ref int step, ref int skip)
			where T : unmanaged
		{
			step = -1;
			ptr += copySize.X - 1;
			skip += copySize.X * 2;
		}

		/// <summary>
		/// Set up a general blit operation, calculating the correct source and destination start pointers,
		/// and the source and destination step/skip amounts based on the blit-flip flags.
		/// </summary>
		/// <typeparam name="TSrc">The type of the source image pixels to blit.</typeparam>
		/// <typeparam name="TDest">The type of the destination image pixels to blit.</typeparam>
		/// <param name="copySize">The dimensions of the copy rectangle.</param>
		/// <param name="blitFlags">The blit flags.</param>
		/// <param name="srcPos">The top-left corner of the source blit area.</param>
		/// <param name="srcImageSize">The source image to blit from.</param>
		/// <param name="srcBase">The base address of the source image data.</param>
		/// <param name="src">The resulting initial source blit pointer.</param>
		/// <param name="srcStep">The resulting source horizontal step amount.</param>
		/// <param name="srcSkip">The resulting source vertical skip amount.</param>
		/// <param name="destPos">The top-left corner of the destination blit area.</param>
		/// <param name="destImageSize">The destination image to blit to.</param>
		/// <param name="destBase">The base address of the destination image data.</param>
		/// <param name="dest">The resulting initial destination blit pointer.</param>
		/// <param name="destStep">The resulting destination horizontal step amount.</param>
		/// <param name="destSkip">The resulting destination vertical skip amount.</param>
		[Pure]
#if NETCOREAPP
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
		public static unsafe void SetupBlit<TSrc, TDest>(Vector2i copySize, BlitFlags blitFlags,
			Vector2i srcPos, Vector2i srcImageSize, TSrc* srcBase, out TSrc* src, out int srcStep, out int srcSkip,
			Vector2i destPos, Vector2i destImageSize, TDest* destBase, out TDest* dest, out int destStep, out int destSkip)
			where TSrc : unmanaged
			where TDest : unmanaged
		{
			src = srcBase + srcImageSize.X * srcPos.Y + srcPos.X;
			dest = destBase + destImageSize.X * destPos.Y + destPos.X;

			srcStep = 1;
			destStep = 1;
			srcSkip = srcImageSize.X - copySize.X;
			destSkip = destImageSize.X - copySize.X;

			if (src < dest)
			{
				// To produce proper "move" semantics, we need to reverse the blit
				// so that we're not accidentally stomping on part of the source data
				// during the operation.  We do this by flipping src in both directions,
				// and then also flipping dest in both directions.

				// Flip src horizontally and vertically.
				BlitFlipVertInternal(srcImageSize, copySize, ref src, ref srcStep, ref srcSkip);
				BlitFlipHorzInternal(srcImageSize, copySize, ref src, ref srcStep, ref srcSkip);

				// Now flip dest too, which will result in the original desired orientation.
				blitFlags ^= BlitFlags.FlipVert | BlitFlags.FlipHorz;
			}

			if ((blitFlags & BlitFlags.FlipVert) != 0)
				BlitFlipVertInternal(destImageSize, copySize, ref dest, ref destStep, ref destSkip);

			if ((blitFlags & BlitFlags.FlipHorz) != 0)
				BlitFlipHorzInternal(destImageSize, copySize, ref dest, ref destStep, ref destSkip);
		}
	}
}

