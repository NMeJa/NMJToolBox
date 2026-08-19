using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Put this on a pooled prefab to send it home automatically — the despawn half of a no-code
	/// pooling setup. Covers the common cases (a timed effect, a projectile, a decal) without
	/// anyone writing a coroutine.
	///
	/// The timer restarts on every spawn because <c>OnEnable</c> runs each time the pool hands the
	/// instance out, which is exactly the behaviour a pooled object needs and the reason a plain
	/// <c>Destroy(go, t)</c> in <c>Start</c> does not work once an object is pooled.
	/// </summary>
	[AddComponentMenu("NMJ/Pooling/Return To Pool")]
	public class ReturnToPool : MonoBehaviour
	{
		[Tooltip("Seconds before this instance returns to its pool. 0 = never (call Return() yourself).")]
		[SerializeField, Min(0f)] private float delay = 2f;

		[Tooltip("Ignore Time.timeScale, so effects still expire while the game is paused or in slow motion.")]
		[SerializeField] private bool unscaledTime;

		[Tooltip("Also return when every ParticleSystem on this instance has finished. Use with " +
				 "delay 0 for one-shot effects of varying length.")]
		[SerializeField] private bool whenParticlesFinished;

		private float expiry;
		private ParticleSystem[] particles;
		private bool waiting;

		private void Awake()
		{
			if (whenParticlesFinished) particles = GetComponentsInChildren<ParticleSystem>(true);
		}

		private void OnEnable()
		{
			waiting = delay > 0f || whenParticlesFinished;
			expiry = Now + delay;
		}

		private void Update()
		{
			if (!waiting) return;

			if (delay > 0f && Now >= expiry)
			{
				Return();
				return;
			}

			if (!whenParticlesFinished || particles == null || particles.Length == 0) return;

			for (var i = 0; i < particles.Length; i++)
				if (particles[i] != null && particles[i].IsAlive(true))
					return;

			Return();
		}

		/// <summary>Returns this instance to its pool now. Wire this into a UnityEvent.</summary>
		public void Return()
		{
			waiting = false;
			PoolManager.Despawn(gameObject);
		}

		private float Now => unscaledTime ? Time.unscaledTime : Time.time;
	}
}
