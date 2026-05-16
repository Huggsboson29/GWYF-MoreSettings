# Changelog

## 0.2.7

- Fix floor unlock progression by preserving the base game's next-floor quota threshold instead of overwriting it with the starting quota value
- Align lobby-time pre-day resets with the vanilla untouched-run state so only true new-run setup gets reinitialized

## 0.2.6

- Fix the lobby-time quota reset guard so progressed runs keep their current floor progression instead of getting pushed back toward the initial quota state
- Preserve starting money and quota values unless the run is still in an untouched floor-1 pre-day setup

## 0.2.5

- Oops, I broke the timer :(
- Fix the native lobby settings layout selection so starting money and quota edits no longer reset day duration to a stale value

## 0.2.4

- Add host-configurable starting money and apply it through runtime, save data, and lobby visibility sync
- Lock starting money after entering the lobby, keep it linked to starting quota, and stabilize the native settings slider inputs
- Add GitHub issue guidance to the package README and fix the live money balance sync during pre-day setup

## 0.2.3

- Rename the package and shipped DLL from ConfigManager to MoreSettings
- Update packaging, docs, and release automation for the MoreSettings package identity
- Replace reflective GameSettings loading with direct game type references for vanilla defaults

## 0.2.2

- Finish the TimeConfig to ConfigManager rename across the repo, packaging, and release workflow
- Fix quota overrides so the active save state is reapplied during lobby runtime setup
- Refresh local package/deploy paths so direct installs and Thunderstore installs load the same ConfigManager build

## 0.2.1

- Retitle the Thunderstore package to ConfigManager
- Replace the package summary and README with player-facing usage instructions
- Clarify that settings live under the lobby Settings tab in the ConfigManager section

## 0.2.0

- Add host-side quota scaling controls with vanilla or custom-pattern growth modes
- Split ConfigManager's settings UI into separate Time and Quota sections
- Fix quota edits so they no longer reset the active run's quota state during setup
- Remove multi-day quota overrides after inconsistent runtime behavior in testing

## 0.1.0

- Initial MVP scaffold
- Host-authoritative overrides for day duration and quota pacing
- Build script for fetching local BepInEx development references
