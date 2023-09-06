using BaseClasses.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Services.StaticDataService.StaticData
{
    [CreateAssetMenu(fileName = "New Monster Static Data", menuName = "Static Data/Monster Static Data")]
    public class MonsterStaticData : ScriptableObject
    {
        public MonsterType Type;
        public int MaxHealth;
        public float Speed;
        public AssetReferenceGameObject PrefabReference;
    }
}