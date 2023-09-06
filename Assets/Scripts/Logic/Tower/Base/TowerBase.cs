using System;
using System.Threading.Tasks;
using BaseClasses.Enums;
using BaseInterfaces.Gameplay;
using Infrastructure.Services.GameFactory;
using Logic.PoolingSystem;
using UnityEngine;

namespace Logic.Tower.Base
{
    public abstract class TowerBase : MonoBehaviour, IObserverInRange, ISpawner
    {
        private ProjectileType projectileType;
        private IDamageable damageable;
        protected float Speed;
        protected int Damage;

        protected Pool ProjectilePool;
        private int preCreatedNumber = 5;

        public event Action OnMonsterInRangeArea;

        public void Construct(float speed, int damage, IGameFactory factory)
        {
            this.Speed = speed;
            this.Damage = damage;

            var instantiatingFunction = new Func<Task<GameObject>>(() => factory.CreateProjectile(projectileType));
            ProjectilePool = new Pool(instantiatingFunction);
        }

        public async Task WarmUp() => await ProjectilePool.AddObjects(preCreatedNumber);

        public void OnInRangeArea(GameObject observable)
        {
            if (observable.TryGetComponent(out IDamageable damageable))
            {
                this.damageable = damageable;
                OnMonsterInRangeArea?.Invoke();
            }
        }
        public void Spawn() => Shoot(damageable);
        protected abstract void Shoot(IDamageable target);
    }
}