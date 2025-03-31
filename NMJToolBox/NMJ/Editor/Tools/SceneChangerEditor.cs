using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
    [CustomEditor(typeof(SceneChanger))]
    public class SceneChangerEditor : Editor
    {
        private SerializedProperty sceneAssetProp;
        private SerializedProperty sceneNameProp;

        private void OnEnable()
        {
            sceneAssetProp = serializedObject.FindProperty("sceneAsset");
            sceneNameProp = serializedObject.FindProperty("scenePath");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw the scene asset field
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(sceneAssetProp);
            if (EditorGUI.EndChangeCheck() && sceneAssetProp.objectReferenceValue != null)
            {
                // Update the scene name when the asset changes
                var asset = sceneAssetProp.objectReferenceValue as UnityEditor.SceneAsset;
                if (asset != null)
                {
                    sceneNameProp.stringValue = AssetDatabase.GetAssetPath(asset);
                }
            }
            
            // Show the scene path (readonly)
            GUI.enabled = false;
            EditorGUILayout.PropertyField(sceneNameProp);
            GUI.enabled = true;
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            
            // Button to add scene to build settings
            if (GUILayout.Button("Add Scene To BuildSettings"))
            {
                if (sceneAssetProp.objectReferenceValue == null) return;
                
                var editorBuildSettingsScenes = (from sceneAsset in EditorBuildSettings.scenes
                                                let scenePath = sceneAsset.path
                                                where !string.IsNullOrEmpty(scenePath)
                                                select new EditorBuildSettingsScene(scenePath, sceneAsset.enabled)).ToList();

                var newScenePath = AssetDatabase.GetAssetPath(sceneAssetProp.objectReferenceValue);
                if (editorBuildSettingsScenes.Any(scene => scene.path == newScenePath)) return;
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(newScenePath, true));
                EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            }
        }
    }
}
