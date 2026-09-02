using UnityEngine;

// ReSharper disable once CheckNamespace
namespace NMJTools.Patterns
{
	[DefaultExecutionOrder(-10)]
	public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
	{
		[SerializeField] protected bool dontDestroy = false;

		private static T singleton;

		// ReSharper disable once UnusedMember.Global
		public static T Singleton
		{
			get
			{
				if (!singleton) CreateInstance();
				return singleton;
			}
		}

		private void Awake()
		{
			if (!singleton) CreateInstance();

			// this changed: returning after Destroy. Without it a duplicate carried on to
			// DontDestroyOnLoad and PersonalAwake - so a second instance ran its whole initialisation,
			// and could mark itself DontDestroyOnLoad, on the very frame it was being destroyed.
			// Destroy is deferred to the end of the frame, so nothing here was stopping it.
			if (singleton != this)
			{
				Destroy(gameObject);
				return;
			}

			if (dontDestroy) DontDestroyOnLoad(gameObject);

			PersonalAwake();
		}

		protected virtual void PersonalAwake() { }

		private static void CreateInstance()
		{
			singleton = FindAnyObjectByType<T>();
			if (singleton) return;
			var obj = new GameObject(typeof(T).ToString());
			singleton = obj.AddComponent<T>();
		}
	}
}