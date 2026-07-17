#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

pass() { printf 'PASS  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

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
grep -q 'Recovered Daggerfall data after iOS container relocation' Assets/Scripts/DaggerfallUnity.cs \
  && pass "iOS app updates recover relocated imported data" \
  || fail "iOS container-relocation recovery is missing"
grep -q 'RegenerateDaggerPadLayoutIfMissing("simplified-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'RegenerateDaggerPadLayoutIfMissing("gesture-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && grep -q 'RegenerateDaggerPadLayoutIfMissing("accessibility-layout")' Assets/Android/Scripts/TouchscreenLayoutsManager.cs \
  && pass "simplified, gesture, and accessibility layouts are bundled" \
  || fail "DaggerPad control presets are missing"
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

arena="$root/ref/The Elder Scrolls Daggerfall/DF/DAGGER/ARENA2"
dagger="$root/ref/The Elder Scrolls Daggerfall/DF/DAGGER"
[[ -d "$arena" ]] || fail "reference ARENA2 folder missing"

texture_count="$(find "$arena" -maxdepth 1 -type f -iname 'TEXTURE.*' | wc -l | tr -d ' ')"
video_count="$(find "$arena" -maxdepth 1 -type f -iname '*.VID' | wc -l | tr -d ' ')"
(( texture_count >= 472 )) && pass "reference data has $texture_count texture archives" || fail "reference data has only $texture_count texture archives"
(( video_count >= 17 )) && pass "reference data has $video_count videos" || fail "reference data has only $video_count videos"

for required in ARCH3D.BSA BLOCKS.BSA MAPS.BSA DAGGER.SND WOODS.WLD MONSTER.BSA FLATS.CFG PAINT.DAT TEXT.RSC SPELLS.STD; do
  [[ -f "$arena/$required" ]] || fail "reference data is missing $required"
done
[[ -f "$dagger/FALL.EXE" ]] || fail "reference data is missing FALL.EXE"
pass "reference data contains the runtime-required files"

git diff --check
pass "git diff has no whitespace errors"
