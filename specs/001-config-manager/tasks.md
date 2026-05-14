# Tasks: Host-Configurable Time Pressure

**Input**: Design documents from `/specs/001-config-manager/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are not generated as a dedicated phase because the feature spec did not
explicitly request TDD. Configuration validation coverage is still included in
implementation tasks where appropriate.

**Organization**: Tasks are grouped by user story to enable independent implementation and
testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the initial plugin, build, and package layout described by the plan.

- [ ] T001 Create the plugin directory structure in `src/MoreSettings/` and test structure in `tests/MoreSettings.Tests/`
- [ ] T002 Create the main project file in `src/MoreSettings/MoreSettings.csproj`
- [ ] T003 [P] Create packaging placeholders in `src/MoreSettings/Packaging/manifest.json`, `src/MoreSettings/Packaging/README.md`, `src/MoreSettings/Packaging/CHANGELOG.md`, and `src/MoreSettings/Packaging/icon.png`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared runtime, configuration, validation, and patch-registration
infrastructure that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Implement plugin bootstrap and logging in `src/MoreSettings/PluginMain.cs`
- [ ] T005 [P] Implement core models in `src/MoreSettings/Models/TimingProfile.cs`, `src/MoreSettings/Models/SessionTimingState.cs`, and `src/MoreSettings/Models/ValidationOutcome.cs`
- [ ] T006 [P] Implement active/default settings binding in `src/MoreSettings/Configuration/ActiveSettings.cs`
- [ ] T007 Implement profile validation rules in `src/MoreSettings/Configuration/Validation/TimingProfileValidator.cs`
- [ ] T008 Implement the authoritative timing coordinator in `src/MoreSettings/Runtime/TimingCoordinator.cs`
- [ ] T009 Implement shared patch registration scaffolding in `src/MoreSettings/Patches/DayTimerPatches.cs` and `src/MoreSettings/Patches/QuotaPatches.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Configure a Custom Run Pace (Priority: P1) 🎯 MVP

**Goal**: Allow the host to set non-default day length and quota pressure for a new run.

**Independent Test**: Start a host session with custom timing values and confirm the
resolved day duration and quota pressure are applied to the shared run.

### Implementation for User Story 1

- [ ] T010 [US1] Wire active session timing resolution into `src/MoreSettings/PluginMain.cs` and `src/MoreSettings/Runtime/TimingCoordinator.cs`
- [ ] T011 [P] [US1] Implement authoritative day-length patch logic in `src/MoreSettings/Patches/DayTimerPatches.cs`
- [ ] T012 [P] [US1] Implement authoritative quota-pressure patch logic in `src/MoreSettings/Patches/QuotaPatches.cs`
- [ ] T013 [US1] Apply validated runtime timing values in `src/MoreSettings/Runtime/TimingCoordinator.cs`
- [ ] T014 [US1] Surface invalid-setting rejection behavior in `src/MoreSettings/Configuration/Validation/TimingProfileValidator.cs` and `src/MoreSettings/Runtime/TimingCoordinator.cs`

**Checkpoint**: User Story 1 should be fully functional and independently testable

---

## Phase 4: User Story 2 - Reuse Trusted Settings (Priority: P2)

**Goal**: Let hosts save, load, and restore reusable timing profiles.

**Independent Test**: Save two profiles, relaunch the game, select one profile, and
confirm the session uses the saved values; then restore vanilla behavior.

### Implementation for User Story 2

- [ ] T015 [US2] Implement named profile persistence in `src/MoreSettings/Configuration/ProfileStore.cs`
- [ ] T016 [US2] Implement profile create, update, and delete flows in `src/MoreSettings/Configuration/ProfileStore.cs`
- [ ] T017 [US2] Integrate profile selection with the runtime coordinator in `src/MoreSettings/Configuration/ActiveSettings.cs` and `src/MoreSettings/Runtime/TimingCoordinator.cs`
- [ ] T018 [US2] Implement restore-default behavior in `src/MoreSettings/Configuration/ActiveSettings.cs` and `src/MoreSettings/Runtime/TimingCoordinator.cs`

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Share Active Settings with the Lobby (Priority: P3)

**Goal**: Publish the active timing configuration to players in read-only form.

**Independent Test**: Join a host session with a non-default profile and confirm the
joining player can identify the active timing state but cannot change it.

### Implementation for User Story 3

- [ ] T019 [US3] Implement lobby/session settings presentation in `src/MoreSettings/Runtime/MoreSettingsPresenter.cs`
- [ ] T020 [US3] Implement visibility patch hooks in `src/MoreSettings/Patches/LobbyVisibilityPatches.cs`
- [ ] T021 [US3] Publish resolved timing state to joiners in `src/MoreSettings/Runtime/TimingCoordinator.cs` and `src/MoreSettings/Runtime/MoreSettingsPresenter.cs`
- [ ] T022 [US3] Block non-host mutation attempts in `src/MoreSettings/Patches/LobbyVisibilityPatches.cs`

**Checkpoint**: All user stories should now be independently functional

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Finish packaging, documentation, and release validation work that touches
multiple stories.

- [ ] T023 [P] Finalize Thunderstore package metadata in `src/MoreSettings/Packaging/manifest.json`
- [ ] T024 [P] Finalize installation and usage guidance in `src/MoreSettings/Packaging/README.md` and `src/MoreSettings/Packaging/CHANGELOG.md`
- [ ] T025 [P] Align quickstart validation steps with final behavior in `specs/001-config-manager/quickstart.md`
- [ ] T026 Prepare release icon asset in `src/MoreSettings/Packaging/icon.png`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: Depend on Foundational completion
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and delivers the MVP
- **User Story 2 (P2)**: Starts after Foundational and builds on the same validated timing model
- **User Story 3 (P3)**: Starts after Foundational and depends on resolved session timing state being available

### Within Each User Story

- Shared runtime state before patch-specific behavior
- Validation before state application
- Session publication before player-facing visibility
- Story complete before polishing related docs and packaging

### Parallel Opportunities

- `T003` can run in parallel with other setup completion work after structure exists
- `T005` and `T006` can run in parallel in the foundational phase
- `T011` and `T012` can run in parallel once the timing coordinator contract is stable
- `T023`, `T024`, and `T025` can run in parallel in the final phase

---

## Parallel Example: User Story 1

```bash
# After foundational runtime contracts are in place:
Task: "Implement authoritative day-length patch logic in src/MoreSettings/Patches/DayTimerPatches.cs"
Task: "Implement authoritative quota-pressure patch logic in src/MoreSettings/Patches/QuotaPatches.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate host-controlled day length and quota pressure in a multiplayer session

### Incremental Delivery

1. Deliver the core timing override flow (US1)
2. Add reusable profile storage and restore-default behavior (US2)
3. Add lobby visibility and non-host read-only behavior (US3)
4. Finish packaging and release assets

### Parallel Team Strategy

1. One developer completes Setup + Foundational
2. After the coordinator/model contracts stabilize:
   - Developer A: User Story 1 patch logic
   - Developer B: User Story 2 profile persistence
   - Developer C: User Story 3 lobby visibility

---

## Notes

- All tasks use explicit file paths
- MVP scope is Phase 3 / User Story 1
- Packaging remains part of the feature definition, not a post-feature cleanup pass
