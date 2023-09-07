using System;
using BaseClasses.Enums;
using UnityEngine;

namespace Infrastructure.Services.StaticData.PointsForStaticData
{
    [Serializable]
    public class TowerPoint
    {
        public Vector3 Position;
        public TowerBaseType TowerBaseType;
        public WeaponType WeaponType;
        public float Range;
        public float ShootInterval;

        public TowerPoint(Vector3 position, TowerBaseType towerBaseType, WeaponType weaponType, float range, float shootIntreval)
        {
            WeaponType = weaponType;
            TowerBaseType = towerBaseType;
            Position = position;
            Range = range;
            ShootInterval = shootIntreval;
        }
    }
}