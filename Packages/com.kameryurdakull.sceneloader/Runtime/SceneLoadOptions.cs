using System;
using UnityEngine.SceneManagement;

namespace SceneLoader
{
    public sealed class SceneLoadOptions
    {
        public static SceneLoadOptions Default { get; } = new SceneLoadOptions();

        public LoadSceneMode Mode { get; }
        public bool ShowLoadingScreen { get; }
        public bool SetActiveScene { get; }
        public TimeSpan MinimumScreenDuration { get; }

        public SceneLoadOptions(LoadSceneMode mode = LoadSceneMode.Single, bool showLoadingScreen = true,
            bool setActiveScene = false, TimeSpan minimumScreenDuration = default)
        {
            if (mode != LoadSceneMode.Single && mode != LoadSceneMode.Additive)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (minimumScreenDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minimumScreenDuration));

            Mode = mode;
            ShowLoadingScreen = showLoadingScreen;
            SetActiveScene = setActiveScene;
            MinimumScreenDuration = minimumScreenDuration;
        }
    }
}
