using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class GameObjectExtensions
	{
		/// <summary>
		/// Returns the component if present, otherwise adds and returns it. Replaces the
		/// <c>TryGetComponent</c>-then-<c>AddComponent</c> pair that shows up in nearly every setup
		/// routine.
		/// </summary>
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component =>
			gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();

		public static T GetOrAddComponent<T>(this Component component) where T : Component =>
			component.gameObject.GetOrAddComponent<T>();

		public static bool HasComponent<T>(this GameObject gameObject) where T : Component =>
			gameObject.TryGetComponent<T>(out _);

		/// <summary>
		/// Sets the layer on this object and everything beneath it. The plain <c>layer</c> setter only
		/// affects the single object, which is almost never what you want.
		/// </summary>
		public static void SetLayerRecursively(this GameObject gameObject, int layer)
		{
			gameObject.layer = layer;
			foreach (var child in gameObject.transform.Descendants()) child.gameObject.layer = layer;
		}

		/// <inheritdoc cref="SetLayerRecursively(GameObject,int)"/>
		public static void SetLayerRecursively(this GameObject gameObject, string layerName)
		{
			var layer = LayerMask.NameToLayer(layerName);
			if (layer < 0)
			{
				Debug.LogError($"[NMJ] Layer '{layerName}' does not exist.", gameObject);
				return;
			}

			gameObject.SetLayerRecursively(layer);
		}

		/// <summary>True when this object's layer is in <paramref name="mask"/>.</summary>
		public static bool IsInLayerMask(this GameObject gameObject, LayerMask mask) =>
			(mask.value & (1 << gameObject.layer)) != 0;

		/// <summary>Destroys the object, choosing the right call for play mode or the editor.</summary>
		public static void SafeDestroy(this GameObject gameObject)
		{
			if (!gameObject) return;
			if (Application.isPlaying) Object.Destroy(gameObject);
			else Object.DestroyImmediate(gameObject);
		}

		/// <summary>
		/// Fluent <c>SetActive</c>, so a spawn expression can stay a single statement:
		/// <c>Instantiate(prefab).SetActive(false)</c> does not compile, this does.
		/// </summary>
		public static GameObject WithActive(this GameObject gameObject, bool active)
		{
			gameObject.SetActive(active);
			return gameObject;
		}
	}
}