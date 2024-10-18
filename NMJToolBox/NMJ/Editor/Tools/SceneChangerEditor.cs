using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	[CustomEditor(typeof(SceneChanger))]
	public class SceneChangerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (!GUILayout.Button("Add Scene To BuildSettings")) return;
			var asset = serializedObject.FindProperty("sceneAsset");
			var editorBuildSettingsScenes = (from sceneAsset in EditorBuildSettings.scenes
			                                 let scenePath = sceneAsset.path
			                                 where !string.IsNullOrEmpty(scenePath)
			                                 select new EditorBuildSettingsScene(scenePath, sceneAsset.enabled))
				.ToList();

			var newScenePath = AssetDatabase.GetAssetPath(asset.objectReferenceValue);
			if (editorBuildSettingsScenes.Any(scene => scene.path == newScenePath)) return;
			editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(newScenePath, true));
			EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
		}
	}
}