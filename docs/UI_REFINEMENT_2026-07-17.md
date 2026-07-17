# DaggerPad touch UI refinement

Date: 2026-07-17  
Branch: `agent/ui-playtest-refinement`  
Base: `ff6ccf71d754908ab6ae7763deb4af980590756e` (`main` / `origin/main` at branch creation)

## Goal

Playtest the merged iPad port more deeply, fix reproducible touch and UI defects without destabilizing the core loop, and prove the result on the existing M2 iPad Simulator install.

## Findings and resolution

| Finding | Impact | Resolution | Runtime result |
| --- | --- | --- | --- |
| The default mobile layout crowded almost every action into the right side. | Gameplay view and touch targets were difficult to parse. | New iOS installs use the balanced simplified preset; existing built-in presets migrate to version 2. | Pass: clear left movement, right action cluster, top-right pause/inventory, and bottom-center editor access. |
| The simplified preset hid pause, inventory, and editor access. | Players could not reliably manage or leave a session. | Essential controls remain visible; secondary utilities live in a closed drawer. | Pass: pause, inventory, and drawer all opened by touch. |
| The editor displayed labels for every control and placed its only exit under the home indicator. | Labels overlapped and the editor could trap the player. | The editor button moved above the safe area, reads `Done`, labels only the selected control, and supports empty-space and Escape exits. | Pass: selection, deselection, empty-space exit, and Done exit. |
| The setup wizard printed the Simulator's absolute persistent-data path. | The path overflowed the iPad UI and was meaningless to users. | iOS shows the Files-visible location. | Pass: `Files > On My iPad > DaggerPad > DaggerfallUnity` renders on one readable line. |
| Existing generated presets never received source improvements. | Upgrades kept stale layouts indefinitely. | Built-in presets carry a version and regenerate only when stale; custom layouts are not migrated. | Pass: all three built-ins reached version 2; `my-layout1` remained untouched. |

## Verification

- `bash scripts/verify-source.sh`: 23/23 pass.
- Unity `2022.3.62f3` Simulator export: pass, batch mode exited successfully.
- `bash scripts/verify-xcode-export.sh Builds/iOS/Simulator`: 9/9 pass.
- Xcode `26.6`, arm64 iOS 18.5 Simulator build: `BUILD SUCCEEDED`.
- Upgrade install: pass without uninstall; imported data, two saves, and custom layout persisted across the new container UUID.
- Runtime flows: setup, saved-game load, HUD, pause, inventory, drawer, editor selection, empty-space exit, Done exit, accessibility sizing, and gesture-mode simplification pass.
- Runtime log: no new DaggerPad exception, fatal error, or crash found during the revised flow.

## Acceptance boundary

This closes the high-impact Simulator UI defects found in the refinement pass. It does not replace tomorrow's physical-device checks for sustained movement/look, real combat, door/lever interaction, loot/equip, automap gestures, thermal behavior, frame pacing, or hardware inputs.
