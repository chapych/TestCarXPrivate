using System.Collections.Generic;
using System.Threading.Tasks;
using BaseClasses.Enums;
using Infrastructure.Services.AssetProviderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService;
using Infrastructure.Services.StaticDataService.StaticData;
using Logic;
using Logic.Tower;
using Logic.Tower.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Infrastructure.Services.GameFactory
{
    public class GameFactory : IGameFactory
    {
        private readonly IStaticDataService staticDataService;
        private readonly IAssetProvider assetProvider;

        private List<TriggerObserver> toUnsubscribe = new List<TriggerObserver>();
        private float weaponHeight = Constants.WEAPON_HEIGHT;
        private float spawnerHeight = Constants.SPAWNER_HEIGHT;

        public GameFactory(IStaticDataService staticDataService, IAssetProvider assetProvider)
        {
            this.staticDataService = staticDataService;
            this.assetProvider = assetProvider;
        }

        public async Task<GameObject> CreateTower(TowerBaseType towerBaseType, WeaponType weaponType, Vector3 at,
            float range)
        {
            Vector3 shift = weaponHeight * Vector3.up;
            TowerWeaponStaticData towerWeaponStaticData = staticDataService.ForTowerWeapon(weaponType);

            AssetReferenceGameObject towerBaseData = staticDataService.ForTowerBase(towerBaseType).PrefabReference;
            AssetReferenceGameObject weaponData = towerWeaponStaticData.PrefabReference;

            var towerBasePrefab = await assetProvider.Load<GameObject>(towerBaseData);
            var weaponPrefab = await assetProvider.Load<GameObject>(weaponData);

            GameObject towerBase = Object.Instantiate(towerBasePrefab, at, Quaternion.identity);
            GameObject weapon = InstantiateAsChild(weaponPrefab, towerBase, shift);

            ConstructWeapon(weapon, range, towerWeaponStaticData.ProjectileSpeed,
                towerWeaponStaticData.ProjectileDamage);

            return towerBase;
        }

        public async Task<GameObject> CreateSpawner(Vector3 at, Vector3 moveTargetPosition)
        {
            var prefab = await assetProvider.Load<GameObject>(AssetAddresses.SPAWNER);
            Vector3 spawnerShift = spawnerHeight * Vector3.up;
            GameObject instantiated = Object.Instantiate(prefab, at + spawnerShift, Quaternion.identity);
            var spawner = instantiated.GetComponent<MonsterSpawner>();

            spawner.Construct(moveTargetPosition,this);
            return instantiated;
        }

        public async Task<GameObject> CreateMonster(MonsterType monsterType)
        {
            MonsterStaticData monsterStaticData = staticDataService.ForMonster(monsterType);
            AssetReferenceGameObject monsterPrefabData = monsterStaticData.PrefabReference;

            var monsterPrefab = await assetProvider.Load<GameObject>(monsterPrefabData);
            GameObject monster = Object.Instantiate(monsterPrefab);

            monster.GetComponent<MonsterMove>().Speed = monsterStaticData.Speed;
            monster.GetComponent<Health>().Max = monsterStaticData.MaxHealth;

            return monster;
        }

        public async Task<GameObject> CreateProjectile(ProjectileType projectileType)
        {
            ProjectileStaticData projectileStaticData = staticDataService.ForProjectile(projectileType);
            AssetReferenceGameObject projectilePrefabData = projectileStaticData.PrefabReference;

            var projectilePrefab = await assetProvider.Load<GameObject>(projectilePrefabData);
            GameObject projectile = Object.Instantiate(projectilePrefab);

            projectile.GetComponent<ProjectileBase>().Construct(projectileStaticData.Speed, projectileStaticData.Damage);

            return projectile;
        }

        public GameObject CreateTarget(Vector3 at)
        {
            var moveTarget = new GameObject("Move Target");
            //prefab target
            moveTarget.transform.position = at;
            return moveTarget;
        }

        public async Task<GameObject> CreateCoroutineRunner()
        {
            var load = await assetProvider.Load<GameObject>(AssetAddresses.COROUTINE_RUNNER);
            return Object.Instantiate(load);
        }

        public GameObject InstantiateFromPrefab(GameObject prefab) => Object.Instantiate(prefab);

        public void CleanUp()
        {
            UnsubscribeTowerWeapon();
        }

        private GameObject InstantiateAsChild(GameObject prefab, GameObject parent, Vector3 shift)
        {
            GameObject weapon = Object.Instantiate(prefab, parent.transform, false);
            weapon.transform.localPosition = shift;
            return weapon;
        }

        private void ConstructWeapon(GameObject weapon, float range, float projectileSpeed,
            int projectileDamage)
        {
            var canon = weapon.GetComponent<TowerBase>();
            canon.Construct(projectileSpeed, projectileDamage, this);

            var triggerObserver = weapon.GetComponentInChildren<TriggerObserver>();
            triggerObserver.Radius = range;
            triggerObserver.OnTrigger += canon.OnInRangeArea;

            toUnsubscribe.Add(triggerObserver);
        }

        private void UnsubscribeTowerWeapon()
        {
            foreach (TriggerObserver observer in toUnsubscribe)
            {
                var canon = observer.GetComponent<IObserverInRange>();
                observer.OnTrigger -= canon.OnInRangeArea;
            }
        }
    }
}