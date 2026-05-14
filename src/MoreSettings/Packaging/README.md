# MoreSettings

MoreSettings lets the lobby host tune the run's time pressure without editing config files.
It adds a host-only section to the lobby settings so you can change how long a day lasts
and how the quota ramps before the run starts.

## Where to find it

1. Host a multiplayer lobby.
2. Open the lobby `Settings` tab.
3. Scroll to the `MoreSettings` section.
4. Change the values before starting the run.

## What you can change

- Day duration in minutes
- Starting quota
- Catch-up factor
- Quota scaling mode: vanilla scaling or custom pattern
- Custom pattern length and each pattern multiplier when custom scaling is enabled

## How it behaves

- Only the lobby host can change MoreSettings settings.
- The active host settings apply to everyone in that lobby.
- Players joining the lobby see the host-selected configuration through the normal lobby flow.
- Quota timing still follows the base game's normal cadence; multi-day quota overrides are not included.

## Requirement

MoreSettings requires BepInEx, which Thunderstore will install automatically as a dependency.
