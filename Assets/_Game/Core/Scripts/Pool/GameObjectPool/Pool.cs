using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Pooling
{
    [Serializable]
    public class Pool
    {
        [HideInInspector] public string Name;
        public GameObject Prefab;
        public int InitSize = 10;
        public bool HasMaxSize = false;
        public int MaxSize = 100;

        
        private Transform originalParent;
        private List<GameObject> pooledInstances;
        private List<GameObject> aliveInstances;


        public Pool(GameObject prefab, int initSize, Transform originalParent = null)
        {
            this.Prefab = prefab;
            this.InitSize = initSize;
            Init(originalParent);
        }

        public void Validate()
        {
            Name = Prefab != null ? Prefab.name : "Pool";
            if (HasMaxSize && MaxSize < InitSize)
                MaxSize = InitSize;
                
        }

        public bool Init(Transform parent = null)
        {
            if (Prefab == null)
                return false;
            pooledInstances = new List<GameObject>();
            aliveInstances = new List<GameObject>();
            originalParent = parent;

            for (int i = 0; i < InitSize; i++)
            {
                GameObject instance = GameObject.Instantiate(Prefab);
                instance.SetActive(false);
                instance.transform.parent = originalParent;
                pooledInstances.Add(instance);
            }
            return true;
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Vector3 scale, Transform parent = null)
        {
            if (pooledInstances == null) return null;
            
            if (pooledInstances.Count <= 0)
            {
                if (HasMaxSize)
                {
                    if (aliveInstances.Count >= MaxSize)
                    {
                        Debug.LogWarning("[PoolManager] MaxSize of \"" + Prefab.name + "\" reached. Cannot create more!");
                        return null;
                    }
                }
                GameObject newInstance = GameObject.Instantiate(Prefab);
                newInstance.SetActive(true);
                newInstance.transform.SetParent(parent);
                newInstance.transform.position = position;
                newInstance.transform.rotation = rotation;
                newInstance.transform.localScale = scale;

                aliveInstances.Add(newInstance);
                return newInstance;
            }

            GameObject obj = pooledInstances[pooledInstances.Count - 1];
            if (obj == null) return null;

            obj.SetActive(true);
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            pooledInstances.RemoveAt(pooledInstances.Count - 1);
            aliveInstances.Add(obj);

            obj.GetComponent<IPoolable>()?.OnSpawned();
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            int index = aliveInstances.FindIndex(o => obj == o);
            if (index == -1)
            {
                GameObject.Destroy(obj);
                return;
            }
            obj.SetActive(false);
            obj.transform.parent = originalParent;
            aliveInstances.RemoveAt(index);
            pooledInstances.Add(obj);
            obj.GetComponent<IPoolable>()?.OnDespawned();
        }

        public bool IsResponsibleForObject(GameObject obj)
        {
            int index = aliveInstances.FindIndex(o => ReferenceEquals(obj, o));
            if (index == -1)
                return false;
            return true;
        }
    }
}
