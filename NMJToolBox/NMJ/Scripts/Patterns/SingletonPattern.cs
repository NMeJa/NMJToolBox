using UnityEngine;

namespace NMJToolBox
{
    public abstract class SingletonPattern<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected bool dontDestroy = false;

        private static T singleton;

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
        }

        private static void CreateInstance()
        {
            singleton = FindObjectOfType<T>();
            if (singleton != null) return;
            GameObject obj = new GameObject(typeof(T).ToString());
            singleton = obj.AddComponent<T>();
        }
    }
}