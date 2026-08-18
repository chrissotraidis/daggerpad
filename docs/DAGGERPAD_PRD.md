# DaggerPad — Product Requirements & Feasibility Document

**A native iPadOS port of Daggerfall Unity**

- Version 1.0 — 2026-07-17
- Status: source-grounded research complete; implementation not started
- Intended reader: an implementation agent (human or bot) who will build this from start to finish
- Evidence grades used throughout: **[V]** verified against source code, commits, or official documentation · **[I]** reasonable technical inference from verified facts · **[U]** unverified community claim · **[X]** requires a physical build/device experiment

---

## 0. Verdict

# **GO, BUT WITH MAJOR LIMITATIONS**

**Executive conclusion (10 lines):**

1. DaggerPad is technically feasible with far less work than the research brief feared, because the hard problems are already solved in `Vwing/daggerfall-unity-android`: it runs Daggerfall Unity on **Unity 2022.3.62f3** with a complete touch UI, on-screen keyboard bridge, zip-based game-data importer, mod importer, and mobile performance work — and it was last committed to **yesterday** (2026-07-16). **[V]**
2. A source-level IL2CPP audit found the base game is essentially AOT-clean: zero `unsafe`, zero desktop P/Invoke, zero Windows-only APIs, zero runtime threading hazards; the fork is **~4 small fixes away from an iOS build that compiles**. **[V]**
3. The permanent limitation: **C# code mods can never work on iOS** (IL2CPP forbids `Assembly.Load(byte[])` and the embedded Mono compiler); asset-only mods work but every `.dfmod` must be **rebuilt for the iOS build target**. **[V]**
4. The upstream 2019.4 path is dead (Apple now requires Xcode 26/iOS 26 SDK for store uploads; the 2019.4 editor is Intel-only on a Rosetta runway ending with macOS 27) — forking the Android fork sidesteps this entirely. **[V]**
5. The single unresolved technical gate is whether a **Unity 2022.3-generated Xcode project builds under Xcode 26** — untested publicly; if it fails, the fallback is a moderate 2022.3 → Unity 6.3 LTS migration, which is required for App Store distribution anyway. **[X]**
6. Performance on an M2 iPad Pro is very likely excellent (desktop min-spec is i3/GTX 660-class); the real measured risk is **memory** — the Android port documents out-of-memory crashes in long sessions and in Daggerfall Castle. **[V]/[X]**
7. Save files are plain JSON + a JPG, cross-platform by design; desktop↔iPad save compatibility is expected to work by copying folders through the Files app. **[V]**
8. Legal posture: engine is MIT; user-supplied freeware game data follows the shipped ScummVM/GenZD App Store precedents; but Bethesda's one documented sensitivity is **mobile ports of TES games** (they asked OpenMW not to promote mobile footage), and "Daggerfall" naming in a store listing invites a Guideline 5.2.1 complaint. **[V]**
9. Therefore: personal builds and TestFlight are low-risk; a public App Store release is a separate legal/strategic decision, not a technical one.
10. Recommended path: run the 1–3 day spike in §15 first; on success, execute the ~6–10 week roadmap in §16 to a polished personal/TestFlight build, and defer the store decision to a gate with counsel.

**Feasibility score: 8 / 10** (technical feasibility of a working, polished personal/sideload/TestFlight iPad port).
**Confidence in that judgment: high (~85%)** for the technical verdict — grounded in file-level code audit of both repos plus official Apple/Unity documentation. **Medium (~55%)** for public App Store distribution, which depends on Apple review discretion and Bethesda's posture, neither of which can be verified in advance.

---

## 1. Inspection record

Both repositories were cloned locally (blob-filtered, full history + working tree) and inspected at source level. They remain in this repo for the implementation agent:

| | Upstream | Android fork |
|---|---|---|
| Local path | `research/daggerfall-unity` | `research/daggerfall-unity-android` |
| Remote | `github.com/Interkarma/daggerfall-unity` | `github.com/Vwing/daggerfall-unity-android` |
| Branch | `master` | `android` (default) |
| HEAD inspected | `81e89e90c27bc3c1a7a61871e545fad129174dec` (2026-06-30) | `0fa65294523a132a0e5389d125f58d6566a1e815` (2026-07-16) |
| `git describe` | `v1.1.1-cve-2025-116-g81e89e90c` | tag `v1.1.1.8` = last release (2026-05-25) |
| Unity version (`ProjectSettings/ProjectVersion.txt`) | **2019.4.41f2** | **2022.3.62f3** |
| Latest release | `v1.1.1-cve-2025` (2025-10-05, security rebuild of v1.1.1 of 2024-05-09) | `v1.1.1.8` (2026-05-25) |
| Maintainer today | KABoissonneault (community maintainer; Interkarma stepped back Jan 2024) | Vivian "Vwing" Wing (solo) |
| Activity | 70 commits Jan–Jul 2026; alive, low-volume maintenance | commits on 2026-07-16; active |
| Merge-base of fork vs upstream | `cb6463d500768ec319c280279f49f8675248a6e3` (2024-06-13, v1.1.1 + 21 commits) | fork is 227 commits ahead, upstream is 264 commits ahead of the base |

Other facts recorded: upstream v1.0.0 (2023-12-30) declared **all game features implemented** ("Daggerfall Unity is finally considered complete") **[V]**; upstream supports Windows/Linux/macOS only **[V]**; there is **no working or seriously-attempted iOS port anywhere** — GitHub search finds exactly one iOS-named fork (`jonathansrich/daggerfall-unity-iOS`, created 2025, zero commits by its owner) and the only historical evidence is a 2017 forum post by user Dimillian reporting DFU "builds and runs" on iOS with no published follow-up **[V]**. Neither Interkarma nor KABoissonneault has ever publicly commented for or against an iOS port **[V-absence]**. Vwing upstreams mobile perf work (PRs #2789–#2792, two merged June 2026) **[V]**.

---

## 2. Recommended foundation

**Fork `Vwing/daggerfall-unity-android`, branch `android`, at commit `0fa65294523a132a0e5389d125f58d6566a1e815`, into a new repo `daggerfall-unity-ipados`. Develop on a branch named `ipados`. Stay on Unity 2022.3.62f3 for the spike and Phase 1; migrate to Unity 6.3 LTS in Phase 4 (or earlier if the spike forces it).**

Why this base beats the four alternatives (§4 of the research brief):

1. **Latest upstream stable tag (v1.1.1)** — still Unity 2019.4.41f2. The 2019.4 iOS toolchain is dead in 2026 (§3). Would require redoing the entire Unity migration *and* building a mobile layer from scratch (upstream has **zero** platform conditionals in `Assets/` **[V]**). Rejected.
2. **Upstream master** — same Unity problem, plus no mobile layer. Rejected.
3. **The Android fork** — the Unity 2022.3 migration is done, shipped, and proven on mobile ARM64 (the fork ships IL2CPP ARM64 Android builds **[V]**). The touch layer is genuinely portable: it gates on `Application.isMobilePlatform`, not `UNITY_ANDROID` — only ~30 `#if UNITY_ANDROID` occurrences across 13 core files, and Android-specific behavior funnels through three seams (`AndroidUtils`, `Paths`, `OpenPointerCapture`) **[V]**. The bundled file-picker plugin (`yasirkula/NativeFilePicker`) **already includes its iOS native backend** (`Assets/Plugins/NativeFilePicker/iOS/NativeFilePicker.mm`) **[V]**. **Selected.**
4. **A new branch cherry-picking Android changes onto upstream** — the mobile work touches ~130 modified core files (screen-abstraction substitutions, input-pipeline split, font-renderer rewrite) plus scenes and prefabs; cherry-picking re-does the merge without the fork's two years of on-device testing. Rejected.
5. **Another fork** — none is viable (the only iOS fork is empty; marcospampi/InconsolableCellist forks are 2021–2022 predecessors Vwing already absorbed). Rejected.

Accepted trade-off: the `android` branch's game-logic base is DFU v1.1.1 + 21 commits (June 2024) and has **not** merged upstream since — it is ~264 upstream bug-fix commits behind **[V]**. Mitigation in §13 (Vwing mirrors upstream on the fork's `master` and develops 2026 features against it, so a future merge is well-trodden).

---

## 3. The exact Unity/Xcode compatibility situation (July 2026)

### Apple's requirements

- Since **2026-04-28**, App Store Connect uploads must be built with **Xcode 26** using the **iOS 26 / iPadOS 26 SDK** (developer.apple.com/news/upcoming-requirements, id=02032026a). **[V]**
- Current shipping toolchain: **Xcode 26.6** (June 25, 2026); Xcode 27 is in beta. Current iPadOS: **26.5.2**. **[V]**
- Xcode 26's supported iOS deployment-target range is **iOS 15–26** — so minimum deployment target must be ≥ iOS 15. **[V]**
- This bar applies to **App Store/TestFlight uploads**. Personal development installs to your own device only require an Xcode recent enough to talk to your iPadOS version — which, on iPadOS 26, is also Xcode 26. So in practice **everything must compile under Xcode 26**. **[I]**

### Unity's situation

| Unity train | Status July 2026 | iOS relevance |
|---|---|---|
| 2019.4 LTS (upstream) | EOL June 2022 | Editor is Intel-only (Rosetta required); Unity's official Xcode-15 fixes were never backported (floor: 2021.3.31f1/2022.3.10f1); Rosetta becomes gaming-only after macOS 27 (Apple WWDC25 statement); Unity Cloud Build cannot even run it on current runners. **Dead end.** **[V]** |
| 2021.3 LTS | EOL Feb 2025 | Not a landing zone. **[V]** |
| **2022.3 LTS (the fork)** | Public support ended May 2025 (final public build 2022.3.62f1; the fork's **2022.3.62f3** is evidently a post-EOL security-patch build — the fork upgraded to it in May 2026 citing the Sept 2025 Unity CVE) **[V]/[I]** | Native Apple Silicon editor; official Xcode 15 support (2022.3.10f1+); **no official validation against Xcode 26** — no public success or failure reports found. This is the project's #1 open technical question. **[X]** |
| Unity 6.0 (6000.0) LTS | Supported until Oct 2026 | Short runway; skip. **[V]** |
| **Unity 6.3 (6000.3) LTS** | **Current LTS, supported to Dec 2027** | Officially requires Xcode 16+; community-confirmed building and submitting with Xcode 26.x, with known workaround-able issues (notably the `UnityRuntime.framework` minOS Info.plist TestFlight rejection, Unity issue IN-144247, fixed by a post-build script). **The store-grade target.** **[V]/[U]** |

Facts that de-risk a later 2022.3 → 6.3 migration: the **Built-in Render Pipeline** (which DFU uses) is deprecated but officially supported through the Unity 6.7 LTS lifecycle (~end-2028) **[V]**; the **legacy Input Manager** (which DFU uses) still works in Unity 6.5 **[V]**; iOS remains IL2CPP under the CoreCLR transition **[V]**; Unity Personal is free (ceiling $200k revenue), splash optional, Runtime Fee cancelled **[V]**.

### Migration risk assessment (per the brief's scale)

- From upstream 2019.4: **MAJOR migration — but it is already done** by the fork (upgrade commits `c2ef7ed66` → `61d9a8c1c`, May 2024; package bumps: TextMeshPro 2.1.4→3.0.9, collections 0.9→2.6.6, addressables, localization, postprocessing 3.4.0; QualitySettings/ProjectSettings serialization migrations; 4 shader touch-ups). **[V]**
- Fork → personal iPad builds: **NO MIGRATION REQUIRED** *if* the spike confirms Xcode 26 builds the 2022.3-generated project. **[X]**
- Fork → App Store builds: **MODERATE migration** (2022.3 → 6000.3; same render pipeline, same input system, same serialization era; the fork has already crossed the big 2019→2022 chasm).
- Mod/asset-bundle compatibility impact of upgrading: existing desktop `.dfmod` files are unusable on iOS **regardless of engine version** (see §11), so the migration does not additionally break anything for iPad users.

---

## 4. Blocker matrix

| # | Severity | Blocker | Evidence | Resolution |
|---|---|---|---|---|
| B1 | **CRITICAL (open)** | Unity 2022.3.62f3-generated Xcode project may fail to build under Xcode 26 (linker, minOS, framework plist) — no public reports either way | §3 **[X]** | Spike step 5. Fallback: migrate to Unity 6.3 LTS (moderate, known-good with Xcode 26) |
| B2 | **HIGH (known fix)** | `PointerCaptureNativeInterface.cs` has `AndroidJavaClass` usage **outside** its `#if UNITY_ANDROID` guard → CS compile errors on iOS target | `research/daggerfall-unity-android/Assets/Android/OpenPointerCapture/Scripts/PointerCaptureNativeInterface.cs:11,66–118` **[V]** | Wrap whole class in `#if UNITY_ANDROID && !UNITY_EDITOR` |
| B3 | **HIGH (known fix)** | iPhone API compatibility level defaults to .NET Standard → `System.CodeDom` in the mod compiler fails to compile for iOS | `ProjectSettings.asset` (`apiCompatibilityLevelPerPlatform` lacks iPhone); `Assets/Game/Addons/CSharpCompiler/Compiler.cs:16–17,47`; `CustomDynamicDriver.cs:254` **[V]** | Either set iPhone → .NET Framework (quick, spike) or excise `CSharpCompiler/` + `mcs.dll` from iOS builds (right long-term; required for store anyway) |
| B4 | **HIGH** | Mods directory lives in `StreamingAssets/Mods`, which is read-only inside an iOS app bundle; the fork's Android answer (extract APK to persistentDataPath) doesn't apply | `Assets/Scripts/Paths.cs:39–80`; `ModManager.cs:135` **[V]** | Add `IPhonePlayer` branch to `Paths`: mirror writable StreamingAssets into Documents on first run; point `Paths.StreamingAssetsPath` there |
| B5 | **HIGH (non-technical)** | App Store listing using "Daggerfall" name/imagery invites a Guideline 5.2.1 IP complaint; Bethesda's documented sensitivity is specifically mobile TES ports | §12 **[V]** | Gate any store release on legal review; name the app "DaggerPad"; no Bethesda assets in metadata |
| B6 | MEDIUM | No `OnApplicationPause`/`OnApplicationFocus` handling anywhere in the fork — iOS suspends apps aggressively; risk of progress loss and audio glitches | grep across fork `Assets/` **[V]** | Implement auto-save + audio pause on `OnApplicationPause(true)`; test resume-from-jetsam |
| B7 | MEDIUM | Memory: Android port documents OOM crashes in long sessions and Daggerfall Castle; iOS jetsam limits are strict | v1.1.1.0 release notes **[V]**; iPad limits **[X]** | Fork's mitigations already present (object culling, `AssetCacheThreshold=10`, GC throttle); measure with Instruments in Phase 2 |
| B8 | MEDIUM | Injected/replacement textures force ARGB32 on Android (no DXT); iOS also lacks DXT | `Assets/Scripts/Utility/AssetInjection/TextureReplacement.cs:107` **[V]** | Extend the guard with `|| UNITY_IOS` (or ASTC path) |
| B9 | MEDIUM | Mod ecosystem requires per-platform rebuilds; zero iOS-target mods exist today | §11 **[V]** | Add iOS BuildTarget to `CreateModEditorWindow.cs:52–59`; curate + rebuild starter mods |
| B10 | LOW | Two unguarded `Process.Start` "open folder" click handlers throw on iOS if tapped | `DaggerfallUnitySetupGameWizard.cs:701` (fork), `DaggerfallUnitySaveGameWindow.cs:691` (both) **[V]** | `#if UNITY_IOS` guard → open Files app URL or hide button |
| B11 | LOW | Android "restart app" flows (`AndroidUtils.RestartAndroid`) have no iOS equivalent (iOS apps cannot relaunch themselves) | `AndroidUtils.cs` **[V]** | Replace with "please close and reopen DaggerPad" prompt |
| B12 | LOW | Bundle ID still `com.Company.ProductName`; landscape/orientation, ProMotion, `targetFrameRate` (mobile default renders at 30 fps) unset for iOS | `ProjectSettings.asset` **[V]** | Trivial project-settings pass in Phase 1 |
| B13 | LOW | Fan-made `TESFonts` pack in `Assets/Resources/Fonts/TESFonts/` needs its bundled permission notice preserved and still merits storefront review | bundled `Readme_TES_Fonts.txt` permits use without additional permission **[V]** | Preview packaging now includes the notice; retain legal review before any store or marketplace submission |

Nothing found rises to "potentially project-killing" for personal builds. The kill risks are concentrated in public distribution (§12) and B1's fallback failing (§15 kill conditions).

---

## 5. IL2CPP / ARM64 compatibility assessment

**Verdict: the base game (mods disabled) compiles and runs under IL2CPP/ARM64 with MINOR changes** (B2, B3, B10 above). Full audit basis — searched both repos for every category in the research brief:

- `Reflection.Emit` / `DynamicMethod` / `AssemblyBuilder`: only inside the mod-compiler chain (`CustomDynamicDriver.cs:254` `AssemblyBuilderAccess.RunAndSave`) — mods-only. **[V]**
- `Assembly.Load`: `Mod.cs:962` (`Assembly.Load(sources[i].sourceTxt.bytes)` — precompiled mod code) and `Mod.cs:1014` (editor-only). Mods-only. **[V]**
- Runtime C# compilation: `Assets/Game/Addons/CSharpCompiler/` wraps the **embedded Mono compiler `mcs.dll`** (MIT/X11-licensed Ximian code, plugin platform "Any"): `Compiler.CompileSource` → `CodeCompiler` → `CustomDynamicDriver` (Reflection.Emit). Reached from `ModManager.CompileFromSourceAssets` (`ModManager.cs:770–792`) and the debug console `compile` command (`DefaultCommands.cs:2443–2459`). Mods/dev-only. **[V]**
- `Process.Start`: 2 UI convenience handlers (B10). `FileSystemWatcher`, `GetCommandLineArgs`, registry, `System.Drawing`, `Windows.Forms`: **zero hits**. **[V]**
- P/Invoke & native plugins: **no `.so`/`.dylib`/`.bundle`/desktop-native `.dll` in either repo**. Managed DLLs: `FullSerializer` (reflection-only save serializer — IL2CPP-OK), `INIFileParser`, `mcs.dll` (exclude on iOS). The fork's only `DllImport`s are NativeFilePicker's `__Internal` iOS bindings — the correct iOS static-link pattern. Android-only `.aar`s (`NativeFilePicker.aar`, `android-input-capture.aar`, `FolderPicker.java`) are inert on iOS. **[V]**
- `unsafe`: zero blocks; `allowUnsafeCode: 0`; no `mcs.rsp`/`csc.rsp` anywhere. **[V]**
- Threading: zero `new Thread`/`ThreadPool`/`Task.Run` in game code. **[V]**
- AOT-risky reflection: `Activator.CreateInstance` on concrete registered types (effects, guilds, custom items, UI windows via `UIWindowFactory.cs:159`) — safe under IL2CPP full generic sharing (default since Unity 2022.1 **[V]**); managed stripping is set to Minimal on iPhone in the fork **[V]**; no `link.xml` exists — add one covering `FullSerializer` + save DTOs as cheap insurance. **[I]**
- Scripting backend reality check: both repos configure **Mono** for all platforms; the fork *ships Mono on Android deliberately so runtime mod compilation keeps working there*, and separately ships IL2CPP builds **without** mod support. iOS has no such choice — IL2CPP is mandatory — so **the fork's own strategy proves the consequence: code mods cannot exist on iOS**. **[V]**
- Packages: every package in both manifests supports iOS; the fork already sets **iOS graphics API = Metal (manual)**, Linear color space, landscape autorotation, targetDevice iPhone+iPad. **[V]**

Files needing `UNITY_IOS` conditional work for feature parity (~20, full list in the audit): `PointerCaptureNativeInterface.cs`, `Paths.cs`, `DaggerfallUnityApplication.cs`, `DaggerfallUnitySetupGameWizard.cs` (~15 sites), `DaggerfallUnitySaveGameWindow.cs`, `SaveLoadManager.cs`, `ModLoaderInterfaceWindow.cs` (3 sites), `InputManager.cs:122`, `PlayerMouseLook.cs:70`, `TextBox.cs:172,180`, `PauseOptionsDropdown.cs:49`, `DaggerfallTalkWindow.cs:471`, `DaggerfallDaedraSummonedWindow.cs:96,129`, `DaggerfallTravelMapWindow.cs:1190`, `DaggerfallJoystickControlsWindow.cs:60,146,535`, `TextureReplacement.cs:107`, plus editor-side `CreateModEditorWindow.cs:52–59` (add iOS BuildTarget).

---

## 6. Android-fork reuse map (file-by-file)

Classification: **DR** = directly reusable · **RA** = reusable after abstraction · **DO** = design reference only · **NR** = not relevant to iPadOS · **RD** = risky divergence from upstream (keep, but audit at every upstream merge).

| Area | Files (fork-relative) | What it does | Class |
|---|---|---|---|
| Touch input hub | `Assets/Android/Scripts/TouchscreenInputManager.cs` | Static axes/keys shim injected into `InputManager.GetPollKey` (`InputManager.cs:1797–1798`, axes `:2032–2033`); renders touch canvas to RenderTexture composited over retro GUI | **DR** |
| Virtual joystick | `Assets/Android/Scripts/VirtualJoystick.cs` | Floating half-screen joysticks: left = movement axes, right = camera look; tap-in-deadzone <0.5 s = `ActivateCenterObject` (tap-to-interact) | **DR** |
| Touch buttons | `Assets/Android/Scripts/TouchscreenButton.cs` + `StaticTouchscreenJoystickOrDPad.cs`, `TouchscreenButtonEnableDisableManager.cs`, `TouchscreenCanvasUXController.cs`, `UnityUIPopup.cs`, `UnityUIUtils.cs`, prefabs under `Assets/Android/Prefabs/` | Drag/resize/remap on-screen buttons; drawer groups; dpad; maps to Actions or raw KeyCodes via `SetKey` | **DR** |
| Layout serialization | `TouchscreenLayoutsManager.cs`, `TouchscreenButtonConfiguration.cs`, `AndroidUpgradeManager.cs` | Named JSON layouts in `PersistentDataPath/TouchscreenLayouts/`; import/export as zip; custom button textures via picker; versioned schema; cross-version migration | **DR** (rename manager) |
| Input pipeline | `Assets/Scripts/Game/InputManager.cs` (378 lines changed), `PlayerMouseLook.cs` | Look-source split (mouse/controller/touch), touch axis merge, `Custom01–10` mod-bindable actions, `ToggleRun` | **RA** (1 ifdef + `CapturedInput` reference) |
| Screen abstraction | `Assets/Android/Scripts/AndroidScreenManager.cs` (`AScreen`) + ~40 call sites | Real-resolution vs window abstraction; rotation/resolution change handling; camera aspect + UI RT rebuild | **DR** (misleading name only) |
| UI text perf | `Assets/Scripts/Game/UserInterface/TextLabel.cs`, `DaggerfallFont.cs`, `Assets/Shaders/DaggerfallSDFFont.shader` | Single-draw-call label rendering (was: one draw call **per letter**, tens of thousands on book screens) | **DR** / **RD-lite** |
| Automap touch | `DaggerfallAutomapWindow.cs` (+`Automap.cs`) | 1-finger pan, 2-finger pinch-zoom + twist-rotate with 3D pivot; gated `Application.isMobilePlatform` | **DR** |
| Travel map | `DaggerfallTravelMapWindow.cs` | Touch-up click, 3× location-dot hit area, keyboard-bridge search | **RA** (swap ifdef for mobile check) |
| Native keyboard | `TouchscreenKeyboardManager.cs` + `TextBox.cs` hooks | Tap a retro-UI TextBox → hidden TMP InputField summons the **OS keyboard**, mirrors text back; identical mechanism works on iOS | **DR** |
| Game-data import | `FolderBrowserAndroid.cs`, `Assets/Scripts/Utility/Unzip.cs`, `PackedDatFileUtils.cs` | Picker (zip) → unzip with progress → find `arena2` → unpack `PACKED.DAT` if needed → `DFValidator` → copy to persistent path → error reporting | **DR** (picker already has iOS backend) |
| Path facade | `Assets/Scripts/Paths.cs` | Android branch self-extracts APK StreamingAssets via SharpZipLib | facade **DR**; Android branch **NR** (iOS StreamingAssets is a real directory); needs iOS writable-mirror branch |
| Mod import | `ModLoaderInterfaceWindow.cs` (+645 lines), `ModManager.cs` | In-game Import/Remove Mod (zip/.dfmod), load-validation (`AssetBundle.LoadFromFile` succeeds + contains `.dfmod.json`), loose-StreamingAssets import, progress UI | **RA** (restart flow + error text are Android) |
| Saves | `SaveLoadManager.cs` (43 lines) | Probe-write before committing save; permission-failure UX | **RA** |
| Lifecycle | — | **Nothing exists** (no OnApplicationPause anywhere) | gap — write new for iOS |
| Perf/culling | `CulledGameObjectManager.cs`, `ActiveGameObjectDatabase.cs` (`includeInactive` semantics), `DFULayerMasks.cs`, commit `a6a95bbe9` (XZ-only cull distance) | Optional distance-culling of dungeon blocks/billboards/loot | code **DR**; **RD** behaviorally (quest logic must see culled objects — audit on merges) |
| GC/memory | `Assets/Scripts/Internal/DaggerfallGC.cs`, `DisableGarbageCollectionDuringMenuing` setting, AudioMixer mute-during-unload hack, `AssetCacheThreshold` 25→10 | Throttled `UnloadUnusedAssets` + cache pruning | **DR** |
| Settings surface | `SettingsManager.cs`, `defaults.ini.txt`: `ScreenOrientationMode`, `EnableObjectCulling`, `HUDGoesOnTop`, `WeaponSwingModeAndroid=2`, framerate slider 5–120 | Mobile settings | **DR** (rename `WeaponSwingModeAndroid`) |
| Weapon input | `WeaponManager.cs`, `FPSSpellCasting.cs` | Touch swing-mode variants, sphere-cast attack option | **DR** |
| Android-native | `AndroidManifest.xml`, `FolderPicker.java`, `android-input-capture.aar`, `AndroidUtils.cs` | SAF pickers, all-files-access, pointer capture, restart | **NR/DO** — iOS equivalents: Info.plist keys, `UIDocumentPicker` (via bundled NativeFilePicker.mm), native cursor lock, relaunch prompt |
| Build tooling | `Assets/Android/Editor/AndroidBuildTool.cs` | Multi-config batch builder | **DO** — template for an iOS build tool |

**Recommendation (restated): fork the fork.** Cherry-picking is re-merging 130+ files without the integration testing; reimplementing controls discards two years of on-device iteration; starting from upstream re-opens the dead 2019.4 toolchain.

---

## 7. Game-data import system

### What the engine actually requires **[V]**

Validation is `DFValidator.ValidateArena2Folder` (`Assets/Scripts/API/DFValidator.cs:88–196`) — existence/count checks only:

- `TEXTURE.???` — **≥ 472 files**; `ARCH3D.BSA`; `BLOCKS.BSA`; `MAPS.BSA`; `DAGGER.SND`; `WOODS.WLD`; `*.VID` — **≥ 17** (only when `requireVideos=true`, which the setup wizard passes: `DaggerfallUnitySetupGameWizard.cs:797`).
- Additionally opened at runtime (`ContentReader.cs:290–302` et al.): `MONSTER.BSA`, `FLATS.CFG`, `PAINT.DAT`, `TEXT.RSC`, `SPELLS.STD`, `FALL.EXE` (item/spell templates), climate/politic `.PAK`, CIF/RCI/IMG/FLC images. Classic `SAVE0–5` folders only for classic-save import. Music does **not** need `MIDI.BSA` (songs ship as engine TextAssets).
- Practical size: a complete `arena2` is **~450–600 MB**, cutscene `.VID`s being the bulk **[I — estimate; verify with a real install]**. Budget ~1.2 GB free space during import (zip + temp extraction + final copy).
- Path config: ini key `MyDaggerfallPath` (`SettingsManager.cs:152`); resolution order in `DaggerfallUnity.SetupArena2Path()` (`DaggerfallUnity.cs:337–419`) ends at `StreamingAssets/GameFiles` — the hook intended for *bundled-data* distributions (do not use; see §12).

### The v1 onboarding flow (technically and legally realistic: **YES**)

1. **Install DaggerPad** (Xcode, TestFlight, or sideload).
2. First launch → setup wizard's iOS data screen (adapted `FolderBrowserAndroid.cs`, which becomes `GameDataImportScreen`): one big **"Import Daggerfall Game Data"** button.
3. Button → `NativeFilePicker.PickFile` → **iPadOS Files document picker** (`UIDocumentPickerViewController` via the already-bundled `NativeFilePicker.mm`). User picks a **ZIP of their Daggerfall install** from iCloud Drive, On My iPad, a USB drive, or anything Files exposes.
4. App copies the picked file into its sandbox (picker import mode — **no security-scoped bookmarks needed** for the copy model **[V]**), unzips to `temporaryCachePath` with the existing progress UI, locates `arena2` in any nested directory, unpacks `PACKED.DAT` if `ARCH3D.BSA`/`DAGGER.SND` are missing (CD-install layout — `PackedDatFileUtils.cs`), runs `DFValidator` with per-missing-file error text, copies to `<container>/Documents/DaggerfallUnity/Daggerfall/arena2`, sets `MyDaggerfallPath`, deletes temps.
5. Game proceeds to resolution/options wizard pages → main menu. **No manual path editing.**

Alternative ingestion paths, all free with two Info.plist keys (`UIFileSharingEnabled`, `LSSupportsOpeningDocumentsInPlace`): drag-and-drop into the app's Files folder, Finder file sharing from a Mac, AirDrop-then-import. Direct **folder** picking (not zip) is a nice-to-have Phase 2 addition (NativeFilePicker supports folder export; folder import can use `UIDocumentPicker` with `.folder` UTType — small native shim) — the zip flow ships first because it is already implemented and tested on Android. **[V]**

Where users get the data, and what the app may say: Daggerfall is free from Bethesda on Steam (app 1812390, "The Elder Scrolls: Daggerfall", free since 2022) and GOG **[V]**. On a Mac/PC: install → compress the game folder → send to iPad. The app may *instruct* users how to do this (ScummVM's docs do exactly this for its App Store app **[V]**); the app must not download or bundle the data itself (§12). Automating extraction of a user-supplied archive on-device is processing the user's own files — the shipped GenZD/ScummVM/iDOS precedents all do equivalent things. **[I — precedent-based; not legal advice]**

Storage: game data ~0.6 GB + saves (a few MB each) + mods; require ~2 GB free at first run for comfort.

---

## 8. Touch controls & input design

### Ground truth about DFU input **[V]**

- Legacy `UnityEngine.Input` throughout; no InputSystem package. Full `Actions` enum a scheme must cover (`InputManager.cs:324–384`): `Escape, ToggleConsole, MoveForwards, MoveBackwards, TurnLeft, MoveLeft, TurnRight, MoveRight, FloatUp, FloatDown, Jump, Crouch, Slide, Run, Rest, Transport, StealMode, GrabMode, InfoMode, TalkMode, CastSpell, RecastSpell, AbortSpell, UseMagicItem, ReadyWeapon, SwingWeapon, SwitchHand, Status, CharacterSheet, Inventory, ActivateCenterObject, ActivateCursor, LookUp, LookDown, CenterView, Sneak, LogBook, NoteBook, AutoMap, TravelMap, QuickSave, QuickLoad, PrintScreen, AutoRun` + axis actions + joystick-UI actions.
- Weapon swing modes (`WeaponManager.cs`, setting `WeaponSwingMode`): **0 Vanilla** = hold SwingWeapon + move mouse, gesture accumulated and bucketed into 15° radial sections → attack direction (Right/Up/Left/DownLeft/Down/DownRight), direction affects damage; **1 Click** = tap attacks with random direction; **2 Hold** = repeat while held. The Android port **did not do swipe combat** — it defaults to Hold (`WeaponSwingModeAndroid=2`).
- Upstream has **complete gamepad support**: axis bindings, controller-agnostic button normalization, a controller-driven virtual cursor for the retro UI. **[V]**
- The retro UI (320×200 virtual space, `DaggerfallBaseWindow`/`BaseScreenComponent`) is driven by `Input.mousePosition` — and Unity's legacy input maps the primary touch to mouse position by default, so **every DFU menu is tap-navigable for free**; what breaks is hover, right/middle-click, and text entry — exactly the three things the fork fixed (keyboard bridge, automap rewrite, hit-target scaling). **[V]**

### The distinguishing interaction: swipe-to-swing

Directional swipes map **naturally** onto Daggerfall's swing system, because Vanilla mode already converts a 2-D pointer gesture into the attack direction — the engine-side work is done. The fork already feeds touch-drag deltas into the mouse-look path (`InputManager.cs:654–660`: pointer `delta * TouchscreenSensitivity` becomes `mouseX/mouseY` when the right-side virtual joystick is in mouse-look mode). Implementation: a "Gesture Combat" mode where, with weapon readied, a right-zone swipe (touch-down → drag → up) synthesizes `SwingWeapon`-held + the drag deltas, driving `TrackMouseAttack`'s existing accumulation and threshold (`WeaponAttackThreshold` setting). Estimated at 2–4 days including tuning. **This is the port's signature feature — no other DFU platform has real gesture combat.** **[I — design; engine plumbing verified]**

### Layouts (all built on the fork's shipped layout system — JSON, editable in-game, import/export)

1. **Default touch**: left floating move joystick (tap = interact); right zone = camera drag; tap center reticle = activate; bottom-right action cluster (Attack [hold-mode], Ready Weapon, Cast, Jump) with a drawer for (Crouch, Sneak, Steal/Grab/Info/Talk mode, Rest, Transport, Use Item); top corners: Automap, Inventory; long-press on interact = Info mode identify. Swing mode: Hold (proven default).
2. **Simplified**: joystick + three buttons (Attack, Interact, Menu drawer), auto-run toggle on, Click-to-attack.
3. **Gesture (optional flagship)**: default layout minus Attack button; swipe-to-swing enabled; two-finger tap = ready/sheathe.
4. **Controller**: upstream bindings + the fork's shipped gamepad touch-layout for the few UI-only needs; verify MFi/DualSense/Xbox over iPadOS GCController→Unity mapping. **[X]**
5. **Keyboard/mouse/trackpad**: desktop keybinds work as-is (`KeyBinds.txt`); mouse look needs iPadOS cursor lock — set `UIApplicationSupportsIndirectInputEvents`, use Unity's `Cursor.lockState`; the fork's `CapturedInput` facade gets an iOS branch that just passes through `Input`. Verify relative-delta behavior on device. **[X]**
6. **Accessibility**: everything toggles (ToggleRun exists; add toggle-crouch/sneak), oversized buttons via the existing resize editor, no-gesture guarantee (all actions reachable as buttons), adjustable long-press/hold thresholds, left/right-handed mirroring (layout editor already supports arbitrary placement).
7. **Apple Pencil**: works automatically as a touch pointer for the retro UI (precision inventory/map taps); explicitly *not* a required input. Optional Phase 3 nicety: Pencil hover (M2 iPad supports hover) → tooltips, restoring the hover affordance touch loses. **[I]**

### Touch-only acceptance test — screens at risk **[V]**

Test: create a character, escape Privateer's Hold, fight, loot, automap, save, load — touch only.

| Screen | Status |
|---|---|
| Character creation incl. name entry | **Solved** — fork's native-keyboard bridge (`CreateCharNameSelect` modified) |
| Interior automap | **Solved** — fork's pan/pinch/rotate rewrite |
| Save/load window (name entry) | **Solved** — keyboard bridge + `SubmittedInput` |
| Inventory / Trade | **At risk** — right-click use/equip variants and middle-click info (`DaggerfallInventoryWindow.cs:438–469,1913–1964,2104–2128`) are unreachable by tap; core loop works, conveniences don't. **DaggerPad fix: long-press = right-click, two-finger tap = middle-click** at the `BaseScreenComponent` event level (one change covers every window) |
| Exterior automap | **At risk** — right-click button functions; same long-press fix |
| Travel map | Medium — hover tooltips lost; fork already tripled dot hit-areas; add tap-hold tooltip |
| Talk window | Medium — fork already adapted layout/textures |
| Quest journal | Medium — right-click delete/move → long-press |
| Rest, spellbook, character sheet, HUD, message boxes, book reader | Fine as-is (fork-verified) |

The single highest-leverage DaggerPad addition is the **global long-press→right-click / two-finger-tap→middle-click mapping** in `BaseScreenComponent` — it retrofits every right-click affordance in the game at once.

---

## 9. Saves, configuration, iCloud

### Save format **[V]**

`<PersistentDataPath>/Saves/SAVE<n>/` — one folder per save containing pretty-printed **JSON text files** (FullSerializer, versioned DTOs like `SaveData_v1`): `SaveInfo.txt`, `SaveData.txt`, `FactionData.txt`, `ContainerData.txt`, `QuestData.txt`, `DiscoveryData.txt`, `ConversationData.txt`, `NotebookData.txt`, `WorldVariationData.txt`, `AutomapData.txt`, `QuestExceptions.txt`, `bio.txt`, `mod_<modFile>.txt` per mod, `Screenshot.jpg`. Nothing endianness-, architecture-, or platform-dependent; paths built with `Path.Combine`. The Android fork uses the identical system.

**Desktop ↔ iPad compatibility: expected to work by folder copy** (verify once on device — **[X]**). Mod state: the save embeds the enabled-mod list; loading with missing mods shows an explicit proceed-warning, missing-mod data is orphaned not fatal (`SaveLoadManager.cs:1280–1325,1520–1542`) — desktop saves made with code mods will load on iPad minus those mods' state, with the warning shown. Set expectations in-app.

### V1 implementation (simplest reliable)

- Saves + settings + layouts live under `Documents/` in the app container; ship `UIFileSharingEnabled` + `LSSupportsOpeningDocumentsInPlace` → the whole tree is visible in **Files ▸ On My iPad ▸ DaggerPad** and in Finder. Import/export = drag folders in Files. Zero custom UI, zero cloud code.
- `settings.ini` deploys from `defaults.ini.txt` exactly as today; add an iPad defaults block: `WeaponSwingMode=2`, `EnableObjectCulling=True`, `HUDGoesOnTop=True`, `DisableGarbageCollectionDuringMenuing=True`, `AssetCacheThreshold=10` (all fork-proven), plus `TargetFrameRate=60`, `VSync=on`, mobile-tuned shadows/view distance, `RetroRenderingMode` exposed in setup.
- Keybinds/touch layouts: existing JSON files; no changes.

### iCloud (defer to Phase 3+)

An iCloud Drive **ubiquity container** (Documents in iCloud) would sync saves automatically but introduces conflict resolution (same save edited on two devices), partial-download stalls on multi-file save folders, and jetsam-during-sync edge cases. **CloudKit is not required and not recommended.** V1 ships local + Files; revisit after real usage. **[I]**

---

## 10. Rendering, performance, thermals, storage

### Calibration facts **[V]**

- Desktop minimums: i3 (Skylake)-class CPU, 1 GB-VRAM DX11 GPU, 2 GB RAM. An M1 iPad exceeds this several-fold; M2 iPad Pro (target device) has 8–16 GB RAM and a GPU in a different class.
- The engine is Built-in Render Pipeline, Metal already configured (fork), Linear color space, 1996-vintage assets. GPU load is trivial by modern iPad standards; the risks are **CPU-side** (streaming-world terrain updates, quest/AI ticks, UI allocation churn) and **memory**.
- Documented mobile pain points (Android port, primary sources): OOM after ~90-minute sessions; Daggerfall Castle crashes without a save/reload; per-letter UI draw calls froze book screens until the font-batching rewrite (v1.1.1.6); over-aggressive culling broke quest spawns until XZ-distance and includeInactive fixes. All fixes are in the base we inherit.
- Unity iOS renders at **30 fps unless `Application.targetFrameRate` is raised** (official docs); "Enable ProMotion Support" player setting + targetFrameRate = display refresh unlocks 120 Hz on ProMotion iPads (no Info.plist key needed on iPad, unlike iPhone).
- Thermals: MrMacRight/Notebookcheck sustained-load data — iPad Pro M4 begins throttling ~9–15 min in RE4/Death Stranding-class Metal loads; M5 ~13–17 min. DFU's GPU load is a small fraction of those titles, so sustained throttling is unlikely to bind; treat as a measurement item, not a design constraint. **[V]/[I]**

### Realistic targets **[I → confirm on device, X]**

| Device | Expectation |
|---|---|
| M1 iPad (Air/Pro) | 60 fps native resolution everywhere except possibly densest city + long-view scenes |
| **M2 iPad Pro (dev target)** | 60 fps native comfortably; **120 fps ProMotion plausible in dungeons/interiors**, to be measured in cities |
| M4 iPad Pro | 120 fps broadly plausible |
| Recent A-series iPad (e.g., A16 iPad 11th gen) | 30–60 fps with reduced view distance/shadows; supported but not the tuning target |

Levers, all existing: resolution setting (`SetScreenResolution` renders sub-native and scales — fork), quality tier, per-environment shadow toggles/distances, view distance, `RetroRenderingMode` (320×200 or 640×400 internal target — the nuclear GPU lever, and an aesthetic feature in its own right), object culling, `AssetCacheThreshold`, framerate slider (5–120, fork).

### Must be measured on hardware (cannot be derived from desktop specs) **[X]**

1. Peak/resident memory in Daggerfall City, Wayrest, Daggerfall Castle, and a 2-hour session (Instruments; find the jetsam ceiling on 8 GB iPads).
2. GC hitch profile during fast travel, dungeon transitions, and menu churn (Unity Profiler over USB).
3. Sustained-load thermal state + clock behavior over 60 min (`ProcessInfo.thermalState` logging).
4. Battery drain per hour at 60 vs 120 fps.
5. Resume-from-background behavior (Metal surface loss, audio session, elapsed-time game-clock jump).
6. Touch-latency/frame-pacing quirk: Unity forums report frame-rate halving while touching the display on some configs — verify.

---

## 11. Mod support: honest compatibility matrix

### How mods actually load **[V]**

A `.dfmod` **is a Unity AssetBundle**. `ModManager` scans `StreamingAssets/Mods`, `AssetBundle.LoadFromFile` (`ModManager.cs:652`), reads `modinfo.json`, then for code: `*.cs.txt` assets → **runtime-compiled** by the embedded Mono compiler; `*.dll.bytes` assets → **`Assembly.Load(byte[])`** (`Mod.cs:946–986`). Both code paths are impossible under iOS IL2CPP (no JIT, no runtime assembly loading) and fail *soft* — try/catch → the mod's code silently doesn't run; its assets still load. There are **zero** AOT guards in the codebase. Asset content inside a bundle only loads if the bundle was **built for the running platform** (shaders/textures are compiled per-target), and mobile bundles are built without type trees, pinning them to the player's engine version. Mod-builder targets today: Windows/macOS/Linux (+Android in fork) — **no iOS target exists yet** (`CreateModEditorWindow.cs:52–59`).

### Compatibility matrix

| Mod category | Example | iOS IL2CPP | App Review (2.5.2) | Verdict on DaggerPad |
|---|---|---|---|---|
| Data-only (loose StreamingAssets: quests, books, text, tables) | quest packs | ✅ works | Data, not code — fine | **Works** via the fork's loose-file import |
| Asset-only `.dfmod` **rebuilt for iOS** | texture packs, sounds, models | ✅ works | Serialized assets = data **[I]** | **Works** once iOS BuildTarget added; needs per-mod rebuild by author or curator |
| Existing desktop/Android `.dfmod` binaries | everything on Nexus today | ❌ wrong platform target | — | **Never loads** (import validation already rejects) |
| Precompiled code mods (`.dll.bytes`) | many gameplay mods | ❌ `Assembly.Load` prohibited | Also prohibited content | **Impossible, permanently** |
| Runtime-source mods (`.cs.txt`) | many gameplay mods | ❌ no JIT/compiler | Prohibited | **Impossible, permanently** |
| Code mods **compiled into the app** at build time | chosen open-source mods | ✅ (they become app code) | Part of the binary — fine | **Works**; per-mod integration + license check |

### Distribution models evaluated

- **Model A (no mods)**: viable v1; loses a major DFU identity point.
- **Model B (data + iOS-rebuilt asset mods)**: the sweet spot. Code changes: iOS `BuildTarget` in `CreateModEditorWindow`, `Paths` writable mods dir (B4), enable `ModLoaderInterfaceWindow` import on iOS, iOS texture-format guard (B8). **Recommended for all channels.**
- **Model C (curated built-in mods)**: compile selected MIT-licensed mods into the app. Technically clean; per-mod maintenance; good App Store companion to B.
- **Model D (iPad-specific *precompiled* mods)**: **not technically possible** under IL2CPP — there is no way to load a managed assembly at runtime, no matter how it was produced. The only "iOS code mod" is Model C. State this plainly to the community.
- **Model E (desktop-style dynamic mods)**: impossible on iOS at the OS/runtime level (not merely an App Store policy) — sideloading does not change IL2CPP. **Dead on all channels.**

### Per-channel mod policy

| Channel | Policy |
|---|---|
| Personal dev builds | B + C, plus keep the in-game console (dev tool) |
| Sideloaded community builds | B + C; document loudly that code mods can't exist |
| TestFlight | B (+C); **strip `CSharpCompiler/` + `mcs.dll` + console `compile` command** from the build (2.5.2 hygiene — dead code that *looks* like a downloadable-code mechanism invites rejection) |
| App Store | B (import) is defensible under the GenZD/ScummVM precedent (imported bundles are serialized asset data, not executable code **[I]**); C is zero-risk; be transparent in review notes |

---

## 12. Legal, trademark, and distribution

*(Research findings, not legal advice. Items marked [LAWYER] need counsel before public distribution.)*

### Engine licensing **[V]**

- DFU and the Vwing fork are **MIT** ("Copyright (c) 2009-2023 Daggerfall Workshop"). Obligation: include the copyright + permission notice in the app (About/credits screen + bundled licenses file). No copyleft.
- Bundled third-party: Daggerfall Connect (MIT), C# Synth (MIT), FullSerializer (MIT), INI File Parser (MIT), Unity Console (MIT), SharpZipLib (fork), NativeFilePicker (yasirkula, MIT), `mcs` compiler pieces (MIT/X11 — being removed anyway), Unity postprocessing (Unity Companion License — fine inside a Unity app), Open Sans (Apache 2.0 notice), and the **TESFonts pack, whose bundled resource notice permits use without requesting additional permission**. Preview packaging preserves these available notices; storefront distribution still remains a legal-review gate (B13) **[V/LAWYER]**.

### Game data & Bethesda posture

- Daggerfall data is **free from Bethesda** (Steam app 1812390 since 2022; GOG) **[V]**. No explicit permission *or* prohibition on engine-reimplementation use was found; ZeniMax's general EULA anti-reverse-engineering language exists but its applicability to the free data files is undetermined **[U][LAWYER]**.
- **GOG Cut precedent**: GOG distributed DFU *bundled with the game data* for ~2.5 years (2022–2025) and Interkarma reported GOG told him **Bethesda approved it** (second-hand; no primary Bethesda statement) **[V-quote/U-underlying]**. Tolerance precedent, not a license.
- **OpenMW precedent (the sharpest warning)**: OpenMW's FAQ documents that Bethesda approved the project *on conditions*, including: *"Bethesda Softworks has asked us to not promote any images or videos of OpenMW running Morrowind on Android or other mobile platforms, and we agreed to comply."* **[V]** Bethesda's one documented sensitivity is exactly this product category. Consequence: DaggerPad's *technology* risk is low, its *promotion* strategy must be deliberate (see §14).
- Copyright vs trademark pattern: no C&D against DFU (12 years, name includes "Daggerfall"), Skyblivion gifted keys, but ZeniMax litigated the "Scrolls" *trademark* (Mojang, 2011–12) **[V]**. THE ELDER SCROLLS is a registered US mark (Reg. 3421731) **[V]**; a live "DAGGERFALL" word-mark registration could not be confirmed either way — direct USPTO TSDR search needed **[U][LAWYER]**.

### Naming decision

**App name: "DaggerPad".** Do not use "Daggerfall", "The Elder Scrolls", or "Unity" in the app name, bundle ID, or icon ( "Unity" is separately Unity Technologies' mark). Nominative references in description text are normal ("an unofficial port of the open-source Daggerfall Unity engine; requires your own copy of the free Daggerfall game data; not affiliated with or endorsed by Bethesda Softworks or ZeniMax Media") — the DevilutionX/OpenMW pattern. A disclaimer reduces confusion risk but does **not** prevent an App Store 5.2.1 complaint takedown, which is Bethesda's cheapest lever and requires no lawsuit. Apple may also ask for proof of rights up front for anything carrying recognizable TES branding (Guideline 5.2.1: "Apps should be submitted by the person or legal entity that owns or has licensed the intellectual property"). **[V]**

### App Review rules that matter **[V — current text fetched 2026-07-17]**

- **2.5.2**: apps "may not download, install, or execute code which introduces or changes features or functionality" — the reason to strip the runtime mod compiler (§11); imported *asset* data does not execute.
- **4.7**: permits "retro game console and PC emulator apps" to offer downloadable games — DaggerPad is *not* an emulator and arguably never triggers 4.7; the operative precedents are **ScummVM** (official, free, GPL, user-imported game data, live on the store) and **GenZD** (GZDoom engine, user WADs, live on the store).
- **5.2.1**: the IP complaint surface (above). Also applies at **notarization** for EU alt-distribution: notarization checks include "cannot download executable code" — Model E is dead in the EU channel too.

### Distribution matrix

| Path | Technical/signing requirements | Mods | Legal exposure | Audience | Maintenance burden |
|---|---|---|---|---|---|
| **Local Xcode install (free Apple ID)** | Mac + Xcode 26; app re-signs every **7 days**, ≤3 apps, ≤10 App IDs/week | Everything incl. dev tools | Effectively none (private use) | You | Weekly re-deploy |
| **Paid dev signing / Ad Hoc ($99/yr)** | 1-year profiles; ≤100 devices/yr | Everything | Minimal (private distribution) | You + friends | Low |
| **TestFlight** | Paid account; **Beta App Review** on first build; builds expire 90 days; ≤10,000 external testers | Model B+C (strip compiler) | Low-moderate: beta review is lighter, but a Bethesda complaint can still kill it | Community beta at real scale | Moderate (90-day re-uploads) |
| **AltStore/SideStore "classic" (worldwide)** | Distribute the signed-ipa on GitHub Releases; users self-sign (7-day cycle, their Apple ID) | Model B+C | Same as Android port today (GitHub-hosted, no store) | The Android port's ~47.5k-download analog | Low for you; friction for users |
| **AltStore PAL / EU alt marketplaces (DMA)** | EU-only users; Apple **notarization** (baseline review, no content/quality review); CTF replaced by 5% Core Technology Commission on *sales* — **a free app owes nothing** **[V]/[I]** | Model B+C | Moderate: notarization checks security not IP, but ZeniMax can still pursue the marketplace | EU enthusiasts | Moderate |
| **EU Web Distribution** | Eligibility bar applies (membership tenure/install thresholds — verify current criteria **[U]**) | B+C | Same as PAL | EU | Moderate |
| **Public App Store** | Full App Review; Xcode 26/iOS 26 SDK; likely Unity 6.3 build | Model B (+C) | **Highest**: 5.2.1 complaint risk + Bethesda mobile sensitivity; ScummVM/GenZD prove the *category* is approvable | Delta-scale reach (4.4M week-one vs 47.5k for sideload-only Android — distribution is the traction multiplier **[V]**) | Highest (annual SDK bar, review cycles) |

**Recommended sequencing: Local → TestFlight (community beta) → decision gate with counsel → App Store or EU/AltStore.** Do not bundle game data on any path (the `StreamingAssets/GameFiles` bundling hook stays empty).

---

## 13. Maintenance and upstream strategy

- **Repo**: `daggerfall-unity-ipados`, forked from `Vwing/daggerfall-unity-android`. Remotes: `vwing` (the fork), `upstream` (Interkarma). Branches: `ipados` = trunk; `vwing-android` = tracking branch; feature branches off `ipados`.
- **Divergence reality [V]**: `android` = v1.1.1-base + 227 commits; upstream has 264 commits since the base; Vwing merges upstream into his `master` mirror but has not rebased `android` since 2024-06-14. DaggerPad inherits this debt knowingly.
- **Merge cadence**: adopt Vwing's `android` merges opportunistically (he is active); attempt one upstream sync per quarter *after* v1 ships. Known merge hotspots (fork rewrote them): `InputManager.cs`, `TextLabel.cs`/`DaggerfallFont.cs`, `SettingsManager.cs`, `SaveLoadManager.cs`, `ModLoaderInterfaceWindow.cs`, `ActiveGameObjectDatabase.cs` (includeInactive semantics), `PlayerSpeedChanger.cs`, automap/travel-map windows.
- **Isolate Apple code**: everything platform-specific under `Assets/iOS/` + an `IPlatformUtils` seam mirroring `AndroidUtils` (folder-open, restart-prompt, permission no-ops). Convert the fork's remaining `#if UNITY_ANDROID` gates that mean "mobile" to `Application.isMobilePlatform` checks or `#if UNITY_ANDROID || UNITY_IOS` — and **offer these as PRs to Vwing** (he upstreams; a shared mobile layer benefits both and shrinks the diff). Longer-term goal: a common `mobile` branch both ports consume.
- **Xcode project is a build artifact, never source of truth**: regenerate from Unity every build ("Append" mode acceptable for iteration, "Replace" must always work); all Xcode-side customization lives in a C# `[PostProcessBuild]` script using `PBXProject`/`PlistDocument` APIs (bundle ID, Info.plist keys: `UIFileSharingEnabled`, `LSSupportsOpeningDocumentsInPlace`, `UIApplicationSupportsIndirectInputEvents`, orientation, ProMotion; signing team; the UnityFramework minOS-plist fix if needed). Commit the post-processor, git-ignore the generated project.
- **Unity upgrades**: pin 2022.3.62f3 now; plan one deliberate migration to 6000.3 LTS at Phase 4; thereafter track LTS annually — Apple raises the SDK bar every April, which sets the annual maintenance clock.
- **Reproducible releases**: a `BuildIos.cs` editor script (modeled on the fork's `AndroidBuildTool.cs`) + a `BUILDING.md` recording exact Unity/Xcode/macOS versions per release; tag releases `v1.1.1.8-ipad-N` mirroring Vwing's scheme.
- **Ongoing effort after 1.0 (estimate)**: 2–6 hrs/week issue triage + fixes; ~1–2 days per Vwing-sync; ~3–5 days per annual Unity/Xcode migration; 90-day TestFlight re-upload if that channel is used. Solo-maintainable, like the Android port.

---

## 14. Product story and public interest

**The story**: "A full Elder Scrolls RPG — the biggest one Bethesda ever made — running natively on iPad: touch-first controls, your own legally-free game data imported through Files, desktop-compatible saves. No emulator, no streaming, no jailbreak."

Evidence-based expectations **[V]**:

- The Android port earned ~**47,500** downloads over 24 months with *zero* launch press — because it was sideload-only. Delta (App Store, free, news hook) did **4.4M in week one**. Distribution channel, not product quality, is the traction multiplier.
- Coverage patterns: "Apple approved X" / policy-conflict narratives outperform feature narratives; "not an emulator" + "legally free data" is both the App Review admission ticket and the headline angle; first-mover vacuum — **no Elder Scrolls engine reimplementation exists on iOS at all** (OpenMW has no iOS build).
- Counterweight: Bethesda asked OpenMW not to promote mobile footage (§12). A viral "look, Elder Scrolls on iPad" push is exactly the scenario most likely to convert quiet tolerance into a takedown. **Strategy: community-first soft launch** (dfworkshop forums, r/daggerfallunity, the Lysandus' Tomb Discord — where Vwing's port lives), neutral naming, no Bethesda imagery in promotional assets; let press find it.

**Demo sequence** (for the eventual video, refined from the brief):
1. Home screen → tap DaggerPad icon (native app, no launcher).
2. Files app: pick `daggerfall.zip` → import progress → validation success.
3. Character creation with the native keyboard sliding up for the name.
4. Escape Privateer's Hold: virtual-stick movement, tap-to-interact on doors/levers, **swipe-to-swing combat** (the money shot — say "the swipe direction *is* the swing direction, like the 1996 mouse combat").
5. Automap: two-finger pinch/rotate through the dungeon in 3-D.
6. Copy a desktop save folder into Files → load it on iPad mid-quest.
7. Close: "Native Unity build for Apple Silicon. No emulator. No streaming. Your own game data."

**What makes it feel *designed for* iPad rather than transplanted**: (1) gesture combat — Daggerfall's directional mouse-swing system is the one classic FPS-RPG mechanic that maps *better* to touch than to a gamepad, and no other platform of DFU has it; (2) Files-native everything — game data, saves, mods, and control layouts all live in a visible Files folder (drag in a save from your Mac, drag out a layout to share); (3) Pencil-hover tooltips as the optional cherry.

---

## 15. Required proof-of-concept spike (1–3 days)

Goal: invalidate the project as fast as possible. Machine: Apple Silicon Mac, current macOS, Xcode 26.x, physical M2 iPad Pro on iPadOS 26. Install Unity **2022.3.62f3** via Unity Hub (Archive → it exists as a security-patch build) with **iOS Build Support** module.

| Step | Action | Most likely failure | Where to look |
|---|---|---|---|
| 1 | Open `research/daggerfall-unity-android` (branch `android`, `0fa652945`) in 2022.3.62f3 on the Mac | Library re-import churn; localization/addressables warnings (harmless) | `~/Library/Logs/Unity/Editor.log` |
| 2 | Switch build target to iOS | Compile errors from `PointerCaptureNativeInterface.cs` (**expected — B2**); CodeDom errors (**expected — B3**) | Console; fix: guard class; set iPhone API level to .NET Framework in Player Settings |
| 3 | Player Settings: bundle ID `com.<you>.daggerpad`, min iOS 15, arm64, Metal (already set), landscape (already set); Build (IL2CPP is forced for iOS) | IL2CPP conversion errors (audit says none expected); long first build (~20–40 min) | Editor console / `il2cpp` output |
| 4 | Xcode project generated | Post-process script absent → fine for spike; sign with personal team manually | — |
| 5 | **Build in Xcode 26** ← the critical unknown (B1) | Linker/minOS/`UnityFramework` plist errors; possible `-ld_classic`-era flags | Xcode build log. Workarounds to try in order: deployment target 15+ everywhere; UnityFramework Info.plist minOS fix; linker-flag adjustments. **If unresolvable in ~half a day → jump to the fallback: open the project in Unity 6000.3 LTS, accept the auto-upgrade, rebuild** (this converts B1 into the Phase-4 migration done early) |
| 6 | Install on the M2 iPad Pro; launch | Black screen (fork had one on fresh install, fixed in `39c020198`); crash on Android-only code path missed by the audit | Xcode device console; `Debug.Log` stream |
| 7 | Reach the setup wizard | Wizard shows Android welcome/import UI — acceptable for spike (NativeFilePicker iOS backend should present the document picker) | on-screen |
| 8 | Import real game data: zip your `arena2` (from the free Steam release, made on the Mac, AirDropped to iPad) | Picker returns path the importer can't read (iOS security-scoped URL handling in NativeFilePicker — it copies into sandbox, should be fine); `PACKED.DAT` path if using a CD-image layout | on-screen validation errors (they name missing files) |
| 9 | Enter the game world (new character, skip name entry issues by accepting defaults if keyboard bridge misbehaves) | TMP keyboard bridge quirk; shader visual glitches (4 touched shaders) | visual inspection |
| 10 | Move (left stick), look (right drag), interact (tap), attack (hold mode), save, kill app, relaunch, load | Touch canvas not rendering (RenderTexture compositing); save write failing (path — B4 class issue) | on-screen + console |

**Hard kill conditions** (any → stop and re-evaluate):
- Neither Unity 2022.3.62f3 **nor** Unity 6000.3 output can be made to build under Xcode 26 within ~2 days of effort.
- IL2CPP conversion of the base game surfaces systemic failures the audit missed (not isolated fixables).
- The game boots but core play (move/look/interact/save/load) is broken in ways that trace to deep engine-level iOS incompatibility rather than enumerable bugs.
- (Later gates, not spike gates: Apple rejects every distribution identity; Bethesda objects formally.)

**Explicit non-kill conditions**: total loss of code-mod support (already priced in — proceed); individual UI screens misbehaving on touch (enumerated, fixable); performance below 60 fps at native res (levers exist).

---

## 16. Phased implementation roadmap with effort ranges

*(Effort in focused engineer-days; solo developer; ranges = smooth ↔ unlucky.)*

**Phase 0 — Spike (§15). 1–3 days.** Exit: game world entered on the M2 iPad Pro, or kill decision.

**Phase 1 — Bootable, playable base. 5–10 days.**
Fork repos + branch setup (§13) · apply B2, B3 (excise-compiler variant), B10–B12 · `Paths` iOS branch + writable StreamingAssets mirror (B4) · `IPlatformUtils` facade replacing `AndroidUtils` call sites · iOS post-build processor (Info.plist keys, signing, minOS fix) · `BuildIos.cs` build script · setup-wizard iOS text/flow pass (remove Android permission prompts, add Files-drag-in hint) · verify keyboard bridge, automap gestures, save/load, travel map on device · iPad `defaults.ini` block · `OnApplicationPause` auto-save + audio handling (B6). Exit: touch-only acceptance test (§8) passes end-to-end.

**Phase 2 — iPad-native polish + performance. 10–20 days.**
Global long-press→right-click / two-finger-tap→middle-click in `BaseScreenComponent` · hardware keyboard/mouse/trackpad path (`CapturedInput` iOS branch, cursor lock, `UIApplicationSupportsIndirectInputEvents`) · controller verification (MFi/DualSense/Xbox) · ProMotion + `targetFrameRate` + framerate settings UI · instrumented performance pass on device (memory in cities/castle, GC hitches, thermals, battery — §10 list) and tuning of culling/cache/shadow defaults · default + simplified + accessibility layouts shipped as bundled JSON · app icon (original art, no Bethesda imagery) · Files-app exposure polish. Exit: 60 fps sustained in representative scenes on M2; no OOM in a 2-hour session.

**Phase 3 — Signature features + mods. 7–14 days.**
Gesture combat (swipe-to-swing) with tuning + setting (2–4 d) · iOS BuildTarget in mod builder + docs for mod authors (1–2 d) · mod import enabled on iOS (`ModLoaderInterfaceWindow` gates, B8 texture guard) (1–2 d) · curate + rebuild 3–5 starter mods (licenses permitting) (2–4 d) · Pencil-hover tooltips (optional, 1–2 d). Exit: an iOS-built `.dfmod` imports and works on device.

**Phase 4 — Distribution readiness. 8–15 days.**
Unity 6000.3 LTS migration + regression pass (3–7 d) · strip `CSharpCompiler`/`mcs.dll`/console-compile from distribution builds (1 d) · licenses/credits screen (MIT notices) + TESFonts audit/replacement (B13) (1–2 d) · TestFlight: App Store Connect setup, beta review notes explaining the ScummVM-pattern data model, first external build (2–3 d) · legal review gate for any store/marketplace step [LAWYER] · store decision per §12 matrix. Exit: community TestFlight running.

**Total to public beta: roughly 31–62 focused days (~6–12 calendar weeks part-time).**

---

## 17. First ten files/directories an implementation agent should inspect

1. `research/daggerfall-unity-android/Assets/Android/Scripts/TouchscreenInputManager.cs` — how touch injects into the game + RenderTexture GUI compositing
2. `research/daggerfall-unity-android/Assets/Scripts/Game/InputManager.cs` (diff vs merge-base `cb6463d50`) — look-source split, touch merge, poll-key injection points (`:654–675, :1797, :2032`)
3. `research/daggerfall-unity-android/Assets/Scripts/Paths.cs` — the path facade; where the iOS branch goes
4. `research/daggerfall-unity-android/Assets/Android/Scripts/AndroidUtils.cs` — the exact native-facade surface to reimplement as `IPlatformUtils`
5. `research/daggerfall-unity-android/Assets/Scripts/Game/UserInterface/FolderBrowserAndroid.cs` (+ `Utility/Unzip.cs`, `Utility/PackedDatFileUtils.cs`) — the full data-import pipeline
6. `research/daggerfall-unity-android/Assets/Android/OpenPointerCapture/Scripts/` (`PointerCaptureNativeInterface.cs`, `CapturedInput.cs`) — the compile blocker + the input shim to back with plain `Input` on iOS
7. `research/daggerfall-unity-android/Assets/Android/Scripts/TouchscreenLayoutsManager.cs` + `TouchscreenButtonConfiguration.cs` — layout schema for the new default layouts
8. `research/daggerfall-unity-android/Assets/Game/Addons/ModSupport/` (`ModManager.cs`, `Mod.cs`, `ModLoaderInterfaceWindow.cs`) — mod load pipeline + import flow + what to strip for stores
9. `research/daggerfall-unity-android/Assets/Android/Scripts/AndroidScreenManager.cs` — `AScreen` abstraction used by ~40 files
10. `research/daggerfall-unity/Assets/Scripts/API/DFValidator.cs` + `research/daggerfall-unity-android/ProjectSettings/ProjectSettings.asset` — data validation contract + every platform setting that must change

## 18. Unanswered questions requiring physical experiments

1. Does the Unity 2022.3.62f3-generated Xcode project build under Xcode 26.x, and with which workarounds? (B1 — spike step 5)
2. Does the NativeFilePicker iOS path hand the importer a readable copy of a multi-hundred-MB zip, with acceptable copy time?
3. Real memory footprint: Daggerfall City / Wayrest / Daggerfall Castle / 2-hour session vs the 8 GB-iPad jetsam ceiling.
4. Sustained thermal/clock behavior and battery drain at 60 and 120 fps.
5. Does a desktop save folder copied via Files load byte-for-byte (and vice versa)?
6. iPadOS trackpad/mouse relative-delta behavior under Unity cursor lock (360° look without the Android pointer-capture plugin).
7. Controller mapping correctness for MFi/DualSense/Xbox through legacy Input axes.
8. Does an iOS-target `.dfmod` built with the same editor version load and render correctly (shaders/textures) on device?
9. Touch frame-pacing quirk (reported frame-rate halving while touching the screen on some Unity/iOS combos).
10. Resume-from-background: Metal surface restore, audio session, game-clock jump after long suspension.

## 19. Final recommendation

**Yes — DaggerPad should be the next iPad port, built as specified here.** The research found a rare alignment: a feature-complete MIT engine, an actively-maintained mobile fork that already paid the Unity-migration and touch-UX costs, an AOT-clean codebase four fixes from compiling, shipped App Store precedents for the exact data model, and an empty field (nobody has ever shipped DFU on iOS). The honest limitations — no code mods ever, per-platform mod rebuilds, a store path that needs counsel and a deliberate low-key launch — are all manageable and none blocks the core product. Run the spike this week; the most likely outcome is a walking, fighting Daggerfall on the M2 iPad Pro within days.

## 20. Epistemic status

**Already proven** (source/code/document-verified): upstream state and versions; fork state, Unity 2022.3.62f3, activity, and merge-base; the complete IL2CPP audit findings (base game AOT-clean; the four named fixes; mod-code impossibility); the reuse map and its three platform seams; NativeFilePicker's bundled iOS backend; the required-game-file list and validator behavior; the save format and its platform independence; the full Actions enum and swing-mode mechanics; touch-drives-retro-UI behavior; Apple's Xcode 26/iOS 26 SDK requirement and deployment floor; Unity version support status; AssetBundle cross-platform/version rules; MIT licensing; App Review guideline text; ScummVM/GenZD/iDOS precedents; the OpenMW mobile-promotion condition; the GOG Cut history; the Android port's download/press record.

**Strongly likely** (inference from proven facts): a 2022.3-or-6.3 build boots and plays well on M2 (Android ARM64 IL2CPP builds ship today; desktop min-spec is far below M1); desktop↔iPad save portability; 60 fps at native resolution on M2; TestFlight viability under the ScummVM pattern; swipe-to-swing implementability on the existing gesture plumbing.

**Speculative**: App Store approval and survival of a 5.2.1 complaint; 120 fps in cities; iCloud sync ergonomics; community mod-rebuild uptake; press traction.

**Could kill the project**: both Unity 2022.3 *and* 6000.3 failing to produce an Xcode 26-buildable project (very low probability; 6.3+Xcode 26 has public successes); a formal Bethesda objection to a public release (mitigable by staying personal/TestFlight; does not kill the build); an unfixable memory ceiling on 8 GB iPads (low — the fork already runs on far weaker Android hardware; M2 Pro has headroom).

---

*Prepared 2026-07-17 from direct inspection of both repositories at the commits in §1 plus primary-source web research (Apple developer documentation and news, Unity documentation and staff posts, GitHub APIs, store listings, project forums via archive snapshots). Key URLs: developer.apple.com/news/upcoming-requirements · developer.apple.com/support/xcode · docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html · unity.com/blog/engine-platform/il2cpp-full-generic-sharing-in-unity-2022-1-beta · docs.unity3d.com/6000.4/Documentation/Manual/AssetBundlesIntro.html · github.com/Interkarma/daggerfall-unity (releases, wiki) · github.com/Vwing/daggerfall-unity-android (releases) · apps.apple.com/us/app/scummvm/id6446184412 · apps.apple.com/us/app/genzd/id6503916449 · openmw.org/faq · developer.apple.com/app-store/review/guidelines · developer.apple.com/support/dma-and-apps-in-the-eu.*
