using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseClasses.Enums;
using Infrastructure.Services.AssetProviderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService.StaticData;
using UnityEngine;

namespace Infrastructure.Services.StaticDataService
{
    public class StaticDataService : IStaticDataService
    {
        private readonly IAssetProvider assetProvider;

        private Dictionary<string, LevelStaticData> levelsByKey;
        private Dictionary<TowerBaseType, TowerBaseStaticData> towerBasesByKey;
        private Dictionary<WeaponType, TowerWeaponStaticData> towerWeaponByKey;
        private Dictionary<MonsterType, MonsterStaticData> monsterByKey;
        private Dictionary<ProjectileType, ProjectileStaticData> projectileByKey;

        private string staticDataKey = "StaticData";

        public StaticDataService(IAssetProvider assetProvider)
        {
            this.assetProvider = assetProvider;
        }

        public async Task Load()
        {
            assetProvider.CleanUp();

            var levels = await assetProvider.LoadAllByKey<LevelStaticData>(staticDataKey);
            var towerBases = await assetProvider.LoadAllByKey<TowerBaseStaticData>(staticDataKey);
            var towerWeapons = await assetProvider.LoadAllByKey<TowerWeaponStaticData>(staticDataKey);
            var projectiles = await assetProvider.LoadAllByKey<ProjectileStaticData>(staticDataKey);
            var monsters = await assetProvider.LoadAllByKey<MonsterStaticData>(staticDataKey);

            levelsByKey = levels.ToDictionary(x => x.LevelName, x => x);
            towerBasesByKey = towerBases.ToDictionary(x => x.Type, x => x);
            towerWeaponByKey = towerWeapons.ToDictionary(x => x.Type, x => x);
            Debug.Log(monsters.Length);
            monsterByKey = monsters.ToDictionary(x => x.Type, x => x);
            projectileByKey = projectiles.ToDictionary(x => x.Type, x => x);
        }

        public LevelStaticData ForLevel(string level) => levelsByKey[level];
        public TowerBaseStaticData ForTowerBase(TowerBaseType type) => towerBasesByKey[type];
        public TowerWeaponStaticData ForTowerWeapon(WeaponType type) => towerWeaponByKey[type];
        public MonsterStaticData ForMonster(MonsterType monsterType) => monsterByKey[monsterType];
        public ProjectileStaticData ForProjectile(ProjectileType projectileType) => projectileByKey[projectileType];
    }
}


