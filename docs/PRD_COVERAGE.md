# DaggerPad PRD coverage

Date: 2026-07-17

This ledger maps the implementation to `DAGGERPAD_PRD.md`. A pass means direct source, build, Simulator, or filesystem evidence exists. It does not convert a physical-device requirement into a Simulator claim.

| PRD outcome | Status | Evidence / next gate |
|---|---|---|
| Vwing mobile baseline at `0fa652945...`, Unity `2022.3.62f3`, `ipados` branch | Pass | Repository root, `ProjectSettings/ProjectVersion.txt`, git branch |
| iOS IL2CPP, arm64, Metal, landscape, iOS 15+, DaggerPad identity | Pass | `ProjectSettings`, `DaggerPadIosBuild`, source verifier, Xcode build |
| Unity 2022.3 output builds with Xcode 26 | Pass | Simulator arm64 `Unity-iPhone` build succeeds under Xcode 26.6 |
| Android-only pointer capture cannot break iOS compile | Pass | Platform guards plus Unity player-script compilation |
| Runtime C# compiler and `mcs.dll` excluded on iOS | Pass | Plugin/platform metadata and source verifier |
| Writable Documents tree, StreamingAssets mirror, and app-update relocation | Pass | 691 MB Simulator Documents container and saves survived an update; stale absolute data path now repairs against the current container |
| Files-native ZIP import, nested data search, validation, cleanup, persisted path | Pass | Native document picker; 1,574 files and 529 MB final data; cached ZIP removed; relaunch finds `ARENA2` |
| Addressables and Localization included in deterministic iOS export | Pass | `BuildPlayerContent`; `Data/Raw/aa/settings.json`; corrected runtime launch |
| iOS asset-mod build target/import path; code mods fail soft | Source pass | iOS build target and import gates present; an actual iOS `.dfmod` remains a physical-device/curation test |
| iOS texture format and desktop folder actions guarded | Pass | `TextureReplacement` and Files-specific UI branches |
| Native text entry | Pass | Character and save names entered in Simulator |
| Tap / long-press / two-finger retro-UI mapping | Source pass | Global gesture adapter implemented; right/middle-click inventory acceptance still needs direct gameplay exercise |
| Default, simplified, gesture, controller, and accessibility layouts | Partial | Default/gamepad inherited; simplified/gesture/accessibility generated. Controller hardware remains unverified |
| Gesture combat and two-finger ready/sheathe | Source pass | Directional gesture plumbing and layout setting compile; live combat remains unverified |
| Touch-only character creation and playable world entry | Pass | Character created and Privateer's Hold entered on M2-class iPad Simulator |
| Save and explicit load | Pass | `Simulator Smoke` persisted in `SAVE0` and loaded back into the world |
| Background autosave, force-close, cold relaunch, recovery | Pass | `DaggerPad AutoSave` persisted in `SAVE1` and loaded after forced termination |
| Deterministic Unity device/Simulator export and Xcode post-processing | Pass | Editor build command, Addressables build, plist/project verifier, smoke harness |
| Touch movement/look, door/lever, combat, loot/equip, automap gestures | Open | Physical M2 iPad touch acceptance; Simulator screen proves layout but not sustained multi-touch gestures |
| 60/120 fps, thermals, battery, one-hour and two-hour memory/jetsam | Open | Instruments/Profiler physical-device gates |
| Hardware keyboard, mouse/trackpad, MFi/DualSense/Xbox | Open | Physical accessory matrix |
| Desktop-to-iPad save round trip through Files | Open | Copy a real desktop save both directions tomorrow |
| Unity 6.3 migration, TestFlight, license/font audit, public distribution | Deferred by PRD | Phase 4 only after playable/device/performance gates; legal review required before public release |

## Current boundary

The solution is Simulator-playable and has closed the PRD's critical toolchain, import, runtime, save/load, and lifecycle unknowns. The remaining acceptance work depends on physical touch duration, accessories, Instruments, signing, or distribution authority. Those items are listed in `TESTING.md` and must not be marked complete from macOS alone.
