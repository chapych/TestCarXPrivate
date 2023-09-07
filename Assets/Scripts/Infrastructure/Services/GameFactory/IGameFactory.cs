using System.Threading.Tasks;
using BaseClasses.Enums;
using Infrastructure.Services.StaticData;
using UnityEngine;

namespace Infrastructure.Services.GameFactory
{
    public interface IGameFactory
    {
        Task<GameObject> CreateTower(TowerBaseType towerBaseType, WeaponType weaponType, Vector3 at, float range,
            float shootInterval);
        Task<GameObject> CreateSpawner(Vector3 at, float interval, Vector3 moveTargetPosition, float speed, int maxHp);
        void CleanUp();
    }
}