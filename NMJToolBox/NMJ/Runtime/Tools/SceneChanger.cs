using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMJToolBox
{
	public class SceneChanger : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField]
		private UnityEditor.SceneAsset sceneAsset;
#endif
		[UnityEngine.Serialization.FormerlySerializedAs("sceneName")]
		[SerializeField]
		private string scenePath;
	        public string ScenePath
	        {
	            get => scenePath;
	            set => scenePath = value;
	        }

		public void _LoadScene(string scene) => SceneManager.LoadScene(scene);
		public void _LoadScene() => SceneManager.LoadScene(sceneName);
		public AsyncOperation _LoadParallelScene() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		public AsyncOperation _LoadParallelScene(LoadSceneMode mode) => SceneManager.LoadSceneAsync(sceneName, mode);
		public void _UnLoadParallelScene() => SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
	}
}
