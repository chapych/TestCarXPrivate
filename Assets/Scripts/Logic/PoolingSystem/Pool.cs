using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.PoolingSystem
{
    public class Pool
    {
        private readonly Func<Task<GameObject>> instantiatingAsyncFunc;

        private Queue<IPooled> queue = new Queue<IPooled>();

        public Pool(Func<Task<GameObject>> instantiatingAsyncFunc)
        {
            this.instantiatingAsyncFunc = instantiatingAsyncFunc;
        }

        public IPooled Get()
        {
            if (queue.Count == 0) AddObjects(1);

            IPooled instance = queue.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void ReturnToPool(IPooled instance)
        {
            instance.gameObject.SetActive(false);
            queue.Enqueue(instance);
        }

        public async Task AddObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject instance = await instantiatingAsyncFunc();
                var pooled = instance.GetComponent<IPooled>();

                ReturnToPool(pooled);
                pooled.OnFree += ReturnToPool;
                pooled.Configure();
            }
        }

        public void CleanUp()
        {
            foreach (IPooled poolable in queue)
            {
                poolable.OnFree -= ReturnToPool;
            }
            queue.Clear();
        }
    }

}