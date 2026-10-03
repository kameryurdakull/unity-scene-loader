# Unity Scene Loader

A small, reusable scene transition service for Unity. Await transitions with **UniTask**, choose scenes through **asset references**, and keep loading UI separate from scene loading.

```csharp
await sceneLoader.LoadSceneAsync(mainMenu.Address);
```

The core has no static singleton, project-specific scene enum, mandatory DI container, or built-in UI prefab. Create one service for your game, or register it once in a persistent VContainer root scope. Share that instance across scenes.

## Features

- Single and additive loads, additive activation, and explicit unload.
- ScriptableObject scene references with scene picker and build-list validation.
- GUID-based reference updates when scenes move or are renamed in the Editor.
- Optional loading screen with normalized loading progress and configurable minimum display time.
- Concurrent request rejection, cancellation handling, and cleanup on failures.
- Optional VContainer registration and typed Event Bus notifications.
- Replaceable scene backend and loading screen interfaces.
- DOTween loading screen and VContainer bootstrap samples.
- No coroutines, `while` loops, forced garbage collection, or fixed transition delay.

## Requirements

| Component | Required for | Version used in this repository |
| --- | --- | --- |
| Unity | All usage | 6000.3.15f1 |
| UniTask | Core | 2.5.11 |
| VContainer | Optional registration / bootstrap sample | 1.19.0 |
| Event Bus | Optional event adapter / bootstrap sample | 1.0.0 |
| DOTween | Optional loading screen sample | Your installed DOTween DLL |
| uGUI | Optional loading screen sample | Host project's compatible version |

The package declares Unity **2022.3** as its minimum API target. Execution and compilation verification are performed on the Unity version above; older versions are not claimed as tested.

## Installation

Install UniTask first using **Window > Package Manager > + > Install package from Git URL**:

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11
```

Then install Scene Loader:

```text
https://github.com/kameryurdakull/unity-scene-loader.git?path=/Packages/com.kameryurdakull.sceneloader
```

After the first release is published, pin the package to the release tag:

```text
https://github.com/kameryurdakull/unity-scene-loader.git?path=/Packages/com.kameryurdakull.sceneloader#v1.0.0
```

The tagged URL becomes usable only after that tag exists on GitHub. The untagged URL uses the repository's default branch.

Unity supports Git dependencies in the **project** manifest, not inside a package's dependency manifest. Consequently UniTask and optional Git integrations must be installed explicitly; this package does not download third-party code through an Editor script. See [Unity's Git dependency documentation](https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/upm-git).

Optional integration URLs:

```text
https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.19.0
https://github.com/kameryurdakull/unity-eventbus.git?path=/Packages/com.kameryurdakull.eventbus#v1.0.0
```

Event Bus itself requires UniTask and VContainer. Install these before adding Event Bus. For the loading screen sample, install DOTween from Demigiant and run its setup utility. DOTween is not a dependency of the core package and is not bundled inside the distributable package.

## Scene setup

1. Create a reference with **Assets > Create > Scene Loader > Scene Reference**.
2. Select a `.unity` scene in its Inspector.
3. Use **Enable in Build Scene List** if the Inspector reports a missing or disabled scene.
4. Assign the reference to your gameplay or menu component.

The reference stores a full scene path, so duplicate scene names in different folders are supported. GUIDs are used by the Editor to synchronize paths after moves and before builds. Deleting a scene clears its resolved path. In Unity 6, also check the active Build Profile's scene list when it overrides the global build scene list; the runtime checks actual scene availability.

Scene references do not automatically add every referenced scene to builds. Keep control of build content in your project.

## Core API

```csharp
using Cysharp.Threading.Tasks;
using SceneLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSceneFlow
{
    private readonly ISceneLoaderService _sceneLoader;

    public GameSceneFlow(ISceneLoaderService sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    public UniTask OpenMenuAsync(SceneReference menu) =>
        _sceneLoader.LoadSceneAsync(menu.Address);

    public UniTask OpenAdditiveAsync(SceneReference environment)
    {
        var options = new SceneLoadOptions(
            mode: LoadSceneMode.Additive,
            showLoadingScreen: false,
            setActiveScene: true);
        return _sceneLoader.LoadSceneAsync(environment.Address, options);
    }

    public UniTask CloseAdditiveAsync(SceneReference environment) =>
        _sceneLoader.UnloadSceneAsync(environment.Address);
}
```

Without a DI container, construct the service in your persistent bootstrap and pass it to consumers:

```csharp
var sceneLoader = new SceneLoaderService(new UnitySceneBackend());
```

With your own loading screen:

```csharp
var sceneLoader = new SceneLoaderService(new UnitySceneBackend(), loadingScreen);
```

`SceneLoadOptions.Default` shows the optional screen, uses Single mode, and adds no delay. If the service has no screen, loading works normally. `showLoadingScreen: false` prevents all loading screen calls. `MinimumScreenDuration` includes the show animation and load time, and only applies when a screen is actually shown.

```csharp
var options = new SceneLoadOptions(
    minimumScreenDuration: System.TimeSpan.FromSeconds(0.5));
await sceneLoader.LoadSceneAsync(gameplay.Address, options, cancellationToken);
```

Prefer references or project-owned constants to literal scene paths. If your project uses a scene enum, keep its mapping in your project rather than modifying package source.

## VContainer and Event Bus

Register once in a root `LifetimeScope` that survives Single loads:

```csharp
using EventSystem;
using SceneLoader;
using SceneLoader.Integrations;
using VContainer;

protected override void Configure(IContainerBuilder builder)
{
    var eventBus = new EventBus();
    builder.RegisterInstance<IEventBus>(eventBus);
    builder.RegisterSceneLoader(loadingScreen, new EventBusSceneObserver(eventBus));
}
```

If a bus is already registered, reuse that instance instead of registering another one. The sample demonstrates bootstrap ownership; production projects should integrate the registration into their existing root scope. Child scene scopes should inherit the root and should not register a second loader. `Lifetime.Singleton` alone does not preserve a scene-owned GameObject across loads.

Subscribe to `SceneTransition` through your shared bus:

```csharp
eventBus.Subscribe<SceneTransition>(OnTransition);

void OnTransition(SceneTransition transition)
{
    if (transition.State == SceneTransitionState.Completed)
        Debug.Log($"Loaded: {transition.Scene}");
}

// Unsubscribe when the consumer is disposed.
eventBus.Unsubscribe<SceneTransition>(OnTransition);
```

Events contain the scene, operation (`Load` / `Unload`), state, progress, and optional exception. The state sequence is `Started`, loading `Progress`, then `Completed`, `Canceled`, or `Failed`. Preflight rejection (busy, invalid scene, pre-canceled token) does not emit transition events because no transition started. Unload does not emit intermediate progress. `Completed` is emitted after cleanup and after the busy lock is released.

Notifications use synchronous `Publish`, so `SubscribeAsync` listeners are not invoked. Use events for notification, not awaited loading gates. Observer exceptions are logged without interrupting scene ownership or cleanup; if the Event Bus stops dispatching on a subscriber exception, later subscribers may not receive that notification.

## Loading screen sample

Import **DOTween Loading Screen** from the package's Samples section.

1. Create a root Canvas with a CanvasGroup and the sample `DOTweenLoadingScreen` component.
2. Set the Canvas sorting order above your gameplay UI.
3. Assign its CanvasGroup and optional Slider. Keep the GameObject active.
4. Give the component to the service constructor or VContainer registration.

The sample persists its root object, fades using unscaled DOTween time, resets its screen on hide, and writes slider progress directly to avoid stacking progress tweens. It uses `DOTween.To`, so it needs neither DOTween UI module assembly definitions nor `UNITASK_DOTWEEN_SUPPORT`.

The screen and service must outlive the loaded scene. UI initialization should happen before the first transition. Only create one persistent loading screen; the sample does not implement a global duplicate singleton policy.

Custom screens implement `ILoadingScreenController`. `HideAsync` must be safe after a partially completed show. A hide failure is propagated if no earlier failure occurred; when an earlier operation failed, cleanup failures are logged and the original exception is preserved.

## Cancellation and concurrency

- Call the API on Unity's main thread and await each returned UniTask once.
- One service owns one transition at a time. Overlapping load/unload requests fail immediately with `InvalidOperationException`; requests are not silently dropped or queued.
- Cancellation before submission prevents loading and cleans up any partially shown screen.
- Unity cannot abort a submitted scene load or unload. The service waits for that operation to complete before releasing the lock, then reports cancellation. **The scene may already have changed even when the caller receives `OperationCanceledException`.**
- Screen cleanup runs without the caller's cancellation token, including after cancellation.
- Cancellation does not roll back a Single load or additive activation.
- Progress reaching `1` indicates loading readiness, not transition completion. Await the method or observe `Completed` to know the screen is closed.

No automatic `Resources.UnloadUnusedAssets` or `GC.Collect` is performed. Explicit unload removes a scene; schedule expensive memory cleanup according to your game's own memory policy.

## Extending the package

| Interface | Responsibility |
| --- | --- |
| `ISceneLoaderService` | Public transition API |
| `ISceneBackend` | Validation, load/unload, active scene selection |
| `ILoadingScreenController` | Loading UI and its animations |
| `ISceneTransitionObserver` | Typed transition notifications |

Provide a different backend to support another scene source. Addressables are not included in this release: an adapter must define how its keys map to `SceneAddress`, retain load handles, release them on unload, and preserve the backend's finish-before-return contract. The built-in backend supports enabled build scenes only.

## Migrating from SceneService

| Previous behavior | Package behavior |
| --- | --- |
| `SceneID.ToString()` decides the scene name | `SceneReference.Address` resolves a full scene path |
| Constructor requires loading UI | Screen is optional |
| `LoadSceneAsync(sceneID, bool)` | `LoadSceneAsync(address, options, cancellationToken)` |
| Screen methods without cancellation | Show takes a token; hide is unconditional cleanup |
| GC and asset cleanup on every transition | Memory cleanup stays with the game |
| Fixed one-second wait | Optional minimum screen duration, default zero |
| No overlap guard | One transition per global service |

Your original SceneService files are not modified. Update consumers to `SceneLoader`, map your project enum to references if needed, and replace old DI registration with the new service. Keep any game-specific initialization after the awaited transition in your game code.

## Tests and repository layout

```text
Packages/com.kameryurdakull.sceneloader/
  Runtime/              Core API and Unity backend
  Editor/               Scene picker and GUID synchronization
  Integrations/         Optional VContainer / Event Bus assemblies
  Samples~/             Importable samples
  Tests/Editor/         Transition and validation tests
```

This repository is a Unity test host. Git installation selects the package subtree; the consumer does not import this repository's Assets or ProjectSettings. UniTask is required by the core assembly. Integration assemblies are enabled by package version constraints when their corresponding packages are installed.

To run package tests in a consumer project, install Unity Test Framework and add the package to the project's manifest:

```json
"testables": ["com.kameryurdakull.sceneloader"]
```

Run `SceneLoader.Tests` through **Window > General > Test Runner > EditMode**. See [release checklist](Documentation~/RELEASING.md) before publishing a tag. The checklist includes installation in a fresh project and player validation; the host's automated tests do not replace those checks.

Verified in this host: **13 EditMode tests passed**, the real Play Mode transition smoke test passed, and core-only compilation passed in a fresh project. See the [validation record](Documentation~/VALIDATION.md) for scope and remaining release checks.

## License

Scene Loader package source is MIT licensed. Third-party dependencies retain their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
