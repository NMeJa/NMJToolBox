using System;
using System.Collections.Generic;
using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// A <see cref="Dictionary{TKey,TValue}"/> Unity can serialize.
	///
	/// Unity's serializer has never supported <see cref="Dictionary{TKey,TValue}"/> — marking one
	/// <c>[SerializeField]</c> does nothing at all, silently, and the field resets on every domain
	/// reload. The standard workaround is two parallel <see cref="List{T}"/>s rebuilt by hand in
	/// <c>OnEnable</c>; this type does exactly that, once, behind a real dictionary API.
	///
	/// <code>
	/// [SerializeField] private SerializableDictionary&lt;string, int&gt; scores = new();
	/// </code>
	///
	/// Unity 2020.1+ serializes generic fields directly, so no subclass is required. Declaring one
	/// anyway still works and gives the type a friendlier name in the inspector:
	/// <code>
	/// [Serializable] public class ScoreTable : SerializableDictionary&lt;string, int&gt; { }
	/// </code>
	/// </summary>
	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<TKey> keys = new();
		[SerializeField] private List<TValue> values = new();

		/// <summary>
		/// Keys that appeared more than once in the serialized lists. Nothing enforces uniqueness
		/// while the inspector is being edited — a freshly added row starts as a duplicate of
		/// <c>default</c> — so instead of throwing, the duplicates are recorded here and the drawer
		/// paints them red. Only the first occurrence of a duplicate key survives into the dictionary.
		/// </summary>
		public IReadOnlyList<int> DuplicateKeyIndices => duplicateKeyIndices;

		[NonSerialized] private readonly List<int> duplicateKeyIndices = new();

		public SerializableDictionary() { }

		public SerializableDictionary(IDictionary<TKey, TValue> source) : base(source) { }

		public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

		public void OnBeforeSerialize()
		{
			// Rebuilding both lists from scratch keeps them exactly in step. Trying to patch them
			// incrementally is where parallel-list implementations usually go wrong.
			keys.Clear();
			values.Clear();

			foreach (var pair in this)
			{
				keys.Add(pair.Key);
				values.Add(pair.Value);
			}
		}

		public void OnAfterDeserialize()
		{
			Clear();
			duplicateKeyIndices.Clear();

			var count = Math.Min(keys.Count, values.Count);
			for (var i = 0; i < count; i++)
			{
				var key = keys[i];

				// A null key would throw from the indexer; skip it and let the drawer flag the row.
				if (key == null)
				{
					duplicateKeyIndices.Add(i);
					continue;
				}

				if (ContainsKey(key))
				{
					duplicateKeyIndices.Add(i);
					continue;
				}

				Add(key, values[i]);
			}
		}

		/// <summary>
		/// Reads the serialized lists directly, without going through the dictionary. Used by the
		/// property drawer, which has to render duplicate and null keys that the dictionary itself
		/// cannot hold.
		/// </summary>
		public int SerializedCount => Math.Min(keys.Count, values.Count);
	}
}
