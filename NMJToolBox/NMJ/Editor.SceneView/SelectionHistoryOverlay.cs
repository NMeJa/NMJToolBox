using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJToolBox.SceneTools
{
	/// <summary>
	/// Back/forward navigation through your selection history, plus the full list.
	///
	/// Enable from the Scene view's ⋮ menu → <b>Selection History</b>.
	/// </summary>
	[Overlay(typeof(SceneView), OverlayId, "Selection History")]
	public class SelectionHistoryOverlay : Overlay
	{
		private const string OverlayId = "nmj-selection-history";
		private const float RowHeight = 18f;

		private Button backButton;
		private Button forwardButton;
		private ListView list;

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement { style = { width = 220, minWidth = 160, minHeight = 100 } };

			var bar = new VisualElement
			{
				style = { flexDirection = FlexDirection.Row, marginBottom = 2, flexShrink = 0 }
			};

			backButton = new Button(SelectionHistory.Back)
			{
				text = "◀",
				tooltip = "Previous selection",
				style = { width = 28 }
			};

			forwardButton = new Button(SelectionHistory.Forward)
			{
				text = "▶",
				tooltip = "Next selection",
				style = { width = 28 }
			};

			var clear = new Button(SelectionHistory.Clear)
			{
				text = "Clear",
				tooltip = "Forget the recorded history",
				style = { flexGrow = 1 }
			};

			bar.Add(backButton);
			bar.Add(forwardButton);
			bar.Add(clear);
			root.Add(bar);

			list = new ListView
			{
				fixedItemHeight = RowHeight,
				selectionType = SelectionType.Single,
				style = { flexGrow = 1, minHeight = 0 },
				makeItem = () => new Label { style = { fontSize = 11, unityTextAlign = TextAnchor.MiddleLeft, height = RowHeight } },
				bindItem = (element, index) =>
					{
						var label = (Label)element;
						label.text = SelectionHistory.Describe(index);

						// The current position is what everything else is relative to, so it earns emphasis.
						var isCursor = index == SelectionHistory.Cursor;
						label.style.unityFontStyleAndWeight = isCursor ? FontStyle.Bold : FontStyle.Normal;
						label.style.opacity = isCursor ? 1f : 0.75f;
					},
				itemsSource = (System.Collections.IList)SelectionHistory.Entries
			};

			list.selectionChanged += _ =>
				{
					if (list.selectedIndex >= 0) SelectionHistory.GoTo(list.selectedIndex);
				};

			root.Add(list);

			Refresh();
			return root;
		}

		public override void OnCreated() => SelectionHistory.Changed += Refresh;

		public override void OnWillBeDestroyed() => SelectionHistory.Changed -= Refresh;

		private void Refresh()
		{
			if (backButton != null) backButton.SetEnabled(SelectionHistory.CanGoBack);
			if (forwardButton != null) forwardButton.SetEnabled(SelectionHistory.CanGoForward);

			if (list == null) return;

			list.itemsSource = (System.Collections.IList)SelectionHistory.Entries;
			list.Rebuild();

			if (SelectionHistory.Cursor >= 0 && SelectionHistory.Cursor < SelectionHistory.Entries.Count)
			{
				list.SetSelectionWithoutNotify(new[] { SelectionHistory.Cursor });
				list.ScrollToItem(SelectionHistory.Cursor);
			}
		}

		/// <summary>
		/// Registered without default bindings. A shortcut that collides with an existing one leaves
		/// <em>both</em> disabled, and there is no safe key here that is free on every platform and
		/// layout — so bind these yourself in Edit ▸ Shortcuts (search "NMJ"). Alt+Left / Alt+Right
		/// match the browser convention if you want a suggestion.
		/// </summary>
		[Shortcut("NMJ/Selection History/Back")]
		private static void BackShortcut() => SelectionHistory.Back();

		[Shortcut("NMJ/Selection History/Forward")]
		private static void ForwardShortcut() => SelectionHistory.Forward();
	}
}
