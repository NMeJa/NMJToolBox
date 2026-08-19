using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJToolBox.SceneTools
{
	/// <summary>
	/// Solo/hide controls for the Scene view.
	///
	/// Drives Unity's own <see cref="SceneVisibilityManager"/> rather than toggling
	/// <c>GameObject.SetActive</c>. That distinction matters: scene visibility is a pure editor
	/// concern that never touches the serialized scene, so nothing here can dirty a scene, break a
	/// prefab override, or leak into a build. Deactivating objects to "hide" them does all three.
	///
	/// Enable from the Scene view's ⋮ menu → <b>Isolate</b>.
	/// </summary>
	[Overlay(typeof(SceneView), OverlayId, "Isolate")]
	public class IsolateOverlay : Overlay
	{
		private const string OverlayId = "nmj-isolate";

		private Label status;
		private Button isolateButton;
		private Button exitButton;
		private Button hideButton;
		private Button showAllButton;

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement { style = { width = 190, minWidth = 150 } };

			isolateButton = new Button(IsolateSelection)
			{
				text = "Isolate Selection",
				tooltip = "Show only the selected objects"
			};
			root.Add(isolateButton);

			exitButton = new Button(ExitIsolation)
			{
				text = "Exit Isolation",
				tooltip = "Return to showing everything"
			};
			root.Add(exitButton);

			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2 } };

			hideButton = new Button(HideSelection)
			{
				text = "Hide",
				tooltip = "Hide the selected objects and their children",
				style = { flexGrow = 1 }
			};
			row.Add(hideButton);

			showAllButton = new Button(ShowAll)
			{
				text = "Show All",
				tooltip = "Unhide everything in the open scenes",
				style = { flexGrow = 1 }
			};
			row.Add(showAllButton);

			root.Add(row);

			status = new Label { style = { fontSize = 10, opacity = 0.6f, marginTop = 4, whiteSpace = WhiteSpace.Normal } };
			root.Add(status);

			Refresh();
			return root;
		}

		public override void OnCreated()
		{
			Selection.selectionChanged += Refresh;
			SceneVisibilityManager.visibilityChanged += Refresh;
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= Refresh;
			SceneVisibilityManager.visibilityChanged -= Refresh;
		}

		private static void IsolateSelection()
		{
			var selection = Selection.gameObjects;
			if (selection == null || selection.Length == 0) return;

			// includeDescendants: isolating a parent without its children would show an empty shell.
			SceneVisibilityManager.instance.Isolate(selection, true);
		}

		private static void ExitIsolation() => SceneVisibilityManager.instance.ExitIsolation();

		private static void HideSelection()
		{
			var selection = Selection.gameObjects;
			if (selection == null || selection.Length == 0) return;
			SceneVisibilityManager.instance.Hide(selection, true);
		}

		private static void ShowAll() => SceneVisibilityManager.instance.ShowAll();

		private void Refresh()
		{
			var hasSelection = Selection.gameObjects is { Length: > 0 };
			var isolated = SceneVisibilityManager.instance.IsCurrentStageIsolated();

			isolateButton?.SetEnabled(hasSelection);
			hideButton?.SetEnabled(hasSelection);
			exitButton?.SetEnabled(isolated);

			if (status == null) return;

			status.text = isolated
							  ? "Isolation active — only isolated objects are shown."
							  : hasSelection
								  ? $"{Selection.gameObjects.Length} object(s) selected."
								  : "Select objects to isolate or hide.";
		}
	}
}
