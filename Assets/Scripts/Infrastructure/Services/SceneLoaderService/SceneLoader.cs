using System;
using Infrastructure.Services.SceneLoaderService;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infrastructure.Services.SceneLoader
{
    public class SceneLoader : ISceneLoader
    {
        public void Load(int index, Action onLoaded = null)
        {
            string name = SceneManager.GetSceneAt(index).name;
            LoadSceneAsync(name, onLoaded);
        }

        public void Load(string name, Action onLoaded = null) =>
            LoadSceneAsync(name, onLoaded);

        private void LoadSceneAsync(string nextScene, Action onLoaded)
        {
            if (SceneManager.GetActiveScene().name == nextScene)
            {
                onLoaded?.Invoke();
                return;
            }
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(nextScene);
            waitNextScene.completed += x => onLoaded();
        }
    }
}