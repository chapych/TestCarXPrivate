using System.Threading.Tasks;
using BaseClasses.Enums;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService.StaticData;

namespace Infrastructure.Services.StaticDataService
{
    public interface IStaticDataService
    {
        Task Load();
        LevelStaticData ForLevel(string level);
        public TowerBaseStaticData ForTowerBase(TowerBaseType type);
        public TowerWeaponStaticData ForTowerWeapon(WeaponType type);
    }
}