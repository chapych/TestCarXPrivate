using BaseClasses.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Services.StaticDataService.StaticData
{
    [CreateAssetMenu(fileName = "New Projectile Static Data", menuName = "Static Data/Projectile Static Data")]
    public class ProjectileStaticData : ScriptableObject
    {
        public ProjectileType Type;
        public float Speed;
        public int Damage;
        public AssetReferenceGameObject PrefabReference;
    }
}