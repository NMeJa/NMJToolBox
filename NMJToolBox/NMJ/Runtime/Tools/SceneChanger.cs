using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMJToolBox
{
    public class SceneChanger : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Header("Make Sure Scene Names are unique\nBecause it is Loading Though name not path")]
        [Space]
#if UNITY_EDITOR
        [SerializeField]
        private UnityEditor.SceneAsset sceneAsset;
#endif
        [SerializeField] [ReadOnly] [AllowNesting]
        private string sceneName;

        public void _LoadScene(string scene) => SceneManager.LoadScene(scene);
        public void _LoadScene() => SceneManager.LoadScene(sceneName);
        public void _LoadParallelScene() => SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        public void _UnLoadParallelScene() =>
            SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset) sceneName = sceneAsset.name;
#endif
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
