using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NMJToolBox.SceneTools
{
	/// <summary>
	/// A compact scene tree docked inside the Scene view, so the full Hierarchy window can stay
	/// closed while you work maximised on the viewport.
	///
	/// Enable it from the Scene view's overlay menu (the ⋮ button, or press <c>`</c>) →
	/// <b>Mini Hierarchy</b>.
	/// </summary>
	[Overlay(typeof(SceneView), OverlayId, "Mini Hierarchy")]
	public class MiniHierarchyOverlay : Overlay
	{
		private const string OverlayId = "nmj-mini-hierarchy";
		private const float RowHeight = 18f;
		private const int MaxRows = 5000;

		private readonly struct Row
		{
			public readonly Transform Target;
			public readonly int Depth;
			public readonly bool HasChildren;

			public Row(Transform target, int depth, bool hasChildren)
			{
				Target = target;
				Depth = depth;
				HasChildren = hasChildren;
			}
		}

		private readonly List<Row> rows = new();
		private readonly HashSet<int> collapsed = new();

		private ListView list;
		private ToolbarSearchField search;
		private Label footer;
		private string filter = string.Empty;
		private bool truncated;

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement
			{
				style = { width = 260, minWidth = 180, minHeight = 120, flexGrow = 1 }
			};

			search = new ToolbarSearchField { style = { marginBottom = 2, flexShrink = 0 } };
			search.RegisterValueChangedCallback(evt =>
				{
					filter = evt.newValue ?? string.Empty;
					Rebuild();
				});
			root.Add(search);

			list = new ListView
			{
				fixedItemHeight = RowHeight,
				selectionType = SelectionType.Single,
				style = { flexGrow = 1, minHeight = 0 },
				makeItem = MakeRow,
				bindItem = BindRow,
				itemsSource = rows
			};

			list.selectionChanged += _ =>
				{
					var index = list.selectedIndex;
					if (index < 0 || index >= rows.Count) return;
					var target = rows[index].Target;
					if (target != null) Selection.activeGameObject = target.gameObject;
				};

			// Double-click frames the object, matching the Hierarchy window's behaviour.
			list.RegisterCallback<PointerDownEvent>(evt =>
				{
					if (evt.clickCount < 2) return;
					var view = SceneView.lastActiveSceneView;
					if (view != null) view.FrameSelected();
				});

			root.Add(list);

			footer = new Label { style = { fontSize = 10, opacity = 0.6f, flexShrink = 0, marginTop = 2 } };
			root.Add(footer);

			Rebuild();
			return root;
		}

		public override void OnCreated()
		{
			EditorApplication.hierarchyChanged += Rebuild;
			Selection.selectionChanged += SyncSelection;
			PrefabStage.prefabStageOpened += OnPrefabStage;
			PrefabStage.prefabStageClosing += OnPrefabStage;
		}

		public override void OnWillBeDestroyed()
		{
			EditorApplication.hierarchyChanged -= Rebuild;
			Selection.selectionChanged -= SyncSelection;
			PrefabStage.prefabStageOpened -= OnPrefabStage;
			PrefabStage.prefabStageClosing -= OnPrefabStage;
		}

		private void OnPrefabStage(PrefabStage _) => Rebuild();

		private static VisualElement MakeRow()
		{
			var row = new VisualElement
			{
				style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, height = RowHeight }
			};

			var indent = new VisualElement { name = "indent", style = { flexShrink = 0 } };
			row.Add(indent);

			var foldout = new Label { name = "foldout", style = { width = 12, flexShrink = 0, fontSize = 9, opacity = 0.7f } };
			row.Add(foldout);

			var icon = new Image { name = "icon", style = { width = 14, height = 14, flexShrink = 0, marginRight = 3 } };
			row.Add(icon);

			var label = new Label { name = "label", style = { flexGrow = 1, overflow = Overflow.Hidden, fontSize = 11 } };
			row.Add(label);

			return row;
		}

		private void BindRow(VisualElement element, int index)
		{
			if (index < 0 || index >= rows.Count) return;

			var row = rows[index];
			var target = row.Target;

			var indent = element.Q<VisualElement>("indent");
			var foldout = element.Q<Label>("foldout");
			var icon = element.Q<Image>("icon");
			var label = element.Q<Label>("label");

			// Searching produces a flat result list, so indentation would be misleading.
			indent.style.width = string.IsNullOrEmpty(filter) ? row.Depth * 12 : 0;

			if (target == null)
			{
				label.text = "<destroyed>";
				foldout.text = string.Empty;
				icon.image = null;
				return;
			}

			var isCollapsed = collapsed.Contains(target.gameObject.GetInstanceID());
			foldout.text = row.HasChildren && string.IsNullOrEmpty(filter) ? (isCollapsed ? "▶" : "▼") : string.Empty;

			// Clicking the arrow must not also select the row, so the toggle is bound to the arrow
			// element itself and stops the event before the ListView sees it.
			foldout.userData = target;
			if (foldout.userData is Transform)
			{
				foldout.UnregisterCallback<PointerDownEvent>(ToggleFold);
				foldout.RegisterCallback<PointerDownEvent>(ToggleFold);
			}

			icon.image = AssetPreview.GetMiniThumbnail(target.gameObject);

			label.text = target.name;
			label.style.opacity = target.gameObject.activeInHierarchy ? 1f : 0.45f;
		}

		private void ToggleFold(PointerDownEvent evt)
		{
			if (evt.currentTarget is not Label { userData: Transform target }) return;

			var id = target.gameObject.GetInstanceID();
			if (!collapsed.Add(id)) collapsed.Remove(id);

			evt.StopPropagation();
			Rebuild();
		}

		private void Rebuild()
		{
			rows.Clear();
			truncated = false;

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				// In prefab isolation mode the scene roots are irrelevant — show what's being edited.
				Walk(prefabStage.prefabContentsRoot.transform, 0);
			}
			else
			{
				for (var i = 0; i < SceneManager.sceneCount; i++)
				{
					var scene = SceneManager.GetSceneAt(i);
					if (!scene.isLoaded) continue;

					foreach (var rootObject in scene.GetRootGameObjects())
						Walk(rootObject.transform, 0);
				}
			}

			if (list != null)
			{
				list.itemsSource = rows;
				list.Rebuild();
			}

			if (footer != null)
			{
				footer.text = truncated
								  ? $"{rows.Count}+ objects (list truncated)"
								  : $"{rows.Count} object{(rows.Count == 1 ? "" : "s")}";
			}

			SyncSelection();
		}

		private void Walk(Transform t, int depth)
		{
			if (rows.Count >= MaxRows)
			{
				truncated = true;
				return;
			}

			var hasChildren = t.childCount > 0;
			var matches = string.IsNullOrEmpty(filter) ||
						  t.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;

			if (matches) rows.Add(new Row(t, depth, hasChildren));

			// While filtering, descend through everything so a deep match is still found even if its
			// ancestors are collapsed or non-matching.
			var descend = !string.IsNullOrEmpty(filter) || !collapsed.Contains(t.gameObject.GetInstanceID());
			if (!hasChildren || !descend) return;

			for (var i = 0; i < t.childCount; i++)
				Walk(t.GetChild(i), depth + 1);
		}

		private void SyncSelection()
		{
			if (list == null) return;

			var active = Selection.activeGameObject;
			if (active == null)
			{
				list.SetSelectionWithoutNotify(System.Array.Empty<int>());
				return;
			}

			for (var i = 0; i < rows.Count; i++)
			{
				if (rows[i].Target == null || rows[i].Target.gameObject != active) continue;
				list.SetSelectionWithoutNotify(new[] { i });
				list.ScrollToItem(i);
				return;
			}

			list.SetSelectionWithoutNotify(System.Array.Empty<int>());
		}
	}
}
