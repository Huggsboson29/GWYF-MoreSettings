# MoreSettings

MoreSettings lets the lobby host tune the run's time pressure and starting economy.
It adds a host-only section to the lobby settings where you can set starting money,
day length, and quota behavior before entering the lobby.

## Where to find it

1. Host a multiplayer lobby.
2. Open the lobby `Settings` tab.
3. Scroll to the `MoreSettings` section.
4. Change the values before entering the lobby.
5. Once the lobby has been entered, starting money is locked while the other MoreSettings values remain editable.

## What you can change

- Starting money
- Day duration in minutes
- Starting quota
- Catch-up factor
- Quota scaling mode: vanilla scaling or custom pattern
- Custom pattern length and each pattern multiplier when custom scaling is enabled

## How it behaves

- Only the lobby host can change MoreSettings settings.
- Starting money can be set before the host enters the lobby and is locked afterward.
- The active host settings apply to everyone in that lobby.
- Players joining the lobby see the host-selected configuration through the normal lobby flow.
- Quota timing still follows the base game's normal cadence; multi-day quota overrides are not included.

## Request features or report problems

Use GitHub issues for feature requests, bug reports, and balance suggestions:

1. Check the existing issues: https://github.com/Huggsboson29/GWYF-MoreSettings/issues
2. Open a new issue if your request is not already tracked: https://github.com/Huggsboson29/GWYF-MoreSettings/issues/new
3. Include the mod version, what you expected, what happened, and steps to reproduce if it is a bug.
4. For feature requests, describe the setting you want, why it would help, and any limits or defaults you expect.

## Requirement

MoreSettings requires BepInEx, which Thunderstore will install automatically as a dependency.
