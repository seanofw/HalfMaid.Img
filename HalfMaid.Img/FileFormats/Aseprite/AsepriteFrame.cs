using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// A single frame in an animation sequence.  Each frame has N layers, and has one cel
	/// for each layer.
	/// </summary>
	[DebuggerDisplay("Frame: {Layers.Count} layers, {Cels.Count} cels, {Duration} msec")]
	public class AsepriteFrame
	{
		/// <summary>
		/// The AsepriteImage this frame is a part of.
		/// </summary>
		public AsepriteImage Image { get; }

		/// <summary>
		/// What index this frame is within the containing AsepriteImage.
		/// </summary>
		public int FrameIndex { get; }

		/// <summary>
		/// How long this frame lasts, in milliseconds.
		/// </summary>
		public ushort Duration => Header.Duration;

		/// <summary>
		/// The number of file chunks that were used to store this frame.
		/// </summary>
		public ushort NumChunks => Header.Chunks;

		/// <summary>
		/// The palette for this frame.  Note that these are NOT premultiplied
		/// color values; they're raw RGBA, and may need to be converted to PM
		/// colors to actually be usable.
		/// </summary>
		public Color32[] Palette { get; private set; }

		/// <summary>
		/// The tree of layers for all pixels in this frame.
		/// </summary>
		public AsepriteGroupLayer LayerTree { get; }

		/// <summary>
		/// All layers for this frame, in order.
		/// </summary>
		public List<AsepriteLayer> Layers { get; }

		/// <summary>
		/// The raw cel data for this frame.
		/// </summary>
		public List<AsepriteCel> Cels { get; }

		/// <summary>
		/// Any tags attached to this frame describing animation behavior.
		/// </summary>
		public List<AsepriteTag> Tags { get; }

		/// <summary>
		/// The original file header this sprite came from.
		/// </summary>
		private AsepriteFrameHeader Header;

		private static readonly Color32[] _emptyPalette = new Color32[0];

		private AsepriteFrame(AsepriteImage image, int frameIndex)
		{
			Image = image;
			FrameIndex = frameIndex;
			Palette = _emptyPalette;
			LayerTree = new AsepriteGroupLayer(this);
			Layers = new List<AsepriteLayer>();
			Cels = new List<AsepriteCel>();
			Tags = new List<AsepriteTag>();
		}

		/// <summary>
		/// Find the given named layer, case-insensitive.  This is *not* a lookup;
		/// it is an O(n) search.
		/// </summary>
		/// <param name="name">The name to search for.</param>
		/// <returns>The matching layer, if it exists.</returns>
		public AsepriteLayer? FindLayerByName(string name)
		{
			foreach (AsepriteLayer layer in Layers)
			{
				if (string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase))
					return layer;
			}
			return null;
		}

		/// <summary>
		/// Find the given named layer, case-insensitive, using a full slash-separated
		/// path, recursively. This is *not* a lookup; it is an O(n) search.
		/// </summary>
		/// <typeparam name="T">The type of layer we expect to find.</typeparam>
		/// <param name="path">The path to search for.</param>
		/// <returns>The matching layer, if it exists.</returns>
		public T? FindLayerByPath<T>(string path)
			where T : AsepriteLayer
		{
			string[] pieces = path.Split(new char[] { '/', '\\' });

			AsepriteLayer? layer = FindLayerByName(pieces[0]);
			if (layer == null) return null;

			for (int i = 1; i < pieces.Length; i++)
			{
				if (!(layer is AsepriteGroupLayer groupLayer))
					return null;
				layer = groupLayer.FindLayerByName(pieces[i]);
			}

			return layer as T;
		}

		/// <summary>
		/// Read a 32-bit RGBA image from the named layer.
		/// </summary>
		/// <param name="layerPath">The full path to the layer to read from.</param>
		/// <returns>The RGBA image, or null if the path is unknown.</returns>
		public Image32? GetImageFromLayer(string layerPath)
		{
			AsepriteImageLayer? imageLayer = FindLayerByPath<AsepriteImageLayer>(layerPath);
			if (imageLayer == null)
				return null;

			AsepriteCel? cel = imageLayer.Cels.FirstOrDefault();
			if (cel == null)
				return null;

			return Image.ColorMode switch
			{
				AsepriteColorMode.Indexed => cel.GetClippedImage8(Palette)?.ToImage32(),
				AsepriteColorMode.Rgb => cel.GetClippedImage(),
				_ => null,
			};
		}

		/// <summary>
		/// Read a full frame's worth of content from the current position in the
		/// source data.
		/// </summary>
		/// <param name="image">The Aseprite image (animation) this frame belongs to.</param>
		/// <param name="frameIndex">The index of this frame, relative to all other frames.</param>
		/// <param name="header">The Aseprite image header.</param>
		/// <param name="colorMode">The current color mode.</param>
		/// <param name="sourceData">The raw source data for this frame.</param>
		/// <param name="frame">The frame that was constructed from the source data.</param>
		/// <returns>The source data pointer, moved past the data for this frame.</returns>
		public static ReadOnlySpan<byte> ReadFrame(AsepriteImage image, int frameIndex,
			in AsepriteHeader header, AsepriteColorMode colorMode,
			ReadOnlySpan<byte> sourceData, out AsepriteFrame frame)
		{
			frame = new AsepriteFrame(image, frameIndex);

			ReadHeader(sourceData, out frame.Header);
			sourceData = sourceData.Slice(16);

			AsepriteLayer previousLayer = frame.LayerTree;

			int numChunks = frame.Header.Chunks;
			for (int i = 0; i < numChunks; i++)
			{
				int size = sourceData[0]
					| (sourceData[1] << 8)
					| (sourceData[2] << 16)
					| (sourceData[3] << 24);
				AsepriteChunkKind kind = (AsepriteChunkKind)(sourceData[4] | (sourceData[5] << 8));

				switch (kind)
				{
					case AsepriteChunkKind.Palette:
						frame.Palette = ReadPalette(sourceData.Slice(6), frame.Palette);
						break;

					case AsepriteChunkKind.Layer:
						previousLayer = AsepriteLayer.ReadLayer(frame, header, sourceData.Slice(6), previousLayer);
						frame.Layers.Add(previousLayer);
						break;

					case AsepriteChunkKind.Cel:
						AsepriteCel cel = new AsepriteCel(frame, header, colorMode, sourceData.Slice(6, size - 6), frame.Palette);
						frame.Cels.Add(cel);
						frame.Layers[cel.LayerIndex].AddCel(cel);
						break;

					case AsepriteChunkKind.Tags:
						int numTags = (sourceData[6] | (sourceData[7] << 8));
						sourceData = sourceData.Slice(8);
						for (int j = 0; j < numTags; j++)
						{
							sourceData = AsepriteTag.ReadTag(frame, sourceData, out AsepriteTag tag);
							frame.Tags.Add(tag);
						}
						break;
				}

				sourceData = sourceData.Slice(size);
			}

			return sourceData;
		}

		private static void ReadHeader(ReadOnlySpan<byte> sourceData, out AsepriteFrameHeader header)
		{
			unsafe
			{
				if (sourceData.Length < sizeof(AsepriteFrameHeader))
					throw new ArgumentException("Source data is too small to be a valid Aseprite image frame header.");

				fixed (byte* srcBase = sourceData)
				fixed (AsepriteFrameHeader* headerBase = &header)
				{
					Buffer.MemoryCopy(srcBase, headerBase, sizeof(AsepriteFrameHeader), sizeof(AsepriteFrameHeader));
				}
			}

			if (header.Size > sourceData.Length)
				throw new ArgumentException($"Aseprite frame header is corrupt; invalid size '{header.Size}'.");
			if (header.Magic != 0xF1FA)
				throw new ArgumentException($"Aseprite frame header is corrupt; invalid magic number '{header.Magic:X4}'.");
		}

		private static Color32[] ReadPalette(ReadOnlySpan<byte> sourceData, Color32[] palette)
		{
			int newSize = sourceData[0]
				| (sourceData[1] << 8)
				| (sourceData[2] << 16)
				| (sourceData[3] << 24);
			int fromColor = sourceData[4]
				| (sourceData[5] << 8)
				| (sourceData[6] << 16)
				| (sourceData[7] << 24);
			int toColor = sourceData[8]
				| (sourceData[9] << 8)
				| (sourceData[10] << 16)
				| (sourceData[11] << 24);

			sourceData = sourceData.Slice(20);

			if (palette.Length != newSize)
			{
				Color32[] newPalette = new Color32[newSize];
				if (palette.Length > 0)
					Array.Copy(palette, newPalette, palette.Length);
				palette = newPalette;
			}

			for (int i = fromColor; i <= toColor; i++)
			{
				ushort flags = (ushort)(sourceData[0] | (sourceData[1] << 8));
				byte r = sourceData[2];
				byte g = sourceData[3];
				byte b = sourceData[4];
				byte a = sourceData[5];
				sourceData = sourceData.Slice(6);

				if ((flags & 1) != 0)
				{
					// This color has a name (is that even supported anymore in Aseprite?)
					short length = (short)(sourceData[0] | (sourceData[1] << 8));
					sourceData = sourceData.Slice(2);
					if (length > 0)
						sourceData = sourceData.Slice(length);
				}

				palette[i] = new Color32(r, g, b, a);
			}

			return palette;
		}
	}
}