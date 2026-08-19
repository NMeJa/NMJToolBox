using System.Collections.Generic;
using UnityEngine;

namespace NMJToolBox
{
	public static class TransformExtensions
	{
		/// <summary>Every direct child, safe to iterate while re-parenting or destroying.</summary>
		public static List<Transform> Children(this Transform transform)
		{
			var children = new List<Transform>(transform.childCount);
			for (var i = 0; i < transform.childCount; i++) children.Add(transform.GetChild(i));
			return children;
		}

		/// <summary>
		/// Destroys every child. Uses <see cref="Object.DestroyImmediate"/> outside play mode, because
		/// <see cref="Object.Destroy"/> is deferred to the end of the frame and edit-mode tooling
		/// would see the children still attached.
		/// </summary>
		public static void DestroyChildren(this Transform transform)
		{
			for (var i = transform.childCount - 1; i >= 0; i--)
			{
				var child = transform.GetChild(i).gameObject;
				if (Application.isPlaying) Object.Destroy(child);
				else Object.DestroyImmediate(child);
			}
		}

		/// <summary>Zeroes local position and rotation and sets local scale to one.</summary>
		public static void ResetLocal(this Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

		public static Transform SetX(this Transform transform, float x)
		{
			var p = transform.position;
			transform.position = new Vector3(x, p.y, p.z);
			return transform;
		}

		public static Transform SetY(this Transform transform, float y)
		{
			var p = transform.position;
			transform.position = new Vector3(p.x, y, p.z);
			return transform;
		}

		public static Transform SetZ(this Transform transform, float z)
		{
			var p = transform.position;
			transform.position = new Vector3(p.x, p.y, z);
			return transform;
		}

		public static Transform Translate(this Transform transform, float x = 0f, float y = 0f, float z = 0f)
		{
			transform.position += new Vector3(x, y, z);
			return transform;
		}

		/// <summary>Direction from this transform to <paramref name="target"/>, normalised.</summary>
		public static Vector3 DirectionTo(this Transform transform, Transform target) =>
			(target.position - transform.position).normalized;

		public static Vector3 DirectionTo(this Transform transform, Vector3 target) =>
			(target - transform.position).normalized;

		/// <summary>
		/// Squared distance. Prefer this for comparisons — it skips the square root that
		/// <see cref="Vector3.Distance"/> pays for a number you then only compare.
		/// </summary>
		public static float SqrDistanceTo(this Transform transform, Transform target) =>
			(target.position - transform.position).sqrMagnitude;

		/// <summary>Slash-separated path from the scene root, matching what the Hierarchy shows.</summary>
		public static string GetPath(this Transform transform)
		{
			var path = transform.name;
			for (var p = transform.parent; p != null; p = p.parent) path = p.name + "/" + path;
			return path;
		}

		/// <summary>Walks up the parent chain looking for <typeparamref name="T"/>, skipping this transform.</summary>
		public static T GetComponentInAncestors<T>(this Transform transform) where T : Component
		{
			for (var p = transform.parent; p != null; p = p.parent)
				if (p.TryGetComponent(out T component))
					return component;

			return null;
		}

		/// <summary>Depth-first walk of this transform and everything below it.</summary>
		public static IEnumerable<Transform> Descendants(this Transform transform)
		{
			for (var i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				yield return child;

				foreach (var grandChild in child.Descendants()) yield return grandChild;
			}
		}
	}
}
