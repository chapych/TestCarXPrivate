using Infrastructure.Services.GameFactory;
using Infrastructure.Services.SceneLoaderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticData.PointsForStaticData;
using Infrastructure.Services.StaticDataService;
using Infrastructure.Services.StaticDataService.PointsForStaticData;
using Infrastructure.Services.StaticDataService.StaticData;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Infrastructure.GameStateMachine.States
{
    public class LoadLevelState : IPayloadedEnteringState<string>
    {
        private readonly IGameStateMachine m_stateMachine;
        private readonly ISceneLoader m_sceneLoader;
        private readonly IStaticDataService m_staticData;
        private readonly IGameFactory m_factory;

        public LoadLevelState(IGameStateMachine stateMachine,
            ISceneLoader sceneLoader,
            IStaticDataService staticData,
            IGameFactory factory)
        {
            this.m_stateMachine = stateMachine;
            this.m_sceneLoader = sceneLoader;
            this.m_staticData = staticData;
            this.m_factory = factory;
        }

        public void Enter(string scene)
        {
            m_sceneLoader.Load(scene, Init);
        }

        private void Init()
        {
            InitGameWorld();
        }

        private void InitGameWorld()
        {
            string name = SceneManager.GetActiveScene().name;
            LevelStaticData forLevel = m_staticData.ForLevel(name);

            foreach (TowerPoint towerPoint in forLevel.TowerPoints)
            {
                m_factory.CreateTower(towerPoint.TowerBaseType, towerPoint.WeaponType, towerPoint.Position,
                    towerPoint.Range, towerPoint.ShootInterval);
            }

            foreach (SpawnerPoint spawner in forLevel.SpawnerPoints)
            {
                m_factory.CreateSpawner(spawner.Position, spawner.Interval, spawner.MoveTargetPosition, spawner.Speed, spawner.MaxHp);
            }
        }

        public void Exit()
        {
            m_factory.CleanUp();
        }
        public class Factory : PlaceholderFactory<IGameStateMachine, LoadLevelState> { }
    }
}