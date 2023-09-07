using System.Threading.Tasks;
using Infrastructure.Services.AssetProviderService;
using Infrastructure.Services.StaticData;
using Infrastructure.Services.StaticDataService;
using UnityEngine;
using Zenject;

namespace Infrastructure.GameStateMachine.States
{
    public class BootstrapGameState : IEnteringState
    {
        private const string LEVEL_NAME = "ForTest";

        private readonly IGameStateMachine m_stateMachine;
        private readonly IStaticDataService m_staticDataService;
        private readonly IAssetProvider m_assetProvider;

        public BootstrapGameState(IGameStateMachine stateMachine, IStaticDataService staticDataService, IAssetProvider assetProvider)
        {
            this.m_stateMachine = stateMachine;
            this.m_staticDataService = staticDataService;
            this.m_assetProvider = assetProvider;
        }

        private async Task InitialiseServices()
        {
            m_assetProvider.Initialise();
            await m_staticDataService.Load();

        }
        public async Task Enter()
        {
            await InitialiseServices();
            m_stateMachine.Enter<LoadLevelState, string>(LEVEL_NAME);
        }

        public void Exit()
        {

        }

        public class Factory : PlaceholderFactory<IGameStateMachine, BootstrapGameState>
        {

        }
    }
}