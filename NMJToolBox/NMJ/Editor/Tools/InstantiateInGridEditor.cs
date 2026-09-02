using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Tools
{
	/// <summary>
	/// Build and clear buttons for <see cref="InstantiateInGrid"/>.
	///
	/// The edit-mode build lives here rather than on the component because the two things that make
	/// it usable are editor-only: <see cref="PrefabUtility.InstantiatePrefab"/>, which keeps the copies
	/// connected to their prefab, and <see cref="Undo"/>, so a 400-cell grid is one Ctrl+Z rather than
	/// 400. The old <c>[ContextMenu]</c> used plain <c>Instantiate</c> and got neither: it produced
	/// disconnected clones that could not be undone.
	/// </summary>
	[CustomEditor(typeof(InstantiateInGrid))]
	public class InstantiateInGridEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var grid = (InstantiateInGrid)target;

			EditorGUILayout.Space(6);

			if (!grid.Prefab)
			{
				EditorGUILayout.HelpBox("Assign a prefab to build a grid.", MessageType.Info);
				return;
			}

			var existing = FindGeneratedChildren(grid).Count;

			EditorGUILayout.LabelField(
									   $"{grid.Columns} × {grid.Rows} = {grid.CellCount} cell(s)" +
									   (existing > 0 ? $"   ·   {existing} already built" : string.Empty),
									   EditorStyles.miniLabel);

			// A grid big enough to lock the editor for a while is worth a warning rather than a
			// surprise: this runs synchronously on the main thread.
			if (grid.CellCount > 2000)
				EditorGUILayout.HelpBox($"{grid.CellCount} objects is a lot to instantiate at once — " +
										"the editor will be unresponsive while it runs.", MessageType.Warning);

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(existing == 0))
				{
					if (GUILayout.Button("Clear", GUILayout.Height(24))) Clear(grid);
				}

				if (GUILayout.Button("Build Grid", GUILayout.Height(24))) Build(grid);
			}
		}

		private static void Build(InstantiateInGrid grid)
		{
			var parent = grid.Parent;
			var group = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Build Grid");

			try
			{
				var isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(grid.Prefab);

				for (var column = 0; column < grid.Columns; column++)
				{
					for (var row = 0; row < grid.Rows; row++)
					{
						// InstantiatePrefab keeps the connection; only fall back for a plain scene
						// object used as a template, which cannot have one.
						var clone = isPrefabAsset
										? (GameObject)PrefabUtility.InstantiatePrefab(grid.Prefab, parent)
										: Instantiate(grid.Prefab, parent);

						if (!clone) continue;

						if (clone.transform.parent != parent) clone.transform.SetParent(parent, false);

						clone.name = grid.Prefab.name + InstantiateInGrid.CellSuffix(column, row);
						clone.transform.localPosition = grid.CellPosition(column, row);

						Undo.RegisterCreatedObjectUndo(clone, "Build Grid");
					}
				}
			}
			finally
			{
				Undo.CollapseUndoOperations(group);
			}

			Debug.Log($"[NMJ] Built a {grid.Columns} × {grid.Rows} grid of '{grid.Prefab.name}'.", grid);
		}

		private static void Clear(InstantiateInGrid grid)
		{
			var children = FindGeneratedChildren(grid);
			if (children.Count == 0) return;

			if (!EditorUtility.DisplayDialog("Clear Grid",
											 $"Delete {children.Count} object(s) this grid created?\n\n" +
											 "Only children whose name carries a [column,row] suffix are removed.",
											 "Delete", "Cancel"))
				return;

			var group = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Clear Grid");

			foreach (var child in children) Undo.DestroyObjectImmediate(child.gameObject);

			Undo.CollapseUndoOperations(group);
		}

		/// <summary>
		/// Children this component created, identified by the <c>[column,row]</c> suffix it stamps on
		/// every copy. Matching on the name rather than deleting every child is what stops Clear from
		/// removing things the user parented there by hand.
		/// </summary>
		private static List<Transform> FindGeneratedChildren(InstantiateInGrid grid)
		{
			if (!grid.Prefab) return new List<Transform>();

			var parent = grid.Parent;
			var prefix = grid.Prefab.name + " [";

			return Enumerable.Range(0, parent.childCount)
							 .Select(parent.GetChild)
							 .Where(child => child.name.StartsWith(prefix, System.StringComparison.Ordinal) &&
											 child.name.EndsWith("]", System.StringComparison.Ordinal))
							 .ToList();
		}
	}
}
