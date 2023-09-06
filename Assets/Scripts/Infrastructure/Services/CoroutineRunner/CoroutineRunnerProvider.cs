using System.Threading.Tasks;
using Infrastructure.Services.GameFactory;
using Logic;
using UnityEngine;

namespace Infrastructure.Services.CoroutineRunner
{
    public class CoroutineRunnerProvider : ICoroutineRunnerProvider
    {
        private readonly IGameFactory factory;
        private ICoroutineRunner coroutineRunner;

        public CoroutineRunnerProvider(IGameFactory factory)
        {
            this.factory = factory;
        }

        public async Task Initialise()
        {
            GameObject gameObject = await factory.CreateCoroutineRunner();
            coroutineRunner = gameObject.GetComponent<ICoroutineRunner>();
            Object.DontDestroyOnLoad(gameObject);
        }

        public ICoroutineRunner GetCoroutineRunner() => coroutineRunner;
    }
}