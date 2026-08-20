using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace NMJTools.Debugging
{
	/// <summary>
	/// Shapes <see cref="Gizmos"/> does not provide. Call these from <c>OnDrawGizmos</c> /
	/// <c>OnDrawGizmosSelected</c>; they respect <see cref="Gizmos.color"/> and
	/// <see cref="Gizmos.matrix"/> like the built-ins.
	///
	/// <see cref="Gizmos"/> gives you cube, sphere, ray, line, frustum, and icon — which leaves out
	/// the shapes tools actually need most: direction arrows, capsules matching a
	/// <see cref="CapsuleCollider"/>, arcs for cones of vision, and text.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class GizmoUtils
	{
		/// <summary>Line with an arrowhead, for directions, forces, and links between objects.</summary>
		/// <param name="from">The starting point of the arrow shaft.</param>
		/// <param name="to">The target point where the arrowhead points.</param>
		/// <param name="headLength">
		/// The length of the head measured back along the shaft. Clamped to half the arrow's length, so a
		/// head can never overshoot <paramref name="from"/> and render pointing the wrong way.
		/// </param>
		/// <param name="headAngle">Half-angle of the head in degrees.</param>
		public static void DrawArrow(Vector3 from, Vector3 to, float headLength = 0.25f, float headAngle = 22f)
		{
			Gizmos.DrawLine(from, to);

			var direction = to - from;
			var distance = direction.magnitude;
			if (distance < 1e-4f) return;

			// The magnitude is already known, so divide by it instead of calling Normalize and paying
			// for a second square root.
			direction /= distance;

			var length = Mathf.Min(headLength, distance * 0.5f);

			// tan diverges at 90 degrees and turns negative beyond it, which would fold the head
			// inside out; clamping keeps a nonsense angle merely ugly rather than wrong.
			var spread = Mathf.Tan(Mathf.Clamp(headAngle, 0.1f, 89f) * Mathf.Deg2Rad) * length;

			// Any vector not parallel to the direction works as the reference for the head's plane;
			// up is the natural choice, with right as the fallback for a vertical arrow.
			var reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
			var side = Vector3.Cross(direction, reference).normalized;

			// direction and side are perpendicular unit vectors, so their cross-product is unit length
			// by construction — normalizing it would be a square root per-draw call for nothing.
			var headUp = Vector3.Cross(direction, side);

			var basePoint = to - direction * length;

			var right = basePoint + side * spread;
			var left = basePoint - side * spread;
			var top = basePoint + headUp * spread;
			var bottom = basePoint - headUp * spread;

			Gizmos.DrawLine(to, right);
			Gizmos.DrawLine(to, left);
			Gizmos.DrawLine(to, top);
			Gizmos.DrawLine(to, bottom);

			// Closing the base into a diamond makes the head read as a solid pyramid from glancing
			// angles, where four loose fins collapse into an ambiguous cross.
			Gizmos.DrawLine(right, top);
			Gizmos.DrawLine(top, left);
			Gizmos.DrawLine(left, bottom);
			Gizmos.DrawLine(bottom, right);
		}

		/// <summary>
		/// Arrow from an origin along a direction, mirroring <see cref="UnityEngine.Gizmos.DrawRay(Vector3,Vector3)"/>.
		/// </summary>
		/// <remarks>
		/// Deliberately not an overload of <see cref="DrawArrow"/>. Both would begin
		/// (Vector3, Vector3, float), so a three-argument call could not be told apart: the compiler
		/// has nothing to choose on but identical parameter types, and a reader has nothing either.
		/// <c>DrawArrow(a, b, 2f)</c> looks like origin/direction/length and would resolve as
		/// from/to/headLength — a silently wrong picture rather than an error. A separate name makes
		/// the call site say which one it means.
		/// </remarks>
		public static void DrawArrowRay(Vector3 origin, Vector3 direction, float length,
										float headLength = 0.25f, float headAngle = 22f) =>
			DrawArrow(origin, origin + direction.normalized * length, headLength, headAngle);

		/// <summary>Circle on an arbitrary plane. <paramref name="normal"/> is the plane's axis.</summary>
		public static void DrawCircle(Vector3 centre, Vector3 normal, float radius, int segments = 32)
		{
			if (segments < 3) segments = 3;

			var axis = normal.sqrMagnitude < 1e-8f ? Vector3.up : normal.normalized;
			var reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
			var right = Vector3.Cross(axis, reference).normalized * radius;

			// axis is unit and right is perpendicular to it with magnitude radius, so this cross-
			// product already has magnitude radius — normalizing and rescaling it would be a wasted
			// square root.
			var forward = Vector3.Cross(axis, right);

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

		/// <summary> Arc starting at <paramref name="from"/> and bending toward <paramref name="towards"/>. </summary>
		/// <param name="centre">The center point of the arc in 3D space.</param>
		/// <param name="towards">
		/// The direction the arc bulges into — <em>not</em> the rotation axis. For a hemisphere cap,
		/// pass the capsule's up vector and the radial direction to start from; the rotation axis is
		/// derived from the two.
		/// </param>
		/// <param name="from">The initial radial direction pointing outward from the center.</param>
		/// <param name="degrees">The total angular sweep of the arc in degrees.</param>
		/// <param name="radius">The radius of the arc.</param>
		/// <param name="segments">The number of linear subdivisions used to approximate the arc curve.</param>
		public static void DrawArc(Vector3 centre, Vector3 towards, Vector3 from, float degrees, float radius, int segments = 16)
		{
			if (segments < 1) segments = 1;

			// Loop-invariant: the rotation axis depends only on the two direction arguments, so
			// computing it per segment cost a cross-product and a square root on every iteration.
			var rotationAxis = Vector3.Cross(towards, from);
			if (rotationAxis.sqrMagnitude < 1e-8f) return; // parallel inputs describe no arc
			rotationAxis.Normalize();

			var start = from.normalized * radius;
			var previous = centre + start;

			for (var i = 1; i <= segments; i++)
			{
				var rotation = Quaternion.AngleAxis(degrees * i / segments, rotationAxis);
				var next = centre + rotation * start;
				Gizmos.DrawLine(previous, next);
				previous = next;
			}
		}

		/// <summary>Cone of vision — the shape you want for FOV, spotlights, and detection ranges.</summary>
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
		/// Text label in the scene. Editor-only — <c>Handles</c> do not exist in a player build, so
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