# DaggerPad testing

## macOS simulator prerequisite

Use the installed iOS 18.5 runtime for the automated smoke path:

```bash
xcodebuild -downloadPlatform iOS -buildVersion 18.5
bash scripts/run-simulator-smoke.sh
```

On macOS 26.5.x with Xcode 26.6, the iOS 26.5 runtime can reach its home screen while launches remain stuck at a black SpringBoard snapshot before any app process starts. This was reproduced with both DaggerPad and Safari, so it is not an application failure. The smoke script pins iOS 18.5 by default and accepts `SIMULATOR_RUNTIME=latest` only as an explicit override.

If every app still stops before process creation after installing a runtime, restart the Mac before collecting evidence. A CoreSimulator service restart alone did not recover that host-level dyld state during the 2026-07-17 setup run.

## iPad Simulator smoke checklist

1. Build a simulator Xcode project from a clean Unity import.
2. Build and install the `Unity-iPhone` app on an Apple-silicon iPad simulator.
3. Launch and confirm the DaggerPad welcome screen renders in landscape.
4. Tap Import and confirm the iOS document picker appears with ZIP files selectable.
5. Import the prepared `daggerfall.zip`; verify unzip, search, copy, and final validation complete.
6. Complete the setup pages and reach the main menu.
7. Start character creation and verify the native keyboard bridge.
8. In retro UI screens, verify tap = left click, stationary long press = right click, and two-finger tap = middle click.
9. Watch the device log for fatal exceptions, missing shaders, read-only path errors, or IL2CPP failures.

### 2026-07-17 Simulator result

Passed on `DaggerPad iPad Pro M2 iOS 18.5`:

- Built Addressables plus the Unity player, then built `DaggerPad.app` for arm64 with Xcode.
- Imported `daggerfall.zip` through Files; 1,574 files were installed and the selected `arena2` path persisted.
- Relaunched cleanly without Addressables, Localization, IL2CPP, or data-path fatal errors.
- Reached the main menu and completed touch-only character creation with the iOS text bridge.
- Entered Privateer's Hold and rendered the full mobile gameplay layout.
- Opened pause and save screens by touch, persisted `Saves/SAVE0/SaveInfo.txt` with save name `Simulator Smoke`, and loaded it back into the game world.
- Backgrounded and resumed the app, confirmed `DaggerPad AutoSave` was written as `SAVE1`, force-terminated the process, cold-launched, and loaded that autosave back into the game world.
- Installed a rebuilt app over the populated Simulator. After iOS assigned a new data-container UUID, DaggerPad recovered the current `Documents/DaggerfallUnity/Daggerfall` path, rewrote settings, retained both saves, and loaded `DaggerPad AutoSave` into the world on the updated binary.

Evidence: `Builds/iOS/Evidence/` (ignored build evidence, not release content).

## Touch-only gameplay acceptance

- Create and name a character.
- Move with the left control and look with the right control.
- Open a door or use a lever.
- Ready a weapon and complete an attack.
- Loot and equip an item, including a long-press secondary action.
- Open the interior automap and pan, pinch, and rotate it.
- Save, load, background, resume, force-close, relaunch, and load `DaggerPad AutoSave`.

Simulator proof currently covers character creation, playable world entry, gameplay-layout rendering, pause, explicit save/load, background/resume autosave, forced termination, cold relaunch, and autosave recovery. Movement/look duration, interaction, combat, loot, and automap gestures remain acceptance checks rather than claimed passes.

## Physical iPad checks

Run these on the M2 iPad Pro; the Simulator cannot close them:

- Native Files provider import of the 500+ MB archive and peak temporary storage.
- 60/120 fps frame pacing while touching the display.
- One-hour memory, thermal, and battery behavior; then a two-hour memory/jetsam run.
- Daggerfall City, Wayrest, Daggerfall Castle, fast travel, and dungeon transitions.
- Hardware keyboard, trackpad/mouse relative look, MFi/DualSense/Xbox controller mapping.
- Background suspension long enough for process termination, then autosave recovery.
- Desktop save copied through Files and loaded on iPad, then copied back.

## Evidence log

Record the Unity editor version, Xcode version, macOS version, simulator/device model and OS, build commit, import duration, peak disk use, and any console errors for every gate run.
