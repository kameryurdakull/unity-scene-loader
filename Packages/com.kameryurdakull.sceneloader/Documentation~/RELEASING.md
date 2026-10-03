# Release checklist

This working tree is prepared for version 1.0.0. Preparing files does not publish a GitHub release or create a remote tag.

1. Review the existing staged host files separately from the new package files. Do not accidentally include unrelated work or third-party assets without reviewing their distribution terms.
2. Run the package's EditMode tests and check the Unity Console for compiler errors.
3. Import both samples and verify they compile with the documented dependencies.
4. In Play Mode, exercise Single loading with a persistent screen, additive load/activation/unload, overlapping requests, cancellation during loading, and `Time.timeScale = 0`.
5. Build and run a standalone player with at least two enabled scenes. Verify full-path loading and scene references after rename/move. In Unity 6 test custom Build Profile scene overrides too.
6. Install the package through its Git URL in a fresh project with only UniTask. Verify the core compiles without VContainer, Event Bus, DOTween, or uGUI. Then install integrations and import their samples.
7. Synchronize package version, CHANGELOG, sample paths, and README tagged URL.
8. Commit the reviewed change to the repository and push it to GitHub.
9. Create and push `v1.0.0` pointing at that commit, then verify the tagged Git URL from a fresh consumer project.
10. Publish a release with the relevant CHANGELOG entry. If attaching an archive, include only the distributable package and its notices.

No Git credentials or local absolute paths should be included in the package.
