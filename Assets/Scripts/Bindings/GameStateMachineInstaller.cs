using System.ComponentModel;
using Infrastructure.GameStateMachine;
using Infrastructure.GameStateMachine.States;
using Zenject;

namespace Bindings
{
    public class GameStateMachineInstaller : Installer<GameStateMachineInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindFactory<IGameStateMachine, BootstrapGameState, BootstrapGameState.Factory>();
            Container.BindFactory<IGameStateMachine, LoadLevelState, LoadLevelState.Factory>();

            Container.Bind<IGameStateMachine>().To<GameStateMachine>().AsSingle();
        }
    }
}