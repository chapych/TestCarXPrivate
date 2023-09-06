using System;
using UnityEngine;

namespace Logic.Tower
{
    [RequireComponent(typeof(SphereCollider))]
    public class TriggerObserver : MonoBehaviour
    {
        [SerializeField] private SphereCollider sphereCollider;
        public float Radius
        {
            set => sphereCollider.radius = value;
        }

        public event Action<GameObject> OnTrigger;
        private void OnTriggerEnter(Collider other)
        {
            OnTrigger?.Invoke(other.gameObject);
        }
    }
}