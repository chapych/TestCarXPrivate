using System;

namespace Infrastructure.Services.SceneLoaderService
{
    public interface ISceneLoader
    {
        void Load(string name, Action onLoaded = null);
        void Load(int index, Action onLoaded = null);
    }
}

