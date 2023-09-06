using System;
using BaseInterfaces;
using Logic.Tower;
using UnityEngine;

namespace Logic.PoolingSystem
{
    public interface IPooled : IComponent
    {
        public event Action<IPooled> OnFree;
        public void Free();
        void Configure();
    }
}