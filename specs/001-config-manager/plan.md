# Implementation Plan: Host-Configurable Time Pressure

**Branch**: `001-config-manager` | **Date**: 2026-05-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-config-manager/spec.md`

## Summary

Build a host-authoritative MoreSettings mod for *Gamble With Your Friends* that exposes day
length and quota pressure as reusable timing profiles, applies the selected profile to
the authoritative shared session state, shows the active configuration to the lobby in
read-only form, and packages the result for Thunderstore distribution.

## Technical Context

**Language/Version**: C# class library for a Mono-backed Unity game; initial scaffold
targets a `netstandard2.1`-compatible plugin layout with implementation-time fallback to
the framework required by the local game assemblies if inspection demands it  
**Primary Dependencies**: BepInExPack 5.4.2305, HarmonyX/HarmonyLib, Unity managed
assemblies from `Gamble With Your Friends_Data\Managed`  
**Storage**: BepInEx configuration for active/default settings plus a profile store under
the plugin configuration directory for reusable named presets  
**Testing**: Manual host/client runtime validation, configuration validation tests for
profile parsing rules, packaging smoke checks  
**Target Platform**: Windows desktop first; document Proton/Steam Deck launch guidance as
supported install notes  
**Project Type**: Unity runtime plugin / Thunderstore package  
**Performance Goals**: Apply selected settings before gameplay begins, add no noticeable
lobby delay, and keep configuration load time effectively instant for a single lobby  
**Constraints**: Host-authoritative shared state only, no direct game binary edits, narrow
Harmony patch surface, explicit runtime diagnostics, Thunderstore-compatible packaging  
**Scale/Scope**: Single mod package for one active session profile per lobby, optimized
for the base 1-6 player game flow described in the architecture note

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Multiplayer-Safe Modding**: Pass. The feature is scoped as host-authoritative and
  applies shared timing rules only at controlled session boundaries.
- **II. BepInEx + Harmony First**: Pass. The design uses a BepInEx plugin with narrow
  Harmony patches against authoritative timer/quota entry points.
- **III. Configuration Before Hardcoding**: Pass. Timing behavior is driven by typed
  profiles with explicit validation and vanilla-safe defaults.
- **IV. Observable Failures and Narrow Patch Surfaces**: Pass. Invalid settings and patch
  failures are treated as visible diagnostics, not silent fallback.
- **V. Ship-Ready Thunderstore Releases**: Pass. The plan includes manifest, README, icon,
  changelog, and dependency tracking as first-class deliverables.

Post-design re-check: Pass. Research, data model, contracts, and quickstart all preserve
host authority, configuration-driven behavior, observable failure states, and package
readiness.

## Project Structure

### Documentation (this feature)

```text
specs/001-config-manager/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── timing-profile-contract.md
│   └── session-visibility-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
└── MoreSettings/
    ├── PluginMain.cs
    ├── Configuration/
    │   ├── ActiveSettings.cs
    │   ├── ProfileStore.cs
    │   └── Validation/
    ├── Models/
    │   ├── TimingProfile.cs
    │   ├── SessionTimingState.cs
    │   └── ValidationOutcome.cs
    ├── Patches/
    │   ├── DayTimerPatches.cs
    │   ├── QuotaPatches.cs
    │   └── LobbyVisibilityPatches.cs
    ├── Runtime/
    │   ├── TimingCoordinator.cs
    │   └── MoreSettingsPresenter.cs
    └── Packaging/
        ├── manifest.json
        ├── README.md
        ├── CHANGELOG.md
        └── icon.png

tests/
└── MoreSettings.Tests/
    ├── Configuration/
    └── Models/
```

**Structure Decision**: Use a single plugin project rooted in `src/MoreSettings/` with
separate folders for configuration, runtime coordination, Harmony patches, and packaging
artifacts. Keep tests focused on deterministic profile validation and data/model logic.

## Complexity Tracking

No constitution exceptions are required for this feature. The design stays within the
single-plugin, configuration-first model described by the project constitution.
