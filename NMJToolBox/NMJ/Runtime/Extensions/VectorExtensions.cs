using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class VectorExtensions
	{
		/// <summary>
		/// Copy with selected components replaced: <c>pos.With(y: 0f)</c>. Optional nullable
		/// arguments make the call site say exactly which axes change and leave the rest alone —
		/// far harder to misread than reconstructing the whole vector by hand.
		/// </summary>
		public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null) =>
			new(x ?? v.x, y ?? v.y, z ?? v.z);

		public static Vector2 With(this Vector2 v, float? x = null, float? y = null) =>
			new(x ?? v.x, y ?? v.y);

		/// <summary>Copy with components added: <c>pos.Add(y: 1f)</c>.</summary>
		public static Vector3 Add(this Vector3 v, float x = 0f, float y = 0f, float z = 0f) =>
			new(v.x + x, v.y + y, v.z + z);

		public static Vector2 Add(this Vector2 v, float x = 0f, float y = 0f) => new(v.x + x, v.y + y);

		/// <summary>Drops the Y component — the usual first step of any ground-plane calculation.</summary>
		public static Vector3 Flatten(this Vector3 v) => new(v.x, 0f, v.z);

		/// <summary>Flattened and normalised, or <see cref="Vector3.zero"/> if it had no horizontal part.</summary>
		public static Vector3 FlatDirection(this Vector3 v)
		{
			var flat = v.Flatten();
			return flat.sqrMagnitude < 1e-8f ? Vector3.zero : flat.normalized;
		}

		/// <summary>XZ plane as a Vector2 — the right projection for a top-down map or navmesh maths.</summary>
		public static Vector2 ToXZ(this Vector3 v) => new(v.x, v.z);

		/// <summary>Lifts a 2D XZ vector back into 3D at the given height.</summary>
		public static Vector3 ToXZ(this Vector2 v, float y = 0f) => new(v.x, y, v.y);

		public static Vector3 Multiply(this Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

		public static Vector3 Divide(this Vector3 a, Vector3 b) =>
			new(b.x == 0f ? 0f : a.x / b.x, b.y == 0f ? 0f : a.y / b.y, b.z == 0f ? 0f : a.z / b.z);

		/// <summary>Rotates a 2D vector by <paramref name="degrees"/> counter-clockwise.</summary>
		public static Vector2 Rotate(this Vector2 v, float degrees)
		{
			var radians = degrees * Mathf.Deg2Rad;
			var sin = Mathf.Sin(radians);
			var cos = Mathf.Cos(radians);
			return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
		}

		/// <summary>
		/// True when the two points are closer than <paramref name="distance"/>. Compares squared
		/// magnitudes, so no square root is taken.
		/// </summary>
		public static bool IsCloserThan(this Vector3 a, Vector3 b, float distance) =>
			(a - b).sqrMagnitude < distance * distance;

		/// <summary>Component-wise absolute value.</summary>
		public static Vector3 Abs(this Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

		/// <summary>Rounds each component to the nearest multiple of <paramref name="gridSize"/>.</summary>
		public static Vector3 SnapToGrid(this Vector3 v, float gridSize)
		{
			if (gridSize <= 0f) return v;
			return new Vector3(
							   Mathf.Round(v.x / gridSize) * gridSize,
							   Mathf.Round(v.y / gridSize) * gridSize,
							   Mathf.Round(v.z / gridSize) * gridSize);
		}

		/// <summary>A random point inside the bounds, useful for scatter and spawn logic.</summary>
		public static Vector3 RandomPointInside(this Bounds bounds) =>
			new(Random.Range(bounds.min.x, bounds.max.x),
				Random.Range(bounds.min.y, bounds.max.y),
				Random.Range(bounds.min.z, bounds.max.z));
	}
}