using Logic.PoolingSystem;
using UnityEngine;

namespace Logic.Tower.Base
{
    public abstract class TowerBase : MonoBehaviour, IObserverInRange
    {
        [SerializeField] protected ProjectileBase ProjectilePrefab;
        protected Pool<ProjectileBase> ProjectilePool;
        protected float Speed;
        protected int Damage;

        private StopWatch m_stopWatch;

        public void Construct(float interval, float speed, int damage)
        {
            m_stopWatch = new StopWatch(interval);
            this.Speed = speed;
            this.Damage = damage;

            ProjectilePool = new Pool<ProjectileBase>(ProjectilePrefab);
        }

        private void Start() => ProjectilePool.AddObjects(5);
        public void OnInRangeArea(GameObject observable)
        {
            if(observable.TryGetComponent(out IDamageable damageable))
                if(m_stopWatch.IsTimeForPeriodicAction()) Shoot(damageable);
        }

        protected abstract void Shoot(IDamageable target);
    }
}