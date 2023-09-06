using Infrastructure.GameStateMachine.States;
using UnityEngine;
using Zenject;

namespace Infrastructure.GameBootstrapper
{
    public class GameBootstrapper : MonoBehaviour
    {
        private Game game;

        [Inject]
        public void Construct(Game game)
        {
            this.game = game;
        }

        private void Awake()
        {
            game.StateMachine.Enter<BootstrapGameState>();
        }
    }
}