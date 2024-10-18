using System;
using System.Collections.Generic;
using System.Linq;

namespace NMJToolBox
{
    public static class CollectionExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> source) => source is null || !source.Any();

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> array, bool canBeNull = false)
        {
            List<T> originalList = array.ToList();
            if (originalList.Count <= 0 && canBeNull) return null;
            if (originalList.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(array), "Shuffle: Collection is Empty");
            if (originalList.Count == 1)
                return originalList;
            List<T> shuffledList = new List<T>();
            var rand = new Random();
            for (var i = originalList.Count - 1; i >= 0; i--)
            {
                var randomArrayIndex = rand.Next(i + 1);
                shuffledList.Add(originalList[randomArrayIndex]);
                originalList.RemoveAt(randomArrayIndex);
            }

            array = shuffledList;
            return array;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> array, int seed, bool canBeNull = false)
        {
            List<T> originalList = array.ToList();
            if (originalList.Count <= 0 && canBeNull) return null;
            if (originalList.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(array), "Shuffled seed: Collection is Empty");
            if (originalList.Count == 1)
                return originalList;
            List<T> shuffledList = new List<T>();
            var rand = new Random(seed);
            for (var i = originalList.Count - 1; i >= 0; i--)
            {
                var randomArrayIndex = rand.Next(i + 1);
                shuffledList.Add(originalList[randomArrayIndex]);
                originalList.RemoveAt(randomArrayIndex);
            }

            array = shuffledList;
            return array;
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
            List<T> originalList = array.ToList();
#if UNITY_EDITOR
            if (originalList.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(array), "RandomElement: Collection is Empty");
#endif
            if (originalList.Count == 1)
                return originalList[0];
            var element = originalList[UnityEngine.Random.Range(0, originalList.Count)];
            return element;
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list.Count <= 0)
                throw new ArgumentNullException(nameof(list),
                                                "Pop: List is empty");
            var lastElementIndex = list.Count - 1;
            var r = list[lastElementIndex];
            list.RemoveAt(lastElementIndex);
            return r;
        }

        public static T PopAt<T>(this List<T> list, int index)
        {
            if (list.Count <= 0)
                throw new ArgumentNullException(nameof(list),
                                                "PopAt: List is empty");
            if (list.Count <= index)
                throw new ArgumentOutOfRangeException(nameof(list), index,
                                                      "PopAt: Index is outside of List element Count");
            var r = list[index];
            list.RemoveAt(index);
            return r;
        }

        public static T Dequeue<T>(this List<T> list)
        {
            if (list.Count <= 0)
                throw new ArgumentNullException(nameof(list),
                                                "Dequeue: List is empty");
            var firstElementIndex = list[0];
            list.RemoveAt(0);
            return firstElementIndex;
        }

        public static T Loop<T>(this T[] element, int index) where T : IConvertible
        {
            if (index >= element.Length)
                index = 0;
            return element[index];
        }

        public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize)
        {
            for (var i = 0; i < chunkSize && queue.Count > 0; i++) yield return queue.Dequeue();
        }
    }
}
