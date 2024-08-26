using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HalfMaid.Img
{
#if !NET5_0_OR_GREATER
    internal sealed partial class ArraySortHelper<TKey, TValue>
    {
        public void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                IntrospectiveSort(keys, values, comparer ?? Comparer<TKey>.Default);
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("Bad comparer");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Comparer failed", e);
            }
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer, int i, int j)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(0 <= i && i < keys.Length && i < values.Length);
            Debug.Assert(0 <= j && j < keys.Length && j < values.Length);
            Debug.Assert(i != j);

            if (comparer!.Compare(keys[i], keys[j]) > 0)
            {
                TKey key = keys[i];
                keys[i] = keys[j];
                keys[j] = key;

                TValue value = values[i];
                values[i] = values[j];
                values[j] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(Span<TKey> keys, Span<TValue> values, int i, int j)
        {
            Debug.Assert(i != j);

            TKey k = keys[i];
            keys[i] = keys[j];
            keys[j] = k;

            TValue v = values[i];
            values[i] = values[j];
            values[j] = v;
        }

        internal static void IntrospectiveSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(keys.Length == values.Length);

            if (keys.Length > 1)
            {
                IntroSort(keys, values, 2 * (Log2((uint)keys.Length) + 1), comparer!);
            }
        }

        private static void IntroSort(Span<TKey> keys, Span<TValue> values, int depthLimit, IComparer<TKey> comparer)
        {
            Debug.Assert(!keys.IsEmpty);
            Debug.Assert(values.Length == keys.Length);
            Debug.Assert(depthLimit >= 0);
            Debug.Assert(comparer != null);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= 16)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 1);
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 2);
                        SwapIfGreaterWithValues(keys, values, comparer!, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), values.Slice(p + 1, partitionSize - (p + 1)), depthLimit, comparer!);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(keys.Length >= 16);
            Debug.Assert(comparer != null);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithValues(keys, values, comparer!, 0, middle);  // swap the low with the mid point
            SwapIfGreaterWithValues(keys, values, comparer!, 0, hi);   // swap the low with the high
            SwapIfGreaterWithValues(keys, values, comparer!, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer!.Compare(keys[++left], pivot) < 0) ;
                while (comparer!.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, values, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(!keys.IsEmpty);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, values, i, n, comparer!);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, values, 0, i - 1);
                DownHeap(keys, values, 1, i - 1, comparer!);
            }
        }

        private static void DownHeap(Span<TKey> keys, Span<TValue> values, int i, int n, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);

            TKey d = keys[i - 1];
            TValue dValue = values[i - 1];

            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && comparer!.Compare(keys[child - 1], keys[child]) < 0)
                {
                    child++;
                }

                if (!(comparer!.Compare(d, keys[child - 1]) < 0))
                    break;

                keys[i - 1] = keys[child - 1];
                values[i - 1] = values[child - 1];
                i = child;
            }

            keys[i - 1] = d;
            values[i - 1] = dValue;
        }

        private static void InsertionSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);

            for (int i = 0; i < keys.Length - 1; i++)
            {
                TKey t = keys[i + 1];
                TValue tValue = values[i + 1];

                int j = i;
                while (j >= 0 && comparer!.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    values[j + 1] = values[j];
                    j--;
                }

                keys[j + 1] = t;
                values[j + 1] = tValue;
            }
        }

		/// <summary>
		/// Returns the integer (floor) log of the specified value, base 2.
		/// Note that by convention, input value 0 returns 0 since log(0) is undefined.
		/// </summary>
		/// <param name="value">The value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			int result = 0;
			if ((value & 0xFFFF0000) != 0) { value >>= 16; result += 16; }
			if ((value & 0x0000FF00) != 0) { value >>=  8; result +=  8; }
			if ((value & 0x000000F0) != 0) { value >>=  4; result +=  4; }
			if ((value & 0x0000000C) != 0) { value >>=  2; result +=  2; }
			if ((value & 0x00000002) != 0) { value >>=  1; result +=  1; }
            return result;
		}
	}
#endif

	internal sealed partial class ArraySortHelper<T>
	{
		public void Sort(Span<T> items, IComparer<T>? comparer)
		{
			// Add a try block here to detect IComparers (or their
			// underlying IComparables, etc) that are bogus.
			try
			{
				IntrospectiveSort(items, comparer ?? Comparer<T>.Default);
			}
			catch (IndexOutOfRangeException)
			{
				throw new ArgumentException("Bad comparer");
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Comparer failed", e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SwapIfGreater(Span<T> items, IComparer<T> comparer, int i, int j)
		{
			Debug.Assert(comparer != null);
			Debug.Assert(0 <= i && i < items.Length);
			Debug.Assert(0 <= j && j < items.Length);
			Debug.Assert(i != j);

			if (comparer!.Compare(items[i], items[j]) > 0)
			{
				T item = items[i];
				items[i] = items[j];
				items[j] = item;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Swap(Span<T> items, int i, int j)
		{
			Debug.Assert(i != j);

			T item = items[i];
			items[i] = items[j];
			items[j] = item;
		}

		internal static void IntrospectiveSort(Span<T> items, IComparer<T> comparer)
		{
			Debug.Assert(comparer != null);

			if (items.Length > 1)
			{
				IntroSort(items, 2 * (Log2((uint)items.Length) + 1), comparer!);
			}
		}

		private static void IntroSort(Span<T> items, int depthLimit, IComparer<T> comparer)
		{
			Debug.Assert(!items.IsEmpty);
			Debug.Assert(depthLimit >= 0);
			Debug.Assert(comparer != null);

			int partitionSize = items.Length;
			while (partitionSize > 1)
			{
				if (partitionSize <= 16)
				{

					if (partitionSize == 2)
					{
						SwapIfGreater(items, comparer!, 0, 1);
						return;
					}

					if (partitionSize == 3)
					{
						SwapIfGreater(items, comparer!, 0, 1);
						SwapIfGreater(items, comparer!, 0, 2);
						SwapIfGreater(items, comparer!, 1, 2);
						return;
					}

					InsertionSort(items.Slice(0, partitionSize), comparer!);
					return;
				}

				if (depthLimit == 0)
				{
					HeapSort(items.Slice(0, partitionSize), comparer!);
					return;
				}
				depthLimit--;

				int p = PickPivotAndPartition(items.Slice(0, partitionSize), comparer!);

				// Note we've already partitioned around the pivot and do not have to move the pivot again.
				IntroSort(items.Slice(p + 1, partitionSize - (p + 1)), depthLimit, comparer!);
				partitionSize = p;
			}
		}

		private static int PickPivotAndPartition(Span<T> keys, IComparer<T> comparer)
		{
			Debug.Assert(keys.Length >= 16);
			Debug.Assert(comparer != null);

			int hi = keys.Length - 1;

			// Compute median-of-three.  But also partition them, since we've done the comparison.
			int middle = hi >> 1;

			// Sort lo, mid and hi appropriately, then pick mid as the pivot.
			SwapIfGreater(keys, comparer!, 0, middle);  // swap the low with the mid point
			SwapIfGreater(keys, comparer!, 0, hi);   // swap the low with the high
			SwapIfGreater(keys, comparer!, middle, hi); // swap the middle with the high

			T pivot = keys[middle];
			Swap(keys, middle, hi - 1);
			int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

			while (left < right)
			{
				while (comparer!.Compare(keys[++left], pivot) < 0) ;
				while (comparer!.Compare(pivot, keys[--right]) < 0) ;

				if (left >= right)
					break;

				Swap(keys, left, right);
			}

			// Put pivot in the right location.
			if (left != hi - 1)
			{
				Swap(keys, left, hi - 1);
			}
			return left;
		}

		private static void HeapSort(Span<T> items, IComparer<T> comparer)
		{
			Debug.Assert(comparer != null);
			Debug.Assert(!items.IsEmpty);

			int n = items.Length;
			for (int i = n >> 1; i >= 1; i--)
			{
				DownHeap(items, i, n, comparer!);
			}

			for (int i = n; i > 1; i--)
			{
				Swap(items, 0, i - 1);
				DownHeap(items, 1, i - 1, comparer!);
			}
		}

		private static void DownHeap(Span<T> items, int i, int n, IComparer<T> comparer)
		{
			Debug.Assert(comparer != null);

			T d = items[i - 1];

			while (i <= n >> 1)
			{
				int child = 2 * i;
				if (child < n && comparer!.Compare(items[child - 1], items[child]) < 0)
				{
					child++;
				}

				if (!(comparer!.Compare(d, items[child - 1]) < 0))
					break;

				items[i - 1] = items[child - 1];
				i = child;
			}

			items[i - 1] = d;
		}

		private static void InsertionSort(Span<T> items, IComparer<T> comparer)
		{
			Debug.Assert(comparer != null);

			for (int i = 0; i < items.Length - 1; i++)
			{
				T t = items[i + 1];

				int j = i;
				while (j >= 0 && comparer!.Compare(t, items[j]) < 0)
				{
					items[j + 1] = items[j];
					j--;
				}

				items[j + 1] = t;
			}
		}

		/// <summary>
		/// Returns the integer (floor) log of the specified value, base 2.
		/// Note that by convention, input value 0 returns 0 since log(0) is undefined.
		/// </summary>
		/// <param name="value">The value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Log2(uint value)
		{
			int result = 0;
			if ((value & 0xFFFF0000) != 0) { value >>= 16; result += 16; }
			if ((value & 0x0000FF00) != 0) { value >>= 8; result += 8; }
			if ((value & 0x000000F0) != 0) { value >>= 4; result += 4; }
			if ((value & 0x0000000C) != 0) { value >>= 2; result += 2; }
			if ((value & 0x00000002) != 0) { value >>= 1; result += 1; }
			return result;
		}
	}
}
