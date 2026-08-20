using System.Diagnostics.CodeAnalysis;
using NMJTools.Attributes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace NMJTools.Scenes
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public class SceneChanger : MonoBehaviour
	{
		// Was backed by a #if UNITY_EDITOR SceneAsset field plus a handwritten custom inspector.
		// [SceneReference] now provides the same scene picker and the "Add to Build Settings" button
		// on any string field, so both of those are gone. The stored value is unchanged - still the
		// scene's asset path - so existing prefabs and scenes keep working.
		[FormerlySerializedAs("sceneName")]
		[SerializeField, SceneReference]
		private string scenePath;

		public string ScenePath
		{
			get => scenePath;
			set => scenePath = value;
		}

		public bool IsActiveScene => SceneManager.GetActiveScene().path.Equals(scenePath);

		public void _LoadScene(string scene) => SceneManager.LoadScene(scene);
		public void _LoadScene() => SceneManager.LoadScene(scenePath);
		public AsyncOperation _LoadParallelScene() => SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
		public AsyncOperation _LoadParallelScene(LoadSceneMode mode) => SceneManager.LoadSceneAsync(scenePath, mode);
		public void _UnLoadParallelScene() => SceneManager.UnloadSceneAsync(scenePath, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
	}
}