using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace HalfMaid.Img.OpenGL
{
	/// <summary>
	/// An OpenGL 8-bit texture.  This is not a *paletted* texture, but a texture
	/// that only has a single red (or gray) channel.
	/// </summary>
	[DebuggerDisplay("{Name} ({Width}x{Height})")]
	public class Texture8 : IDisposable
	{
		#region Properties

		/// <summary>
		/// The width of this texture, in texels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// The height of this texture, in texels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// An optional name for this texture (useful for debugging).
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// The OpenGL handle for this texture.
		/// </summary>
		public int Handle { get; private set; } = 0;

		/// <summary>
		/// This texture's size, as a Vector2i.
		/// </summary>
		public Vector2i Size => new Vector2i(Width, Height);

#if DEBUG
		/// <summary>
		/// During debugging, the stack where this texture was allocated.
		/// Useful for tracking down memory leaks.
		/// </summary>
		public string? AllocationStack { get; }
#endif

		/// <summary>
		/// A name to use for this texture when displaying error messages.
		/// </summary>
		public string DebugName => $"Texture ({Handle}) \"{Name}\"";

		/// <summary>
		/// Whether this texture has been disposed (or is still valid).
		/// </summary>
		public bool IsDisposed => _isDisposed != 0;
		private int _isDisposed;

		#endregion

		#region OpenGL state properties

		/// <summary>
		/// Get or set the border color of this texture.
		/// </summary>
		public byte BorderColor
		{
			get => _borderColor;
			set
			{
				if (_borderColor != value)
				{
					UpdateTexParam(() =>
					{
						float[] color = new float[4];
						color[0] = color[1] = color[2] = color[3] = _borderColor * (1.0f / 255);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor,
							color);
					});
				}
			}
		}
		private byte _borderColor = default;

		/// <summary>
		/// Get or set the texture magnification filter.  The default is Nearest.
		/// </summary>
		public TextureMagFilter MagFilter
		{
			get => _magFilter;
			set
			{
				UpdateTexParam(() =>
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
						(int)value));
				_magFilter = value;
			}
		}
		private TextureMagFilter _magFilter;

		/// <summary>
		/// Get or set the texture minification filter.  The default is Nearest.
		/// </summary>
		public TextureMinFilter MinFilter
		{
			get => _minFilter;
			set
			{
				UpdateTexParam(() =>
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
						(int)value));
				_minFilter = value;
			}
		}
		private TextureMinFilter _minFilter;

		/// <summary>
		/// Get or set the texture wrap mode in the S direction.  The default is Repeat.
		/// </summary>
		public TextureWrapMode WrapS
		{
			get => _wrapS;
			set
			{
				UpdateTexParam(() =>
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
						(int)value));
				_wrapS = value;
			}
		}
		private TextureWrapMode _wrapS;

		/// <summary>
		/// Get or set the texture wrap mode in the T direction.  The default is Repeat.
		/// </summary>
		public TextureWrapMode WrapT
		{
			get => _wrapT;
			set
			{
				UpdateTexParam(() =>
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
						(int)value));
				_wrapT = value;
			}
		}
		private TextureWrapMode _wrapT;

		/// <summary>
		/// Whether this texture has valid mipmaps or not.  Set to true by UpdateMipMaps(),
		/// and automatically reset to false any time the level 0 pixels are updated by
		/// Replace() or by Blit().<br />
		/// <br />
		/// As there are many ways that mipmaps for a texture can be updated, you can
		/// update this flag yourself as necessary so that it remains meaningful if you
		/// update the mipmaps in a custom way.
		/// </summary>
		public bool HasMipMaps { get; set; }

		#endregion

		#region Construction

		/// <summary>
		/// Create a new OpenGL texture using the given dataset.
		/// </summary>
		/// <param name="data">A pointer to the source data.  The source data can be presumed
		/// to have been copied into the texture, and can safely be disposed after this call.</param>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		/// <exception cref="ArgumentException">Thrown if the width or height are invalid.</exception>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public unsafe Texture8(IntPtr data, int width, int height, string? name,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
		{
			Width = width;
			Height = height;
			Name = name;
#if DEBUG
			AllocationStack = PruneStackTrace(Environment.StackTrace);
#else
			AllocationStack = null;
#endif

			Handle = CreateOpenGLObject((void*)data, width, height, name,
				minFilter, magFilter, wrapS, wrapT, autoMipMap);

			MinFilter = minFilter;
			MagFilter = magFilter;
			WrapS = wrapS;
			WrapT = wrapT;
			HasMipMaps = autoMipMap;
		}

		/// <summary>
		/// Create a new OpenGL texture from the given color data.
		/// </summary>
		/// <param name="data">An array of source data.  The source data can be presumed to have
		/// been copied into the texture, and can safely be disposed after this call.</param>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		/// <exception cref="ArgumentException">Thrown if the width or height are invalid.</exception>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public unsafe Texture8(byte[] data, int width, int height, string? name,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
		{
			Width = width;
			Height = height;
			Name = name;
#if DEBUG
			AllocationStack = PruneStackTrace(Environment.StackTrace);
#else
			AllocationStack = null;
#endif

			unsafe
			{
				fixed (byte* dataPtr = data)
				{
					Handle = CreateOpenGLObject((void*)dataPtr, width, height, name,
						minFilter, magFilter, wrapS, wrapT, autoMipMap);
				}
			}

			MinFilter = minFilter;
			MagFilter = magFilter;
			WrapS = wrapS;
			WrapT = wrapT;
			HasMipMaps = autoMipMap;
		}

		/// <summary>
		/// Construct a new, empty texture of the given size.
		/// </summary>
		/// <param name="size">The size (width and height) of the texture to create, in texels.</param>
		/// <param name="name">An optional name for this texture (useful for debugging).</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Texture8(Vector2i size, string? name = null,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
			: this(IntPtr.Zero, size.X, size.Y, name, minFilter, magFilter, wrapS, wrapT, autoMipMap)
		{
		}

		/// <summary>
		/// Construct a new, empty texture of the given size.
		/// </summary>
		/// <param name="width">The width of the texture to create, in texels.</param>
		/// <param name="height">The height of the texture to create, in texels.</param>
		/// <param name="name">An optional name for this texture (useful for debugging).</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Texture8(int width, int height, string? name = null,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
			: this(IntPtr.Zero, width, height, name, minFilter, magFilter, wrapS, wrapT, autoMipMap)
		{
		}

		/// <summary>
		/// Create a new OpenGL texture from the given dataset.
		/// </summary>
		/// <param name="data">An array of source data.  The source data can be presumed to have
		/// been copied into the texture, and can safely be disposed after this call.</param>
		/// <param name="size">The size (width and height) of the texture, in texels.</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		/// <exception cref="ArgumentException">Thrown if the width or height are invalid.</exception>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Texture8(byte[] data, Vector2i size, string? name,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
			: this(data, size.X, size.Y, name, minFilter, magFilter, wrapS, wrapT, autoMipMap)
		{
		}

		/// <summary>
		/// Create a new OpenGL texture from the given image.
		/// </summary>
		/// <param name="image">A 8-bit image to use to create the texture.  The source
		/// image can be presumed to have been copied into the texture, and can safely be
		/// disposed after this call.</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.
		/// The default is nearest-neighbor mapping, not any form of filtering.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.
		/// The default is to wrap to the other side.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.
		/// The default is false.</param>
		/// <exception cref="ArgumentException">Thrown if the width or height are invalid.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Texture8(Image8 image, string? name = null,
			TextureMinFilter minFilter = TextureMinFilter.Nearest, TextureMagFilter magFilter = TextureMagFilter.Nearest,
			TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat,
			bool autoMipMap = false)
			: this(image.Data, image.Width, image.Height, name, minFilter, magFilter, wrapS, wrapT, autoMipMap)
		{
		}

		#endregion

		#region Construction internals

		/// <summary>
		/// Create a new OpenGL texture from the given color data.
		/// </summary>
		/// <param name="data">A pointer to the source data.  The source data can be presumed to
		/// have been copied into the texture, and can safely be disposed after this call.</param>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="minFilter">The minification filter to use when the texture is rendered small.</param>
		/// <param name="magFilter">The magnification filter to use when the texture is rendered large.</param>
		/// <param name="wrapS">How to wrap or cut off this texture at its horizontal edges.</param>
		/// <param name="wrapT">How to wrap or cut off this texture at its vertical edges.</param>
		/// <param name="autoMipMap">Whether to automatically generate mipmaps for this texture.</param>
		/// <returns>An OpenGL handle for the newly-allocated texture.</returns>
		/// <exception cref="ArgumentException">Thrown if the width or height are invalid.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int CreateOpenGLObject(void* data, int width, int height, string? name,
			TextureMinFilter minFilter, TextureMagFilter magFilter,
			TextureWrapMode wrapS, TextureWrapMode wrapT, bool autoMipMap)
		{
			if (width < 0 || height < 0)
				throw new ArgumentException($"Cannot create texture of size '{width}x{height}'.");
			width = Math.Max(width, 1);
			height = Math.Max(height, 1);

			RaisePriorErrors();

			GL.GetInteger(GetPName.TextureBinding2D, out int oldHandle);
			try
			{
				int handle = GL.GenTexture();

				RaiseErrors("Failed to create texture: glGenTexture");

				GL.BindTexture(TextureTarget.Texture2D, handle);

				RaiseErrors("Failed to create texture: glBindTexture");

				if (data != null)
				{
					GL.TexImage2D(TextureTarget.Texture2D, 0,
						PixelInternalFormat.R8,
						width, height, 0,
						PixelFormat.Red, PixelType.UnsignedByte, (IntPtr)data);

					RaiseErrors("Failed to create texture: glTexImage2D");
				}

				if (autoMipMap)
				{
					GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
					RaiseErrors("Failed to create texture: glGenerateMipmap");
				}

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);

				RaiseErrors("Failed to create texture: glTexParameter");

				if (!string.IsNullOrEmpty(name))
				{
					GL.ObjectLabel(ObjectLabelIdentifier.Texture, handle, name.Length, name);
					RaiseErrors("Failed to create texture: glObjectLabel");
				}

				return handle;
			}
			finally
			{
				GL.BindTexture(TextureTarget.Texture2D, oldHandle);
			}
		}

		#endregion

		#region Replacing/updating pixels

		/// <summary>
		/// Replace the texture's current contents with those of the given image.
		/// </summary>
		/// <param name="image">The image to copy into the texture.  This must be
		/// of the same width/height as the texture or an exception will be raised.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		public void Replace(Image8 image, int level = 0)
			=> Replace(image.Data, level);

		/// <summary>
		/// Replace the texture's current contents with those of the given color data.
		/// </summary>
		/// <param name="srcData">A pointer to the color data, which must be at least
		/// as large as the width/height of the texture.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		public unsafe void Replace(void* srcData, int level = 0)
			=> Replace((IntPtr)srcData, level);

		/// <summary>
		/// Replace the texture's current contents with those of the given color data.
		/// </summary>
		/// <param name="srcData">A pointer to the color data, which must be at least
		/// as large as the width/height of the texture.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		public void Replace(IntPtr srcData, int level = 0)
		{
			RaisePriorErrors();

			GL.BindTexture(TextureTarget.Texture2D, Handle);
			RaiseErrors("Failed to update texture: glBindTexture");

			GL.TexSubImage2D(TextureTarget.Texture2D, level,
				0, 0, Width, Height,
				PixelFormat.Red, PixelType.UnsignedByte, srcData);
			RaiseErrors("Failed to update texture: glTexSubImage2D");

			if (level == 0)
				HasMipMaps = false;
		}

		/// <summary>
		/// Replace the texture's current contents with the given color data.
		/// </summary>
		/// <param name="srcData">The data to copy into the texture.  This must be
		/// of the same width/height as the texture or an exception will be raised.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		/// <exception cref="ArgumentException">Thrown if the source data is too small for the texture.</exception>
		public void Replace(byte[] srcData, int level = 0)
		{
			if (srcData.Length < Width * Height)
				throw new ArgumentException($"Not enough pixel data for a texture of size {Width}x{Height}.");

			RaisePriorErrors();

			GL.BindTexture(TextureTarget.Texture2D, Handle);
			RaiseErrors("Failed to update texture: glBindTexture");

			GL.TexSubImage2D(TextureTarget.Texture2D, level,
				0, 0, Width, Height,
				PixelFormat.Red, PixelType.UnsignedByte, srcData);
			RaiseErrors("Failed to update texture: glTexSubImage2D");

			if (level == 0)
				HasMipMaps = false;
		}

		/// <summary>
		/// Copy the given data to the given target rectangle of this texture.
		/// </summary>
		/// <param name="srcData">The data to replace that rectangle with.</param>
		/// <param name="destRect">The destination rectangle to update.  This must be
		/// entirely contained within the texture, or an exception will be raised.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		/// <exception cref="ArgumentException">Thrown if the source data is too small for
		/// the destination rectangle, or if the destination rectangle is not fully
		/// contained within the texture's dimensions.</exception>
		public void Blit(byte[] srcData, Rect destRect, int level = 0)
		{
			// Legal degenerate case.
			if (destRect.Width == 0 || destRect.Height == 0)
				return;

			// Safety checks first.
			if (srcData.Length < destRect.Width * destRect.Height)
				throw new ArgumentException($"Not enough color data ({srcData.Length} pixels) for the given rectangle dimensions, {destRect.Width}x{destRect.Height}.");

			if (destRect.X < 0 || destRect.Y < 0 || destRect.Width < 0 || destRect.Height < 0
				|| destRect.X + destRect.Width > Width || destRect.Y + destRect.Height > Height)
				throw new ArgumentException($"Destination rectangle {destRect} must be fully contained within the texture dimensions {Width}x{Height}.");

			BlitInternal(srcData, destRect, level);
		}

		/// <summary>
		/// Copy the given image to the given target rectangle of this texture.
		/// </summary>
		/// <param name="srcImage">The source image to replace the destination rectangle with.</param>
		/// <param name="destX">The X coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="destY">The Y coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		public void Blit(Image8 srcImage, int destX, int destY, int level = 0)
			=> Blit(srcImage, 0, 0, destX, destY, srcImage.Width, srcImage.Height, level);

		/// <summary>
		/// Copy the given subimage to the given target rectangle of this texture.
		/// </summary>
		/// <param name="srcImage">The source image to replace the destination rectangle with.</param>
		/// <param name="srcImage">The source image to copy from.</param>
		/// <param name="srcX">The X coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="srcY">The Y coordinate of the top-left corner in the source image to start copying from.</param>
		/// <param name="destX">The X coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="destY">The Y coordinate of the top-left corner in the destination image to start copying to.</param>
		/// <param name="width">The width of the rectangle of pixels to copy.</param>
		/// <param name="height">The height of the rectangle of pixels to copy.</param>
		/// <param name="level">The mipmap level to update (0 by default).</param>
		public void Blit(Image8 srcImage, int srcX, int srcY, int destX, int destY, int width, int height, int level = 0)
		{
			if (!Image32.ClipBlit(Size, srcImage.Size, ref srcX, ref srcY, ref destX, ref destY, ref width, ref height))
				return;

			Image8 subImage = (srcX != 0 || srcY != 0 || width != srcImage.Width || height != srcImage.Height
				? srcImage.Extract(srcX, srcY, width, height)
				: srcImage);

			BlitInternal(subImage.Data, new Rect(destX, destY, width, height), level);
		}

		/// <summary>
		/// Copy the given source data to the image's destination rectangle, with
		/// no safety checks.
		/// </summary>
		/// <param name="srcData">The color data to replace that rectangle with.</param>
		/// <param name="destRect">The destination rectangle to update.  This must be
		/// entirely contained within the texture, or an exception will be raised.</param>
		/// <param name="level">The mipmap level to update.</param>
		private void BlitInternal(byte[] srcData, Rect destRect, int level)
		{
			RaisePriorErrors();

			GL.BindTexture(TextureTarget.Texture2D, Handle);
			RaiseErrors("Failed to update texture: glBindTexture");

			GL.TexSubImage2D(TextureTarget.Texture2D, level,
				destRect.X, destRect.Y, destRect.Width, destRect.Height,
				PixelFormat.Red, PixelType.UnsignedByte, srcData);
			RaiseErrors("Failed to update texture: glTexSubImage2D");

			if (level == 0)
				HasMipMaps = false;
		}

		#endregion

		#region Extracting existing pixels

		/// <summary>
		/// Copy all pixels from this texture to the given target array, which must be
		/// large enough to hold those pixels.
		/// </summary>
		/// <param name="buffer">The buffer to copy to.</param>
		/// <param name="level">The mipmap level to extract (0 by default).</param>
		/// <exception cref="ArgumentException">Thrown if the buffer is not large enough.</exception>
		public void GetData(byte[] buffer, int level = 0)
		{
			if (buffer.Length < Width * Height)
				throw new ArgumentException("Buffer size must be as large as or larger than the texture.");

			RaisePriorErrors();

			GL.BindTexture(TextureTarget.Texture2D, Handle);
			RaiseErrors("Failed to query texture: glBindTexture");

			GL.GetTexImage(TextureTarget.Texture2D, level,
				PixelFormat.Red, PixelType.UnsignedByte, buffer);
			RaiseErrors("Failed to query texture: glGetTexImage");
		}

		/// <summary>
		/// Copy this OpenGL Texture to a new Image8 object.
		/// </summary>
		/// <param name="level">The mipmap level to extract (0 by default).</param>
		/// <returns>A copy of this texture, as an Image.</returns>
		public Image8 ToImage(int level = 0)
		{
			Image8 image = new Image8(Width, Height);
			GetData(image.Data, level);
			return image;
		}

		#endregion

		#region Mipmap suppport

		/// <summary>
		/// After any pixels of the texture change, invoke this method to generate
		/// (or regenerate) mipmaps for this texture.
		/// </summary>
		public void UpdateMipMaps()
		{
			RaisePriorErrors();

			GL.GetInteger(GetPName.TextureBinding2D, out int oldHandle);
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, Handle);

				RaiseErrors("Failed to bind texture: glBindTexture");

				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
				HasMipMaps = true;
			}
			finally
			{
				GL.BindTexture(TextureTarget.Texture2D, oldHandle);
			}
		}

		#endregion

		#region Disposal and cleanup

		/// <summary>
		/// Finalize this texture.  You should always prefer Dispose(), as that
		/// has deterministic cleanup, but this finalizer can catch common mistakes.
		/// </summary>
		~Texture8()
		{
#if DEBUG
			Debug.WriteLine($"'{DebugName}' was GC'ed but not Disposed().  Allocated by:\r\n{AllocationStack}");
#endif
			Dispose(false);
		}

		/// <summary>
		/// Dispose of this texture, releasing all resources held by it.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _isDisposed, 0, 1) != 0)
			{
#if DEBUG
				Debug.WriteLine($"'{DebugName}' was Disposed() twice.  Allocated by:\r\n{AllocationStack}");
#endif
				return;
			}

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose of this texture, releasing all resources held by it.
		/// </summary>
		/// <param name="isDisposing">True if this was called by Dispose(), false if this
		/// was called by the finalizer.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (Handle != 0)
			{
				RaisePriorErrors();

				GL.DeleteTexture(Handle);
				RaiseErrors("Failed to delete texture: glDeleteTexture");

				Handle = 0;
			}
		}

		#endregion

		#region OpenGL helpers

		/// <summary>
		/// Bind this texture into an OpenGL texture unit so that it can be used
		/// for rendering.
		/// </summary>
		/// <param name="unit">The texture unit to bind to (Texture0 by default).</param>
		public void BindToTextureUnit(TextureUnit unit = TextureUnit.Texture0)
		{
			RaisePriorErrors();

			GL.ActiveTexture(unit);
			RaiseErrors("Failed to bind texture: glActiveTexture");
			GL.BindTexture(TextureTarget.Texture2D, Handle);
			RaiseErrors("Failed to bind texture: glBindTexture");
		}

		/// <summary>
		/// Update the given texture parameter in OpenGL for this texture, correctly
		/// handling errors, but leave the texture binding unchanged after the call completes.
		/// </summary>
		/// <param name="action">The update method to call.</param>
		private void UpdateTexParam(Action action)
		{
			RaisePriorErrors();

			GL.GetInteger(GetPName.TextureBinding2D, out int oldHandle);
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, Handle);
				action();
				RaiseErrors("Failed to change texture parameter");
			}
			finally
			{
				GL.BindTexture(TextureTarget.Texture2D, oldHandle);
			}
		}

		#endregion

		#region Error management and debugging support

		/// <summary>
		/// Prune the given standard .NET stack trace by removing any lines that
		/// start with "System." or "Microsoft." and keeping the first three that
		/// are left.  Three lines of non-system calls are usually enough to identify
		/// what caused something to happen (and if they aren't, your code is probably
		/// too abstract!).
		/// </summary>
		/// <param name="stackTrace">The stack trace to prune.</param>
		/// <returns>The same stack trace, with the non-system and blank lines removed,
		/// and only the top three remaining.</returns>
		private static string PruneStackTrace(string stackTrace)
			=> string.Join("\n",
				stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("System.") && !s.StartsWith("Microsoft."))
					.Take(3)
					.ToArray());

		/// <summary>
		/// Raise any OpenGL errors that occurred before invoking Texture methods as a new exception.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if there were any prior unhandled
		/// OpenGL errors.</exception>
		protected static void RaisePriorErrors()
		{
			ErrorCode errorCode = GL.GetError();
			if (errorCode == ErrorCode.NoError)
				return;

			List<ErrorCode> errors = new List<ErrorCode> { errorCode };
			while ((errorCode = GL.GetError()) != ErrorCode.NoError
				&& errors.Count < 10)
				errors.Add(errorCode);

			throw new InvalidOperationException("OpenGL error: Unhandled prior error(s): " +
				string.Join(", ", errors.Select(e => e.ToString())));
		}

		/// <summary>
		/// Raise any new OpenGL errors as an exception.
		/// </summary>
		/// <param name="message">A message to include.</param>
		/// <param name="args">Any arguments to format in that message.</param>
		/// <exception cref="InvalidOperationException">The exception, which will be
		/// raised if there were any OpenGL errors.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void RaiseErrors(string message, params object?[] args)
		{
			ErrorCode errorCode = GL.GetError();
			if (errorCode == ErrorCode.NoError)
				return;

			throw new InvalidOperationException("OpenGL error: " + string.Format(message, args) + ": " + errorCode);
		}

		#endregion

		#region Stringification (for debugging)

		/// <summary>
		/// Convert this texture to a string, primarily for debugging purposes.
		/// </summary>
		/// <returns>A simple string representation of the texture.</returns>
		public override string ToString()
			=> !string.IsNullOrEmpty(Name)
				? $"\"{Name}\" ({Width},{Height})"
				: $"texture8 ({Width},{Height})";

		#endregion
	}
}