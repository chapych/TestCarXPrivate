using System.Collections.Generic;
using Infrastructure.Services.StaticData.PointsForStaticData;
using Infrastructure.Services.StaticDataService.PointsForStaticData;
using UnityEngine;

namespace Infrastructure.Services.StaticDataService.StaticData
{
    [CreateAssetMenu(fileName = "New Level Static Data", menuName = "Static Data/Level Static Data")]
    public class LevelStaticData : ScriptableObject
    {
        public string LevelName;
        public List<TowerPoint> TowerPoints;
        public List<SpawnerPoint> SpawnerPoints;
    }
}