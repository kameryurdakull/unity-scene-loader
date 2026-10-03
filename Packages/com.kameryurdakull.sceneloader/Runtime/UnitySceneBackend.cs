using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneLoader
{
    public sealed class UnitySceneBackend : ISceneBackend
    {
        private const float ReadyToActivateProgress = 0.9f;

        public void ValidateLoad(SceneAddress scene, LoadSceneMode mode)
        {
            if (!scene.IsValid) throw new ArgumentException("Scene address is empty.", nameof(scene));
            if (!Application.CanStreamedLevelBeLoaded(scene.Path))
                throw new InvalidOperationException($"Scene '{scene.Path}' is missing or disabled in the build scene list.");
            if (mode == LoadSceneMode.Additive && SceneManager.GetSceneByPath(scene.Path).isLoaded)
                throw new InvalidOperationException($"Scene '{scene.Path}' is already loaded.");
        }

        public void ValidateUnload(SceneAddress scene)
        {
            if (!scene.IsValid) throw new ArgumentException("Scene address is empty.", nameof(scene));
            if (!SceneManager.GetSceneByPath(scene.Path).isLoaded)
                throw new InvalidOperationException($"Scene '{scene.Path}' is not loaded.");
            if (SceneManager.loadedSceneCount <= 1)
                throw new InvalidOperationException("The last loaded scene cannot be unloaded.");
        }

        public async UniTask LoadAsync(SceneAddress scene, LoadSceneMode mode, IProgress<float> progress)
        {
            var operation = SceneManager.LoadSceneAsync(scene.Path, mode);
            if (operation == null) throw new InvalidOperationException($"Unity could not start loading '{scene.Path}'.");
            var normalizedProgress = Progress.Create<float>(value =>
                progress?.Report(Mathf.Clamp01(value / ReadyToActivateProgress)));
            await operation.ToUniTask(progress: normalizedProgress);
            progress?.Report(1f);
        }

        public async UniTask UnloadAsync(SceneAddress scene)
        {
            var operation = SceneManager.UnloadSceneAsync(scene.Path);
            if (operation == null) throw new InvalidOperationException($"Unity could not start unloading '{scene.Path}'.");
            await operation.ToUniTask();
        }

        public void SetActiveScene(SceneAddress scene)
        {
            if (!SceneManager.SetActiveScene(SceneManager.GetSceneByPath(scene.Path)))
                throw new InvalidOperationException($"Unity could not activate '{scene.Path}'.");
        }
    }
}
