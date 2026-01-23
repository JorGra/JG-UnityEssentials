using System.Collections.Generic;
using JG.Tools;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;


namespace JG.Flyweights
{
    public class FlyweightFactory : PersistentSingleton<FlyweightFactory>
    {
        [SerializeField] bool collectionCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;

        readonly Dictionary<string, IObjectPool<Flyweight>> pools = new();


        protected override void Awake()
        {
            base.Awake();
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        public static Flyweight Spawn(FlyweightSettings settings) => instance.GetFromPool(settings);

        public static Flyweight Spawn(FlyweightSettings settings, Vector3 position, Quaternion rotation)
        {
            var f = instance.GetFromPool(settings);
            if (f != null)
            {
                f.transform.SetPositionAndRotation(position, rotation);
            }
            return f;
        }

        public static Flyweight Spawn(FlyweightSettings settings, Vector3 position, Quaternion rotation, Transform parent)
        {
            var f = instance.GetFromPool(settings);
            if (f != null)
            {
                f.transform.SetPositionAndRotation(position, rotation);
                f.transform.SetParent(parent);
            }
            return f;
        }
        public static void ReturnToPool(Flyweight f)
        {
            if (f == null || f.settings == null) return;
            instance.GetPoolFor(f.settings)?.Release(f);
        }

        IObjectPool<Flyweight> GetPoolFor(FlyweightSettings settings)
        {
            if(settings == null)
            {
                Debug.LogError("FlyweightSettings is null");
                return null;
            }


            if (pools.TryGetValue(settings.Name, out IObjectPool<Flyweight> pool)) return pool;

            pool = new ObjectPool<Flyweight>(
                settings.Create,
                settings.OnGet,
                settings.OnRelease,
                settings.OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize
            );
            pools.Add(settings.Name, pool);
            return pool;
        }

        void HandleSceneUnloaded(Scene _)
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
            pools.Clear();
        }

        Flyweight GetFromPool(FlyweightSettings settings)
        {
            var pool = GetPoolFor(settings);
            if (pool == null) return null;

            const int maxAttempts = 3;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var f = pool.Get();
                if (f != null)
                {
                    return f;
                }
            }

            Debug.LogWarning($"FlyweightFactory could not spawn a valid instance for '{settings?.Name}'.");
            return null;
        }
    }
}
