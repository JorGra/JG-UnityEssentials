using UnityEngine;


namespace JG.Flyweights
{
    public abstract class FlyweightSettings : ScriptableObject
    {
        public GameObject prefab;
        public string Name => prefab.name;

        public abstract Flyweight Create();

        public virtual void OnGet(Flyweight f) => f.gameObject.SetActive(true);
        public virtual void OnRelease(Flyweight f) => f.gameObject.SetActive(false);
        public virtual void OnDestroyPoolObject(Flyweight f)
        {
            if (f == null) return;
            Destroy(f.gameObject);
        }
    }
}