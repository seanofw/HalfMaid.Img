using System;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// Base class for all types of Aseprite layers.
	/// </summary>
	public abstract class AsepriteLayer
	{
		/// <summary>
		/// The frame this layer belongs to.
		/// </summary>
		public AsepriteFrame Frame { get; }

		/// <summary>
		/// The parent group for this layer, if any.
		/// </summary>
		public AsepriteGroupLayer? Parent { get; internal set; }

		/// <summary>
		/// Optional flags describing this layer.
		/// </summary>
		public AsepriteLayerFlags Flags { get; }

		/// <summary>
		/// What kind of layer this is (an image or a group).
		/// </summary>
		public AsepriteLayerKind Kind { get; }

		/// <summary>
		/// The level of this layer relative to its parent layer (i.e., its group depth).
		/// </summary>
		public short ChildLevel { get; }

		/// <summary>
		/// The name of this layer.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Read a layer from the given raw Aseprite file bytes.
		/// </summary>
		/// <param name="frame">The frame this layer is part of.</param>
		/// <param name="header">The image header.</param>
		/// <param name="sourceData">The raw Aseprite file bytes for this layer.</param>
		/// <param name="previousLayer">The previous layer that was just read (used for
		/// building the layer tree).</param>
		/// <returns>The newly-constructed layer.</returns>
		public static AsepriteLayer ReadLayer(AsepriteFrame frame, in AsepriteHeader header, ReadOnlySpan<byte> sourceData, AsepriteLayer previousLayer)
		{
			AsepriteLayerKind layerKind = (AsepriteLayerKind)(sourceData[2] | (sourceData[3] << 8));

			AsepriteLayer layer;
			switch (layerKind)
			{
				case AsepriteLayerKind.Image:
					layer = new AsepriteImageLayer(frame, header, sourceData);
					break;

				case AsepriteLayerKind.Group:
					layer = new AsepriteGroupLayer(frame, sourceData);
					break;

				default:
					throw new InvalidOperationException($"Source data is invalid: Unknown layer kind {(short)layerKind}.");
			}

			// Build up the layer tree.
			int previousLevel = previousLayer.ChildLevel;
			int newLevel = layer.ChildLevel;
			if (newLevel == previousLevel)
				previousLayer.Parent?.AddLayer(layer);
			else if (newLevel > previousLevel)
				previousLayer.AddLayer(layer);
			else if (newLevel < previousLevel)
			{
				AsepriteLayer? parent = previousLayer.Parent;
				if (parent == null)
					throw new InvalidOperationException($"Source data is invalid: Invalid layer level {newLevel}.");
				int levels = previousLevel - newLevel;
				while (levels-- != 0)
				{
					parent = parent.Parent;
					if (parent == null)
						throw new InvalidOperationException($"Source data is invalid: Invalid layer level {newLevel}.");
				}
				parent.AddLayer(layer);
			}

			return layer;
		}

		internal abstract void AddLayer(AsepriteLayer layer);
		internal abstract void AddCel(AsepriteCel cel);

		/// <summary>
		/// Construct a new empty layer that belongs to the given frame.
		/// </summary>
		/// <param name="frame">The frame this layer belongs to.</param>
		protected AsepriteLayer(AsepriteFrame frame)
		{
			Frame = frame;
			Flags = 0;
			Kind = AsepriteLayerKind.Group;
			ChildLevel = -1;
			Name = string.Empty;
		}

		/// <summary>
		/// Construct a new layer that belongs to the given frame from the given raw Aseprite file bytes.
		/// </summary>
		/// <param name="frame">The frame this layer belongs to.</param>
		/// <param name="sourceData">The raw Aseprite file bytes for this layer.</param>
		protected AsepriteLayer(AsepriteFrame frame, ReadOnlySpan<byte> sourceData)
		{
			Frame = frame;
			Flags = (AsepriteLayerFlags)(sourceData[0] | (sourceData[1] << 8))
				& AsepriteLayerFlags.PersistentFlagsMask;
			Kind = (AsepriteLayerKind)(sourceData[2] | (sourceData[3] << 8));
			ChildLevel = (short)(sourceData[4] | (sourceData[5] << 8));
			// 6-7 reserved
			// 8-9 reserved
			// 10-11 BlendMode
			// 12 Opacity
			// 13-15 reserved

			Name = AsepriteImage.ReadString(sourceData.Slice(16));
		}
	}
}
