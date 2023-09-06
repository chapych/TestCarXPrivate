using System.Threading.Tasks;

namespace BaseInterfaces.Gameplay
{
    public interface ISpawner
    {
        void Spawn();
        Task WarmUp();
    }
}