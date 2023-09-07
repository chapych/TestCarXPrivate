using BaseClasses.Enums;
using Infrastructure.Services.StaticData;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Services.StaticDataService.StaticData
{
    [CreateAssetMenu(fileName = "New Tower Base Static Data", menuName = "Static Data/Tower Base Static Data")]
    public class TowerBaseStaticData : ScriptableObject
    {
        public TowerBaseType Type;
        public AssetReferenceGameObject PrefabReference;
    }
}