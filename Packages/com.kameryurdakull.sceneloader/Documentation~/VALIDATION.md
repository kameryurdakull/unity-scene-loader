# Validation record

Validated on **2 October 2026**, using Unity **6000.3.15f1** on Windows.

| Check | Result |
| --- | --- |
| Core, Editor, VContainer and Event Bus assemblies | Compiled without errors |
| Imported DOTween and VContainer samples | Compiled without errors |
| EditMode package tests | 13 passed, 0 failed, 0 skipped |
| Scene reference move and deletion | Passed; paths update after move and clear after deletion |
| Play Mode Single load | Passed |
| Play Mode additive load and active scene selection | Passed |
| Play Mode additive unload and last-scene guard | Passed |
| Overlapping load rejection | Passed |
| Cancellation after Unity load submission | Passed; load settles, screen closes, busy lock releases |
| DOTween fades with `Time.timeScale = 0` | Passed |
| Fresh project with only UniTask and Scene Loader | Core and Editor compiled; Unity batch process exited with code 0 |
| Whitespace diff check | Passed |

The fresh project used local package paths to the same package source and UniTask 2.5.11. It did not install VContainer, Event Bus, DOTween, uGUI or the Test Framework. This validates optional assembly exclusion; it does not validate a remote Git checkout.

The development-host `SceneLoaderSmokeRunner` exercises actual Unity loads and the sample loading screen. To repeat it, enter Play Mode in SampleScene, keep both host scenes enabled in the build list, and add the runner to a root GameObject. The runner logs one PASS message, restores the original time scale, and removes its temporary objects. It is excluded from player compilation.

The following remain release checks: standalone player execution, Unity 2022.3 execution, custom Unity 6 Build Profile scene overrides, remote Git installation, and tagged release installation. No commit, push, tag, or GitHub release is created by this preparation.
