using System;
using UnityEngine;
using UnityEngine.Pool;

namespace NMJToolBox
{
	/// <summary>
	/// One inspector-configurable prefab pool. A thin, Unity-lifecycle-aware wrapper around
	/// <see cref="ObjectPool{T}"/> (built into Unity since 2021.1) — this type deliberately does not
	/// reimplement pooling, it only adds the parts <see cref="ObjectPool{T}"/> leaves to the caller:
	/// instantiation, activation ordering, parenting, and the <see cref="IPoolable"/> callbacks.
	///
	/// Serialized as a plain field, so a list of these on <see cref="PoolManager"/> is fully
	/// configurable from the inspector without writing any code.
	/// </summary>
	[Serializable]
	public class PrefabPool
	{
		[Tooltip("Name used to look this pool up. Leave empty to use the prefab's name.")]
		[SerializeField] private string id = string.Empty;

		[SerializeField] private GameObject prefab;

		[Tooltip("Instances created up-front, when the PoolManager starts.")]
		[SerializeField, Min(0)] private int prewarm;

		[Tooltip("Instances kept when returned. Anything beyond this is destroyed instead of pooled.")]
		[SerializeField, Min(1)] private int maxSize = 100;

		[Tooltip("Throw if an instance is returned twice. Costs a little performance; worth keeping " +
				 "on until the project ships.")]
		[SerializeField] private bool collectionCheck = true;

		private ObjectPool<GameObject> pool;
		private Transform root;
		private bool initialised;

		/// <summary>Lookup name. Falls back to the prefab's name when the id field is blank.</summary>
		public string Id => string.IsNullOrWhiteSpace(id) ? (prefab ? prefab.name : "<no prefab>") : id;

		public GameObject Prefab => prefab;

		/// <summary>Instances currently spawned in the world.</summary>
		public int CountActive => initialised ? pool.CountActive : 0;

		/// <summary>Instances resting in the pool, ready to be reused.</summary>
		public int CountInactive => initialised ? pool.CountInactive : 0;

		/// <summary>Every instance this pool has created and still owns.</summary>
		public int CountAll => initialised ? pool.CountAll : 0;

		public int MaxSize => maxSize;

		/// <summary>
		/// Configures a pool created at runtime rather than in the inspector. Used by
		/// <see cref="PoolManager"/>'s auto-create path; call it before <see cref="Initialise"/>.
		/// </summary>
		public void ConfigureRuntime(GameObject poolPrefab, int prewarmCount = 0, int max = 100)
		{
			if (initialised)
			{
				Debug.LogError($"[NMJ] PrefabPool '{Id}' is already initialised and cannot be reconfigured.");
				return;
			}

			prefab = poolPrefab;
			id = poolPrefab ? poolPrefab.name : string.Empty;
			prewarm = Mathf.Max(0, prewarmCount);
			maxSize = Mathf.Max(1, max);
		}

		/// <summary>
		/// Builds the underlying <see cref="ObjectPool{T}"/> and pre-warms it. Safe to call twice —
		/// the second call is a no-op — because a pool may be initialised either by
		/// <see cref="PoolManager"/> at startup or lazily by the first spawn.
		/// </summary>
		/// <param name="parent">Transform that inactive instances are parked under.</param>
		public void Initialise(Transform parent)
		{
			if (initialised) return;
			if (prefab == null)
			{
				Debug.LogError($"[NMJ] PrefabPool '{id}' has no prefab assigned and was skipped.");
				return;
			}

			root = parent;

			pool = new ObjectPool<GameObject>(
											  Create,
											  actionOnGet: null, // activation is deliberately deferred, see Spawn
											  OnReturned,
											  OnDestroyInstance,
											  collectionCheck,
											  Mathf.Max(1, prewarm),
											  maxSize);

			initialised = true;

			if (prewarm <= 0) return;

			// Get() then Release() is the only way to seed ObjectPool<T>; there is no Fill API.
			var warm = new GameObject[prewarm];
			for (var i = 0; i < prewarm; i++) warm[i] = pool.Get();
			for (var i = 0; i < prewarm; i++) pool.Release(warm[i]);
		}

		/// <summary>
		/// Takes an instance from the pool, places it, then activates it.
		///
		/// Order matters: <see cref="ObjectPool{T}"/> would run its actionOnGet before the caller can
		/// position anything, so an instance activated there would fire <c>OnEnable</c> at whatever
		/// transform the previous user left it at. Activating last means <c>OnEnable</c>, particle
		/// systems and physics all see the correct position on their very first frame.
		/// </summary>
		public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent)
		{
			if (!initialised) Initialise(root);
			if (!initialised) return null;

			var instance = pool.Get();
			if (instance == null) return null;

			var t = instance.transform;
			t.SetParent(parent, false);
			t.SetPositionAndRotation(position, rotation);

			instance.SetActive(true);

			var marker = instance.GetComponent<PooledInstance>();
			if (marker != null)
			{
				marker.IsSpawned = true;
				var poolables = marker.Poolables;
				for (var i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();
			}

			return instance;
		}

		/// <summary>Returns an instance this pool created. Ignores instances it does not own.</summary>
		public void Despawn(GameObject instance)
		{
			if (!initialised || instance == null) return;

			var marker = instance.GetComponent<PooledInstance>();
			if (marker == null || marker.Owner != this)
			{
				Debug.LogWarning($"[NMJ] '{instance.name}' was not created by pool '{Id}' and was not returned.", instance);
				return;
			}

			// Guard against double-despawn even when collectionCheck is off, since a duplicate
			// release would otherwise corrupt CountActive and hand the same instance out twice.
			if (!marker.IsSpawned) return;
			marker.IsSpawned = false;

			var poolables = marker.Poolables;
			for (var i = 0; i < poolables.Length; i++) poolables[i].OnDespawned();

			pool.Release(instance);
		}

		/// <summary>Destroys every pooled instance and resets the pool.</summary>
		public void Clear()
		{
			if (!initialised) return;
			pool.Clear();
		}

		private GameObject Create()
		{
			var instance = UnityEngine.Object.Instantiate(prefab, root);
			instance.name = prefab.name;
			instance.SetActive(false);

			var marker = instance.GetComponent<PooledInstance>();
			if (marker == null) marker = instance.AddComponent<PooledInstance>();
			marker.Bind(this);

			return instance;
		}

		private void OnReturned(GameObject instance)
		{
			if (instance == null) return;
			instance.SetActive(false);
			instance.transform.SetParent(root, false);
		}

		private static void OnDestroyInstance(GameObject instance)
		{
			if (instance != null) UnityEngine.Object.Destroy(instance);
		}
	}
}
