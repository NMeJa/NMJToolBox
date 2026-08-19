using UnityEngine;

// ReSharper disable once CheckNamespace
namespace NMJTools.WhyEvenUsing
{
	[DefaultExecutionOrder(-10)]
	public abstract class SingletonPattern<T> : MonoBehaviour where T : MonoBehaviour
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
			if (!singleton)
			{
				CreateInstance();
			}

			if (singleton != this)
			{
				Destroy(gameObject);
			}

			if (dontDestroy)
			{
				DontDestroyOnLoad(gameObject);
			}

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