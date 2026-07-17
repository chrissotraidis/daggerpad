#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
unity="${UNITY:-/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity}"
simulator_name="${SIMULATOR_NAME:-DaggerPad iPad Pro M2 iOS 18.5}"
simulator_device_type="${SIMULATOR_DEVICE_TYPE:-com.apple.CoreSimulator.SimDeviceType.iPad-Pro-12-9-inch-6th-generation-8GB}"
simulator_runtime="${SIMULATOR_RUNTIME:-com.apple.CoreSimulator.SimRuntime.iOS-18-5}"
build_root="${BUILD_ROOT:-$root/Builds/iOS}"
xcode_project="$build_root/Simulator"
derived_data="$build_root/DerivedData"
evidence_dir="$build_root/Evidence"
archive="$root/Builds/TestData/daggerfall.zip"
bundle_id="com.chrissotraidis.daggerpad"

fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }
step() { printf '\n== %s ==\n' "$1"; }

[[ -x "$unity" ]] || fail "Unity 2022.3.62f3 was not found at $unity"
command -v xcodebuild >/dev/null || fail "full Xcode is not active"
xcrun --find simctl >/dev/null || fail "simctl is not available from the active Xcode"
[[ -f "$archive" ]] || bash "$root/scripts/prepare-game-data.sh"

runtime_id="$(xcrun simctl list runtimes -j | /usr/bin/python3 -c '
import json, sys
requested = sys.argv[1]
runtimes = json.load(sys.stdin).get("runtimes", [])
available = [runtime for runtime in runtimes if ".iOS-" in runtime.get("identifier", "") and runtime.get("isAvailable", True)]
matching = [runtime for runtime in available if runtime.get("identifier") == requested]
if matching:
    print(matching[0]["identifier"])
elif requested == "latest" and available:
    print(sorted(available, key=lambda runtime: runtime.get("version", ""))[-1]["identifier"])
' "$simulator_runtime")"
[[ -n "$runtime_id" ]] || fail "Simulator runtime $simulator_runtime is unavailable. Install iOS 18.5 with: xcodebuild -downloadPlatform iOS -buildVersion 18.5"

simulator_udid="$(xcrun simctl list devices -j | /usr/bin/python3 -c '
import json, sys
name = sys.argv[1]
runtime = sys.argv[2]
devices = json.load(sys.stdin).get("devices", {}).get(runtime, [])
for device in devices:
    if device.get("name") == name and device.get("isAvailable", True):
        print(device["udid"])
        raise SystemExit
' "$simulator_name" "$runtime_id")"
if [[ -z "$simulator_udid" ]]; then
    simulator_udid="$(xcrun simctl create "$simulator_name" "$simulator_device_type" "$runtime_id")"
fi

mkdir -p "$build_root" "$evidence_dir"

step "Verify source and test data"
bash "$root/scripts/verify-source.sh"
unzip -tq "$archive"

step "Export the iPad Simulator project from Unity"
"$unity" -batchmode -quit \
  -projectPath "$root" \
  -buildTarget iOS \
  -executeMethod DaggerPad.Editor.DaggerPadIosBuild.BuildSimulator \
  -daggerpadBuildPath "$xcode_project" \
  -logFile "$build_root/unity-simulator.log"

bash "$root/scripts/verify-xcode-export.sh" "$xcode_project"

step "Build DaggerPad with Xcode"
xcodebuild \
  -project "$xcode_project/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone \
  -configuration Debug \
  -sdk iphonesimulator \
  -destination "platform=iOS Simulator,id=$simulator_udid" \
  -derivedDataPath "$derived_data" \
  CODE_SIGNING_ALLOWED=NO \
  build | tee "$build_root/xcode-simulator.log"

app_path="$(find "$derived_data/Build/Products" -maxdepth 3 -type d -name 'DaggerPad.app' -print -quit)"
[[ -n "$app_path" ]] || fail "Xcode completed but DaggerPad.app was not found"

step "Boot the iPad Simulator"
xcrun simctl boot "$simulator_udid" 2>/dev/null || true
open -a Simulator --args -CurrentDeviceUDID "$simulator_udid"
xcrun simctl bootstatus "$simulator_udid" -b

step "Install DaggerPad and seed the Files import archive"
xcrun simctl uninstall "$simulator_udid" "$bundle_id" 2>/dev/null || true
xcrun simctl install "$simulator_udid" "$app_path"
container="$(xcrun simctl get_app_container "$simulator_udid" "$bundle_id" data)"
mkdir -p "$container/Documents/Import"
cp "$archive" "$container/Documents/Import/daggerfall.zip"

step "Launch and collect smoke evidence"
player_stdout="$evidence_dir/player.stdout.log"
player_stderr="$evidence_dir/player.stderr.log"
: >"$player_stdout"
: >"$player_stderr"
launch_app() {
  xcrun simctl launch \
    --terminate-running-process \
    --stdout="$player_stdout" \
    --stderr="$player_stderr" \
    "$simulator_udid" "$bundle_id"
}
if ! launch_app | tee "$evidence_dir/launch.txt"; then
  # SpringBoard can briefly deny a launch while registering a newly installed app.
  sleep 2
  launch_app | tee "$evidence_dir/launch.txt"
fi
sleep 20
xcrun simctl io "$simulator_udid" screenshot "$evidence_dir/first-launch.png"
xcrun simctl spawn "$simulator_udid" log show \
  --last 3m \
  --style compact \
  --predicate 'process == "DaggerPad"' >"$evidence_dir/first-launch.log" || true

if ! xcrun simctl spawn "$simulator_udid" launchctl list | rg -q "$bundle_id"; then
  fail "DaggerPad was no longer running after the first-launch wait"
fi

if rg -n \
  'NullReferenceException|DllNotFoundException|TypeLoadException|MissingMethodException|OperationException|RuntimeData is null|Unable to load runtime data|fatal error|abort\(\)|SIGABRT' \
  "$evidence_dir/first-launch.log" "$player_stdout" "$player_stderr"; then
  fail "fatal-looking errors were found in the first-launch simulator log"
fi

printf '\nPASS  DaggerPad built, installed, launched, and produced simulator evidence.\n'
printf 'NEXT  In Simulator, tap Import and select On My iPad > DaggerPad > Import > daggerfall.zip.\n'
printf 'EVIDENCE  %s\n' "$evidence_dir"
