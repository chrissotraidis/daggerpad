# DaggerPad

DaggerPad is a native iPadOS adaptation of Daggerfall Unity. It builds on Vwing's actively maintained mobile fork and keeps the classic Daggerfall data outside the app: users import their own ZIP through Files.

Current implementation work is source-pinned to Unity `2022.3.62f3` and the mobile fork commit `0fa65294523a132a0e5389d125f58d6566a1e815`.

Start with:

- [Product and feasibility requirements](docs/DAGGERPAD_PRD.md)
- [Goal-based implementation loop](docs/DAGGERPAD_GOAL_LOOP.md)
- [Build instructions](BUILDING.md)
- [Simulator and device testing](docs/TESTING.md)
- [Current implementation evidence](docs/STATUS.md)
- [PRD coverage and remaining gates](docs/PRD_COVERAGE.md)

## Important limitations

- iOS requires IL2CPP. Runtime C# mods and precompiled managed mod assemblies cannot work.
- Asset-only mods must be rebuilt for the iOS Unity target.
- Classic Daggerfall files are never bundled or distributed with DaggerPad.
- Simulator testing is a build and UI gate, not a substitute for performance and lifecycle testing on a physical iPad.

## Provenance

Daggerfall Unity and the Vwing fork are MIT-licensed. See `LICENSE` and the notices under `Assets/Licenses/`. DaggerPad is unofficial and is not affiliated with or endorsed by Bethesda Softworks, ZeniMax Media, or Unity Technologies.
