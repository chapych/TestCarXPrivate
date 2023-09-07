using Infrastructure.GameStateMachine.States;
using UnityEngine;
using Zenject;

namespace Infrastructure.GameBootstrapper
{
    public class GameBootstrapper : MonoBehaviour
    {
        private Game m_game;

        [Inject]
        public void Construct(Game game)
        {
            this.m_game = game;
        }

        private void Awake()
        {
            m_game.StateMachine.Enter<BootstrapGameState>();
        }
    }
}