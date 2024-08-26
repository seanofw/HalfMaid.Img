using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HalfMaid.Img
{
	/// <summary>
	/// A class that provides an efficient nearest-neighbor search for a color
	/// against a palette of known colors.  Internally, this uses either k-d trees
	/// or linear searches, depending on the structure of the source data.
	/// </summary>
	public class Color32Searcher
	{
		#region Nested classes (there are several of these)

		/// <summary>
		/// A single node in the tree.
		/// </summary>
		private struct KDNode
		{
			public int Parent;                  // Parent index, or -1 if none
			public int Left;                    // Left child index, or -1 if none
			public int Right;                   // Right child index, or -1 if none
			public ColorChannel SplitChannel;   // The channel along which this KDNode splits (None if a leaf).
			public int ColorIndex;              // The index of the color for this node.

#if DEBUG
			public Color32 Color;
#endif

			public bool IsLeaf => SplitChannel == ColorChannel.None;
			public bool HasLeft => Left >= 0;
			public bool HasRight => Right >= 0;
			public bool HasParent => Parent >= 0;

#if DEBUG
			public override string ToString()
				=> $"parent={Parent}, left={Left}, right={Right}, channel={SplitChannel}, color={Color}";
#endif
		}

		/// <summary>
		/// A comparer for sorting the colors by their red values, tracking the source
		/// index of each color along with its actual color values.
		/// </summary>
		private class SortByRed : IComparer<(Color32 Color, int SourceIndex)>
		{
			public static SortByRed Instance = new SortByRed();
			public int Compare((Color32 Color, int SourceIndex) a, (Color32 Color, int SourceIndex) b)
				=> a.Color.R - b.Color.R;
		}

		/// <summary>
		/// A comparer for sorting the colors by their green values, tracking the source
		/// index of each color along with its actual color values.
		/// </summary>
		private class SortByGreen : IComparer<(Color32 Color, int SourceIndex)>
		{
			public static SortByGreen Instance = new SortByGreen();
			public int Compare((Color32 Color, int SourceIndex) a, (Color32 Color, int SourceIndex) b)
				=> a.Color.G - b.Color.G;
		}

		/// <summary>
		/// A comparer for sorting the colors by their blue values, tracking the source
		/// index of each color along with its actual color values.
		/// </summary>
		private class SortByBlue : IComparer<(Color32 Color, int SourceIndex)>
		{
			public static SortByBlue Instance = new SortByBlue();
			public int Compare((Color32 Color, int SourceIndex) a, (Color32 Color, int SourceIndex) b)
				=> a.Color.B - b.Color.B;
		}

		/// <summary>
		/// A comparer for sorting the colors by their alpha values, tracking the source
		/// index of each color along with its actual color values.
		/// </summary>
		private class SortByAlpha : IComparer<(Color32 Color, int SourceIndex)>
		{
			public static SortByAlpha Instance = new SortByAlpha();
			public int Compare((Color32 Color, int SourceIndex) a, (Color32 Color, int SourceIndex) b)
				=> a.Color.A - b.Color.A;
		}

		/// <summary>
		/// This builder class creates a KD tree from a given set of source data.
		/// It has a single use:  Build once, and then throw it away.
		/// </summary>
		private struct KDTreeBuilder
		{
			private readonly (Color32 Color, int SourceIndex)[] _colors;
			private readonly KDNode[] _nodes;
			private int _numNodes;
			private readonly IComparer<(Color32 Color, int SourceIndex)>[] _comparers;

			public KDTreeBuilder((Color32 Color, int SourceIndex)[] colors,
				IComparer<(Color32 Color, int SourceIndex)>[] comparers)
			{
				_comparers = comparers;
				_colors = colors;
				_nodes = new KDNode[colors.Length];
				_numNodes = 0;
			}

			public (KDNode[] Nodes, int RootNode) Build()
			{
				int rootNode = Build(-1, 0, _colors.Length, ColorChannel.Red);
				return (_nodes, rootNode);
			}

			private int Build(int parent, int start, int length, ColorChannel channel)
			{
				int nodeIndex;

				// Base case:  Only one color left.
				if (length == 1)
				{
					nodeIndex = _numNodes;
					_nodes[_numNodes++] = new KDNode
					{
						Parent = parent,
						Left = -1,
						Right = -1,
						SplitChannel = ColorChannel.None,
						ColorIndex = start,
#if DEBUG
						Color = _colors[start].Color,
#endif
					};
					return nodeIndex;
				}

				IComparer<(Color32 Color, int SourceIndex)> comparer = _comparers[(int)channel];

				// Trivial fast case:  Two colors.
				if (length == 2)
				{
					// If they're out of order, swap them to result in a sorted subarray.
					if (comparer.Compare(_colors[start], _colors[start + 1]) > 0)
					{
						(Color32 Color, int SourceIndex) temp = _colors[start];
						_colors[start] = _colors[start + 1];
						_colors[start + 1] = temp;
					}

					// Now make the new nodes for the colors.
					nodeIndex = _numNodes;
					_nodes[_numNodes++] = new KDNode
					{
						Parent = parent,
						Left = nodeIndex + 1,
						Right = -1,
						SplitChannel = channel,
						ColorIndex = start + 1,
#if DEBUG
						Color = _colors[start + 1].Color,
#endif
					};
					_nodes[_numNodes++] = new KDNode
					{
						Parent = nodeIndex,
						Left = -1,
						Right = -1,
						SplitChannel = ColorChannel.None,
						ColorIndex = start,
#if DEBUG
						Color = _colors[start].Color,
#endif
					};
					return nodeIndex;
				}

				// Default case:  N colors to construct a subtree from.  So first, sort
				// them by whatever channel this is.
				ArraySortHelper<(Color32 Color, int SourceIndex)>.IntrospectiveSort(
					_colors.AsSpan().Slice(start, length), comparer);

				// Construct a new node from the midpoint color.
				int midpt = length / 2;
				nodeIndex = _numNodes;
				_nodes[_numNodes++] = new KDNode
				{
					Parent = parent,
					Left = -1,
					Right = -1,
					SplitChannel = channel,
					ColorIndex = start + midpt,
#if DEBUG
					Color = _colors[start + midpt].Color,
#endif
				};

				// Move to the next color channel.
				ColorChannel nextChannel = (ColorChannel)((int)channel + 1);
				if ((int)nextChannel >= _comparers.Length)
					nextChannel = ColorChannel.Red;

				// Recurse on the children.
				_nodes[nodeIndex].Left = Build(nodeIndex, start, midpt, nextChannel);
				_nodes[nodeIndex].Right = Build(nodeIndex, start + midpt + 1, length - (midpt + 1), nextChannel);

				return nodeIndex;
			}
		}

		#endregion

		#region Private state

		/// <summary>
		/// A function that provides a distance metric between two colors.
		/// </summary>
		private readonly Func<Color32, Color32, double> _dist;

		/// <summary>
		/// A function that provides a distance metric for a single color axis.
		/// This should return a result equivalent to _dist, if all fields of
		/// both colors were zero except for the given channel.
		/// </summary>
		private readonly Func<ColorChannel, Color32, Color32, double> _singleAxisDistance;

		/// <summary>
		/// This array holds a lookup table into the original palette.  Each KDNode maps
		/// to a subrange of this array.
		/// </summary>
		private readonly (Color32 Color, int SourceIndex)[] _colors;

		/// <summary>
		/// A small LRU cache of 4 entries, which is useful for repeated color queries.
		/// </summary>
		private readonly (Color32 TestColor, Color32 MatchColor, int SourceIndex)[] _cache;
		private int _cacheIndex;

		/// <summary>
		/// The tree is represented as an (immutable) sequence of nodes.
		/// </summary>
		private readonly KDNode[] _nodes;

		/// <summary>
		/// The index of the root node in the tree.
		/// </summary>
		private readonly int _rootNode;

		#endregion

		#region Public state

		/// <summary>
		/// Stateful configuration:  Colors that should *not* be returned.  This is directly
		/// exposed so that it can be updated on-the-fly with minimal memory overhead; but its
		/// existence does mean that the ColorSearcher is neither immutable nor thread-safe.
		/// </summary>
		public HashSet<int> Exclusions { get; } = new HashSet<int>();

		#endregion

		#region Construction

		/// <summary>
		/// Construct a k-d tree for the given color palette for fast nearest-neighbor search.
		/// </summary>
		/// <param name="colors">The set of colors to analyze, which must be between 1 and 65536
		/// colors.</param>
		/// <param name="dist">A function that provides a distance metric between two colors.
		/// If not provided, the default function will use squared Euclidean distance in RGB
		/// (or RGBA) space.</param>
		/// <param name="singleAxisDist">A function that provides a distance metric for a single
		/// color axis.  This should return a result equivalent to _dist, if all fields of
		/// both colors were zero except for the given channel.  If not provided, this will
		/// use Euclidean distance in RGB (or RGBA) space.</param>
		/// <param name="includeAlpha">Whether to include alpha values in the tree construction,
		/// or ignore alpha values.</param>
		public Color32Searcher(ReadOnlySpan<Color32> colors,
			Func<Color32, Color32, double>? dist = null,
			Func<ColorChannel, Color32, Color32, double>? singleAxisDist = null,
			bool includeAlpha = false)
		{
			if (colors.Length < 1)
				throw new ArgumentException("Number of colors must be at least 1.");

			if (dist != null)
				_dist = dist;
			else if (includeAlpha)
			{
				_dist = (a, b) =>
					  (a.R - b.R) * (a.R - b.R)
					+ (a.G - b.G) * (a.G - b.G)
					+ (a.B - b.B) * (a.B - b.B)
					+ (a.A - b.A) * (a.A - b.A);
			}
			else
			{
				_dist = (a, b) =>
					  (a.R - b.R) * (a.R - b.R)
					+ (a.G - b.G) * (a.G - b.G)
					+ (a.B - b.B) * (a.B - b.B);
			}

			_singleAxisDistance = singleAxisDist ?? ((axis, a, b) => {
				byte ca = a[axis];
				byte cb = b[axis];
				return (ca - cb) * (ca - cb);
			});

			(Color32 Color, int SourceIndex)[] colorMap = new (Color32 Color, int SourceIndex)[colors.Length];
			for (int i = 0; i < colorMap.Length; i++)
				colorMap[i] = (colors[i], i);
			_colors = colorMap;

			_cache = new (Color32 TestColor, Color32 MatchColor, int SourceIndex)[4];

			(_nodes, _rootNode) = includeAlpha ? CreateTree4D(colorMap) : CreateTree3D(colorMap);

#if DEBUG
			RecursivelyValidateTree(_nodes, _rootNode, -1);
#endif
		}

#if DEBUG
		private void RecursivelyValidateTree(KDNode[] nodes, int node, int parentNode)
		{
			Debug.Assert(nodes[node].Parent == parentNode);
			Debug.Assert(nodes[node].Color == _colors[nodes[node].ColorIndex].Color);

			if (!nodes[node].HasLeft && !nodes[node].HasRight)
				Debug.Assert(nodes[node].IsLeaf);
			else
				Debug.Assert(!nodes[node].IsLeaf);

			if (nodes[node].HasLeft)
			{
				int left = nodes[node].Left;
				Color32 c = nodes[node].Color;
				Color32 lc = nodes[left].Color;
				ColorChannel channel = nodes[node].SplitChannel;
				Debug.Assert(lc[channel] <= c[channel]);
				RecursivelyValidateTree(nodes, left, node);
			}

			if (nodes[node].HasRight)
			{
				int right = nodes[node].Right;
				Color32 c = nodes[node].Color;
				Color32 rc = nodes[right].Color;
				ColorChannel channel = nodes[node].SplitChannel;
				Debug.Assert(c[channel] <= rc[channel]);
				RecursivelyValidateTree(nodes, right, node);
			}
		}
#endif

		private (KDNode[] Nodes, int RootNode) CreateTree4D((Color32 Color, int SourceIndex)[] colors)
		{
			IComparer<(Color32 Color, int SourceIndex)>[] comparers = new IComparer<(Color32 Color, int SourceIndex)>[]
			{
				null!,
				SortByRed.Instance,
				SortByGreen.Instance,
				SortByBlue.Instance,
				SortByAlpha.Instance,
			};

			return new KDTreeBuilder(colors, comparers).Build();
		}

		private (KDNode[] Nodes, int RootNode) CreateTree3D((Color32 Color, int SourceIndex)[] colors)
		{
			IComparer<(Color32 Color, int SourceIndex)>[] comparers = new IComparer<(Color32 Color, int SourceIndex)>[]
			{
				null!,
				SortByRed.Instance,
				SortByGreen.Instance,
				SortByBlue.Instance,
			};

			return new KDTreeBuilder(colors, comparers).Build();
		}

		#endregion

		#region Searches

		/// <summary>
		/// Find the color in the given palette that is closest to the given color.
		/// </summary>
		/// <param name="color">The color to search for.</param>
		/// <returns>The closest matching color, and its index in the given palette.</returns>
		public (Color32 Color, int SourceIndex) FindNearest(Color32 color)
		{
			// First try the (small!) color cache.  For repeated exact-color queries,
			// this is a performance win, but it reaches the point of diminishing
			// returns pretty fast, which is why there's only four entries in it.
			if (_cache[0].SourceIndex >= 0 && _cache[0].TestColor == color
				&& !Exclusions.Contains(_cache[0].SourceIndex))
				return (_cache[0].MatchColor, _cache[0].SourceIndex);
			if (_cache[1].SourceIndex >= 0 && _cache[1].TestColor == color
				&& !Exclusions.Contains(_cache[1].SourceIndex))
				return (_cache[1].MatchColor, _cache[1].SourceIndex);
			if (_cache[2].SourceIndex >= 0 && _cache[2].TestColor == color
				&& !Exclusions.Contains(_cache[2].SourceIndex))
				return (_cache[2].MatchColor, _cache[2].SourceIndex);
			if (_cache[3].SourceIndex >= 0 && _cache[3].TestColor == color
				&& !Exclusions.Contains(_cache[3].SourceIndex))
				return (_cache[3].MatchColor, _cache[3].SourceIndex);

			// Recursion costs are pretty high and don't play nice with branch prediction,
			// so if there are 16 colors or less, it's usually faster to perform a linear
			// search.  Otherwise, we perform tree traversal for real.
			(Color32 matchColor, int sourceIndex) = _colors.Length <= 16
				? FindNearestByLinearSearch(color)
				: FindNearestByTreeSearch(color);

			// Now that we found the color, put it in the cache for the next pass.
			_cache[_cacheIndex] = (color, matchColor, sourceIndex);
			_cacheIndex = (_cacheIndex + 1) & 3;

			return (matchColor, sourceIndex);
		}

		/// <summary>
		/// Find the color in the given palette that is closest to the given color
		/// using k-d tree traversal.
		/// </summary>
		/// <param name="color">The color to search for.</param>
		/// <returns>The closest matching color, and its index in the given palette.</returns>
		private (Color32 Color, int SourceIndex) FindNearestByTreeSearch(Color32 color)
		{
			int bestNode = -1;
			double bestDist = double.PositiveInfinity;

			void RecursivelySearch(int node)
			{
				if (node < 0)
					return;

				int colorIndex = _nodes[node].ColorIndex;
				(Color32 nodeColor, _) = _colors[colorIndex];

				if (!Exclusions.Contains(colorIndex))
				{
					double dist = _dist(nodeColor, color);
					if (bestNode < 0 || dist < bestDist)
					{
						bestDist = dist;
						bestNode = node;
					}
				}

				ColorChannel channel = _nodes[node].SplitChannel;

				if (bestDist == 0 || channel == ColorChannel.None)
					return;

				(int firstSide, int lastSide) = nodeColor[channel] > color[channel]
					? (_nodes[node].Left, _nodes[node].Right)
					: (_nodes[node].Right, _nodes[node].Left);

				RecursivelySearch(firstSide);

				if (_singleAxisDistance(channel, nodeColor, color) >= bestDist)
					return;

				RecursivelySearch(lastSide);
			}

			RecursivelySearch(_rootNode);

			int colorIndex = _nodes[bestNode].ColorIndex;
			return _colors[colorIndex];
		}

		/// <summary>
		/// Find the nearest color just by performing a linear search for it.  Low
		/// constant-time overhead, but bad growth for large N.
		/// </summary>
		/// <param name="color">The color to search for.</param>
		/// <returns>The closest matching color, and its index in the given palette.</returns>
		private (Color32 Color, int SourceIndex) FindNearestByLinearSearch(Color32 color)
		{
			double bestDist = double.PositiveInfinity;
			int bestIndex = -1;

			for (int i = 0; i < _colors.Length; i++)
			{
				if (Exclusions.Contains(i))
					continue;

				double dist = _dist(color, _colors[i].Color);
				if (dist < bestDist)
				{
					bestDist = dist;
					bestIndex = i;
				}
			}

			return bestIndex >= 0 ? _colors[bestIndex] : (Color32.Transparent, -1);
		}

		#endregion
	}
}
