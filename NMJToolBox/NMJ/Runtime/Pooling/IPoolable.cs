namespace NMJToolBox
{
	/// <summary>
	/// Optional interface for pooled prefabs that need to reset state between uses.
	///
	/// A pooled object is never destroyed and re-created, so <c>Awake</c> runs once for its whole
	/// lifetime and <c>OnEnable</c>/<c>OnDisable</c> run on every spawn/despawn. Implement this when
	/// <c>OnEnable</c> is the wrong place because you need to run <em>after</em> the pool has already
	/// positioned and parented the instance.
	///
	/// Implemented on any component of the prefab (including children); the pool searches the whole
	/// instance with <c>GetComponentsInChildren</c> once, at creation, and caches the result.
	/// </summary>
	public interface IPoolable
	{
		/// <summary>Called after the instance has been positioned, parented and activated.</summary>
		void OnSpawned();

		/// <summary>Called before the instance is deactivated and returned to its pool.</summary>
		void OnDespawned();
	}
}
