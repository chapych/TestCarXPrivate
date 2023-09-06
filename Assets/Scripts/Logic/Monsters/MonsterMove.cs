using System;
using BaseInterfaces.Gameplay;
using Extensions;
using Logic.PoolingSystem;
using Logic.Tower;
using UnityEngine;

namespace Logic
{
    public class MonsterMove : MonoBehaviour, ISpawnableRigidBody
    {
        private Rigidbody rigidbody;
        private TriggerObserver triggerObserver;
        [HideInInspector] public GameObject Target;
        [HideInInspector] public float Speed;

        public event Action<IPooled> OnFree;

        public void Configure()
        {
            rigidbody = GetComponent<Rigidbody>();
            triggerObserver = GetComponent<TriggerObserver>();

            triggerObserver.OnTrigger += TriggerObserverHandle;
        }

        public void Free() => OnFree?.Invoke(this);

        public void TriggerObserverHandle(GameObject obj)
        {
            if(obj != Target) return;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            OnFree?.Invoke(this);
        }

        public void SetMovementDirection(Vector3 direction)
        {
            rigidbody.AddForce(Speed * direction.normalized, ForceMode.VelocityChange);
        }

        private void OnDestroy()
        {
            triggerObserver.OnTrigger -= TriggerObserverHandle;
        }
    }
}