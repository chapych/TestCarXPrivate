using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseInterfaces.Gameplay;
using Infrastructure.Services.CoroutineRunner;
using Infrastructure.Services.GameFactory;
using Infrastructure.Services.SceneLoaderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticData.PointsForStaticData;
using Infrastructure.Services.StaticDataService;
using Infrastructure.Services.StaticDataService.PointsForStaticData;
using Infrastructure.Services.StaticDataService.StaticData;
using Logic;
using Logic.Tower;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Infrastructure.GameStateMachine.States
{
    public class LoadLevelState : IPayloadedEnteringState<string>
    {
        private readonly IGameStateMachine stateMachine;
        private readonly ISceneLoader sceneLoader;
        private readonly IStaticDataService staticData;
        private readonly IGameFactory factory;
        private readonly ICoroutineRunnerProvider coroutineRunnerProvider;
        private Dictionary<Task<GameObject>, StopWatch> creatingMonsterSpawners;
        private Dictionary<Task<GameObject>, StopWatchDecoratorWithBool> creatingTowers;

        public LoadLevelState(IGameStateMachine stateMachine,
            ISceneLoader sceneLoader,
            IStaticDataService staticData,
            IGameFactory factory,
            ICoroutineRunnerProvider coroutineRunnerProvider)
        {
            this.stateMachine = stateMachine;
            this.sceneLoader = sceneLoader;
            this.staticData = staticData;
            this.factory = factory;
            this.coroutineRunnerProvider = coroutineRunnerProvider;
        }

        public void Enter(string scene)
        {
            sceneLoader.Load(scene, Init);
        }

        private async void InitGameWorld()
        {
            ICoroutineRunner coroutineRunner = coroutineRunnerProvider.GetCoroutineRunner();

            string name = SceneManager.GetActiveScene().name;
            LevelStaticData forLevel = staticData.ForLevel(name);
            creatingTowers = new Dictionary<Task<GameObject>, StopWatchDecoratorWithBool>(forLevel.TowerPoints.Count);
            creatingMonsterSpawners = new Dictionary<Task<GameObject>, StopWatch>(forLevel.SpawnerPoints.Count);
            foreach (TowerPoint towerPoint in forLevel.TowerPoints)
            {
                var creatingTower = factory.CreateTower(towerPoint.TowerBaseType, towerPoint.WeaponType, towerPoint.Position,
                    towerPoint.Range);

                creatingTowers.Add(creatingTower, new StopWatchDecoratorWithBool(towerPoint.ShootInterval, coroutineRunner));
            }

            foreach (SpawnerPoint spawner in forLevel.SpawnerPoints)
            {
                var creatingMonsterSpawner = factory.CreateSpawner(spawner.Position, spawner.MoveTargetPosition);
                creatingMonsterSpawners.Add(creatingMonsterSpawner, new StopWatch(spawner.Interval, coroutineRunner));
            }

            foreach (var monsterStopWatchPair in creatingMonsterSpawners)
            {
                GameObject temp = await monsterStopWatchPair.Key;
                ConfigureMonsterSpawner(monsterStopWatchPair);
            }
            foreach (var towerStopWatchPair in creatingTowers)
            {
                await towerStopWatchPair.Key;
                ConfigureTower(towerStopWatchPair);
            }
        }

        private void Init()
        {
            InitGameWorld();
        }

        private async void ConfigureMonsterSpawner(KeyValuePair<Task<GameObject>, StopWatch> monsterStopWatchPair)
        {
            var spawner = monsterStopWatchPair.Key.Result.GetComponent<ISpawner>();
            StopWatch stopwatch = monsterStopWatchPair.Value;

            await spawner.WarmUp();
            stopwatch.OnTime += spawner.Spawn;

            stopwatch.Run();
        }

        private async void ConfigureTower(KeyValuePair<Task<GameObject>, StopWatchDecoratorWithBool> towerStopWatchPair)
        {
            var spawner = towerStopWatchPair.Key.Result.GetComponentInChildren<ISpawner>();
            StopWatchDecoratorWithBool stopwatch = towerStopWatchPair.Value;
            var observer = towerStopWatchPair.Key.Result.GetComponentInChildren<IObserverInRange>();

            await spawner.WarmUp();
            stopwatch.OnTime += spawner.Spawn;
            observer.OnMonsterInRangeArea += stopwatch.SetBoolToTrue;

            stopwatch.Run();
        }

        public void Exit()
        {
            UnsubscribeSpawners();

            UnsubscribeTowers();
            factory.CleanUp();

        }

        private void UnsubscribeTowers()
        {
            foreach (var towerStopWatchPair in creatingTowers)
            {
                var spawner = towerStopWatchPair.Key.Result.GetComponent<ISpawner>();
                var stopwatch = towerStopWatchPair.Value;
                var observable = towerStopWatchPair.Key.Result.GetComponentInChildren<IObserverInRange>();

                stopwatch.Stop();
                stopwatch.OnTime -= spawner.Spawn;
                observable.OnMonsterInRangeArea -=
                    stopwatch.SetBoolToTrue;
            }
        }

        private void UnsubscribeSpawners()
        {
            foreach (var monsterStopWatchPair in creatingMonsterSpawners)
            {
                var spawner = monsterStopWatchPair.Key.Result.GetComponent<ISpawner>();
                StopWatch stopwatch = monsterStopWatchPair.Value;

                stopwatch.Stop();
                stopwatch.OnTime -= spawner.Spawn;
            }
        }

        public class Factory : PlaceholderFactory<IGameStateMachine, LoadLevelState> { }
    }
}