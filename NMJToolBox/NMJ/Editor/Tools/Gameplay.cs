using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace NMJTools.Tools
{
	/// <summary>
	/// Small selection-level commands that have no better home.
	/// </summary>
	public static class GameplayTools
	{
		/// <summary>
		/// Resets the selected transforms to identity.
		/// </summary>
		/// <remarks>
		/// this changed: the menu item used to carry the shortcut <c>%_R</c> — Ctrl+R — which is
		/// Unity's own Assets ▸ Refresh. Unity resolves that collision by silently disabling *both*,
		/// so the old binding cost the refresh shortcut and did not work itself. It is now registered
		/// through the shortcut system with no default binding; assign one in Edit ▸ Shortcuts
		/// (search "NMJ") if you want it, exactly like the batch-rename and selection-history commands
		/// in this package.
		/// </remarks>
		[MenuItem("Tools/NMJ/Reset Transform", false, 21)]
		[Shortcut("NMJ/Reset Transform")]
		public static void ResetTransform()
		{
			var selected = Selection.transforms;
			if (selected == null || selected.Length == 0) return;

			var group = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Reset Transform");

			foreach (var element in selected)
			{
				if (!element) continue;

				Undo.RecordObject(element, "Reset Transform");

				if (element is RectTransform rect)
				{
					// A RectTransform's anchors and pivot are part of its transform for every practical
					// purpose - resetting position while leaving a stretched anchor behind moves the
					// element somewhere nobody asked for.
					var half = Vector2.one * 0.5f;
					rect.pivot = half;
					rect.anchorMin = half;
					rect.anchorMax = half;
					rect.sizeDelta = Vector2.one * 100f;
				}

				element.localPosition = Vector3.zero;
				element.localRotation = Quaternion.identity;
				element.localScale = Vector3.one;

				// this changed: without this the scene was never marked dirty, so a reset done and
				// then not followed by another edit was silently dropped on scene close.
				EditorUtility.SetDirty(element);
			}

			Undo.CollapseUndoOperations(group);
		}

		[MenuItem("Tools/NMJ/Reset Transform", true)]
		private static bool ValidateResetTransform() => Selection.transforms is { Length: > 0 };
	}
}
