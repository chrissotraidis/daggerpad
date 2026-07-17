# DaggerPad goal loop

## Objective

Deliver a reproducible DaggerPad iPadOS build that imports the user's Daggerfall data, reaches the game world, supports the complete touch-only core loop, survives suspend/resume, and can be exercised in an iPad Simulator before physical-device testing.

The source baseline is Vwing's `android` branch at `0fa65294523a132a0e5389d125f58d6566a1e815`, pinned to Unity `2022.3.62f3`. The classic game data remains untracked under `ref/` and is never bundled into a release.

## Loop

Each checkpoint repeats the same five actions:

1. Pick the smallest unmet user-visible outcome.
2. Implement only the platform seam needed for that outcome.
3. Run the checkpoint's proof command or runtime flow.
4. Repair regressions before expanding scope.
5. Record evidence and advance only when the exit condition is true.

## Checkpoints

### G0 — Reproducible source baseline

- Unity project lives at the repository root.
- `ProjectVersion.txt` is pinned to `2022.3.62f3`.
- `ref/` and generated Unity/Xcode state are ignored.
- Exit proof: `bash scripts/verify-source.sh`.

### G1 — iOS compile surface

- iOS uses IL2CPP, iOS 15+, Metal, landscape, and the DaggerPad bundle identity.
- Android pointer capture and runtime C# compilation cannot enter an iOS player build.
- FullSerializer/save DTOs are preserved from managed stripping.
- Exit proof: Unity batch-mode iOS export completes with zero compiler errors.

### G2 — Files-native data and mods

- Shipped StreamingAssets are mirrored into the writable Documents container without deleting user files.
- The setup wizard imports a ZIP through the iOS document picker.
- Import rejects traversal outside its cache and validates the real `arena2` contract.
- Asset-only iOS `.dfmod` files can be built and imported; C# mod code is explicitly skipped.
- Exit proof: import `daggerfall.zip`, then validate and persist the selected data path.

### G3 — Touch-only playable loop

- Existing mobile move/look/action controls are enabled for iOS.
- Native keyboard hooks work on iOS.
- Tap, long-press, and two-finger tap map to left, right, and middle click in the retro UI.
- Save/load, inventory, automap, travel map, and character creation are reachable without a hardware keyboard or mouse.
- Exit proof: create a character, move, look, interact, fight, loot, automap, save, and load.

### G4 — iPad lifecycle and performance baseline

- Focus loss/backgrounding queues a dedicated DaggerPad autosave and pauses audio.
- Default frame rate is 60; ProMotion can be selected up to 120.
- Exit proof: background/resume twice, force-close, relaunch, and load the DaggerPad autosave.
- Status 2026-07-17: one background/resume produced `DaggerPad AutoSave`; a forced termination, cold relaunch, and autosave load returned to the game world. Repeat twice and measure performance on physical hardware.

### G5 — Deterministic Xcode export

- One editor command exports device or Apple-silicon Simulator projects.
- The post-processor configures Files sharing, indirect input, iPad-only target family, and minimum OS consistently.
- Exit proof: `xcodebuild` succeeds from a fresh generated project.

### G6 — macOS iPad Simulator smoke

- Boot an iPad simulator, install DaggerPad, launch it, and capture console/screenshot evidence.
- Exercise first launch, setup wizard, native document picker, and the touch UI.
- Automation entrypoint: `bash scripts/run-simulator-smoke.sh`.
- Exit proof: app remains running with no fatal exception and the smoke checklist is recorded in `docs/TESTING.md`.
- Status 2026-07-17: passed through Files import, persisted data path, main menu, touch-only character creation, playable world entry, touch pause UI, named save/load, and lifecycle autosave recovery.

### G7 — physical-device handoff

- Preserve the exact Unity/Xcode versions, signing steps, prepared data ZIP command, known simulator limitations, and device-only checks.
- Exit proof: tomorrow's tester can build and install without rediscovering setup.
- Status 2026-07-17: build artifacts, prepared ZIP, exact tools, Simulator evidence, and device-only checklist are ready; signing and physical acceptance remain for the connected iPad.

## Stop conditions

Stop and reassess only if both Unity 2022.3 and Unity 6.3 fail to produce an Xcode 26-buildable project, IL2CPP exposes systemic failures rather than isolated platform seams, or core move/look/interact/save/load behavior fails for an engine-level reason. Loss of C# mods, an individual touch screen defect, or performance below 60 fps is not a stop condition.
