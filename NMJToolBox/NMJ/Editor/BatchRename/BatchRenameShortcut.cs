using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace NMJTools.BatchRename
{
	/// <summary>
	/// Routes F2 to the right renamer: the batch window for a multi-selection, Unity's own inline
	/// rename for a single object.
	///
	/// Unity already binds F2 to "Main Menu/Edit/Rename", and the shortcut system disables both
	/// entries when two shortcuts claim one key. That conflict cannot be resolved from an attribute,
	/// so this class ships the binding <em>unassigned</em> and offers a one-click takeover that
	/// clears Unity's binding and installs ours. The takeover is lossless: for one or zero selected
	/// objects the handler simply invokes Edit/Rename, so plain F2 keeps behaving exactly as before.
	///
	/// Until it is enabled, Ctrl+Shift+R and the context menus reach the same window.
	/// </summary>
	internal static class BatchRenameShortcut
	{
		private const string ShortcutId = "NMJ/Batch Rename";
		private const string UnityRenameId = "Main Menu/Edit/Rename";
		private const string NoticeShownKey = "NMJ.BatchRename.F2NoticeShown";

		/// <summary>
		/// Registered with no default binding on purpose — see the class summary. Assign F2 here via
		/// <c>Tools ▸ NMJ ▸ Batch Rename ▸ Take Over F2</c>, or by hand in Edit ▸ Shortcuts.
		/// </summary>
		[Shortcut(ShortcutId)]
		private static void RenameShortcut()
		{
			if (Selection.objects is { Length: > 1 })
			{
				BatchRenameWindow.Open();
				return;
			}

			// Single or empty selection: hand straight back to Unity so F2 is unchanged.
			EditorApplication.ExecuteMenuItem("Edit/Rename");
		}

		[MenuItem("Tools/NMJ/Shortcuts/Batch Rename — Take Over F2", false, 100)]
		private static void TakeOverF2()
		{
			if (!EditorUtility.DisplayDialog(
											 "Bind F2 to Batch Rename",
											 "F2 will open Batch Rename when several objects are selected.\n\n" +
											 "With one object selected it still starts Unity's inline rename, so nothing is lost.\n\n" +
											 "This edits your Shortcuts profile and can be undone with " +
											 "Tools > NMJ > Shortcuts.",
											 "Bind F2", "Cancel"))
				return;

			try
			{
				ShortcutManager.instance.RebindShortcut(UnityRenameId, ShortcutBinding.empty);
				ShortcutManager.instance.RebindShortcut(ShortcutId, F2Binding());
				EditorPrefs.SetBool(NoticeShownKey, true);
				Debug.Log("[NMJ] F2 now opens Batch Rename for multi-selections.");
			}
			catch (System.Exception e)
			{
				Debug.LogError("[NMJ] Could not rebind F2 automatically: " + e.Message +
							   "\nUnity only allows shortcut overrides on a non-default profile. " +
							   "Create or select a custom profile in Edit > Shortcuts, then try again — " +
							   "or bind \"" + ShortcutId + "\" to F2 there by hand.");
			}
		}

		[MenuItem("Tools/NMJ/Shortcuts/Batch Rename — Restore Unity F2", false, 101)]
		private static void RestoreF2()
		{
			try
			{
				ShortcutManager.instance.ClearShortcutOverride(ShortcutId);
				ShortcutManager.instance.ClearShortcutOverride(UnityRenameId);
				EditorPrefs.SetBool(NoticeShownKey, false);
				Debug.Log("[NMJ] F2 restored to Unity's inline rename.");
			}
			catch (System.Exception e)
			{
				Debug.LogError("[NMJ] Could not restore F2 automatically: " + e.Message +
							   "\nReset the bindings by hand in Edit > Shortcuts.");
			}
		}

		private static ShortcutBinding F2Binding() => new(new KeyCombination(KeyCode.F2));
	}
}