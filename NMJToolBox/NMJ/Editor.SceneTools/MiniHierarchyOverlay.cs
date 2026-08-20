using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NMJTools.SceneTools
{
	/// <summary>
	/// A compact scene tree docked inside the Scene view, so the full Hierarchy window can stay
	/// closed while you work maximized on the viewport.
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
			public Transform Target { get; }
			public int Depth { get; }
			public bool HasChildren { get; }

			public Row(Transform target, int depth, bool hasChildren)
			{
				Target = target;
				Depth = depth;
				HasChildren = hasChildren;
			}
		}

		private readonly List<Row> rows = new();

		/// <summary>
		/// Nodes the user has folded shut, keyed by the Transform itself.
		///
		/// Deliberately not keyed by instance id: <c>GetInstanceID</c> is obsolete on current Unity
		/// (superseded by <c>GetEntityId</c>), and switching to <c>GetEntityId</c> would raise this
		/// package's minimum Unity version for no benefit. A Unity object is a perfectly good
		/// dictionary key — its <c>Equals</c>/<c>GetHashCode</c> are well-defined — so the id was
		/// never buying anything here.
		///
		/// Named <c>collapsedNodes</c> rather than <c>collapsed</c> because <see cref="Overlay"/>
		/// already declares a <c>collapsed</c> property.
		/// </summary>
		private readonly HashSet<Transform> collapsedNodes = new();

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
					if (target) Selection.activeGameObject = target.gameObject;
				};

			// Double-click frames the object, matching the Hierarchy window's behavior.
			list.RegisterCallback<PointerDownEvent>(evt =>
				{
					if (evt.clickCount < 2) return;
					var view = SceneView.lastActiveSceneView;
					if (view) view.FrameSelected();
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

			if (!target)
			{
				label.text = "<destroyed>";
				foldout.text = string.Empty;
				icon.image = null;
				return;
			}

			var isCollapsed = collapsedNodes.Contains(target);
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

			if (!collapsedNodes.Add(target)) collapsedNodes.Remove(target);

			evt.StopPropagation();
			Rebuild();
		}

		private void Rebuild()
		{
			rows.Clear();
			truncated = false;

			// Holding Transforms keeps their managed wrappers alive after the native objects are gone,
			// so entries left behind by a scene unloaded would accumulate for the session. Cheap to
			// sweep here: the set only ever holds nodes the user actually folded.
			collapsedNodes.RemoveWhere(node => !node);

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage)
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
			var descend = !string.IsNullOrEmpty(filter) || !collapsedNodes.Contains(t);
			if (!hasChildren || !descend) return;

			for (var i = 0; i < t.childCount; i++)
				Walk(t.GetChild(i), depth + 1);
		}

		private void SyncSelection()
		{
			if (list == null) return;

			var active = Selection.activeGameObject;
			if (!active)
			{
				list.SetSelectionWithoutNotify(System.Array.Empty<int>());
				return;
			}

			for (var i = 0; i < rows.Count; i++)
			{
				if (!rows[i].Target || rows[i].Target.gameObject != active) continue;
				list.SetSelectionWithoutNotify(new[] { i });
				list.ScrollToItem(i);
				return;
			}

			list.SetSelectionWithoutNotify(System.Array.Empty<int>());
		}
	}
}