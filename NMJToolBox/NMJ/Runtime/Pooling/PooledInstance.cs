using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Added automatically to every instance a <see cref="PrefabPool"/> creates. It is what lets
	/// <see cref="PoolManager.Despawn(GameObject)"/> work on a bare <see cref="GameObject"/> — the
	/// caller never has to remember which pool an instance came from.
	///
	/// Never add this by hand; an instance carrying one but owned by no pool cannot be returned.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")] // hidden from the Add Component menu on purpose
	public sealed class PooledInstance : MonoBehaviour
	{
		/// <summary>The pool that created this instance.</summary>
		public PrefabPool Owner { get; private set; }

		/// <summary>Cached <see cref="IPoolable"/> components, resolved once at creation.</summary>
		public IPoolable[] Poolables { get; private set; }

		/// <summary>True while the instance is out in the world rather than resting in its pool.</summary>
		public bool IsSpawned { get; internal set; }

		internal void Bind(PrefabPool owner)
		{
			Owner = owner;
			Poolables = GetComponentsInChildren<IPoolable>(true);
		}

		/// <summary>
		/// Returns this instance to its pool. Safe to wire directly to a UnityEvent, so a prefab can
		/// despawn itself from an Animation Event, a Button, or a collision callback with no code.
		/// </summary>
		public void Despawn()
		{
			PoolManager.Despawn(gameObject);
		}
	}
}
