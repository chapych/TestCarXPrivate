using System;
using BaseInterfaces;
using BaseInterfaces.Gameplay;
using Logic.PoolingSystem;
using UnityEngine;

namespace Logic.Tower.Base
{
    public abstract class ProjectileBase : MonoBehaviour, IPooled
    {
        private TriggerObserver triggerObserver;
        protected float m_speed;
        protected int m_damage;
        public IDamageable Target;
        public event Action<IPooled> OnFree;

        public void Construct(float speed, int damage)
        {
            m_speed = speed;
            m_damage = damage;
        }

        public virtual void Configure()
        {
            triggerObserver = GetComponent<TriggerObserver>();
            triggerObserver.OnTrigger += OnTriggerHandle;
        }

        protected virtual void Update()
        {
            if (Target.IsDead()) OnFreeAction(this);
        }
        private void OnTriggerHandle(GameObject other)
        {
            if(!other.TryGetComponent(out IDamageable monster))
                return;
            monster.GetDamage(m_damage);
            OnFree?.Invoke(this);
        }

        protected void OnFreeAction(ProjectileBase projectileBase) => OnFree?.Invoke(projectileBase);

        public void Free() => OnFree?.Invoke(this);

        private void OnDestroy()
        {
            triggerObserver.OnTrigger -= OnTriggerHandle;
        }
    }
}