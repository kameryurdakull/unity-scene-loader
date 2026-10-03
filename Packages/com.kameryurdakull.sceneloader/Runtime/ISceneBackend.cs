using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace SceneLoader
{
    /// <summary>Extension point for alternative scene providers. Started operations must finish before returning.</summary>
    public interface ISceneBackend
    {
        void ValidateLoad(SceneAddress scene, LoadSceneMode mode);
        void ValidateUnload(SceneAddress scene);
        UniTask LoadAsync(SceneAddress scene, LoadSceneMode mode, IProgress<float> progress);
        UniTask UnloadAsync(SceneAddress scene);
        void SetActiveScene(SceneAddress scene);
    }
}
