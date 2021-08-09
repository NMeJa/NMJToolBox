using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace HelperTools.Tools
{
    [CustomEditor(typeof(SceneChanger))]
    public class SceneChangerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!GUILayout.Button("Add Scene To BuildSettings")) return;
            var asset = serializedObject.FindProperty("sceneAsset");
            var editorBuildSettingsScenes = (from sceneAsset in EditorBuildSettings.scenes
                select sceneAsset.path
                into scenePath
                where !string.IsNullOrEmpty(scenePath)
                select new EditorBuildSettingsScene(scenePath, true)).ToList();

            var newScenePath = AssetDatabase.GetAssetPath(asset.objectReferenceValue);
            editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(newScenePath, true));
            editorBuildSettingsScenes = editorBuildSettingsScenes.DistinctBy(e => e.path).ToList();
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }
    }
}