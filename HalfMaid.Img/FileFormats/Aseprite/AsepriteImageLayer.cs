using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// An image layer.
	/// </summary>
	[DebuggerDisplay("Image layer {Name}, {Cels.Count} cels")]
	public class AsepriteImageLayer : AsepriteLayer
	{
		/// <summary>
		/// What kind of blending to use 
		/// </summary>
		public AsepriteBlendMode BlendMode { get; }

		/// <summary>
		/// The opacity of this layer, from 0 (transparent) to 255 (opaque).
		/// Only valid if header flags has the 'Opacity' flag set.
		/// </summary>
		public byte Opacity { get; }

		/// <summary>
		/// The cels that belong to this layer.
		/// </summary>
		public IReadOnlyList<AsepriteCel> Cels => _cels;
		private List<AsepriteCel> _cels = new List<AsepriteCel>();

		/// <summary>
		/// Construct a new image layer from raw source file bytes.
		/// </summary>
		/// <param name="frame">The frame this layer is part of.</param>
		/// <param name="header">The image header.</param>
		/// <param name="sourceData">The raw source bytes for this layer.</param>
		public AsepriteImageLayer(AsepriteFrame frame, in AsepriteHeader header, ReadOnlySpan<byte> sourceData)
			: base(frame, sourceData)
		{
			if ((Flags & AsepriteLayerFlags.Background) == 0)
			{
				BlendMode = (AsepriteBlendMode)(ushort)(sourceData[10] | (sourceData[11] << 8));
				if ((header.Flags & AsepriteHeaderFlags.Opacity) != 0)
					Opacity = sourceData[12];
			}
		}

		internal override void AddLayer(AsepriteLayer layer)
			=> throw new NotSupportedException("Image layers cannot support child layers.");

		internal override void AddCel(AsepriteCel cel)
		{
			_cels.Add(cel);
			cel.Layer = this;
		}
	}
}
