#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

pass() { printf 'PASS  %s\n' "$1"; }
skip() { printf 'SKIP  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

(( $# <= 1 )) || fail 'Usage: bash scripts/verify-source.sh [path/to/DAGGER]'
default_dagger="$root/ref/The Elder Scrolls Daggerfall/DF/DAGGER"
dagger="${1:-$default_dagger}"

[[ "$(awk '/m_EditorVersion:/ { print $2; exit }' ProjectSettings/ProjectVersion.txt)" == "2022.3.62f3" ]] \
  && pass "Unity version pinned to 2022.3.62f3" \
  || fail "Unity version is not pinned to 2022.3.62f3"

grep -q 'iPhone: com.chrissotraidis.daggerpad' ProjectSettings/ProjectSettings.asset \
  && pass "iOS bundle identifier configured" \
  || fail "iOS bundle identifier missing"
grep -q 'guid: 1156c369796c940ab98cf21db31c8bd3' ProjectSettings/ProjectSettings.asset \
  && [[ -f Assets/Brand/DaggerPadAppIcon.png ]] \
  && pass "original DaggerPad app icon configured" \
  || fail "original DaggerPad app icon missing"
grep -q 'iOSTargetOSVersionString: 15.0' ProjectSettings/ProjectSettings.asset \
  && pass "iOS 15 deployment floor configured" \
  || fail "iOS deployment floor missing"
scripting_backend="$(awk '
  /scriptingBackend:/ { in_backend=1; next }
  /il2cppCompilerConfiguration:/ { in_backend=0 }
  in_backend && /iPhone:/ { print $2 }
' ProjectSettings/ProjectSettings.asset)"
[[ "$scripting_backend" == '1' ]] \
  && pass "iOS IL2CPP backend configured" \
  || fail "iOS IL2CPP backend missing"
grep -q '#if UNITY_ANDROID && !UNITY_EDITOR' Assets/Android/OpenPointerCapture/Scripts/PointerCaptureNativeInterface.cs \
  && pass "Android pointer-capture implementation guarded" \
  || fail "Android pointer-capture guard missing"
grep -q '#if !UNITY_IOS' Assets/Game/Addons/CSharpCompiler/Compiler.cs \
  && pass "runtime C# compiler excluded from iOS" \
  || fail "runtime compiler iOS exclusion missing"
awk '
  /iPhone:/ { in_iphone=1; next }
  in_iphone && /enabled:/ { if ($2 == 0) found=1; exit }
  END { exit found ? 0 : 1 }
' Assets/Game/Addons/CSharpCompiler/Plugins/mcs.dll.meta \
  && pass "mcs.dll plugin disabled for iOS" \
  || fail "mcs.dll is not disabled for iOS"
grep -q 'UIFileSharingEnabled' Assets/iOS/Editor/DaggerPadIosBuild.cs \
  && pass "Files sharing post-processor configured" \
  || fail "Files sharing post-processor missing"
grep -q 'AddressableAssetSettings.BuildPlayerContent' Assets/iOS/Editor/DaggerPadIosBuild.cs \
  && pass "iOS build generates Addressables player content" \
  || fail "iOS Addressables player content build missing"
grep -q 'GameController.framework' Assets/iOS/Editor/DaggerPadIosBuild.cs \
  && grep -q 'GCMouse' Assets/Plugins/iOS/DaggerPadPointer.mm \
  && grep -q 'DaggerPadPointerInput.IsConnected' Assets/Scripts/Game/PlayerMouseLook.cs \
  && pass "native iPadOS mouse and trackpad input configured" \
  || fail "native iPadOS mouse and trackpad input is incomplete"
grep -q 'Recovered Daggerfall data after iOS container relocation' Assets/Scripts/DaggerfallUnity.cs \
  && pass "iOS app updates recover relocated imported data" \
  || fail "iOS container-relocation recovery is missing"
grep -q 'RegenerateDaggerPadLayoutIfMissing("simplified-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'RegenerateDaggerPadLayoutIfMissing("gesture-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'RegenerateDaggerPadLayoutIfMissing("accessibility-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "simplified, gesture, and accessibility layouts are bundled" \
  || fail "DaggerPad control presets are missing"
grep -q 'DaggerPadPresetVersion = 10' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'daggerPadPresetVersion >= DaggerPadPresetVersion' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "built-in touch presets migrate forward without replacing custom layouts" \
  || fail "DaggerPad preset migration guard is missing"
grep -q 'enabledButtons.Add("jump")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'case "jump":' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'new Vector2(-136, -205)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q '"jump" => "daggerpad_button_frame"' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "built-in iPad presets provide a dedicated Jump button beside Attack" \
  || fail "DaggerPad Jump control is missing"
grep -q 'enabledButtons.Add("mount-toggle")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'button.Name = "mount-toggle"' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'new Vector2(-136, -95)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'transportManager.ToggleMount()' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"mount-toggle" => "MOUNT / DISMOUNT"' Assets/Android/Scripts/TouchscreenButton.cs \
  && pass "built-in iPad presets provide a direct mount and dismount toggle" \
  || fail "DaggerPad mount toggle is missing"
grep -q 'ScrollStepInches = 0.10f' Assets/Android/Scripts/MobileUIGestureInput.cs \
  && grep -q 'MobileUIGestureInput.GetMouseScroll()' Assets/Scripts/Game/InputManager.cs \
  && grep -q 'InputManager.Instance.GetMouseScroll()' Assets/Scripts/Game/UserInterface/BaseScreenComponent.cs \
  && grep -q 'verticalScrollMode == VerticalScrollModes.PixelWise ? 6 : 1' Assets/Scripts/Game/UserInterface/ListBox.cs \
  && pass "one-finger vertical swipes scroll classic UI lists" \
  || fail "DaggerPad touch list scrolling is incomplete"
grep -q 'button.Name == "toggle-run" ? "RUN OFF"' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'runEnabled ? "RUN ON" : "RUN OFF"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'new Vector2(108, 108)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "Run state is visible and Attack has a larger hit target" \
  || fail "DaggerPad Run or Attack feedback is incomplete"
grep -q 'UpdateDaggerPadContextualText()' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"enter-key" => !GameManager.HasInstance || GameManager.IsGamePaused ? "ENTER"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"escape" => GameManager.HasInstance && !GameManager.IsGamePaused ? "PAUSE" : "BACK"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"drawer" => isDrawerOpen ? "CLOSE" : "MORE"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"edit-controls" => TouchscreenInputManager.Instance.*"DONE" : "EDIT"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q '"auto-map" => "LOCAL MAP"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'Hold any button to see its name' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "contextual command labels and control-name hint are configured" \
  || fail "DaggerPad contextual command labels are incomplete"
grep -q 'dummyInputField.ActivateInputField()' Assets/Android/Scripts/TouchscreenKeyboardManager.cs \
  && pass "touch text entry explicitly requests the software keyboard" \
  || fail "DaggerPad software keyboard activation is incomplete"
grep -q 'PlayerPrefs.GetString("TouchscreenLayoutsManager_LastSelectedLayout", "simplified-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'layout.leftJoystickEnabled = true' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'layout.rightJoystickEnabled = false' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'SelectSimplifiedLayoutForLegacyIpadSelection' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'position = new Vector2(148, 30)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'position = new Vector2(-170, 30)' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "iOS starts with dual touch zones and a safe editor control" \
  || fail "DaggerPad iOS layout defaults are missing"
grep -q 'CurrentPointerEventData = eventData' Assets/Android/Scripts/VirtualJoystick.cs \
  && grep -q 'UpdateInputFromPosition(myTouch.position)' Assets/Android/Scripts/VirtualJoystick.cs \
  && grep -q 'GetCurrentTouchDelta' Assets/Android/Scripts/VirtualJoystick.cs \
  && grep -q 'inputRadiusMultiplier: 1.6666666' Assets/Android/Prefabs/TouchscreenControlsManager.prefab \
  && grep -q 'lastLookTapTime' Assets/Android/Scripts/VirtualJoystick.cs \
  && grep -q 'TouchscreenInputManager.ClearInputState' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'iPadKeyboardMovementScale = 0.55f' Assets/Scripts/Game/InputManager.cs \
  && grep -q 'A single source owns movement each frame' Assets/Scripts/Game/InputManager.cs \
  && pass "touch, keyboard, and controller movement are sampled and arbitrated independently" \
  || fail "DaggerPad dual-zone input routing is incomplete"
for themed_control in attack use ready inventory pause more settings automap rest save load status travel logbook notebook switch_hand magic run; do
  [[ -f "Assets/Android/Textures/Resources/daggerpad_${themed_control}.png" ]] \
    || fail "themed touch asset is missing: daggerpad_${themed_control}.png"
done
grep -q '"daggerpad_attack"' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "Daggerfall-style compact touch theme is configured" \
  || fail "DaggerPad touch theme is not configured"
grep -q 'base.OnPointerDown(eventData)' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'standard actions use their normal key binding' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'Weapon readied - tap ATTACK again' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'USE / TAKE' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'SetDaggerfallGUIActive' Assets/Android/Scripts/TouchscreenInputManager.cs \
  && grep -q 'GetKeyUp(KeyCode.Escape, false)' Assets/Scripts/Game/InputManager.cs \
  && grep -q 'Pointer mode' Assets/Scripts/Game/PlayerMouseLook.cs \
  && pass "touch actions and keyboard pointer mode provide immediate feedback" \
  || fail "DaggerPad input feedback is incomplete"
grep -q 'label.text = TouchscreenInputManager.Instance.*"Done"' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'CurrentlyEditingButton == this' Assets/Android/Scripts/TouchscreenButton.cs \
  && grep -q 'CloseAllDrawersForEditing' Assets/Android/Scripts/TouchscreenInputManager.cs \
  && grep -q 'SetEditControlsActive(false)' Assets/Android/Scripts/TouchscreenInputManager.cs \
  && pass "touch editor has focused labels and reliable exit paths" \
  || fail "touch editor usability guards are missing"
grep -q 'Files > On My iPad > DaggerPad > DaggerfallUnity' Assets/Scripts/Game/UserInterfaceWindows/DaggerfallUnitySetupGameWizard.cs \
  && pass "iOS setup shows the Files-visible data location" \
  || fail "iOS setup still exposes an internal data path"
grep -q 'Actions.ReadyWeapon' Assets/iOS/Scripts/DaggerPadLifecycle.cs \
  && pass "gesture layout two-finger ready/sheathe action configured" \
  || fail "gesture layout two-finger ready/sheathe action missing"
if rg -n 'NativeFilePicker\.Pick(File|MultipleFiles).*application/zip' Assets --glob '*.cs' >/dev/null; then
  fail "a native picker still passes Android MIME syntax to iOS"
else
  pass "native ZIP pickers use platform file-type conversion"
fi

bash -n scripts/verify-source.sh scripts/prepare-game-data.sh scripts/verify-xcode-export.sh scripts/run-simulator-smoke.sh
pass "build and simulator harness scripts parse"

if (( $# == 1 )) || [[ -e "$default_dagger" ]]; then
  arena="$dagger/ARENA2"
  [[ -d "$arena" ]] || fail "Daggerfall data folder is missing ARENA2: $dagger"

  texture_count="$(find "$arena" -maxdepth 1 -type f -iname 'TEXTURE.*' | wc -l | tr -d ' ')"
  video_count="$(find "$arena" -maxdepth 1 -type f -iname '*.VID' | wc -l | tr -d ' ')"
  (( texture_count >= 472 )) && pass "reference data has $texture_count texture archives" || fail "reference data has only $texture_count texture archives"
  (( video_count >= 17 )) && pass "reference data has $video_count videos" || fail "reference data has only $video_count videos"

  for required in ARCH3D.BSA BLOCKS.BSA MAPS.BSA DAGGER.SND WOODS.WLD MONSTER.BSA FLATS.CFG PAINT.DAT TEXT.RSC SPELLS.STD; do
    [[ -f "$arena/$required" ]] || fail "reference data is missing $required"
  done
  [[ -f "$dagger/FALL.EXE" ]] || fail "reference data is missing FALL.EXE"
  pass "reference data contains the runtime-required files"
else
  skip "local Daggerfall data not present; source checks remain complete"
fi

git diff --check
pass "git diff has no whitespace errors"
