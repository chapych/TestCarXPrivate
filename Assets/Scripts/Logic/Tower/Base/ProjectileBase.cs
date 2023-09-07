using System;
using Logic.PoolingSystem;
using UnityEngine;

namespace Logic.Tower.Base
{
    public class ProjectileBase : MonoBehaviour, IPoolable<ProjectileBase>
    {
        private TriggerObserver m_triggerObserver;
        protected float Speed;
        protected int Damage;
        protected IDamageable Target;
        public event Action<ProjectileBase> OnFree;

        public void Construct(float speed, int damage, IDamageable target)
        {
            Speed = speed;
            Damage = damage;
            Target = target;
        }
        
        private void Awake()
        {
            m_triggerObserver = GetComponent<TriggerObserver>();
            m_triggerObserver.OnTrigger += OnTriggerHandle;
        }

        protected virtual void Update()
        {
            if (Target.IsDead())
            {
                OnFreeAction(this);
            }
        }

        private void OnTriggerHandle(GameObject other)
        {
            if(!other.TryGetComponent(out IDamageable monster))
                return;
            monster.GetDamage(Damage);
            OnFree?.Invoke(this);
        }

        protected void OnFreeAction(ProjectileBase projectileBase) => OnFree?.Invoke(projectileBase);

        private void OnDestroy()
        {
            m_triggerObserver.OnTrigger -= OnTriggerHandle;
        }
    }
}