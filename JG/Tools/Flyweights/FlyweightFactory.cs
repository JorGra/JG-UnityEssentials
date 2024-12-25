using System.Collections.Generic;
using JG.Tools;
using UnityEngine;
using UnityEngine.Pool;


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
        }

        public static Flyweight Spawn(FlyweightSettings settings) => instance.GetPoolFor(settings)?.Get();

        public static Flyweight Spawn(FlyweightSettings settings, Vector3 position, Quaternion rotation)
        {
            var f = instance.GetPoolFor(settings)?.Get();
            f.transform.SetPositionAndRotation(position, rotation);
            return f;
        }

        public static Flyweight Spawn(FlyweightSettings settings, Vector3 position, Quaternion rotation, Transform parent)
        {
            var f = instance.GetPoolFor(settings)?.Get();
            f.transform.SetPositionAndRotation(position, rotation);
            f.transform.SetParent(parent);
            return f;
        }
        public static void ReturnToPool(Flyweight f) => instance.GetPoolFor(f.settings)?.Release(f);

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
    }
}