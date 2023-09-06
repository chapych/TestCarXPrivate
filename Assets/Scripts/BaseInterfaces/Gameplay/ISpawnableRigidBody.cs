using Logic.PoolingSystem;
using UnityEngine;

namespace BaseInterfaces.Gameplay
{
    public interface ISpawnableRigidBody : IPooled
    {
        void SetMovementDirection(Vector3 direction);

    }

    public interface ISpawnableTransform : IPooled
    {
        void SetMovementTarget(Transform target);
    }

}