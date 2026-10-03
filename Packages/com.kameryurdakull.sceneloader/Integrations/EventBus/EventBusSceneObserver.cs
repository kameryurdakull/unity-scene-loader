using System;
using EventSystem;

namespace SceneLoader.Integrations
{
    /// <summary>Publishes synchronous typed notifications. SubscribeAsync is intentionally not awaited by transitions.</summary>
    public sealed class EventBusSceneObserver : ISceneTransitionObserver
    {
        private readonly IEventBus _eventBus;

        public EventBusSceneObserver(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void OnTransition(SceneTransition transition) => _eventBus.Publish(transition);
    }
}
