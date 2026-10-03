using System;
using EventSystem;
using SceneLoader.Integrations;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SceneLoader.Samples
{
    public sealed class SceneLoaderLifetimeScope : LifetimeScope
    {
        [Tooltip("Optional persistent component implementing ILoadingScreenController.")]
        [SerializeField] private MonoBehaviour loadingScreen;

        protected override void Awake()
        {
            if (transform.parent != null)
                throw new InvalidOperationException("Use a root GameObject for the global LifetimeScope.");
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (loadingScreen != null && !(loadingScreen is ILoadingScreenController))
                throw new InvalidOperationException("Loading screen must implement ILoadingScreenController.");

            var eventBus = new EventBus();
            builder.RegisterInstance<IEventBus>(eventBus);
            builder.RegisterSceneLoader(loadingScreen as ILoadingScreenController,
                new EventBusSceneObserver(eventBus));
            builder.RegisterEntryPoint<TransitionLogger>();
        }
    }

    internal sealed class TransitionLogger : IStartable, IDisposable
    {
        private readonly IEventBus _eventBus;

        public TransitionLogger(IEventBus eventBus) => _eventBus = eventBus;
        public void Start() => _eventBus.Subscribe<SceneTransition>(OnTransition);
        public void Dispose() => _eventBus.Unsubscribe<SceneTransition>(OnTransition);

        private static void OnTransition(SceneTransition transition)
        {
            if (transition.State == SceneTransitionState.Completed)
                Debug.Log($"{transition.Operation} completed: {transition.Scene}");
        }
    }
}
