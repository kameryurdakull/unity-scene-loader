using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace SceneLoader.Tests
{
    public sealed class SceneLoaderServiceTests
    {
        private const string TestScenePath = "Assets/Scenes/Test.unity";
        private static SceneAddress TestScene => new SceneAddress(TestScenePath);

        [Test]
        public void ExplicitDefaultOptionsMatchDefaultCall()
        {
            var options = new SceneLoadOptions();
            Assert.That(options.ShowLoadingScreen, Is.True);
            Assert.That(options.Mode, Is.EqualTo(LoadSceneMode.Single));
            Assert.That(options.MinimumScreenDuration, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void InvalidScenePathIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new SceneAddress("MainMenu"));
            Assert.Throws<ArgumentException>(() => new SceneAddress(string.Empty));
            Assert.That(default(SceneAddress).IsValid, Is.False);
        }

        [Test]
        public void DefaultCallShowsScreenAndReportsCompletionAfterCleanup()
        {
            var backend = new FakeBackend();
            var screen = new FakeScreen();
            var observer = new RecordingObserver();
            var loader = new SceneLoaderService(backend, screen, observer);
            observer.OnNotification = transition =>
            {
                if (transition.State != SceneTransitionState.Completed) return;
                Assert.That(screen.HideCount, Is.EqualTo(1));
                Assert.That(loader.IsBusy, Is.False);
            };

            loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult();

            Assert.That(screen.ShowCount, Is.EqualTo(1));
            Assert.That(screen.LastProgress, Is.EqualTo(1f));
            Assert.That(backend.Mode, Is.EqualTo(LoadSceneMode.Single));
            Assert.That(observer.Events[observer.Events.Count - 1].State, Is.EqualTo(SceneTransitionState.Completed));
        }

        [Test]
        public void HiddenScreenReceivesNoCallsAndAdditiveCanBeActivated()
        {
            var backend = new FakeBackend();
            var screen = new FakeScreen();
            var loader = new SceneLoaderService(backend, screen);
            var options = new SceneLoadOptions(LoadSceneMode.Additive, false, true);

            loader.LoadSceneAsync(TestScene, options).GetAwaiter().GetResult();

            Assert.That(screen.ShowCount + screen.HideCount + screen.ProgressCount, Is.Zero);
            Assert.That(backend.Mode, Is.EqualTo(LoadSceneMode.Additive));
            Assert.That(backend.Activated, Is.True);
        }

        [Test]
        public void PartialShowFailureStillHidesAndReleasesLock()
        {
            var screen = new FakeScreen { ShowFailure = new InvalidOperationException() };
            var backend = new FakeBackend();
            var observer = new RecordingObserver();
            var loader = new SceneLoaderService(backend, screen, observer);

            Assert.Throws<InvalidOperationException>(() => loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult());

            Assert.That(screen.HideCount, Is.EqualTo(1));
            Assert.That(backend.LoadCount, Is.Zero);
            Assert.That(loader.IsBusy, Is.False);
            Assert.That(observer.Events[observer.Events.Count - 1].State, Is.EqualTo(SceneTransitionState.Failed));
        }

        [Test]
        public void BackendFailureHidesAndNextRequestCanSucceed()
        {
            var backend = new FakeBackend { LoadFailure = new InvalidOperationException() };
            var screen = new FakeScreen();
            var loader = new SceneLoaderService(backend, screen);
            Assert.Throws<InvalidOperationException>(() => loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult());
            backend.LoadFailure = null;
            loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult();
            Assert.That(screen.HideCount, Is.EqualTo(2));
            Assert.That(loader.IsBusy, Is.False);
        }

        [Test]
        public void HideFailureIsReportedAndReleasesLock()
        {
            var screen = new FakeScreen { HideFailure = new InvalidOperationException() };
            var observer = new RecordingObserver();
            var loader = new SceneLoaderService(new FakeBackend(), screen, observer);
            Assert.Throws<InvalidOperationException>(() => loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult());
            Assert.That(loader.IsBusy, Is.False);
            Assert.That(observer.Events[observer.Events.Count - 1].State, Is.EqualTo(SceneTransitionState.Failed));
        }

        [Test]
        public void ConcurrentLoadAndUnloadAreRejectedUntilCurrentLoadFinishes()
        {
            var pending = new UniTaskCompletionSource();
            var backend = new FakeBackend { Pending = pending };
            var loader = new SceneLoaderService(backend);
            var first = loader.LoadSceneAsync(TestScene);
            Assert.That(loader.IsBusy, Is.True);
            Assert.Throws<InvalidOperationException>(() => loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() => loader.UnloadSceneAsync(TestScene).GetAwaiter().GetResult());
            pending.TrySetResult();
            first.GetAwaiter().GetResult();
            Assert.That(loader.IsBusy, Is.False);
        }

        [Test]
        public void CancellationAfterSubmissionWaitsForCompletionAndCleanup()
        {
            var pending = new UniTaskCompletionSource();
            var screen = new FakeScreen();
            var observer = new RecordingObserver();
            var loader = new SceneLoaderService(new FakeBackend { Pending = pending }, screen, observer);
            using var cancellation = new CancellationTokenSource();
            var task = loader.LoadSceneAsync(TestScene, cancellationToken: cancellation.Token);
            cancellation.Cancel();
            Assert.That(loader.IsBusy, Is.True);
            Assert.That(screen.HideCount, Is.Zero);
            pending.TrySetResult();
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.That(screen.HideCount, Is.EqualTo(1));
            Assert.That(loader.IsBusy, Is.False);
            Assert.That(observer.Events[observer.Events.Count - 1].State, Is.EqualTo(SceneTransitionState.Canceled));
        }

        [Test]
        public void PreCanceledRequestDoesNotStartTransition()
        {
            var backend = new FakeBackend();
            var screen = new FakeScreen();
            var observer = new RecordingObserver();
            var loader = new SceneLoaderService(backend, screen, observer);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => loader.LoadSceneAsync(TestScene,
                cancellationToken: cancellation.Token).GetAwaiter().GetResult());
            Assert.That(screen.ShowCount, Is.Zero);
            Assert.That(backend.LoadCount, Is.Zero);
            Assert.That(observer.Events, Is.Empty);
        }

        [Test]
        public void InvalidBuildSceneFailsBeforeShowingScreen()
        {
            var screen = new FakeScreen();
            var loader = new SceneLoaderService(new UnitySceneBackend(), screen);
            Assert.Throws<InvalidOperationException>(() => loader.LoadSceneAsync(TestScene).GetAwaiter().GetResult());
            Assert.That(screen.ShowCount, Is.Zero);
            Assert.That(loader.IsBusy, Is.False);
        }

        [Test]
        public void UnloadedSceneCannotBeUnloaded()
        {
            var backend = new UnitySceneBackend();
            Assert.Throws<InvalidOperationException>(() => backend.ValidateUnload(TestScene));
        }

        private sealed class FakeBackend : ISceneBackend
        {
            public UniTaskCompletionSource Pending;
            public Exception LoadFailure;
            public LoadSceneMode Mode;
            public int LoadCount;
            public bool Activated;
            public void ValidateLoad(SceneAddress scene, LoadSceneMode mode) { }
            public void ValidateUnload(SceneAddress scene) { }
            public UniTask LoadAsync(SceneAddress scene, LoadSceneMode mode, IProgress<float> progress)
            {
                LoadCount++;
                Mode = mode;
                if (LoadFailure != null) return UniTask.FromException(LoadFailure);
                progress.Report(1f);
                return Pending?.Task ?? UniTask.CompletedTask;
            }
            public UniTask UnloadAsync(SceneAddress scene) => Pending?.Task ?? UniTask.CompletedTask;
            public void SetActiveScene(SceneAddress scene) => Activated = true;
        }

        private sealed class FakeScreen : ILoadingScreenController
        {
            public Exception ShowFailure;
            public Exception HideFailure;
            public int ShowCount;
            public int HideCount;
            public int ProgressCount;
            public float LastProgress;
            public UniTask ShowAsync(CancellationToken cancellationToken)
            {
                ShowCount++;
                return ShowFailure == null ? UniTask.CompletedTask : UniTask.FromException(ShowFailure);
            }
            public UniTask HideAsync()
            {
                HideCount++;
                return HideFailure == null ? UniTask.CompletedTask : UniTask.FromException(HideFailure);
            }
            public void UpdateProgress(float progress)
            {
                ProgressCount++;
                LastProgress = progress;
            }
        }

        private sealed class RecordingObserver : ISceneTransitionObserver
        {
            public readonly List<SceneTransition> Events = new List<SceneTransition>();
            public Action<SceneTransition> OnNotification;
            public void OnTransition(SceneTransition transition)
            {
                Events.Add(transition);
                OnNotification?.Invoke(transition);
            }
        }
    }
}
