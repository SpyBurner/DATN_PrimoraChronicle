using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public static class PoolExtensions
    {
        public static GameObject Spawn(this GameObject obj)
        {
            return PoolManager.Spawn(obj, Vector3.zero, obj.transform.rotation, obj.transform.localScale);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position)
        {
            return PoolManager.Spawn(obj, position, obj.transform.rotation, obj.transform.localScale);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Quaternion rotation)
        {
            return PoolManager.Spawn(obj, position, rotation, obj.transform.localScale);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Vector3 scale)
        {
            return PoolManager.Spawn(obj, position, obj.transform.rotation, scale);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Transform parent)
        {
            return PoolManager.Spawn(obj, position, obj.transform.rotation, obj.transform.localScale, parent);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return PoolManager.Spawn(obj, position, rotation, scale);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
        {
            return PoolManager.Spawn(obj, position, rotation, scale, parent);
        }
        public static GameObject Spawn(this GameObject obj, Vector3 position, Quaternion rotation, Transform parent)
        {
            return PoolManager.Spawn(obj, position, rotation, obj.transform.localScale, parent);
        }
        public static GameObject Spawn(this GameObject obj, Transform parent)
        {
            return PoolManager.Spawn(obj, Vector3.zero, obj.transform.rotation, obj.transform.localScale, parent);
        }


        public static void Despawn(this GameObject obj)
        {
            if (!obj.activeSelf)
                return;
            PoolManager.Despawn(obj);
        }
    }
}
