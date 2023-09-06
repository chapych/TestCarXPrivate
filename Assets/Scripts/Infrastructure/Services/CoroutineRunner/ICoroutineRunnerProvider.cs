using System.Threading.Tasks;
using Logic;

namespace Infrastructure.Services.CoroutineRunner
{
    public interface ICoroutineRunnerProvider
    {
        Task Initialise();
        ICoroutineRunner GetCoroutineRunner();
    }
}