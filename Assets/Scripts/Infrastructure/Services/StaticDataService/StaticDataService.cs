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
        private readonly IAssetProvider m_assetProvider;

        private Dictionary<string, LevelStaticData> m_levelsByKey;
        private Dictionary<TowerBaseType, TowerBaseStaticData> m_towerBasesByKey;
        private Dictionary<WeaponType, TowerWeaponStaticData> m_towerWeaponByKey;

        private string m_staticDataKey = "StaticData";

        public StaticDataService(IAssetProvider assetProvider)
        {
            this.m_assetProvider = assetProvider;
        }

        public async Task Load()
        {
            m_assetProvider.CleanUp();

            var levels = await m_assetProvider.LoadAllByKey<LevelStaticData>(m_staticDataKey);
            var towerBases = await m_assetProvider.LoadAllByKey<TowerBaseStaticData>(m_staticDataKey);
            var towerWeapons = await m_assetProvider.LoadAllByKey<TowerWeaponStaticData>(m_staticDataKey);

            m_levelsByKey = levels.ToDictionary(x => x.LevelName, x => x);
            m_towerBasesByKey = towerBases.ToDictionary(x => x.Type, x => x);
            m_towerWeaponByKey = towerWeapons.ToDictionary(x => x.Type, x => x);
        }

        public LevelStaticData ForLevel(string level) => m_levelsByKey[level];
        public TowerBaseStaticData ForTowerBase(TowerBaseType type) => m_towerBasesByKey[type];
        public TowerWeaponStaticData ForTowerWeapon(WeaponType type) => m_towerWeaponByKey[type];
    }
}


