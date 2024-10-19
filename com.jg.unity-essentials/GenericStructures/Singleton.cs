using UnityEngine;

namespace JG.Tools
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        [SerializeField] private bool _persistent = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = $"[Singleton] {typeof(T).Name}";

                            Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton.name}' was created.");
                        }
                        else
                        {
                            Debug.Log($"[Singleton] Using instance already created: {_instance.gameObject.name}");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (_persistent)
                    DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Another instance of {typeof(T)} already exists. Destroying this duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void ResetInstance()
        {
            _instance = null;
            _applicationIsQuitting = false;
        }
    }
}