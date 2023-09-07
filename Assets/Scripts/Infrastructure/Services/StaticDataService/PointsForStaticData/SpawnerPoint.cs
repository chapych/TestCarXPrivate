using System;
using UnityEngine;

namespace Infrastructure.Services.StaticDataService.PointsForStaticData
{
    [Serializable]
    public class SpawnerPoint
    {
        public Vector3 Position;
        public float Interval;
        public Vector3 MoveTargetPosition;
        public float Speed;
        public int MaxHp;

        public SpawnerPoint(Vector3 position, float interval, Vector3 moveTarget, float speed, int maxHp)
        {
            Position = position;
            Interval = interval;
            MoveTargetPosition = moveTarget;
            Speed = speed;
            MaxHp = maxHp;
        }
    }
}