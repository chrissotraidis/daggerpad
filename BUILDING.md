# Building DaggerPad

## Required toolchain

- Apple-silicon Mac
- Unity `2022.3.62f3` with iOS Build Support
- Xcode 26.x with an iOS/iPadOS Simulator runtime
- iOS deployment target 15 or newer

The generated Xcode project is disposable. All durable Apple configuration lives in `Assets/iOS/Editor/DaggerPadIosBuild.cs`.

## Verify source and reference data

```sh
bash scripts/verify-source.sh
```

## Prepare the test import archive

```sh
bash scripts/prepare-game-data.sh
```

This creates `Builds/TestData/daggerfall.zip` from the untracked `ref/` install. Never add the archive or classic game data to Git or a distributed app.

## Export from the Unity editor

Use one of:

- `DaggerPad > Build > iPadOS Device`
- `DaggerPad > Build > iPad Simulator`

Default outputs are `Builds/iOS/Device` and `Builds/iOS/Simulator`.

## Batch-mode export

```sh
UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity

"$UNITY" -batchmode -quit \
  -projectPath "$PWD" \
  -buildTarget iOS \
  -executeMethod DaggerPad.Editor.DaggerPadIosBuild.BuildSimulator \
  -daggerpadBuildPath "$PWD/Builds/iOS/Simulator" \
  -logFile "$PWD/Builds/iOS/unity-simulator.log"
```

Use `BuildDevice` and a different output path for a physical-device export.

## Xcode build

Simulator:

```sh
xcodebuild \
  -project Builds/iOS/Simulator/Unity-iPhone.xcodeproj \
  -scheme Unity-iPhone \
  -configuration Debug \
  -sdk iphonesimulator \
  -destination 'platform=iOS Simulator,name=iPad Pro 13-inch (M4)' \
  build
```

Device builds require an Apple development team. Set the team in Unity Player Settings or Xcode's Signing & Capabilities panel; do not commit personal signing identifiers.

## Simulator smoke

After the prerequisites are installed, the complete build/install/launch harness is:

```sh
bash scripts/run-simulator-smoke.sh
```

Set `UNITY`, `SIMULATOR_NAME`, or `BUILD_ROOT` to override its defaults. The harness validates the generated Info.plist and Xcode project, boots the named iPad Simulator, installs DaggerPad, places the verified import archive in the app's Files folder, launches the app, and saves first-launch logs and a screenshot under `Builds/iOS/Evidence/`.

See `docs/TESTING.md` for the interactive import and gameplay checklist. Simulator success proves the generated project, launch path, Files picker presentation, and UI flow. It does not prove Metal performance, memory limits, touch latency, controller mappings, background jetsam behavior, or physical Files-provider behavior.
