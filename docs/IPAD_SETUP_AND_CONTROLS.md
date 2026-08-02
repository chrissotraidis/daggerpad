# iPad setup and controls

This is the canonical handoff for building DaggerPad, installing it on an iPad, preserving an existing installation, and using the current version 9 input layout.

## What DaggerPad requires

- macOS with Xcode. The current verified device build used Xcode 26.6.
- Unity Hub with Unity `2022.3.62f3` and iOS Build Support installed.
- An iPad running iPadOS 15 or later with Developer Mode enabled.
- An Apple ID selected as the Xcode development team.
- A legally obtained Daggerfall folder containing `ARENA2` and `FALL.EXE` at the same level.

DaggerPad does not include Bethesda game data. The Unity project, touch interface, and iPad platform code are in this repository; the classic game files stay local.

## Fresh setup

Clone and validate the source:

```bash
git clone https://github.com/chrissotraidis/daggerpad.git
cd daggerpad
bash scripts/prepare-game-data.sh "/path/to/DAGGER"
bash scripts/verify-source.sh
```

`prepare-game-data.sh` creates `Builds/TestData/daggerfall.zip`. The ZIP is ignored by Git and must not be committed or distributed.

Open the repository in Unity Hub with exactly `2022.3.62f3`. Let Unity finish its first import before building.

## Build and install on a physical iPad

1. Connect and trust the iPad, then enable Developer Mode if iPadOS requests it.
2. In Unity, choose **DaggerPad > Build > iPadOS Device**.
3. Open `Builds/iOS/Device/Unity-iPhone.xcodeproj`.
4. Select the `Unity-iPhone` target in Xcode.
5. Under **Signing & Capabilities**, enable automatic signing and select your development team.
6. Select the connected iPad as the run destination and press **Run**.
7. Make `Builds/TestData/daggerfall.zip` available through Files, iCloud Drive, AirDrop, or another local transfer.
8. In DaggerPad, tap **Import**, select the ZIP, finish the setup pages, and tap **Play**.

Imported data, settings, layouts, and saves are available in **Files > On My iPad > DaggerPad > DaggerfallUnity**.

### Updating an existing iPad installation

Build and run the same bundle identifier, `com.chrissotraidis.daggerpad`, over the installed app. Do not uninstall the app and do not use a clean-install command if its imported data or saves matter.

An in-place Xcode or `devicectl` install replaces the application bundle while retaining the app data container. Back up `DaggerfallUnity/Saves` through Files before any signing, bundle-identifier, or provisioning change.

The version 9 build was exported with Unity, signed by Xcode, installed over the existing physical-iPad app, launched, and read back from the device container. The selected `simplified-layout`, existing app container, and prior saves were retained; device read-back confirmed the Mount / Dismount control beside Use, Jump beside Attack, the larger Attack target, and the visible Run state.

## Command-line device build

Unity export:

```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "$PWD" \
  -buildTarget iOS \
  -executeMethod DaggerPad.Editor.DaggerPadIosBuild.BuildDevice \
  -daggerpadBuildPath "$PWD/Builds/iOS/Device" \
  -logFile "$PWD/Builds/iOS/unity-device.log"
```

Signed build, using values from Xcode's Devices and Simulators window and your Apple developer account:

```bash
xcodebuild \
  -project Builds/iOS/Device/Unity-iPhone.xcodeproj \
  -scheme Unity-iPhone \
  -configuration Debug \
  -sdk iphoneos \
  -destination 'id=YOUR_XCODE_DEVICE_ID' \
  -derivedDataPath Builds/iOS/DeviceDerivedData \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM=YOUR_TEAM_ID \
  CODE_SIGN_STYLE=Automatic \
  build
```

Install the resulting app without uninstalling the existing copy:

```bash
xcrun devicectl device install app \
  --device YOUR_COREDEVICE_ID \
  Builds/iOS/DeviceDerivedData/Build/Products/Debug-iphoneos/DaggerPad.app
```

## Version 9 touch layout

The built-in `simplified-layout` is the default iPad layout. It uses two simultaneous screen-half surfaces rather than a permanent pair of sticks.

| Surface or control | Behavior |
| --- | --- |
| Left half | Touch and drag to move. A floating ring appears at the initial touch point. Its version 6 artwork is 40 percent smaller than version 5 while retaining the same logical movement radius and sensitivity. |
| Right half | Touch and drag to look. The surface is invisible so the game remains readable. |
| Right-side double tap | Uses, opens, activates, or takes the object under the crosshair. A single tap does not activate anything. |
| Use | Explicit alternative to the right-side double tap. |
| Mount / Dismount | Mounts an owned horse or cart while outdoors, or immediately returns to foot when already riding. It does not open the transport selection window. Daggerfall stores the transport off-screen when dismounted; a message explains that Mount recalls it. |
| Attack | If the weapon is sheathed, the first tap readies it and explains that the next tap attacks. With a ready weapon, the button attacks. Version 9 enlarges its target without moving its center. |
| Jump | Jumps using Daggerfall's normal `Jump` action. The button sits immediately left of Attack. |
| Draw / Sheathe | Toggles the equipped weapon's ready state. |
| Enter | Switches the hardware pointer between Pointer mode and captured Look mode. Its HUD message confirms the new mode. |
| More | Opens the secondary-action tray below Daggerfall's native status bars. |
| Inventory | Opens the backpack/inventory window. |
| Back / Pause | Pauses during gameplay and closes or backs out of Daggerfall windows. |
| Edit | Opens the on-screen control editor. Any open More tray closes first. |

Hold a visible control for about half a second to show its name in the HUD, away from the finger covering the button.

In Daggerfall's classic menus, drag one finger vertically over a list to scroll it. This uses the same component path as a mouse wheel and works in conversation topics and responses, inventory-style lists, spell lists, journals, and other windows that already support wheel scrolling.

### More tray

More contains automap, rest, quick save, quick load, status, travel, logbook, notebook, switch hand, magic item, and run/walk. It is a compact three-column tray below the health and status region and starts closed. Run reads `RUN OFF` or `RUN ON`; its icon also highlights while the toggle is enabled.

### Editing and presets

Tap the bottom gear to open **On-Screen Control Options**. The editor can move, resize, enable, disable, and remap controls. Tap the gear/Done control again, tap empty space after deselecting a control, or press Escape to leave.

The bundled presets are:

- `simplified-layout`: standard version 9 iPad controls.
- `gesture-layout`: gesture combat without a redundant Attack button.
- `accessibility-layout`: enlarged controls and full opacity.

Built-in presets migrate forward by version. User-created layouts are preserved. If an older custom layout still shows the phone-style wall of controls, select `simplified-layout` in the editor rather than deleting the custom file.

Use **Reset All Button Positions** to restore the selected preset's placement and **Reset All Button Mappings** to restore its actions. These commands affect the selected layout, so export a custom layout first if it needs to be preserved exactly.

## Keyboard and trackpad

| Input | Default behavior |
| --- | --- |
| `W`, `A`, `S`, `D` | Move at the iPad-tuned keyboard scale. Touch, keyboard, and controller movement are arbitrated so sources do not add together. |
| Trackpad or mouse motion | Relative look during captured Look mode. |
| Enter | Toggle Pointer mode and Look mode. |
| Primary, secondary, middle click | Feed Daggerfall's normal mouse actions and menus. |
| `Shift` | Run while held. |
| `Space` | Jump. |
| `C` | Crouch. |
| `M` | Automap. |
| `I` | Status. |
| `Z` | Ready or sheathe weapon. |
| Escape | Pause, close, or back. |

Daggerfall's bindings remain configurable. Trackpad pointer lock is requested only during active gameplay, not while Daggerfall menus are open.

## Verification and current limits

Before handing off a build, run:

```bash
bash scripts/verify-source.sh
bash scripts/verify-xcode-export.sh Builds/iOS/Device
```

The version 9 source compiled in Unity, passed the Xcode export checks, produced a signed physical-device build, installed, launched, and retained the expected device-side preset configuration. Device read-back also confirmed the corrected horse ownership record in `SHOWCASE - Level 10 Horse`. Physical-input checks remain below.

Physical touch feel cannot be closed by source or build verification alone. The remaining acceptance pass is:

1. Move and look simultaneously for at least 30 seconds.
2. Repeat with `WASD` plus right-thumb look.
3. Dismount and remount the showcase horse, confirming the stored-horse message and `MOUNT` / `DISMOUNT` label changes.
4. Swipe vertically through both conversation columns and at least one inventory-style list.
5. Toggle Run in More and confirm both the text and highlight change.
6. Verify Use, Attack, Jump, Draw, Inventory, Pause, Enter, and every other More action in-game.
7. Double-tap a door, lever, or lootable object and confirm it activates exactly once.
8. Open More, save the game, and confirm neither interface covers the other.
9. Enter and exit control editing with More open.
10. Compare walk, run, touch, and keyboard travel over the same timed route.
11. Complete the longer frame-pacing, thermal, battery, memory, and suspension-recovery passes in `docs/TESTING.md`.

The complete physical-input history and screenshot audits are in `docs/PHYSICAL_IPAD_INPUT_REVIEW_2026-07-19.md`.
