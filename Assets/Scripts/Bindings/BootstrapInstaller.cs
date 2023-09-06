using System.Threading.Tasks;
using Infrastructure.GameBootstrapper;
using Infrastructure.GameStateMachine;
using Infrastructure.Services.AssetProviderService;
using Infrastructure.Services.CoroutineRunner;
using Infrastructure.Services.GameFactory;
using Infrastructure.Services.SceneLoader;
using Infrastructure.Services.SceneLoaderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService;
using Logic;
using Logic.PoolingSystem;
using UnityEngine;
using Zenject;

namespace Bindings
{
    public class BootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindGame();
            BindSceneLoader();
            BindGameStateMachine();
            BindAssetProvider();
            BindStaticDataService();
            BindGameFactory();
            BindCoroutineRunnerProvider();
        }

        private void BindCoroutineRunnerProvider()
        {
            Container.Bind<ICoroutineRunnerProvider>()
                .To<CoroutineRunnerProvider>()
                .AsSingle();
        }

        private void BindGameFactory()
        {
            Container.Bind<IGameFactory>()
                .To<GameFactory>()
                .AsSingle();
        }

        private void BindAssetProvider()
        {
            Container.Bind<IAssetProvider>()
                .To<AssetProvider>()
                .AsSingle();
        }

        private void BindStaticDataService()
        {
            Container.Bind<IStaticDataService>()
                .To<StaticDataService>()
                .AsSingle();
        }

        private void BindGame()
        {
            Container.Bind<Game>()
                .AsSingle();
        }
        private void BindGameStateMachine()
        {
            Container.Bind<IGameStateMachine>()
                .FromSubContainerResolve()
                .ByInstaller<GameStateMachineInstaller>()
                .AsSingle();
        }
        private void BindSceneLoader()
        {
            Container.Bind<ISceneLoader>()
                .To<SceneLoader>()
                .AsSingle();
        }
    }
}