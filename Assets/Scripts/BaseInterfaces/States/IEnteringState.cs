using System.Threading.Tasks;

namespace Infrastructure.GameStateMachine
{
    public interface IEnteringState : IExitableState
    {
        Task Enter();
    }
}