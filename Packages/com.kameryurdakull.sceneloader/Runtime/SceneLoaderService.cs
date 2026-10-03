using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SceneLoader
{
    /// <summary>Use one instance in a persistent root scope. All calls belong on Unity's main thread.</summary>
    public sealed class SceneLoaderService : ISceneLoaderService
    {
        private readonly ISceneBackend _backend;
        private readonly ILoadingScreenController _loadingScreen;
        private readonly ISceneTransitionObserver _observer;

        public bool IsBusy { get; private set; }

        public SceneLoaderService(ISceneBackend backend, ILoadingScreenController loadingScreen = null,
            ISceneTransitionObserver observer = null)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _loadingScreen = loadingScreen;
            _observer = observer;
        }

        public async UniTask LoadSceneAsync(SceneAddress scene, SceneLoadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            EnsureAvailable(cancellationToken);
            var settings = options ?? SceneLoadOptions.Default;
            _backend.ValidateLoad(scene, settings.Mode);
            IsBusy = true;
            var useScreen = settings.ShowLoadingScreen && _loadingScreen != null;
            var startedAt = Time.realtimeSinceStartupAsDouble;
            var failure = default(Exception);
            Notify(new SceneTransition(scene, SceneOperation.Load, SceneTransitionState.Started));

            try
            {
                if (useScreen) await _loadingScreen.ShowAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var progress = Progress.CreateOnlyValueChanged<float>(value =>
                {
                    if (useScreen) UpdateScreenProgress(value);
                    Notify(new SceneTransition(scene, SceneOperation.Load, SceneTransitionState.Progress, value));
                });

                // Unity cannot cancel a submitted scene load. Keep ownership until activation completes.
                await _backend.LoadAsync(scene, settings.Mode, progress);
                if (settings.SetActiveScene) _backend.SetActiveScene(scene);
                cancellationToken.ThrowIfCancellationRequested();

                var remaining = settings.MinimumScreenDuration.TotalSeconds -
                    (Time.realtimeSinceStartupAsDouble - startedAt);
                if (useScreen && remaining > 0d)
                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), ignoreTimeScale: true,
                        cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                try
                {
                    if (useScreen) await _loadingScreen.HideAsync();
                }
                catch (Exception exception)
                {
                    if (failure != null) Debug.LogException(exception);
                    else
                    {
                        failure = exception;
                        throw;
                    }
                }
                finally
                {
                    IsBusy = false;
                    NotifyTerminal(scene, SceneOperation.Load, failure);
                }
            }
        }

        public async UniTask UnloadSceneAsync(SceneAddress scene, CancellationToken cancellationToken = default)
        {
            EnsureAvailable(cancellationToken);
            _backend.ValidateUnload(scene);
            IsBusy = true;
            var failure = default(Exception);
            Notify(new SceneTransition(scene, SceneOperation.Unload, SceneTransitionState.Started));
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _backend.UnloadAsync(scene);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                IsBusy = false;
                NotifyTerminal(scene, SceneOperation.Unload, failure);
            }
        }

        private void EnsureAvailable(CancellationToken cancellationToken)
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("Scene transitions must be requested on Unity's main thread.");
            cancellationToken.ThrowIfCancellationRequested();
            if (IsBusy) throw new InvalidOperationException("A scene transition is already in progress.");
        }

        private void UpdateScreenProgress(float progress)
        {
            try { _loadingScreen.UpdateProgress(progress); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void NotifyTerminal(SceneAddress scene, SceneOperation operation, Exception failure)
        {
            var state = failure == null ? SceneTransitionState.Completed :
                failure is OperationCanceledException ? SceneTransitionState.Canceled : SceneTransitionState.Failed;
            Notify(new SceneTransition(scene, operation, state, failure == null ? 1f : 0f, failure));
        }

        private void Notify(SceneTransition transition)
        {
            try { _observer?.OnTransition(transition); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
