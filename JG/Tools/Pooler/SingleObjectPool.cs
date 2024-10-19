using System;
using System.Collections.Generic;
using UnityEngine;


namespace JG.Tools.Pooling
{

    [Serializable]
    public class SingleObjectPoolDefinition : ObjectPoolDefinitionBase
    {
        public GameObject GameObjectToPool;
    }

    /// <summary>
    /// SingleObjectPool class that hold a queue with references to the instantiated objects
    /// It gets filled by the ObjectPoolManager
    /// It implements the IObjectPool interface
    /// </summary>
    public class SingleObjectPool : MonoBehaviour, IObjectPool
    {
        public SingleObjectPoolDefinition Definition;
        Queue<IPoolableObject> objectQueue = new Queue<IPoolableObject>();
        HashSet<IPoolableObject> activeObjects = new HashSet<IPoolableObject>();
        public int queueCount { get { return objectQueue.Count; } }
        [SerializeField] private bool initOnStart;
        public GameObject PoolContainer { get; set; }

        public void Start()
        {
            if (initOnStart)
            {
                var poolContainer = new GameObject("[POOL] " + Definition.name + "Pool");
                PoolContainer = poolContainer;
                FillPool();
            }
        }

        public GameObject GetObjectFromPool()
        {
            IPoolableObject poolableObject;
            if (objectQueue.Count > 0)
            {
                poolableObject = objectQueue.Dequeue();
            }
            else if (Definition.PoolCanExpand)
            {
                poolableObject = AddNewObjectToPool();
                Debug.LogWarning($"Queue of {name} had to expand.");
            }
            else
            {
                Debug.LogError("Queue empty and not expandable");
                return null;
            }

            activeObjects.Add(poolableObject);
            poolableObject.SceneGameObject.SetActive(true);
            return poolableObject.SceneGameObject;
        }

        public void ReturnObjectToPool(IPoolableObject poolableObject)
        {
            if (activeObjects.Remove(poolableObject))
            {
                poolableObject.SceneGameObject.SetActive(false);
                objectQueue.Enqueue(poolableObject);
            }
            //else
            //{
            //    Debug.LogWarning($"Attempted to return an object that wasn't active: {poolableObject.SceneGameObject.name}");
            //}
        }

        public void FillPool()
        {
            for (int i = 0; i < Definition.PoolSize; i++)
            {
                AddNewObjectToPool();
            }
        }

        private IPoolableObject AddNewObjectToPool()
        {
            var newObject = Instantiate(Definition.GameObjectToPool, PoolContainer.transform);
            var poolableObject = newObject.GetComponent<IPoolableObject>();
            if (poolableObject == null)
            {
                Debug.LogError("No Poolable Object component found on the gameobject you want to pool");
                return null;
            }
            poolableObject.SpawnedFromPool = this;
            newObject.SetActive(false);
            objectQueue.Enqueue(poolableObject);
            return poolableObject;
        }

        public void LogPoolStatus()
        {
            Debug.Log($"Pool {name} status - Queue count: {objectQueue.Count}, Active objects: {activeObjects.Count}");
        }
    }
}