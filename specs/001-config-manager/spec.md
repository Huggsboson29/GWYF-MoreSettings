# Feature Specification: Host-Configurable Time Pressure

**Feature Branch**: `001-config-manager`  
**Created**: 2026-05-10  
**Status**: Draft  
**Input**: User description: "Reference the initial architecture and create the first MoreSettings feature for Gamble With Your Friends."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configure a Custom Run Pace (Priority: P1)

As a host, I want to set the session's day length and quota pressure before the run
starts so my group can choose a more relaxed or more intense pace without modifying the
base game files.

**Why this priority**: This is the core value of MoreSettings. Without host-controlled time
pressure settings, the mod does not solve the main player problem.

**Independent Test**: Start a fresh host session with non-default time settings and verify
that the chosen day duration and quota pressure are applied throughout the run for all
connected players.

**Acceptance Scenarios**:

1. **Given** the host has selected a longer day duration before the run starts, **When**
   the session begins, **Then** the run uses the selected day duration instead of the
   default value.
2. **Given** the host has selected a reduced quota pressure profile, **When** a new day
   starts, **Then** the session uses the selected quota pacing rules for that day.

---

### User Story 2 - Reuse Trusted Settings (Priority: P2)

As a host, I want to save and reuse named timing profiles so I can switch between common
session styles quickly and consistently.

**Why this priority**: Repeatable presets reduce setup friction and make the feature
practical for regular play groups.

**Independent Test**: Save two different timing profiles, switch between them across
separate runs, and confirm each run uses the selected profile without manual re-entry.

**Acceptance Scenarios**:

1. **Given** the host has created a named timing profile, **When** they select that
   profile for a later run, **Then** all saved time-pressure settings from that profile
   are used for the new session.
2. **Given** the host no longer wants a custom profile, **When** they choose to restore
   default behavior, **Then** the next run uses vanilla timing values.

---

### User Story 3 - Share Active Settings with the Lobby (Priority: P3)

As a player in the lobby, I want to know which timing rules are active so I understand
the run's difficulty and pacing expectations before and during play.

**Why this priority**: Visibility reduces confusion in multiplayer sessions and helps
players trust that the host-selected configuration is the one being used.

**Independent Test**: Join a host session that uses a non-default timing profile and
verify that connected players can identify the active settings without being able to
change them.

**Acceptance Scenarios**:

1. **Given** the host has enabled custom time-pressure settings, **When** another player
   joins the lobby or run, **Then** that player can view the active configuration in a
   read-only form.
2. **Given** the session is using vanilla timing, **When** players review the active run
   settings, **Then** they can clearly tell that no custom timing profile is applied.

---

### Edge Cases

- What happens when the host enters a time or quota value outside the supported safe range?
- How does the system handle a player joining after a custom run has already started?
- What happens when a saved profile references values that are no longer valid?
- How does the system behave when the host restores defaults after previously using a
  custom profile?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow the host to define a non-default day length for a new
  session.
- **FR-002**: The system MUST allow the host to define the session's quota pressure,
  including at least the starting quota and the pace at which quota difficulty increases.
- **FR-003**: The system MUST apply the selected time-pressure settings consistently to
  the shared session so all players experience the same active rules.
- **FR-004**: The system MUST prevent non-host players from changing shared time-pressure
  settings during a session.
- **FR-005**: The system MUST allow hosts to save and reuse named timing profiles.
- **FR-006**: The system MUST allow hosts to restore vanilla timing behavior without
  deleting the mod.
- **FR-007**: The system MUST reject unsupported timing or quota values and explain why
  they were rejected before a run begins.
- **FR-008**: The system MUST make the active session timing configuration visible to
  players in read-only form.
- **FR-009**: The system MUST preserve vanilla behavior when no custom timing profile is
  selected.
- **FR-010**: The system MUST surface an explicit failure state when a saved or selected
  timing profile cannot be loaded or applied.

### Key Entities *(include if feature involves data)*

- **Timing Profile**: A named collection of host-selected time-pressure values, including
  day length, quota settings, and whether the profile represents vanilla behavior.
- **Session Timing State**: The active time-pressure rules currently applied to a lobby or
  run, including who selected them and whether they are custom or default.
- **Validation Outcome**: The result of checking a profile or manual value set, including
  whether it is valid and any user-visible reason for rejection.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In host validation runs, 100% of new sessions started with a selected custom
  profile use the chosen day length and quota pressure instead of vanilla defaults.
- **SC-002**: In multiplayer validation runs, all connected players can identify the
  active session timing configuration within one join cycle.
- **SC-003**: Hosts can switch between a saved custom profile and vanilla behavior in no
  more than two deliberate actions before starting a run.
- **SC-004**: Invalid timing configurations never apply silently; every rejected value set
  produces a visible rejection reason before gameplay begins.

## Assumptions

- The first release is scoped to host-authoritative shared timing behavior because the
  architecture note identifies time and quota systems as synchronized session state.
- Custom timing is chosen before a run or day starts; mid-day live editing is out of
  scope for this feature.
- The first release targets time pressure only and does not include unrelated economy,
  inventory, or custom-asset features.
- Users already have the required game mod loader environment installed before using this
  feature.
