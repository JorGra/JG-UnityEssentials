using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JG.Tools.Pooling
{
    public class TagBasedMultipleObjectPooler : MultipleObjectPool
    {
        public string[] Tags;
        public string[] CanSpawnTags;
        public float SpawnBias = 1;

        public static TagBasedMultipleObjectPooler GetPoolerWithTag(string tag, ICollection<TagBasedMultipleObjectPooler> poolerList)
        {
            if (tag.Length == 0 || tag.Equals(""))
                Debug.LogError("Searched for empty tag or tag is null.");

            var poolersWithTag = poolerList.Where(o => (o.Tags.Any(t => t.Equals(tag)))).ToList();

            if (poolersWithTag.Count == 0)
                Debug.LogError("Searched pooler with tag: " + tag + " but couldn't find one.");

            float totalSum = 0;
            poolersWithTag.ForEach(o => totalSum += o.SpawnBias);

            var randVal = Random.Range(0, totalSum);

            foreach (var pooler in poolersWithTag)
            {
                randVal -= pooler.SpawnBias;
                if (randVal <= 0)
                    return pooler;
            }

            //Failsave when totalSum is zero: return randomElement
            return poolersWithTag[Random.Range(0, poolersWithTag.Count)];
        }
    }
}
