using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJTools.SceneTools
{
	/// <summary>
	/// The selected object's inspector, docked inside the Scene view — the other half of working
	/// with the viewport maximized.
	///
	/// Built from <see cref="InspectorElement"/>, so every custom editor, property drawer, and
	/// attribute in the project renders exactly as it does in the real Inspector window; this is a
	/// second view onto the same machinery, not a reimplementation of it.
	///
	/// Enable it from the Scene view's overlay menu (the ⋮ button, or press <c>`</c>) →
	/// <b>Inspector</b>.
	/// </summary>
	[Overlay(typeof(SceneView), OverlayId, "Inspector")]
	public class SelectionInspectorOverlay : Overlay
	{
		private const string OverlayId = "nmj-selection-inspector";

		private VisualElement root;
		private ScrollView body;

		public override VisualElement CreatePanelContent()
		{
			root = new VisualElement
			{
				style = { width = 320, minWidth = 240, minHeight = 160, maxHeight = 640, flexGrow = 1 }
			};

			body = new ScrollView { style = { flexGrow = 1, minHeight = 0 } };
			root.Add(body);

			BuildForSelection();
			return root;
		}

		public override void OnCreated() => Selection.selectionChanged += BuildForSelection;

		public override void OnWillBeDestroyed() => Selection.selectionChanged -= BuildForSelection;

		private void BuildForSelection()
		{
			if (body == null) return;

			body.Clear();

			var active = Selection.activeObject;
			if (!active)
			{
				body.Add(Hint("Nothing selected."));
				return;
			}

			if (Selection.objects.Length > 1)
				body.Add(Hint($"{Selection.objects.Length} objects selected — showing '{active.name}'."));

			if (active is GameObject go)
			{
				BuildGameObject(go);
				return;
			}

			body.Add(Header(active.name, active.GetType().Name));
			body.Add(new InspectorElement(active));
		}

		private void BuildGameObject(GameObject go)
		{
			body.Add(Header(go.name, "GameObject"));

			// The GameObject's own row (name/tag/layer/static) comes from an InspectorElement on the
			// GameObject itself; components each get their own, mirroring the real Inspector's stack.
			var goInspector = new InspectorElement(go);
			body.Add(goInspector);

			var components = go.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!component)
				{
					body.Add(Hint("Missing script"));
					continue;
				}

				var foldout = new Foldout
				{
					text = ObjectNames.NicifyVariableName(component.GetType().Name),
					value = component is Transform, // Transform open by default; it's the one people reach for
					style = { marginTop = 2 }
				};

				foldout.Add(new InspectorElement(component));
				body.Add(foldout);
			}
		}

		private static VisualElement Header(string title, string subtitle)
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					marginBottom = 4,
					flexShrink = 0
				}
			};

			row.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1, overflow = Overflow.Hidden } });
			row.Add(new Label(subtitle) { style = { fontSize = 10, opacity = 0.6f, flexShrink = 0 } });

			return row;
		}

		private static Label Hint(string text) =>
			new(text) { style = { fontSize = 10, opacity = 0.6f, whiteSpace = WhiteSpace.Normal, marginBottom = 4 } };
	}
}
