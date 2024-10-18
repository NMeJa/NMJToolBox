using UnityEngine;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace NMJToolBox
{
	public class SceneChanger : MonoBehaviour, ISerializationCallbackReceiver
	{
#if UNITY_EDITOR
		[SerializeField]
		private UnityEditor.SceneAsset sceneAsset;
#endif


#if ODIN_INSPECTOR
	[ReadOnly]
#endif
		[SerializeField]
		private string sceneName;

		public void _LoadScene(string scene) => SceneManager.LoadScene(scene);
		public void _LoadScene() => SceneManager.LoadScene(sceneName);
		public AsyncOperation _LoadParallelScene() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		public AsyncOperation _LoadParallelScene(LoadSceneMode mode) => SceneManager.LoadSceneAsync(sceneName, mode);

		public void _UnLoadParallelScene() => SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);

		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			if (sceneAsset) sceneName = UnityEditor.AssetDatabase.GetAssetOrScenePath(sceneAsset);
#endif
		}

		public void OnAfterDeserialize() { }
	}
}