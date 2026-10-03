using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SceneLoader.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneLoader.Validation
{
    /// <summary>Development-host smoke test. Run in Play Mode with both host scenes enabled in the build list.</summary>
    public sealed class SceneLoaderSmokeRunner : MonoBehaviour, ISceneTransitionObserver
    {
        public const string PrimaryScenePath = "Assets/Scenes/SampleScene.unity";
        public const string SecondaryScenePath = "Assets/SceneLoaderValidation/SmokeScene.unity";
        public const string SuccessMessage = "[SceneLoader Smoke] PASS: additive, activation, unload, Single, cancellation, busy guard, DOTween at timeScale=0, last-scene guard.";
        private CancellationTokenSource _cancellation;
        private bool _cancelOnProgress;

        private void Awake() => DontDestroyOnLoad(gameObject);
        private void Start() => RunAsync().Forget(Debug.LogException);

        public void OnTransition(SceneTransition transition)
        {
            if (_cancelOnProgress && transition.State == SceneTransitionState.Progress)
                _cancellation.Cancel();
        }

        private async UniTask RunAsync()
        {
            var originalTimeScale = Time.timeScale;
            var screenObject = new GameObject(nameof(DOTweenLoadingScreen), typeof(Canvas), typeof(CanvasGroup));
            var screen = screenObject.AddComponent<DOTweenLoadingScreen>();
            var loader = new SceneLoaderService(new UnitySceneBackend(), screen, this);
            var primary = new SceneAddress(PrimaryScenePath);
            var secondary = new SceneAddress(SecondaryScenePath);
            try
            {
                Time.timeScale = 0f;
                var options = new SceneLoadOptions(LoadSceneMode.Additive, true, true);
                var loading = loader.LoadSceneAsync(secondary, options);
                Require(loader.IsBusy, "Transition should hold the busy lock during screen fade.");
                var rejected = false;
                try { await loader.LoadSceneAsync(primary); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Overlapping load was not rejected.");
                await loading;
                Require(SceneManager.GetActiveScene().path == secondary.Path, "Additive activation failed.");
                Require(screenObject.GetComponent<CanvasGroup>().alpha == 0f, "Screen did not hide.");
                await loader.UnloadSceneAsync(secondary);
                Require(!SceneManager.GetSceneByPath(secondary.Path).isLoaded, "Unload failed.");

                _cancellation = new CancellationTokenSource();
                _cancelOnProgress = true;
                var canceled = false;
                try { await loader.LoadSceneAsync(secondary, options, _cancellation.Token); }
                catch (OperationCanceledException) { canceled = true; }
                _cancelOnProgress = false;
                Require(canceled && !loader.IsBusy, "Submitted cancellation did not complete safely.");
                Require(SceneManager.GetSceneByPath(secondary.Path).isLoaded, "Canceled Unity load did not settle.");
                await loader.UnloadSceneAsync(secondary);

                await loader.LoadSceneAsync(primary);
                Require(SceneManager.GetActiveScene().path == primary.Path, "Single load failed.");
                rejected = false;
                try { await loader.UnloadSceneAsync(primary); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Last-scene unload was not rejected.");
                Debug.Log(SuccessMessage);
            }
            finally
            {
                _cancellation?.Dispose();
                Time.timeScale = originalTimeScale;
                Destroy(screenObject);
                Destroy(gameObject);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
