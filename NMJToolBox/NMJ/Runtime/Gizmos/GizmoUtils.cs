using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Shapes <see cref="Gizmos"/> does not provide. Call these from <c>OnDrawGizmos</c> /
	/// <c>OnDrawGizmosSelected</c>; they respect <see cref="Gizmos.color"/> and
	/// <see cref="Gizmos.matrix"/> like the built-ins.
	///
	/// <see cref="Gizmos"/> gives you cube, sphere, ray, line, frustum and icon — which leaves out
	/// the shapes tools actually need most: direction arrows, capsules matching a
	/// <see cref="CapsuleCollider"/>, arcs for cones of vision, and text.
	/// </summary>
	public static class GizmoUtils
	{
		/// <summary>Line with an arrowhead, for directions, forces and links between objects.</summary>
		public static void DrawArrow(Vector3 from, Vector3 to, float headLength = 0.25f, float headAngle = 22f)
		{
			Gizmos.DrawLine(from, to);

			var direction = to - from;
			if (direction.sqrMagnitude < 1e-8f) return;

			direction.Normalize();

			// Any vector not parallel to the direction works as the reference for the head's plane;
			// up is the natural choice, with right as the fallback for a vertical arrow.
			var reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
			var side = Vector3.Cross(direction, reference).normalized;

			var back = -direction * headLength;
			var spread = Mathf.Tan(headAngle * Mathf.Deg2Rad) * headLength;

			Gizmos.DrawLine(to, to + back + side * spread);
			Gizmos.DrawLine(to, to + back - side * spread);
			Gizmos.DrawLine(to, to + back + Vector3.Cross(direction, side).normalized * spread);
			Gizmos.DrawLine(to, to + back - Vector3.Cross(direction, side).normalized * spread);
		}

		public static void DrawArrow(Vector3 origin, Vector3 direction, float length, float headLength = 0.25f) =>
			DrawArrow(origin, origin + direction.normalized * length, headLength);

		/// <summary>Circle on an arbitrary plane. <paramref name="normal"/> is the plane's axis.</summary>
		public static void DrawCircle(Vector3 centre, Vector3 normal, float radius, int segments = 32)
		{
			if (segments < 3) segments = 3;

			var axis = normal.sqrMagnitude < 1e-8f ? Vector3.up : normal.normalized;
			var reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
			var right = Vector3.Cross(axis, reference).normalized * radius;
			var forward = Vector3.Cross(axis, right).normalized * radius;

			var previous = centre + right;
			for (var i = 1; i <= segments; i++)
			{
				var angle = i / (float)segments * Mathf.PI * 2f;
				var next = centre + right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
				Gizmos.DrawLine(previous, next);
				previous = next;
			}
		}

		/// <summary>Three orthogonal circles — reads as a sphere but far cheaper than a wire sphere.</summary>
		public static void DrawWireSphere(Vector3 centre, float radius, int segments = 32)
		{
			DrawCircle(centre, Vector3.up, radius, segments);
			DrawCircle(centre, Vector3.right, radius, segments);
			DrawCircle(centre, Vector3.forward, radius, segments);
		}

		/// <summary>
		/// Capsule matching a <see cref="CapsuleCollider"/>'s shape, which <see cref="Gizmos"/> has no
		/// equivalent for — the usual stand-in is a cube that misleads about the real overlap.
		/// </summary>
		public static void DrawWireCapsule(Vector3 centre, Quaternion rotation, float radius, float height, int segments = 24)
		{
			var halfSpan = Mathf.Max(0f, height * 0.5f - radius);

			var up = rotation * Vector3.up;
			var right = rotation * Vector3.right;
			var forward = rotation * Vector3.forward;

			var top = centre + up * halfSpan;
			var bottom = centre - up * halfSpan;

			DrawCircle(top, up, radius, segments);
			DrawCircle(bottom, up, radius, segments);

			Gizmos.DrawLine(top + right * radius, bottom + right * radius);
			Gizmos.DrawLine(top - right * radius, bottom - right * radius);
			Gizmos.DrawLine(top + forward * radius, bottom + forward * radius);
			Gizmos.DrawLine(top - forward * radius, bottom - forward * radius);

			DrawArc(top, up, right, 180f, radius, segments / 2);
			DrawArc(top, up, forward, 180f, radius, segments / 2);
			DrawArc(bottom, -up, right, 180f, radius, segments / 2);
			DrawArc(bottom, -up, forward, 180f, radius, segments / 2);
		}

		/// <summary>Arc swept around <paramref name="axis"/>, starting from <paramref name="from"/>.</summary>
		public static void DrawArc(Vector3 centre, Vector3 axis, Vector3 from, float degrees, float radius, int segments = 16)
		{
			if (segments < 1) segments = 1;

			var start = from.normalized * radius;
			var previous = centre + start;

			for (var i = 1; i <= segments; i++)
			{
				var rotation = Quaternion.AngleAxis(degrees * i / segments, Vector3.Cross(axis, from).normalized);
				var next = centre + rotation * start;
				Gizmos.DrawLine(previous, next);
				previous = next;
			}
		}

		/// <summary>Cone of vision — the shape you want for FOV, spotlights and detection ranges.</summary>
		public static void DrawViewCone(Vector3 origin, Vector3 direction, float angleDegrees, float distance, int segments = 24)
		{
			if (direction.sqrMagnitude < 1e-8f) return;

			direction.Normalize();
			var half = angleDegrees * 0.5f;
			var up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
			var side = Vector3.Cross(direction, up).normalized;

			var left = Quaternion.AngleAxis(-half, side) * direction * distance;
			var right = Quaternion.AngleAxis(half, side) * direction * distance;

			Gizmos.DrawLine(origin, origin + left);
			Gizmos.DrawLine(origin, origin + right);

			var previous = origin + left;
			for (var i = 1; i <= segments; i++)
			{
				var angle = Mathf.Lerp(-half, half, i / (float)segments);
				var next = origin + Quaternion.AngleAxis(angle, side) * direction * distance;
				Gizmos.DrawLine(previous, next);
				previous = next;
			}
		}

		/// <summary>Three axis-aligned lines through a point — a precise position marker.</summary>
		public static void DrawCross(Vector3 position, float size = 0.25f)
		{
			var half = size * 0.5f;
			Gizmos.DrawLine(position - Vector3.right * half, position + Vector3.right * half);
			Gizmos.DrawLine(position - Vector3.up * half, position + Vector3.up * half);
			Gizmos.DrawLine(position - Vector3.forward * half, position + Vector3.forward * half);
		}

		public static void DrawBounds(Bounds bounds) => Gizmos.DrawWireCube(bounds.center, bounds.size);

		/// <summary>
		/// A polyline through the points, optionally closed. Handy for patrol routes and splines.
		/// </summary>
		public static void DrawPath(System.Collections.Generic.IReadOnlyList<Vector3> points, bool closed = false, float nodeSize = 0f)
		{
			if (points == null || points.Count < 2) return;

			for (var i = 0; i < points.Count - 1; i++) Gizmos.DrawLine(points[i], points[i + 1]);
			if (closed) Gizmos.DrawLine(points[^1], points[0]);

			if (nodeSize <= 0f) return;
			foreach (var point in points) Gizmos.DrawWireCube(point, Vector3.one * nodeSize);
		}

		/// <summary>
		/// Text label in the scene. Editor-only — <c>Handles</c> does not exist in a player build, so
		/// in a build this compiles to nothing rather than breaking the call site.
		/// </summary>
		public static void DrawLabel(Vector3 position, string text)
		{
#if UNITY_EDITOR
			UnityEditor.Handles.Label(position, text);
#endif
		}
	}
}
