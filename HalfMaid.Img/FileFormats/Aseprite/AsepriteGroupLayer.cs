using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HalfMaid.Img.FileFormats.Aseprite
{
	/// <summary>
	/// A group layer, in an Aseprite image.  Group layers combine a set of child layers together.
	/// </summary>
	[DebuggerDisplay("Group layer {Name}: {Count} child layers")]
	public class AsepriteGroupLayer : AsepriteLayer, IReadOnlyList<AsepriteLayer>
	{
		/// <summary>
		/// The child layers of this group.
		/// </summary>
		public IReadOnlyList<AsepriteLayer> Layers => _layers;
		private readonly List<AsepriteLayer> _layers = new List<AsepriteLayer>();

		/// <summary>
		/// The number of child layers in this group.
		/// </summary>
		public int Count => _layers.Count;

		/// <summary>
		/// Get a child layer, by index.
		/// </summary>
		/// <param name="index">The zero-based index of the child layer to retrieve.</param>
		/// <returns>The child layer.</returns>
		public AsepriteLayer this[int index] => _layers[index];

		/// <summary>
		/// Construct a new, empty group layer associated with the given frame.
		/// </summary>
		/// <param name="frame">The frame that owns this group.</param>
		public AsepriteGroupLayer(AsepriteFrame frame)
			: base(frame)
		{
		}

		/// <summary>
		/// Construct a group layer for the given frame, from the given Aseprite file raw bytes.
		/// </summary>
		/// <param name="frame">The frame that owns this group.</param>
		/// <param name="sourceData">The file data used to construct this group.</param>
		public AsepriteGroupLayer(AsepriteFrame frame, ReadOnlySpan<byte> sourceData)
			: base(frame, sourceData)
		{
		}

		/// <summary>
		/// Find the given named child layer, case-insensitive.  This is *not* a lookup;
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

		internal override void AddLayer(AsepriteLayer layer)
		{
			_layers.Add(layer);
			layer.Parent = this;
		}

		internal override void AddCel(AsepriteCel cel)
			=> throw new NotSupportedException("Cannot add a cel directly to a group layer.");

		/// <summary>
		/// Enumerate all of the child layers of this group.
		/// </summary>
		/// <returns>An enumerator that will yield each of the child layers of this group, in order.</returns>
		public IEnumerator<AsepriteLayer> GetEnumerator()
			=> ((IEnumerable<AsepriteLayer>)_layers).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable)_layers).GetEnumerator();
	}
}
