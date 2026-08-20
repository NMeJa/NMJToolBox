using LitMotion;
using LitMotion.Extensions;
using NMJTools.Pooling;
using UnityEngine;

namespace NMJTools.Integrations
{
	/// <summary>
	/// Tweened spawn and despawn for pooled objects.
	///
	/// Only compiles when <c>NMJ_LITMOTION</c> is set, which <c>PackageDetector</c> does
	/// while LitMotion is in the project. The core assembly never references LitMotion.
	/// </summary>
	[AddComponentMenu("NMJ/Pooling/Motion Return To Pool")]
	public class MotionReturnToPool : MonoBehaviour
	{
		private enum Transition
		{
			// ReSharper disable once UnusedMember.Local
			ScaleOut,
			ScaleInThenOut,
			None
		}

		[Header("Timing")]

		[Tooltip("Seconds the instance stays fully visible before the exit transition starts.")]
		[SerializeField, Min(0f)] private float lifetime = 1.5f;

		[Header("Transitions")]

		[SerializeField] private Transition transition = Transition.ScaleInThenOut;
		[SerializeField, Min(0f)] private float inDuration = 0.15f;
		[SerializeField, Min(0f)] private float outDuration = 0.2f;
		[SerializeField] private Ease inEase = Ease.OutBack;
		[SerializeField] private Ease outEase = Ease.InBack;

		private Vector3 baseScale;
		private bool captured;
		private MotionHandle handle;
		private float despawnAt;
		private bool waiting;

		private void Awake()
		{
			// Captured once, before any tween has touched the transform. Reading it on enabling would
			// eventually capture a mid-tween scale from a previous use and shrink the object a little
			// more on every spawn.
			baseScale = transform.localScale;
			captured = true;
		}

		private void OnEnable()
		{
			if (!captured) baseScale = transform.localScale;

			transform.localScale = baseScale;
			waiting = lifetime > 0f;
			despawnAt = Time.time + lifetime;

			if (transition != Transition.ScaleInThenOut || inDuration <= 0f) return;

			transform.localScale = Vector3.zero;
			handle = LMotion.Create(Vector3.zero, baseScale, inDuration)
							.WithEase(inEase)
							.BindToLocalScale(transform);
		}

		private void OnDisable()
		{
			// A live handle pointing at a recycled transform would keep animating the next user of
			// this instance.
			if (handle.IsActive()) handle.Cancel();
			waiting = false;
			transform.localScale = baseScale;
		}

		private void Update()
		{
			if (!waiting || Time.time < despawnAt) return;
			waiting = false;
			Despawn();
		}

		/// <summary>Plays the exit transition, then returns to the pool. Wire into a UnityEvent.</summary>
		public void Despawn()
		{
			waiting = false;

			if (transition == Transition.None || outDuration <= 0f)
			{
				PoolManager.Despawn(gameObject);
				return;
			}

			if (handle.IsActive()) handle.Cancel();

			handle = LMotion.Create(transform.localScale, Vector3.zero, outDuration)
							.WithEase(outEase)
							.WithOnComplete(() =>
								{
									if (this) PoolManager.Despawn(gameObject);
								})
							.BindToLocalScale(transform);
		}
	}

	/// <summary>Spawn helpers that add a motion in one call.</summary>
	// ReSharper disable once UnusedType.Global
	public static class PoolMotionExtensions
	{
		/// <summary>
		/// Spawns from a pool and pops it in from a zero scale. Returns the instance so the caller can
		/// keep configuring it.
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public static GameObject SpawnWithPop(string poolId, Vector3 position, Quaternion rotation,
											  float duration = 0.15f, Ease ease = Ease.OutBack)
		{
			var instance = PoolManager.Spawn(poolId, position, rotation);
			if (!instance) return null;

			var target = instance.transform;
			var scale = target.localScale;

			target.localScale = Vector3.zero;
			LMotion.Create(Vector3.zero, scale, duration).WithEase(ease).BindToLocalScale(target);

			return instance;
		}
	}
}