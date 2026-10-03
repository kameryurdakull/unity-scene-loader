using System;
using VContainer;

namespace SceneLoader.Integrations
{
    public static class SceneLoaderRegistration
    {
        /// <summary>Call once in the persistent root scope. Pass a custom backend to replace Unity scene loading.</summary>
        public static void RegisterSceneLoader(this IContainerBuilder builder,
            ILoadingScreenController loadingScreen = null, ISceneTransitionObserver observer = null,
            ISceneBackend backend = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.RegisterInstance<ISceneLoaderService>(
                new SceneLoaderService(backend ?? new UnitySceneBackend(), loadingScreen, observer));
        }
    }
}
