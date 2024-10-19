using System;
using System.Collections.Generic;
using UnityEngine;


namespace JG.Tools.Pooling
{
    //Interface for PoolableObjects
    public interface IPoolableObject
    {
        public IObjectPool SpawnedFromPool { get; set; }
        //What happens if the object gets destroyed, used to enqueue the object again in the SingleObjectPooler
        void ReturnToCollection();
        //The scene reference
        GameObject SceneGameObject { get; }


        public delegate void Events();
        public event Events OnSpawnComplete;
    }

    /// <summary>
    /// Interface for ObjectPoolers 
    /// Implemented by SingleObjectPool and MultipleObjectPool
    /// </summary>
    public interface IObjectPool
    {
        public GameObject GetObjectFromPool();
        public void ReturnObjectToPool(IPoolableObject poolableObject);
        public void FillPool();
        GameObject PoolContainer { get; set; }
    }

    /// <summary>
    /// Describes the pool and how it is populated
    /// </summary>
    [Serializable]
    public abstract class ObjectPoolDefinitionBase
    {
        public string name;
        public int PoolSize;
        public bool PoolCanExpand = true;
        public bool Enabled = true;
    }


    /// <summary>
    /// Manager script that can be used to hold all poolers and set them up simultainiously
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        //The List of ObjectPoolDefinitions, this defines how many objects are going to be instatiated 
        public List<SingleObjectPoolDefinition> singleObjectPoolDefinitions = new List<SingleObjectPoolDefinition>();
        public List<MultipleObjectPoolDefinition> multipleObjectPoolDefinitions = new List<MultipleObjectPoolDefinition>();
        // The actual object pool holding references to scene objects
        List<IObjectPool> objectPoolers = new List<IObjectPool>();


        protected override void Awake()
        {
            base.Awake(); 
        }

        // Start is called before the first frame update
        void Start()
        {
            CreatePoolObjects();
        }

        private void CreatePoolObjects()
        {
            //Create objects that hold the instantiated scene objects
            foreach (var def in singleObjectPoolDefinitions)
            {
                var poolContainer = new GameObject("[POOL] " + def.GameObjectToPool.name + "Pool");
                var pooler = poolContainer.AddComponent<SingleObjectPool>();
                pooler.PoolContainer = poolContainer;
                pooler.Definition = def;
                pooler.FillPool();
                objectPoolers.Add(pooler);
            }

            foreach (var def in multipleObjectPoolDefinitions)
            {
                var poolContainer = new GameObject("[POOL] " + def.name + "Pool");
                var pooler = poolContainer.AddComponent<MultipleObjectPool>();
                pooler.PoolContainer = poolContainer;
                pooler.Definition = def;
                objectPoolers.Add(pooler);
                pooler.FillPool();
            }
        }

        public void AddMultipleObjectPool(MultipleObjectPoolDefinition poolToAdd)
        {
            multipleObjectPoolDefinitions.Add(poolToAdd);
            var poolContainer = new GameObject("[POOL] " + poolToAdd.name + "Pool");
            var pooler = poolContainer.AddComponent<MultipleObjectPool>();
            pooler.Definition = poolToAdd;
            pooler.PoolContainer = poolContainer;
            objectPoolers.Add(pooler);
            pooler.FillPool();
        }

        //TODO: ADD Dictonary that hold references to all Poolers and their gameobject
    }

}