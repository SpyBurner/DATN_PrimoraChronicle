using UnityEngine;
using System.Collections.Generic;
using Factory;

namespace ObjectPool
{
    public abstract class PoolSO<T> : ScriptableObject, IPool<T>
    {
        protected readonly Stack<T> Available = new Stack<T>();
        public bool HasBeenPrewarmed { get; protected set; }
		public abstract IFactory<T> Factory { get; set; } // let factory controll the creation of the desired object

        protected virtual T Create()
        {
            return Factory.Create();
        }

        public virtual void Prewarm(int num)
        {
            if (HasBeenPrewarmed)
            {
                Debug.LogWarning($"Pool {name} has already been prewarmed.");
                return;
            }
            for (int i = 0; i < num; i++)
            {
                Available.Push(Create());
            }
            HasBeenPrewarmed = true;
        }

        public virtual T Request()
        {
            return Available.Count > 0 ? Available.Pop() : Create();
        }

        /// <summary>
        /// Returns a <typeparamref name="T"/> to the pool.
        /// </summary>
        /// <param name="member">The <typeparamref name="T"/> to return.</param>
        public virtual void Return(T member)
        {
            Available.Push(member);
        }

        public virtual void OnDisable()
        {
            Available.Clear();
            HasBeenPrewarmed = false;
        }
    }
}