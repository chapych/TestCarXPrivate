namespace Infrastructure.GameStateMachine
{
    public interface IGameStateMachine
    {
        void Enter<TState>() where TState : class, IEnteringState;
        void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedEnteringState<TPayload>;
    }
}