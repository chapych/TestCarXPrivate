using System.Collections.Generic;
using System.Threading.Tasks;
using BaseClasses.Enums;
using BaseInterfaces.Gameplay;
using Infrastructure.Services.AssetProviderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService;
using Logic;
using Logic.Tower;
using Logic.Tower.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Infrastructure.Services.GameFactory
{
    public class GameFactory : IGameFactory
    {
        private readonly IStaticDataService m_staticDataService;
        private readonly IAssetProvider m_assetProvider;

        private List<TriggerObserver> m_toUnsubscribe = new List<TriggerObserver>();
        private float m_weaponHeight = Constants.WEAPON_HEIGHT;
        private float m_spawnerHeight = Constants.SPAWNER_HEIGHT;

        public GameFactory(IStaticDataService staticDataService, IAssetProvider assetProvider)
        {
            this.m_staticDataService = staticDataService;
            this.m_assetProvider = assetProvider;
        }

        public async Task<GameObject> CreateTower(TowerBaseType towerBaseType, WeaponType weaponType, Vector3 at,
            float range, float shootInterval)
        {
            Vector3 shift = m_weaponHeight * Vector3.up;
            AssetReferenceGameObject towerBaseData = m_staticDataService.ForTowerBase(towerBaseType).PrefabReference;
            AssetReferenceGameObject weaponData = m_staticDataService.ForTowerWeapon(weaponType).PrefabReference;

            var towerBasePrefab = await m_assetProvider.Load<GameObject>(towerBaseData);
            var weaponPrefab = await m_assetProvider.Load<GameObject>(weaponData);

            GameObject towerBase = Object.Instantiate(towerBasePrefab, at, Quaternion.identity);
            GameObject weapon = InstantiateAsChild(weaponPrefab, towerBase, shift);
            ConstructWeapon(weapon, range, shootInterval, m_staticDataService.ForTowerWeapon(weaponType).ProjectileSpeed,
                m_staticDataService.ForTowerWeapon(weaponType).ProjectileDamage);

            return towerBase;
        }

        private static GameObject InstantiateAsChild(GameObject prefab, GameObject parent, Vector3 shift)
        {
            GameObject weapon = Object.Instantiate(prefab, parent.transform, false);
            weapon.transform.localPosition = shift;
            return weapon;
        }

        private void ConstructWeapon(GameObject weapon, float range, float shootInterval, float projectileSpeed,
            int projectileDamage)
        {
            var canon = weapon.GetComponent<TowerBase>();
            canon.Construct(shootInterval, projectileSpeed, projectileDamage);

            var triggerObserver = weapon.GetComponentInChildren<TriggerObserver>();
            triggerObserver.Radius = range;
            triggerObserver.OnTrigger += canon.OnInRangeArea;

            m_toUnsubscribe.Add(triggerObserver);
        }

        public async Task<GameObject> CreateSpawner(Vector3 at, float interval, Vector3 moveTargetPosition,
                                                    float speed, int maxHp)
        {
            var prefab = await m_assetProvider.Load<GameObject>(AssetAddresses.SPAWNER);
            Vector3 spawnerShift = m_spawnerHeight * Vector3.up;
            GameObject instantiated = Object.Instantiate(prefab, at + spawnerShift, Quaternion.identity);
            var spawner = instantiated.GetComponent<ISpawner>();

            spawner.Construct(interval, moveTargetPosition, speed, maxHp);
            return instantiated;
        }

        public void CleanUp()
        {
            UnsubscribeTowerWeapon();
        }

        private void UnsubscribeTowerWeapon()
        {
            foreach (TriggerObserver observer in m_toUnsubscribe)
            {
                var canon = observer.GetComponent<IObserverInRange>();
                observer.OnTrigger -= canon.OnInRangeArea;
            }
        }
    }
}