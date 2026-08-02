# DaggerPad implementation status

Date: 2026-08-02

## Version 10 contextual command labels

- The bottom command strip now names what each control will do: Pointer / Look, More / Close, Pause / Back, and Edit / Done. Text-entry and classic dialog contexts show Enter and Back.
- Automap's held-control name is now `LOCAL MAP`, distinguishing it from the travel map, and a first-run hint explains that any control can be held to reveal its name.
- Tapping a writable Daggerfall text field now explicitly activates the hidden iPad input field that requests Apple's software keyboard.
- The change is deliberately limited to labels and framed button artwork. Control positions, hit targets, input bindings, and the accepted gameplay behavior are unchanged.
- Preset migration advances to version 10 for the three built-in layouts only. User-created layouts remain untouched.
- The source gate, Unity device export, nine-point Xcode export gate, automatic signing, arm64 build, and strict code-signature verification passed.
- The build installed over `com.chrissotraidis.daggerpad` on the paired 12.9-inch iPad Pro without uninstalling it. `SAVE0`, `SAVE1`, and `SAVE2` retained their prior timestamps, and the launched process was live after installation.
- Loading gameplay is still required to trigger device-side preset migration and visually accept the contextual labels. Detached-keyboard software-keyboard behavior, physical touch feel, and longer performance checks also remain open.

## Version 9 touch usability controls

- The built-in iPad presets now expose a dedicated Jump button immediately left of Attack.
- The control sends Daggerfall's existing `Jump` action and uses the same framed visual language as the bottom command strip.
- A context-sensitive Mount / Dismount button now sits immediately left of Use. It toggles an owned horse or cart directly without opening Daggerfall's transport window.
- One-finger vertical swipes now feed Daggerfall's standard mouse-wheel path in classic UI windows, including the topic and response lists in conversations. Pixel-scrolled lists advance in useful touch-sized increments.
- The More tray's Run control now reads `RUN OFF` or `RUN ON` and highlights while enabled.
- Attack retains its accepted center position but grows from `96 x 96` to `108 x 108` points.
- Dismounting now explains that the horse or cart is stored off-screen and can be recalled with Mount.
- Preset migration advances to version 9; built-in presets update in place while user-created layouts remain untouched.
- The source gate, Unity device export, nine-point Xcode export gate, signed arm64 device build, in-place install, and launch all passed on the paired 12.9-inch iPad Pro.
- Device read-back confirmed `simplified-layout` version 9, the `108 x 108` Attack target, `RUN OFF`, Mount / Dismount, and Jump.
- The showcase save's malformed horse inventory index was corrected from template ID `94` to runtime group index `1`. Device read-back confirmed `SHOWCASE - Level 10 Horse` still starts mounted and now contains a valid owned horse.
- The launched DaggerPad process remained live. Physical swipe feel, Run feedback, remounting, and the larger Attack target remain explicit hands-on acceptance checks.

## Physical iPad version 6 snapshot

- `main` now contains the version 6 iPad input pass and the Daggerfall-styled touch icon set.
- Physical left-thumb movement works. The visible movement ring and knob are 40 percent smaller than version 5 while the accepted logical radius and sensitivity are unchanged.
- The normal action-to-key path is restored after a version 5 regression that left More working while Use, Attack, Draw, Inventory, and Pause were dead.
- The right look surface supports deliberate double-tap Use/Take. Use, Attack, and Draw are positioned lower on the right edge.
- More starts closed in a compact grid below the native status bars. Edit is standalone in the bottom strip and closes More before presenting the options panel.
- Held control names appear in the HUD instead of beneath the player's finger, and Enter text is capped to fit its button.
- Unity completed the physical-device export, Xcode completed signing and the arm64 device build, and the app installed and launched over the existing iPad installation.
- A device-container read-back confirmed `simplified-layout` version 6, the revised action positions, standalone Edit, and the closed drawer configuration.
- The canonical handoff for fresh setup, in-place update, controls, troubleshooting, and remaining checks is `docs/IPAD_SETUP_AND_CONTROLS.md`.
- Hands-on simultaneous movement/look, combat, object activation, keyboard/trackpad feel, and long-session device stability remain explicit physical acceptance gates.

## Input and README refinement snapshot

- Working branch: `agent/input-playtest-readme`, based on the touch-refinement commit `99a581a`.
- Rechecked touch menus, inventory, automap, text entry, named save/load, keyboard movement/turning, Escape, automap, and status shortcuts on the populated M2 iPad Pro Simulator.
- Found a platform defect in mouse/trackpad activation: Unity 2022 reports `Input.mousePresent == false` on iOS by design, so the existing code could never enter mouse-look mode.
- Added a native Apple `GCMouse` bridge for raw mouse/trackpad deltas and three buttons, plus gameplay-only pointer locking and deterministic `GameController.framework` export.
- Unity completed a clean Simulator export; the updated export gate passed; Xcode compiled the Objective-C++ bridge and produced `BUILD SUCCEEDED` for arm64 iOS 18.5.
- The installed app logged a native `GCMouse` connection. Simulator automation does not generate physical `GCMouse` deltas, so look feel and pointer buttons remain explicit physical-iPad checks.
- Installed over the existing app without uninstalling it; imported Daggerfall data and the `Input Run`, `DaggerPad AutoSave`, and `Simulator Smoke` saves remained available.
- Replaced the minimal README with a screenshot-led fresh-clone install, controls, Simulator, status, limits, and documentation guide; added `BUILDING.md` and a detailed playtest record.
- Detailed evidence and remaining gates: `docs/PLAYTEST_2026-07-17.md`.

## Touch UI refinement snapshot

- Working branch: `agent/ui-playtest-refinement`, based on merged `main` at `ff6ccf71d754908ab6ae7763deb4af980590756e`.
- A deeper M2 iPad Simulator pass found three reproducible defects: the simplified preset hid pause, inventory, and editor access; the editor opened with overlapping labels and could trap the player behind the home indicator; and the iOS setup page exposed an unreadable internal container path.
- Built-in DaggerPad presets are now versioned and migrate in place. User-created layouts remain untouched. New iOS installs select the balanced simplified preset by default.
- The simplified preset now keeps pause, inventory, use, attack, ready weapon, run, drawer, joystick, and editor access visible. Secondary utility actions remain in a drawer that starts closed.
- The editor control is clear of the home indicator, shows `Done` while editing, labels only the selected control, supports deselect-then-exit through empty-space taps, and exits through Escape.
- The setup page now presents `Files > On My iPad > DaggerPad > DaggerfallUnity` while retaining the tap guidance for opening Files.
- The source gate passes all 23 assertions. Unity `2022.3.62f3` exported successfully, the 9-point Xcode export gate passed, and Xcode `26.6` produced `BUILD SUCCEEDED` for the arm64 iOS 18.5 Simulator target.
- The updated app was installed over the populated Simulator without uninstalling it. iOS assigned a new container UUID; DaggerPad retained the imported data, both saves, the custom `my-layout1`, and migrated all three built-in DaggerPad presets to version 2.
- Runtime rechecks passed for setup rendering, `Simulator Smoke` load, balanced HUD rendering, pause, inventory, utility drawer, editor selection, empty-space exit, Done exit, enlarged accessibility controls, and gesture mode without a redundant attack button. No new application exception or crash was observed; the only error-level unified-log entry was an older Simulator PlugInKit XPC interruption.
- Evidence is local and ignored under `Builds/iOS/Evidence/ui-refinement-2026-07-17/`. Physical-device combat, sustained movement, thermal, and hardware-input gates remain scheduled for the M2 iPad Pro.

## Pre-publication handoff snapshot

This snapshot was recorded before publishing the iPadOS implementation to GitHub.

- Working branch: `ipados`.
- Starting point: `main` / `origin/main` at `7ef64b2` (`Add DaggerPad feasibility PRD`).
- Intended publication scope: the complete Unity project, iPadOS platform seams, build and verification scripts, project documentation, license, and repository configuration listed by `git status`. These changes were produced for this implementation and belong together.
- Explicitly excluded from publication: classic Daggerfall data under `ref/`; prepared ZIPs, generated Unity/Xcode projects, application binaries, logs, Simulator containers, screenshots, and other contents under ignored build/cache paths.
- Durable implementation lives in `Assets/`, `Packages/`, `ProjectSettings/`, `scripts/`, and the root/docs Markdown files. Generated Xcode output is disposable.
- Final source gate: `bash scripts/verify-source.sh` passed all 19 assertions, including real reference-data structure, Addressables generation, iOS container relocation, touch layouts, and whitespace checks.
- Final Xcode-export gate: `bash scripts/verify-xcode-export.sh Builds/iOS/Simulator` passed all 9 assertions.
- Final Apple-silicon Simulator build: `Builds/iOS/relocation-xcode.log` contains `** BUILD SUCCEEDED **` for Xcode 26.6.
- Final runtime target: `DaggerPad iPad Pro M2 iOS 18.5`, device identifier `99B296D5-8819-4637-8C62-347E1F49B85F`.
- Final runtime state: imported game data contains 1,574 files and occupies approximately 529 MB; `Simulator Smoke` and `DaggerPad AutoSave` both persist; the updated binary repaired its relocated Documents path and loaded the autosave into Privateer's Hold.
- Evidence remains local and ignored under `Builds/iOS/Evidence/`; the published repository contains the repeatable scripts and acceptance record, not licensed game data or generated evidence.
- Publication does not claim the physical-device gates below. Those remain the next acceptance phase.

## Completed source gates

- Pinned mobile source baseline copied into the repository root at Vwing commit `0fa65294523a132a0e5389d125f58d6566a1e815`.
- Unity project remains pinned to `2022.3.62f3`.
- iOS project identity, IL2CPP backend, iOS 15 floor, Apple-silicon Simulator architecture, Metal settings, landscape support, and ProMotion support configured.
- Android pointer capture isolated from iOS compilation.
- Runtime C# compiler and `mcs.dll` excluded from iOS; code mods fail soft while assets remain available.
- iOS writable StreamingAssets mirror, Files sharing post-processing, ZIP import UTI, traversal-safe unzip, iOS mod target, and iOS restart guidance implemented.
- Original 1024px DaggerPad app icon generated and configured; inherited Daggerfall artwork is no longer used as the installed app identity.
- Touch UI hooks enabled for iOS; tap/long-press/two-finger mouse mapping, optional directional gesture combat, and default/simplified/gesture/accessibility presets implemented.
- Background/focus autosave and audio pause/resume implemented.
- Deterministic device and Simulator Xcode export commands added.
- Source and real-data verification passes with 472 texture archives and 17 videos.
- A clean 148 MB ignored test archive exists at `Builds/TestData/daggerfall.zip`; `unzip -t` passes and it contains no macOS metadata directory.
- Unity `2022.3.62f3` imports the project in batch mode with zero C# errors.
- Unity exports the Apple-silicon Simulator Xcode project, and all post-process assertions pass.
- The deterministic export explicitly builds Addressables/Localization content before the iOS player, preventing a black screen caused by missing `Data/Raw/aa/settings.json`.
- Xcode `26.6` (build `17F113`) builds the exported `DaggerPad.app` for arm64 Simulator with no errors.
- iOS 26.5 and iOS 18.5 Simulator runtimes are installed; the smoke harness pins iOS 18.5 because the iOS 26.5 runtime has a current CoreSimulator launch defect on this host configuration.
- The corrected app launches on the pinned M2-class iPad Pro Simulator, renders the setup wizard, imports and extracts the 155 MB test ZIP into 529 MB of validated Daggerfall data, and relaunches using the persisted `arena2` path.
- Imported data is recovered from the current Documents container if iPadOS changes the container UUID during an app update, and the stale absolute path is repaired automatically.
- A touch-only run reached the Daggerfall main menu, completed character creation including native text entry, entered Privateer's Hold, displayed the gameplay overlay, opened the pause and save UI, persisted `Saves/SAVE0` as `Simulator Smoke`, and loaded it back into the world.
- Background/resume created `Saves/SAVE1` as `DaggerPad AutoSave`; after a forced process termination and cold relaunch, the autosave was visible and loaded back into Privateer's Hold.
- Runtime evidence is preserved under ignored `Builds/iOS/Evidence/`, including Unity stdout/stderr and touch/gameplay screenshots.

## Current acceptance boundary

The macOS Simulator end-to-end gate is closed through first playable world entry and save persistence. The source, Unity export, Xcode build, Files import, Addressables runtime, touch character creation, gameplay HUD, pause UI, and save system now have direct evidence.

The earlier black DaggerPad screen after process launch was diagnosed separately from the transient host Simulator issue: the Xcode export lacked Addressables runtime data. `DaggerPadIosBuild` now builds that content before every player export, and the Xcode verifier rejects an export without it.

Do not label the port device-stable until the remaining physical and complete-gameplay evidence exists:

1. On the physical M2 iPad Pro, repeat Files import and first launch using the same prepared archive.
2. Prove sustained movement/look, door or lever interaction, combat, loot/equip, and automap gestures by touch.
3. Repeat the already-passing Simulator save/load and background autosave recovery flows after iPadOS terminates the process.
4. Complete the one-hour thermal/frame-pacing pass and two-hour memory/jetsam pass.
5. Exercise hardware keyboard, pointer, and controller input, then validate save round-tripping through Files.

## Tomorrow's action

Open `Builds/iOS/Simulator/Unity-iPhone.xcodeproj`, select the connected iPad, set the development team, and run. Use `Builds/TestData/daggerfall.zip` for Files import and follow the physical checklist in `docs/TESTING.md`. Re-run `bash scripts/run-simulator-smoke.sh` first if a fresh Simulator regression baseline is wanted.
