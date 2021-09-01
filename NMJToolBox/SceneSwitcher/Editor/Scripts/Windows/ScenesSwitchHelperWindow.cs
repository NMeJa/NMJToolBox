using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vis.SceneSwitcher
{
    public class SceneSwitcherWindow : EditorWindow
    {
        private static SerializableScenesContainer __scenesContainer;
        private static SerializableScenesContainer _scenesContainer
        {
            get
            {
                if (__scenesContainer == null)
                    __scenesContainer = initContainer();
                return __scenesContainer;
            }

            set
            {
                __scenesContainer = value;
            }
        }

        private const string _developerName = "Vis";
        private const string _productName = "Scene Switcher";
        private const string _pointersName = "PointerToSceneSwitcherDB";
        private const string _scenesContainerName = "DefaultScenesContainer.asset";

        private static SerializableScenesContainer initContainer()
        {
            string[] searchResult = AssetDatabase.FindAssets(_pointersName);
            if (searchResult.Length == 0)
            {
                Debug.LogError("[" + _developerName + "][" + _productName + "] " + _pointersName + " wasn't found!");
                return null;
            }

            string pointersGUID = searchResult[0];
            string pathToPointer = AssetDatabase.GUIDToAssetPath(pointersGUID);
            string pathToDB = pathToPointer.Substring(0, pathToPointer.Length - _pointersName.Length - ".bytes".Length) + _scenesContainerName;

            var result = (SerializableScenesContainer)AssetDatabase.LoadAssetAtPath(pathToDB, typeof(SerializableScenesContainer));

            if (result == null)
            {
                result = CreateInstance<SerializableScenesContainer>();
                result.SceneAssets = new List<SceneAsset>();
                for (int i = 0; i < 10; i++)
                    result.SceneAssets.Add(null);

                AssetDatabase.CreateAsset(result, pathToDB);
                AssetDatabase.SaveAssets();
            }
            return result;
        }

        private static void FindWindow()
        {
            var instance = GetWindow<SceneSwitcherWindow>();

            GUIContent title = new GUIContent();
            title.text = _productName;
            instance.titleContent = title;
        }

        #region Menu Items

        [MenuItem(_developerName + "/" + _productName + "/Edit #e")]
        public static void Init()
        {
            FindWindow();
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 1 #1")]
        public static void SwitchTo0()
        {
            if (_scenesContainer.SceneAssets.Count < 1)
                return;
            if (_scenesContainer.SceneAssets[0] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[0]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 2 #2")]
        public static void SwitchTo1()
        {
            if (_scenesContainer.SceneAssets.Count < 2)
                return;
            if (_scenesContainer.SceneAssets[1] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[1]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 3 #3")]
        public static void SwitchTo2()
        {
            if (_scenesContainer.SceneAssets.Count < 3)
                return;
            if (_scenesContainer.SceneAssets[2] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[2]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 4 #4")]
        public static void SwitchTo3()
        {
            if (_scenesContainer.SceneAssets.Count < 4)
                return;
            if (_scenesContainer.SceneAssets[3] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[3]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 5 #5")]
        public static void SwitchTo4()
        {
            if (_scenesContainer.SceneAssets.Count < 5)
                return;
            if (_scenesContainer.SceneAssets[4] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[4]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 6 #6")]
        public static void SwitchTo5()
        {
            if (_scenesContainer.SceneAssets.Count < 6)
                return;
            if (_scenesContainer.SceneAssets[5] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[5]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 7 #7")]
        public static void SwitchTo6()
        {
            if (_scenesContainer.SceneAssets.Count < 7)
                return;
            if (_scenesContainer.SceneAssets[6] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[6]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 8 #8")]
        public static void SwitchTo7()
        {
            if (_scenesContainer.SceneAssets.Count < 8)
                return;
            if (_scenesContainer.SceneAssets[7] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[7]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 9 #9")]
        public static void SwitchTo8()
        {
            if (_scenesContainer.SceneAssets.Count < 9)
                return;
            if (_scenesContainer.SceneAssets[8] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[8]), OpenSceneMode.Single);
        }

        [MenuItem(_developerName + "/" + _productName + "/Switch to scene 10 #0")]
        public static void SwitchTo9()
        {
            if (_scenesContainer.SceneAssets.Count < 10)
                return;
            if (_scenesContainer.SceneAssets[9] == null)
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_scenesContainer.SceneAssets[9]), OpenSceneMode.Single);
        }

        #endregion Menu Items

        private void OnGUI()
        {
            if (_scenesContainer == null)
                return;

            for (int i = 0; i < _scenesContainer.SceneAssets.Count; i++)
            {
                var sceneNumber = (i + 1).ToString();
                SceneAsset newAsset = EditorGUILayout.ObjectField(new GUIContent("Scene " + sceneNumber + " (Shift+" + (i + 1).ToString().Substring(sceneNumber.Length - 1) + ")"), _scenesContainer.SceneAssets[i], typeof(SceneAsset), false) as SceneAsset;
                if (newAsset != _scenesContainer.SceneAssets[i])
                {
                    _scenesContainer.SceneAssets[i] = newAsset;
                    EditorUtility.SetDirty(_scenesContainer);
                }
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear"))
            {
                for (int i = 0; i < 10; i++)
                    _scenesContainer.SceneAssets[i] = null;
                EditorUtility.SetDirty(_scenesContainer);
            }

            GUILayout.EndHorizontal();
        }
    }
}
