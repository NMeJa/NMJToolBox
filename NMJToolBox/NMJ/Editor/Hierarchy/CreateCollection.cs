using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Hierarchy
{
	/// <summary>
	/// Groups the selected objects under a new empty parent — the "collection" Unity has never
	/// shipped — and the inverse operation that dissolves one again.
	///
	/// Works at any depth. The new parent is created as a sibling of the selection rather than at
	/// the scene root, so grouping three objects that live six levels deep keeps them exactly where
	/// they were instead of yanking them to the top of the hierarchy.
	/// </summary>
	public static class CreateCollection
	{
		private const string DefaultName = "Collection";

		[MenuItem("GameObject/NMJ/Create Collection %g", false, 0)]
		private static void CreateFromSelection()
		{
			var roots = TopMostSelection();
			if (roots.Count == 0) return;

			// Every selected object's shared parent, or null when they span different parents (in
			// which case the collection lands at the scene root, the only place that can hold them all).
			var parent = CommonParent(roots);

			// Insert where the topmost selected object currently sits so the hierarchy does not
			// reshuffle under the user.
			var siblingIndex = roots.Min(t => t.GetSiblingIndex());

			// A group of RectTransforms must itself be a RectTransform, or the UI layout collapses.
			var isUi = roots.All(t => t is RectTransform) && parent is RectTransform;

			var group = new GameObject(DefaultName);
			Undo.RegisterCreatedObjectUndo(group, "Create Collection");

			if (isUi) Undo.AddComponent<RectTransform>(group);

			var groupTransform = group.transform;
			Undo.SetTransformParent(groupTransform, parent, "Create Collection");

			if (isUi) StretchToParent((RectTransform)groupTransform);
			else groupTransform.position = BoundsCentre(roots);

			groupTransform.SetSiblingIndex(siblingIndex);

			// Sort by current sibling index so the children keep their relative order inside the
			// new parent rather than being reordered by selection order.
			foreach (var child in roots.OrderBy(t => t.GetSiblingIndex()))
				Undo.SetTransformParent(child, groupTransform, "Create Collection");

			Selection.activeGameObject = group;
			EditorGUIUtility.PingObject(group);

			// Drop straight into inline rename, so naming the collection is part of the same gesture.
			EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Rename");
		}

		[MenuItem("GameObject/NMJ/Create Collection %g", true)]
		private static bool ValidateCreate() => Selection.transforms is { Length: > 0 };

		/// <summary>
		/// Removes a collection: re-parents its children into its parent, at its position in the
		/// sibling order, then deletes the now-empty object.
		/// </summary>
		[MenuItem("GameObject/NMJ/Dissolve Collection %#g", false, 1)]
		private static void DissolveSelection()
		{
			var groups = TopMostSelection();
			if (groups.Count == 0) return;

			foreach (var group in groups)
			{
				var parent = group.parent;
				var insertAt = group.GetSiblingIndex();

				// Snapshot first: re-parenting mutates the child collection we would be iterating.
				var children = new List<Transform>(group.childCount);
				for (var i = 0; i < group.childCount; i++) children.Add(group.GetChild(i));

				foreach (var child in children)
				{
					Undo.SetTransformParent(child, parent, "Dissolve Collection");
					child.SetSiblingIndex(insertAt++);
				}

				Undo.DestroyObjectImmediate(group.gameObject);
			}
		}

		[MenuItem("GameObject/NMJ/Dissolve Collection %#g", true)]
		private static bool ValidateDissolve() =>
			Selection.transforms is { Length: > 0 } && Selection.transforms.Any(t => t.childCount > 0);

		/// <summary>
		/// The selection with any object that is a descendant of another selected object removed.
		/// Without this, selecting a parent and its child would re-parent the child twice and pull
		/// it out of the parent that is itself being moved.
		/// </summary>
		private static List<Transform> TopMostSelection()
		{
			var selected = Selection.transforms;
			if (selected == null || selected.Length == 0) return new List<Transform>();

			var set = new HashSet<Transform>(selected);
			return selected.Where(t => !HasSelectedAncestor(t, set)).ToList();
		}

		private static bool HasSelectedAncestor(Transform t, HashSet<Transform> selection)
		{
			for (var p = t.parent; p; p = p.parent)
				if (selection.Contains(p))
					return true;

			return false;
		}

		private static Transform CommonParent(IReadOnlyList<Transform> transforms)
		{
			var parent = transforms[0].parent;
			for (var i = 1; i < transforms.Count; i++)
				if (transforms[i].parent != parent)
					return null;

			return parent;
		}

		/// <summary>
		/// Center of the selection's renderer bounds, falling back to the average pivot when nothing
		/// selected has a renderer. Putting the pivot in the middle is what makes the collection
		/// usable as a rotation/scale handle afterward.
		/// </summary>
		private static Vector3 BoundsCentre(IReadOnlyList<Transform> transforms)
		{
			var hasBounds = false;
			var bounds = new Bounds();

			foreach (var t in transforms)
			{
				var renderers = t.GetComponentsInChildren<Renderer>();
				foreach (var renderer in renderers)
				{
					if (!hasBounds)
					{
						bounds = renderer.bounds;
						hasBounds = true;
						continue;
					}

					bounds.Encapsulate(renderer.bounds);
				}
			}

			if (hasBounds) return bounds.center;

			var sum = transforms.Aggregate(Vector3.zero, (current, t) => current + t.position);
			return sum / transforms.Count;
		}

		private static void StretchToParent(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.localScale = Vector3.one;
			rect.localRotation = Quaternion.identity;
		}
	}
}