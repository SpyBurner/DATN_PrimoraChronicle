using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public class PoolManager : MonoBehaviour
    {
        private static Dictionary<GameObject, Pool> poolDict;
        private static List<Pool> poolList;

        private static Transform trans;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            trans = this.transform;
            poolDict = new Dictionary<GameObject, Pool>();
            poolList = new List<Pool>();
            ObjectPool[] objectPools = gameObject.GetComponentsInChildren<ObjectPool>();
            if (objectPools == null || objectPools.Length == 0)
                return;
            for (int i = 0; i < objectPools.Length; i++)
            {
                foreach (Pool p in objectPools[i].PoolList)
                {
                    if (p == null)
                        continue;
                    if (!p.Init(objectPools[i].transform))
                        continue;
                    poolDict.Add(p.Prefab, p);
                }
            }
        }

        private static GameObject SpawnNonPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent = null)
        {
            Debug.LogWarning("[PoolManager] You are spawning a non-pooled prefab \"" + prefab.name + "\".");
            Pool pool = NewPool(prefab, 1);
            return pool.Spawn(position, rotation, scale, parent);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent = null)
        {
            if (poolDict == null)
                poolDict = new Dictionary<GameObject, Pool>();
            if (!poolDict.ContainsKey(prefab))
                return SpawnNonPooledObject(prefab, position, rotation, scale, parent);
            return poolDict[prefab].Spawn(position, rotation, scale, parent);
        }

        public static Pool NewPool(GameObject obj, int initSize)
        {
            GameObject newContainer = new GameObject(obj.name);
            newContainer.transform.parent = trans;
            Pool pool = new Pool(obj, initSize, newContainer.transform);
            if (poolList == null)
                poolList = new List<Pool>();
            poolList.Add(pool);
            poolDict.Add(pool.Prefab, pool);
            return pool;
        }

        public static bool CheckPoolExist(GameObject prefab)
        {
            return poolDict.ContainsKey(prefab);
        }

        public static void Despawn(GameObject obj)
        {
            if (poolDict == null)
                return;
            foreach (KeyValuePair<GameObject, Pool> pool in poolDict)
            {
                if (pool.Value.IsResponsibleForObject(obj))
                {
                    pool.Value.Despawn(obj);
                    return;
                }
            }
            Debug.LogWarning("Object -" + obj.name + "- is killed but not in pool! Use Destroy instead!");
            //Destroy(obj);
            obj.SetActive(false);
        }

        private void OnDestroy()
        {
            poolDict.Clear();
            poolList.Clear();
            trans = null;
        }
    }
}

