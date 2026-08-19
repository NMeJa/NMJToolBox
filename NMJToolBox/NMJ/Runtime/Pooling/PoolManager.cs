using System.Collections.Generic;
using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Scene component that owns a list of <see cref="PrefabPool"/>s configured entirely from the
	/// inspector. Drop it on an empty GameObject, add prefabs to the list, and pooling works — no
	/// code required to set it up, and none required to use it if you drive spawning through
	/// <see cref="PoolSpawner"/> and <see cref="ReturnToPool"/>.
	///
	/// The static API (<see cref="Spawn(string, Vector3, Quaternion, Transform)"/> /
	/// <see cref="Despawn(GameObject)"/>) is there for code that wants it, and is a drop-in
	/// replacement for <c>Instantiate</c>/<c>Destroy</c>.
	/// </summary>
	[DefaultExecutionOrder(-50)] // pools must exist before anything tries to spawn in Start
	[AddComponentMenu("NMJ/Pooling/Pool Manager")]
	public class PoolManager : MonoBehaviour
	{
		[Tooltip("Survive scene loads. Turn this off for pools that only make sense in one scene.")]
		[SerializeField] private bool dontDestroyOnLoad;

		[Tooltip("Spawning an unregistered prefab creates a pool for it on the fly instead of failing.")]
		[SerializeField] private bool autoCreateMissingPools = true;

		[SerializeField] private List<PrefabPool> pools = new();

		private readonly Dictionary<string, PrefabPool> poolsById = new();
		private readonly Dictionary<GameObject, PrefabPool> poolsByPrefab = new();

		private static PoolManager active;

		/// <summary>
		/// The manager the static API routes through. Returns null rather than creating one, so a
		/// missing manager is a loud, findable error instead of a silent second pool hierarchy.
		/// </summary>
		public static PoolManager Active => active;

		public IReadOnlyList<PrefabPool> Pools => pools;

		private void Awake()
		{
			if (active != null && active != this)
			{
				Debug.LogWarning($"[NMJ] A PoolManager is already active ('{active.name}'). " +
								 $"'{name}' will register its pools into it instead of replacing it.", this);
				foreach (var pool in pools) active.Register(pool);
				return;
			}

			active = this;
			if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

			foreach (var pool in pools) Register(pool);
		}

		private void OnDestroy()
		{
			if (active == this) active = null;
		}

		private void Register(PrefabPool pool)
		{
			if (pool?.Prefab == null)
			{
				Debug.LogError($"[NMJ] PoolManager '{name}' has a pool entry with no prefab; skipped.", this);
				return;
			}

			if (poolsById.ContainsKey(pool.Id))
			{
				Debug.LogError($"[NMJ] Duplicate pool id '{pool.Id}' on PoolManager '{name}'. " +
							   "Ids must be unique; the later entry was skipped.", this);
				return;
			}

			var root = new GameObject($"[Pool] {pool.Id}").transform;
			root.SetParent(transform, false);
			root.gameObject.SetActive(false); // parking inactive instances under an inactive root is cheaper

			pool.Initialise(root);

			poolsById[pool.Id] = pool;
			poolsByPrefab[pool.Prefab] = pool;
		}

		/// <summary>Finds a pool by its inspector id, or null.</summary>
		public PrefabPool GetPool(string id) => poolsById.GetValueOrDefault(id);

		/// <summary>Finds the pool serving a prefab, optionally creating one on demand.</summary>
		public PrefabPool GetPool(GameObject prefab)
		{
			if (prefab == null) return null;
			if (poolsByPrefab.TryGetValue(prefab, out var existing)) return existing;
			if (!autoCreateMissingPools) return null;

			var created = new PrefabPool();
			created.ConfigureRuntime(prefab);
			Register(created);
			pools.Add(created);
			return poolsByPrefab.GetValueOrDefault(prefab);
		}

		// ========== Static convenience API ==========

		public static GameObject Spawn(string poolId, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
		{
			if (!EnsureActive()) return null;
			var pool = active.GetPool(poolId);
			if (pool != null) return pool.Spawn(position, Norm(rotation), parent);

			Debug.LogError($"[NMJ] No pool named '{poolId}' on PoolManager '{active.name}'.", active);
			return null;
		}

		public static GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
		{
			if (!EnsureActive()) return null;
			var pool = active.GetPool(prefab);
			if (pool != null) return pool.Spawn(position, Norm(rotation), parent);

			Debug.LogError($"[NMJ] No pool for prefab '{(prefab ? prefab.name : "null")}' and " +
						   "auto-create is disabled on the active PoolManager.", active);
			return null;
		}

		/// <summary>
		/// Returns a pooled instance. Falls back to <c>Destroy</c> for objects that were never
		/// pooled, so this is safe to use everywhere as a blanket replacement for <c>Destroy</c>.
		/// </summary>
		public static void Despawn(GameObject instance)
		{
			if (instance == null) return;

			var marker = instance.GetComponent<PooledInstance>();
			if (marker == null || marker.Owner == null)
			{
				Destroy(instance);
				return;
			}

			marker.Owner.Despawn(instance);
		}

		private static bool EnsureActive()
		{
			if (active != null) return true;
			Debug.LogError("[NMJ] No active PoolManager in the scene. Add one via " +
						   "GameObject > NMJ > Pool Manager before spawning.");
			return false;
		}

		// A default(Quaternion) is all-zeroes, which is not a valid rotation. Optional parameters
		// can't default to Quaternion.identity (not a compile-time constant), so normalise here.
		private static Quaternion Norm(Quaternion rotation) =>
			rotation is { x: 0f, y: 0f, z: 0f, w: 0f } ? Quaternion.identity : rotation;
	}
}
