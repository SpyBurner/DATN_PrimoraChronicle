using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Pooling
{
    public class ObjectPool : MonoBehaviour
    {
        public List<Pool> PoolList;

        protected void OnValidate() 
        {
            if (PoolList == null || PoolList.Count == 0)
                return;
            for(int i = 0; i < PoolList.Count; i++)
            {
                PoolList[i].Validate();
            }
        }
    }
}
