using System.Collections.Generic;
using UnityEngine;

namespace Logic.PoolingSystem
{
    public class Pool<T> where T : MonoBehaviour, IPoolable<T>
    {
        private readonly T m_prefab;

        private Queue<T> m_queue = new Queue<T>();

        public Pool(T prefab)
        {
            this.m_prefab = prefab;
        }

        public T Get()
        {
            if (m_queue.Count == 0) AddObjects(1);

            T instance = m_queue.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void ReturnToPool(T instance)
        {
            instance.gameObject.SetActive(false);
            m_queue.Enqueue(instance);
        }

        public void AddObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = Object.Instantiate(m_prefab);
                ReturnToPool(instance);

                instance.GetComponent<IPoolable<T>>().OnFree += ReturnToPool;
            }
        }

        public void CleanUp()
        {
            foreach (IPoolable<T> poolable in m_queue)
            {
                poolable.OnFree -= ReturnToPool;
            }
            m_queue.Clear();
        }
    }

}