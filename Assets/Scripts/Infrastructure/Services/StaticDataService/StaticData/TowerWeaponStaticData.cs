using Infrastructure.Services.StaticData;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Services.StaticDataService.StaticData
{
    [CreateAssetMenu(fileName = "New Tower Weapon Static Data", menuName = "Static Data/Tower Weapon Static Data")]
    public class TowerWeaponStaticData : ScriptableObject
    {
        public WeaponType Type;
        public float ProjectileSpeed = 0.2f;
        public int ProjectileDamage = 10;
        public AssetReferenceGameObject PrefabReference;
    }
}