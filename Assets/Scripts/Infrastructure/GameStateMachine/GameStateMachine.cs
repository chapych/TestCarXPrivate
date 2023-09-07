using System.Collections.Generic;
using Infrastructure.GameStateMachine.States;


namespace Infrastructure.GameStateMachine
{
    public class GameStateMachine : IGameStateMachine
    {
        private IExitableState m_active;
        private readonly Dictionary<System.Type, IExitableState> m_states;

        public GameStateMachine(BootstrapGameState.Factory bootstrapFactory,
            LoadLevelState.Factory loadLevelFactory)
        {
            m_states = new Dictionary<System.Type, IExitableState>
            {
                [typeof(BootstrapGameState)] = bootstrapFactory.Create(this),
                [typeof(LoadLevelState)] = loadLevelFactory.Create(this)
            };
        }

        public void Enter<TState>() where TState : class, IEnteringState
        {
            m_active?.Exit();
            IEnteringState state = GetState<TState>();
            m_active = state;
            state.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload)
            where TState : class, IPayloadedEnteringState<TPayload>
        {
            m_active?.Exit();
            IPayloadedEnteringState<TPayload> state = GetState<TState>();
            m_active = state;
            state.Enter(payload);
        }

        private TState GetState<TState>() where TState : class
        {
            return m_states[typeof(TState)] as TState;
        }
    }
}

