using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JG.Tools.Pooling
{
    [Serializable]
    public class MultipleObjectPoolDefinition : ObjectPoolDefinitionBase
    {
        public MultipleObjectPoolDefinitionEntry[] GameObjectsToPool;
    }

    [Serializable]
    public class MultipleObjectPoolDefinitionEntry
    {
        public GameObject GameObjectToPool;
        public int PoolSize;
    }

    /// <summary>
    /// MultipleObjectPool instantiates multiple objectPools and adds them to a list
    /// It ether returns the objects in a sequential or random manner
    /// </summary>
    public class MultipleObjectPool : MonoBehaviour, IObjectPool
    {

        public MultipleObjectPoolDefinition Definition;
        List<IPoolableObject> objectList = new List<IPoolableObject>();
        [SerializeField] bool returnRandomObject = true;
        [SerializeField] bool initOnAwake = true;

        int currentPoolIndex = 0;

        public GameObject PoolContainer { get; set; }

        protected void Awake()
        {
            if (initOnAwake)
            {
                var poolContainer = new GameObject("[POOL] " + Definition.name + "Pool");
                PoolContainer = poolContainer;
                FillPool();
            }
        }

        public GameObject GetObjectFromPool()
        {
            if (!returnRandomObject)
            {
                return ReturnSequential();
            }
            else
            {
                return ReturnRandom();
            }
        }
        public void FillPool()
        {
            foreach (var entry in Definition.GameObjectsToPool)
            {
                for (int i = 0; i < entry.PoolSize; i++)
                {
                    if(entry.GameObjectToPool == null)
                    {
                        Debug.LogError("Entry has no gameobject assigned! Definition: " + Definition.name);
                        continue;
                    }
                    AddNewObjectToPool(entry.GameObjectToPool);
                }
            }
        }


        void AdvancePoolIndex()
        {
            currentPoolIndex++;
            if (currentPoolIndex > objectList.Count)
                currentPoolIndex = 0;
        }

        public void ReturnObjectToPool(IPoolableObject poolableObject)
        {

        }

        private GameObject ReturnRandom()
        {
            int randomIndex = UnityEngine.Random.Range(0, objectList.Count);

            var objectToReturn = objectList[randomIndex].SceneGameObject;

            if (!objectToReturn.activeInHierarchy)
            {
                objectToReturn.SetActive(true);
                return objectToReturn;
            }
            else
            {
                //var firstInactive = objectList.DefaultIfEmpty(null).FirstOrDefault(o => !o.SceneGameObject.activeInHierarchy);
                for (int i = randomIndex; i < objectList.Count + randomIndex; i++)
                {
                    if (!objectList[i % objectList.Count].SceneGameObject.activeInHierarchy)
                    {
                        objectList[i % objectList.Count].SceneGameObject.SetActive(true);
                        return objectList[i % objectList.Count].SceneGameObject;
                    }
                }

                //No inactive object was found add a new one ore return first
                if (Definition.PoolCanExpand)
                {
                    //We expand the pool with a random Gameobject
                    objectToReturn = AddNewRandomObjectToPool();
                    objectToReturn.gameObject.SetActive(true);
                    return objectToReturn;
                }
                else
                {
                    //Pool cannot expand and gameobject found is already active, we still return it
                    objectToReturn.SetActive(true);
                    return objectToReturn;
                }
            }
        }

        private GameObject ReturnSequential()
        {
            var objectToReturn = objectList[currentPoolIndex].SceneGameObject;
            AdvancePoolIndex();

            if (!objectToReturn.activeInHierarchy)
            {
                objectToReturn.SetActive(true);
                return objectToReturn;
            }
            else
            {
                //Object is active in hierarchy, search for another one
                var firstInactive = objectList.DefaultIfEmpty(null).FirstOrDefault(o => !o.SceneGameObject.activeInHierarchy);
                if (firstInactive != null)
                {
                    //found an inactive object
                    firstInactive.SceneGameObject.SetActive(true);
                    return firstInactive.SceneGameObject;
                }
                else
                {
                    //All objects currently active, expand pool or reuse first one;
                    if (Definition.PoolCanExpand)
                    {
                        //We expand the pool with a random Gameobject
                        objectToReturn = AddNewObjectToPool(objectList[currentPoolIndex].SceneGameObject);
                        objectToReturn.gameObject.SetActive(true);
                        return objectToReturn;
                    }
                    else
                    {
                        //Pool cannot expand and gameobject found is already active, we still return it
                        objectToReturn.SetActive(true);
                        return objectToReturn;
                    }
                }
            }
        }

        private GameObject AddNewObjectToPool(GameObject objectToAdd)
        {
            if (objectToAdd == null)
                Debug.LogError("objectToAdd is null");

            var newObject = Instantiate(objectToAdd);
            if(newObject.GetComponent<IPoolableObject>() == null)
            {
                Debug.LogError("No PoolableObject component found on gameobject: " + objectToAdd.name);
                return null;
            }
            newObject.GetComponent<IPoolableObject>().SpawnedFromPool = this;
            objectList.Add(newObject.GetComponent<IPoolableObject>());
            newObject.transform.parent = PoolContainer.transform;
            newObject.SetActive(false);
            return newObject;
        }

        private GameObject AddNewRandomObjectToPool()
        {
            var newObject = Instantiate(Definition.GameObjectsToPool[UnityEngine.Random.Range(0, Definition.GameObjectsToPool.Length)].GameObjectToPool);
            newObject.GetComponent<IPoolableObject>().SpawnedFromPool = this;
            objectList.Add(newObject.GetComponent<IPoolableObject>());
            newObject.transform.parent = PoolContainer.transform;
            newObject.SetActive(false);
            return newObject;
        }
    }

}