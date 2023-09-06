using BaseClasses.Enums;
using Infrastructure;
using Infrastructure.Services.StaticData;
using UnityEngine;

namespace BaseClasses.OnScenePoints
{
    public class OnSceneTowerPoint : MonoBehaviour
    {
        public TowerBaseType TowerBaseType;
        public WeaponType WeaponType;
        public float ShootInterval;
        public float Range;

        private float height = Constants.WEAPON_HEIGHT;

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position + height * Vector3.up;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, Range);
        }
    }
}