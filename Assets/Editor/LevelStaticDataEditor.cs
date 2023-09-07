using System.Linq;
using BaseClasses.OnScenePoints;
using Infrastructure.Services.StaticData.PointsForStaticData;
using Infrastructure.Services.StaticDataService.PointsForStaticData;
using Infrastructure.Services.StaticDataService.StaticData;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [CustomEditor(typeof(LevelStaticData))]
    public class LevelStaticDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var levelStaticData = (LevelStaticData) target;

            if (GUILayout.Button("Collect"))
            {
                levelStaticData.SpawnerPoints = FindObjectsOfType<OnSceneSpawnerPoint>()
                    .Select(x=> new SpawnerPoint(x.transform.position, x.Interval, x.MoveTarget.transform.position, x.Speed, x.MaxHp))
                    .ToList();
                levelStaticData.TowerPoints = FindObjectsOfType<OnSceneTowerPoint>()
                    .Select(x => new TowerPoint(x.transform.position, x.TowerBaseType, x.WeaponType, x.Range, x.ShootInterval))
                    .ToList();

                levelStaticData.LevelName = SceneManager.GetActiveScene().name;
            }
            EditorUtility.SetDirty(target);
        }
    }
}