namespace Infrastructure.GameStateMachine
{
    public interface IPayloadedEnteringState<TPayload> : IExitableState
    {
        void Enter(TPayload activeState);
    }
}