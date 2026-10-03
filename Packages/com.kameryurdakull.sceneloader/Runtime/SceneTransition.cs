using System;

namespace SceneLoader
{
    public enum SceneOperation { Load, Unload }
    public enum SceneTransitionState { Started, Progress, Completed, Canceled, Failed }

    /// <summary>Progress describes loading only; Completed means loading and screen cleanup have finished.</summary>
    public readonly struct SceneTransition
    {
        public SceneAddress Scene { get; }
        public SceneOperation Operation { get; }
        public SceneTransitionState State { get; }
        public float Progress { get; }
        public Exception Error { get; }

        public SceneTransition(SceneAddress scene, SceneOperation operation, SceneTransitionState state,
            float progress = 0f, Exception error = null)
        {
            Scene = scene;
            Operation = operation;
            State = state;
            Progress = progress;
            Error = error;
        }
    }

    public interface ISceneTransitionObserver
    {
        void OnTransition(SceneTransition transition);
    }
}
