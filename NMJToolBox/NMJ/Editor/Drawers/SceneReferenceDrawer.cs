using System.Linq;
using NMJTools.Attributes;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Drawers
{
	/// <summary>
	/// Drawer for <see cref="SceneReferenceAttribute"/>. Shows a <c>SceneAsset</c> object field but
	/// stores the scene's asset path in the underlying string.
	/// </summary>
	[CustomPropertyDrawer(typeof(SceneReferenceAttribute))]
	public class SceneReferenceDrawer : PropertyDrawer
	{
		private const float WarningHeight = 32f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var height = EditorGUIUtility.singleLineHeight;
			if (NeedsBuildSettingsWarning(property)) height += WarningHeight + 2f;
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.LabelField(position, label.text, "[SceneReference] only works on string fields");
				return;
			}

			var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			var scene = string.IsNullOrEmpty(property.stringValue)
							? null
							: AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);

			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();

			var picked = (SceneAsset)EditorGUI.ObjectField(fieldRect, label, scene, typeof(SceneAsset), false);

			if (EditorGUI.EndChangeCheck())
				property.stringValue = !picked ? string.Empty : AssetDatabase.GetAssetPath(picked);

			// The asset may have been moved or renamed since the path was stored. Re-resolving by GUID
			// is not possible from a path alone, but when the asset still loads, we can refresh the
			// path for free — which quietly repairs references after a move.
			if (scene)
			{
				var currentPath = AssetDatabase.GetAssetPath(scene);
				if (currentPath != property.stringValue) property.stringValue = currentPath;
			}

			EditorGUI.EndProperty();

			if (!NeedsBuildSettingsWarning(property)) return;

			var warnRect = new Rect(position.x, fieldRect.yMax + 2f, position.width, WarningHeight);

			var missing = string.IsNullOrEmpty(property.stringValue);
			if (missing)
			{
				EditorGUI.HelpBox(warnRect, "No scene assigned.", MessageType.Warning);
				return;
			}

			const float buttonWidth = 110f;
			var boxRect = new Rect(warnRect.x, warnRect.y, warnRect.width - buttonWidth - 4f, warnRect.height);
			var buttonRect = new Rect(boxRect.xMax + 4f, warnRect.y + 6f, buttonWidth, warnRect.height - 12f);

			EditorGUI.HelpBox(boxRect, "Scene is not enabled in Build Settings and cannot be loaded at runtime.",
							  MessageType.Warning);

			if (GUI.Button(buttonRect, "Add to Build")) AddToBuildSettings(property.stringValue);
		}

		private bool NeedsBuildSettingsWarning(SerializedProperty property)
		{
			if (attribute is not SceneReferenceAttribute { RequireInBuildSettings: true }) return false;
			if (property.propertyType != SerializedPropertyType.String) return false;
			if (string.IsNullOrEmpty(property.stringValue)) return true;

			return !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == property.stringValue);
		}

		private static void AddToBuildSettings(string path)
		{
			var scenes = EditorBuildSettings.scenes.ToList();

			var existing = scenes.FindIndex(s => s.path == path);
			if (existing >= 0) scenes[existing] = new EditorBuildSettingsScene(path, true);
			else scenes.Add(new EditorBuildSettingsScene(path, true));

			EditorBuildSettings.scenes = scenes.ToArray();
			Debug.Log($"[NMJ] Added '{path}' to Build Settings.");
		}
	}
}