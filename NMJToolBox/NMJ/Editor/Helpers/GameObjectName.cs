#if UEE
using InfinityCode.UltimateEditorEnhancer;
using InfinityCode.UltimateEditorEnhancer.Attributes;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	public static class GameObjectName
	{
		private static bool inited;
		private static GUIContent content;

		private static void Init()
		{
			inited = true;
			GUIContent guiContent = new(EditorGUIUtility.IconContent("Mirror"))
			{
				tooltip = "Rename To this class This GameObject"
			};
			content = guiContent;
		}

		[ComponentHeaderButton]
		public static bool Rename(Rect rect, Object[] targets)
		{
			if (targets.Length != 1) return false;

			var target = targets[0];
			var component = target as Component;
			if (component == null) return false;

			if (!inited) Init();

			rect.xMin += 1;
			rect.xMax -= 1;
			rect.yMin += 1;
			rect.yMax -= 1;

			var buttonEvent = GUILayoutUtils.Button(rect, content, GUIStyle.none);

			// ReSharper disable once InvertIf
			if (buttonEvent == ButtonEvent.click)
			{
				Undo.RecordObject(Selection.activeObject, "Rename GameObject");
				Selection.activeObject.name = component.GetType().Name.Beautify();
			}

			return true;
		}
	}
}
#endif
