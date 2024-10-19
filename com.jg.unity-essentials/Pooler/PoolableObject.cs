using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Tools.Pooling
{
    /// <summary>
    /// This class get attatched to objects that are pooled
    /// It handles destruction of the object and returns it to the queue
    /// </summary>
    public class PoolableObject : MonoBehaviour, IPoolableObject
    {
        //Reference to the pool that holds the queue
        public IObjectPool SpawnedFromPool { get; set; }
        public GameObject SceneGameObject { get => gameObject; }

        public event IPoolableObject.Events OnSpawnComplete;

        public void ReturnToCollection()
        {
            gameObject.SetActive(false);
            if(SpawnedFromPool != null)
            {
                SpawnedFromPool.ReturnObjectToPool(this);

            }
        }

        public void OnEnable()
        {
            OnSpawnComplete?.Invoke();
        }

        private void OnDisable()
        {
            ReturnToCollection();
        }
    }
}
