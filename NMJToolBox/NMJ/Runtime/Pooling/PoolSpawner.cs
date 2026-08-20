using UnityEngine;
using UnityEngine.Events;

namespace NMJTools.Pooling
{
	/// <summary>
	/// Spawns from a pool without any code. Every entry point is a public no-argument (or single
	/// argument) method, so it can be driven from a UnityEvent, a UI Button, an Animation Event, or
	/// a Timeline signal.
	/// </summary>
	[AddComponentMenu("NMJ/Pooling/Pool Spawner")]
	public class PoolSpawner : MonoBehaviour
	{
		private enum SpawnAt
		{
			ThisTransform,
			SpawnPoint,
			WorldOrigin
		}

		[Header("What")]

		[Tooltip("Pool id to spawn from. Leave blank to use the prefab field instead.")]
		[SerializeField] private string poolId = string.Empty;

		[Tooltip("Used when Pool Id is blank. The PoolManager will create a pool for it on demand " +
				 "if auto-create is enabled.")]
		[SerializeField] private GameObject prefab;

		[Header("Where")]

		[SerializeField] private SpawnAt spawnAt = SpawnAt.ThisTransform;
		[SerializeField] private Transform spawnPoint;

		[Tooltip("Offset applied in the spawn transform's local space.")]
		[SerializeField] private Vector3 offset;

		[Tooltip("Random offset added on top, per axis (±half of each value).")]
		[SerializeField] private Vector3 randomSpread;

		[Tooltip("Parent the spawned instance under the spawn transform. Leave off for projectiles, " +
				 "impact effects and anything that should keep moving independently.")]
		[SerializeField] private bool parentToSpawnPoint;

		[Header("When")]

		[SerializeField] private bool spawnOnEnable;

		[Tooltip("0 = off. Repeats every N seconds while this component is enabled.")]
		[SerializeField, Min(0f)] private float repeatInterval;

		[Header("Events")]

		[SerializeField] private UnityEvent<GameObject> onSpawned;

		private float nextSpawnTime;

		private Transform Origin => spawnAt switch
		{
			SpawnAt.SpawnPoint  => spawnPoint ? spawnPoint : transform,
			SpawnAt.WorldOrigin => null,
			_                   => transform
		};

		private void OnEnable()
		{
			nextSpawnTime = Time.time + repeatInterval;
			if (spawnOnEnable) Spawn();
		}

		private void Update()
		{
			if (repeatInterval <= 0f) return;
			if (Time.time < nextSpawnTime) return;
			nextSpawnTime = Time.time + repeatInterval;
			Spawn();
		}

		/// <summary>Spawns one instance. Wire this straight into a UnityEvent.</summary>
		public void Spawn()
		{
			var origin = Origin;

			var position = origin ? origin.TransformPoint(offset) : offset;
			var rotation = origin ? origin.rotation : Quaternion.identity;

			if (randomSpread != Vector3.zero)
			{
				position += new Vector3(
										Random.Range(-randomSpread.x, randomSpread.x) * 0.5f,
										Random.Range(-randomSpread.y, randomSpread.y) * 0.5f,
										Random.Range(-randomSpread.z, randomSpread.z) * 0.5f);
			}

			var parent = parentToSpawnPoint ? origin : null;

			var instance = string.IsNullOrWhiteSpace(poolId)
							   ? PoolManager.Spawn(prefab, position, rotation, parent)
							   : PoolManager.Spawn(poolId, position, rotation, parent);

			if (instance) onSpawned?.Invoke(instance);
		}

		/// <summary>Spawns <paramref name="count"/> instances in one call.</summary>
		public void SpawnMany(int count)
		{
			for (var i = 0; i < count; i++) Spawn();
		}

		private void OnDrawGizmosSelected()
		{
			var origin = Origin;
			var position = origin ? origin.TransformPoint(offset) : offset;

			Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.9f);
			Gizmos.DrawWireSphere(position, 0.15f);

			if (randomSpread == Vector3.zero) return;
			Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.25f);
			Gizmos.DrawWireCube(position, randomSpread);
		}
	}
}