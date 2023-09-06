using Infrastructure.GameStateMachine;

namespace Infrastructure.GameBootstrapper
{
    public class Game
    {
        public IGameStateMachine StateMachine { get; private set; }
        public Game(IGameStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
    }
}