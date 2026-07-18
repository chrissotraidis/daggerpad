# Build and installation

This is the shortest supported path from a fresh clone to a running iPad build.

## Prerequisites

| Tool | Required version |
| --- | --- |
| macOS | A version supported by Xcode 26 |
| Xcode | 26.6 used by the current verified build |
| Unity | `2022.3.62f3` |
| Unity modules | iOS Build Support, IL2CPP, Apple silicon toolchain |
| iPadOS | 15 or later |

You also need an Apple ID for development signing and your own Daggerfall installation. The expected source directory directly contains `ARENA2` and `FALL.EXE`.

## Fresh-clone setup

```bash
git clone https://github.com/chrissotraidis/daggerpad.git
cd daggerpad
bash scripts/prepare-game-data.sh "/path/to/your/DAGGER"
bash scripts/verify-source.sh
```

Add the repository folder in Unity Hub and open it with `2022.3.62f3`. Let the first import finish before exporting an Xcode project.

## Physical iPad

1. Choose **DaggerPad > Build > iPadOS Device** in Unity.
2. Open `Builds/iOS/Device/Unity-iPhone.xcodeproj`.
3. In Xcode, select the `Unity-iPhone` target and choose your team under **Signing & Capabilities**.
4. Connect the iPad, trust the Mac when prompted, select the iPad as the destination, and run.
5. Move `Builds/TestData/daggerfall.zip` into iCloud Drive, AirDrop it, or otherwise make it available in Files.
6. In DaggerPad, tap **Import**, select the ZIP, complete setup, and tap **Play**.

A free Apple developer account installs a development build for a limited signing period. A paid account provides longer-lived provisioning and distribution options.

## Command-line device export

```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "$PWD" \
  -buildTarget iOS \
  -executeMethod DaggerPad.Editor.DaggerPadIosBuild.BuildDevice \
  -daggerpadBuildPath "$PWD/Builds/iOS/Device" \
  -logFile "$PWD/Builds/iOS/unity-device.log"
```

Then open the generated Xcode project and sign it as described above.

## iPad Simulator

Install the verified runtime if needed:

```bash
xcodebuild -downloadPlatform iOS -buildVersion 18.5
```

Run the full clean smoke path:

```bash
bash scripts/run-simulator-smoke.sh
```

This script exports, builds, creates or reuses `DaggerPad iPad Pro M2 iOS 18.5`, uninstalls any existing DaggerPad app from that Simulator, installs the new build, seeds the prepared ZIP, launches, and captures evidence. The uninstall is intentional and deletes that Simulator app's existing data.

## Verification commands

```bash
bash scripts/verify-source.sh
bash scripts/verify-xcode-export.sh Builds/iOS/Simulator
```

The first checks the pinned Unity/iOS configuration and real game-data contract. The second checks a generated Xcode export, including Addressables, Files integration, iPad targeting, indirect input, and the native pointer framework.

## Common fixes

### Unity opens with the wrong version

Install `2022.3.62f3` in Unity Hub and reopen the project with that editor. Do not allow an automatic project upgrade for a release build.

### Xcode says signing is required

Choose the `Unity-iPhone` app target, set a unique bundle identifier if your account requires one, enable automatic signing, and select your team. The generated `UnityFramework` target normally inherits the correct settings.

### DaggerPad rejects the ZIP

The ZIP must contain a folder with both `ARENA2` and `FALL.EXE`. Use `scripts/prepare-game-data.sh` instead of compressing only `ARENA2`.

### First launch is black

Regenerate the Xcode project through the DaggerPad build menu. The export command builds Addressables before the player; opening an old or partially generated Xcode project can omit required runtime data.

### Simulator launch fails before the app process starts

Use the pinned iOS 18.5 runtime. On the current macOS/Xcode host, iOS 26.5 can fail before any app process launches; this also reproduces with Safari and is not a DaggerPad crash.
