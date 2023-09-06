using System.Threading.Tasks;
using BaseClasses.Enums;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService.StaticData;
using UnityEngine;

namespace Infrastructure.Services.GameFactory
{
    public interface IGameFactory
    {
        Task<GameObject> CreateTower(TowerBaseType towerBaseType, WeaponType weaponType, Vector3 at, float range);
        Task<GameObject> CreateSpawner(Vector3 at, Vector3 moveTargetPosition);
        Task<GameObject> CreateMonster(MonsterType monsterType);
        Task<GameObject> CreateProjectile(ProjectileType projectileType);
        GameObject CreateTarget(Vector3 at);
        Task<GameObject> CreateCoroutineRunner();
        void CleanUp();
        GameObject InstantiateFromPrefab(GameObject prefab);
    }
}