using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMJToolBox
{
	/// <summary>
	/// Awaitable wrappers around the pool and <see cref="SceneChanger"/>.
	///
	/// This whole assembly only compiles when <c>NMJ_UNITASK</c> is set, which
	/// <see cref="Setup.PackageDetector"/> does automatically while UniTask is in the project. The
	/// core NMJ ToolBox assembly never references UniTask, so the package still works without it.
	/// </summary>
	public static class UniTaskPoolExtensions
	{
		/// <summary>
		/// Returns an instance to its pool after a delay, without a per-instance <c>Update</c>.
		///
		/// <see cref="ReturnToPool"/> polls a timer in <c>Update</c>, which is fine for a handful of
		/// objects and wasteful for hundreds: every live instance pays a managed call per frame just
		/// to compare two floats. A UniTask delay is driven by one shared PlayerLoop hook instead, so
		/// the per-object cost is zero.
		/// </summary>
		public static async UniTask DespawnAfterAsync(this GameObject instance, float seconds,
													 bool unscaledTime = false, CancellationToken token = default)
		{
			if (instance == null) return;

			// Tie the delay to the instance so a despawn (or a scene unload) cancels it instead of
			// firing later against a recycled object that now belongs to someone else.
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(
																			   token, instance.GetCancellationTokenOnDestroy());

			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(seconds), unscaledTime, PlayerLoopTiming.Update, linked.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (instance != null) PoolManager.Despawn(instance);
		}

		/// <summary>Spawns, waits, then returns — a one-shot effect in a single await.</summary>
		public static async UniTask SpawnForAsync(string poolId, Vector3 position, Quaternion rotation,
												  float seconds, CancellationToken token = default)
		{
			var instance = PoolManager.Spawn(poolId, position, rotation);
			if (instance == null) return;

			await instance.DespawnAfterAsync(seconds, false, token);
		}
	}

	/// <summary>Awaitable scene loading for <see cref="SceneChanger"/>.</summary>
	public static class SceneChangerAsyncExtensions
	{
		public static UniTask LoadAsync(this SceneChanger changer, IProgress<float> progress = null,
										CancellationToken token = default) =>
			SceneManager.LoadSceneAsync(changer.ScenePath, LoadSceneMode.Single)
						.ToUniTask(progress, cancellationToken: token);

		public static UniTask LoadAdditiveAsync(this SceneChanger changer, IProgress<float> progress = null,
												CancellationToken token = default) =>
			SceneManager.LoadSceneAsync(changer.ScenePath, LoadSceneMode.Additive)
						.ToUniTask(progress, cancellationToken: token);

		public static UniTask UnloadAsync(this SceneChanger changer, CancellationToken token = default) =>
			SceneManager.UnloadSceneAsync(changer.ScenePath).ToUniTask(cancellationToken: token);
	}

	/// <summary>
	/// Drop-in replacement for <see cref="ReturnToPool"/> with no <c>Update</c> at all.
	///
	/// Prefer this over <see cref="ReturnToPool"/> whenever UniTask is available — same inspector
	/// fields, none of the per-frame cost.
	/// </summary>
	[AddComponentMenu("NMJ/Pooling/Return To Pool (UniTask)")]
	public class UniTaskReturnToPool : MonoBehaviour
	{
		[Tooltip("Seconds before this instance returns to its pool.")]
		[SerializeField, Min(0f)] private float delay = 2f;

		[Tooltip("Ignore Time.timeScale, so effects still expire while paused or in slow motion.")]
		[SerializeField] private bool unscaledTime;

		private CancellationTokenSource cts;

		private void OnEnable()
		{
			if (delay <= 0f) return;

			cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
			WaitThenReturn(cts.Token).Forget();
		}

		private void OnDisable()
		{
			// Cancelling on disable is what makes this pool-safe: the object is deactivated on
			// despawn, so without this the old timer would still be pending when it is spawned again.
			cts?.Cancel();
			cts?.Dispose();
			cts = null;
		}

		private async UniTaskVoid WaitThenReturn(CancellationToken token)
		{
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay), unscaledTime, PlayerLoopTiming.Update, token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (this != null && gameObject.activeSelf) PoolManager.Despawn(gameObject);
		}

		/// <summary>Returns immediately. Wire into a UnityEvent.</summary>
		public void Return() => PoolManager.Despawn(gameObject);
	}
}
