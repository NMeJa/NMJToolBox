using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class CollectionExtensions
	{
		public static bool IsEmpty<T>(this IEnumerable<T> source) => source is null || !source.Any();

		public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
		{
			return items.GroupBy(property).Select(x => x.First());
		}

		/// <summary>
		/// A shuffled copy, using a shared generator.
		/// </summary>
		/// <remarks>
		/// this changed: the old version constructed <c>new Random()</c> per call. Mono seeds a
		/// parameterless Random from the system clock, so two shuffles in the same frame - shuffling a
		/// deck and then a loot table, say - came out in lockstep. One shared instance also removes the
		/// per-call allocation. The algorithm is now a Fisher-Yates over the copy rather than repeated
		/// <c>RemoveAt</c> on a List, which was O(n^2) for no gain.
		/// </remarks>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> array, bool canBeNull = false) =>
			ShuffleInternal(array, SharedRandom, canBeNull, nameof(Shuffle));

		/// <summary>A shuffled copy that is reproducible for a given seed.</summary>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> array, int seed, bool canBeNull = false) =>
			ShuffleInternal(array, new Random(seed), canBeNull, nameof(Shuffle));

		/// <summary>
		/// One generator for every unseeded shuffle. Not thread-safe, which is correct here: Unity
		/// gameplay code runs on the main thread, and guarding it would cost every caller a lock.
		/// </summary>
		private static readonly Random SharedRandom = new();

		private static List<T> ShuffleInternal<T>(IEnumerable<T> source, Random random, bool canBeNull, string caller)
		{
			var items = source?.ToList();

			if (items == null || items.Count == 0)
			{
				if (canBeNull) return null;
				throw new ArgumentOutOfRangeException(nameof(source), $"{caller}: Collection is empty");
			}

			for (var i = items.Count - 1; i > 0; i--)
			{
				var j = random.Next(i + 1);
				(items[i], items[j]) = (items[j], items[i]);
			}

			return items;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			//System.Random randomGenerator = new System.Random();
			var n = list.Count;

			while (n > 1)
			{
				n--;
				var k = UnityEngine.Random.Range(0, n + 1);
				//int k = randomGenerator.Next(0, n + 1);
				(list[k], list[n]) = (list[n], list[k]);
			}
		}

		public static T GetRandomElement<T>(this IList<T> list)
		{
			return list[UnityEngine.Random.Range(0, list.Count)];
		}

		public static T GetRandomElement<T>(this T[] list)
		{
			return list[UnityEngine.Random.Range(0, list.Length)];
		}

		public static TVal GetRandomValue<TKey, TVal>(this IDictionary<TKey, TVal> dictionary)
		{
			return dictionary.Values.ElementAt(UnityEngine.Random.Range(0, dictionary.Values.Count));
		}

		public static TKey GetRandomKey<TKey, TVal>(this IDictionary<TKey, TVal> dictionary)
		{
			return dictionary.Keys.ElementAt(UnityEngine.Random.Range(0, dictionary.Keys.Count));
		}

		public static KeyValuePair<TKey, TVal> GetRandomKeyValuePair<TKey, TVal>(
			this IDictionary<TKey, TVal> dictionary)
		{
			var index = UnityEngine.Random.Range(0, dictionary.Count);
			return new KeyValuePair<TKey, TVal>(dictionary.Keys.ElementAt(index), dictionary.Values.ElementAt(index));
		}

		public static T RandomElement<T>(this IEnumerable<T> array)
		{
			var originalList = array.ToList();
#if UNITY_EDITOR
			// ReSharper disable once ConvertIfStatementToSwitchStatement
			if (originalList.Count <= 0) throw new ArgumentOutOfRangeException(nameof(array), "RandomElement: Collection is Empty");
#endif
			if (originalList.Count == 1)
				return originalList[0];
			var element = originalList[UnityEngine.Random.Range(0, originalList.Count)];
			return element;
		}

		public static T Pop<T>(this List<T> list)
		{
			if (list.Count <= 0) throw new ArgumentNullException(nameof(list), "Pop: List is empty");
			var lastElementIndex = list.Count - 1;
			var r = list[lastElementIndex];
			list.RemoveAt(lastElementIndex);
			return r;
		}

		public static T PopAt<T>(this List<T> list, int index)
		{
			if (list.Count <= 0) throw new ArgumentNullException(nameof(list), "PopAt: List is empty");
			if (list.Count <= index) throw new ArgumentOutOfRangeException(nameof(list), index, "PopAt: Index is outside of List element Count");
			var r = list[index];
			list.RemoveAt(index);
			return r;
		}

		public static T Dequeue<T>(this List<T> list)
		{
			if (list.Count <= 0) throw new ArgumentNullException(nameof(list), "Dequeue: List is empty");
			var first = list[0]; // this changed: was named firstElementIndex but holds the element
			list.RemoveAt(0);
			return first;
		}

		/// <summary>
		/// Indexes an array with wraparound, so an ever-increasing counter keeps cycling.
		/// </summary>
		// this changed: dropped the `where T : IConvertible` constraint, which excluded every
		// reference type for no reason - the method never converts anything. The index now wraps
		// properly with a modulo instead of a single `if`: before, index 7 into a 3-element array
		// returned element 0 rather than element 1, and a negative index threw.
		public static T Loop<T>(this T[] element, int index)
		{
			if (element == null || element.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(element), "Loop: array is null or empty");

			var wrapped = index % element.Length;
			if (wrapped < 0) wrapped += element.Length;
			return element[wrapped];
		}

		public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize)
		{
			for (var i = 0; i < chunkSize && queue.Count > 0; i++) yield return queue.Dequeue();
		}
	}
}