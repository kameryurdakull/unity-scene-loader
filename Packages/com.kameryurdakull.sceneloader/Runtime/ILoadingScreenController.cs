using System.Threading;
using Cysharp.Threading.Tasks;

namespace SceneLoader
{
    /// <summary>Implementations must survive Single loads and support HideAsync after a partial ShowAsync.</summary>
    public interface ILoadingScreenController
    {
        UniTask ShowAsync(CancellationToken cancellationToken);
        UniTask HideAsync();
        void UpdateProgress(float progress);
    }
}
