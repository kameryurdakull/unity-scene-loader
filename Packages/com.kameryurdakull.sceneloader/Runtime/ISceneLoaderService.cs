using System.Threading;
using Cysharp.Threading.Tasks;

namespace SceneLoader
{
    public interface ISceneLoaderService
    {
        bool IsBusy { get; }
        UniTask LoadSceneAsync(SceneAddress scene, SceneLoadOptions options = null,
            CancellationToken cancellationToken = default);
        UniTask UnloadSceneAsync(SceneAddress scene, CancellationToken cancellationToken = default);
    }
}
