# Thunderstore Release

## Local testing

1. Run `scripts\Deploy-ToGame.ps1`.
2. Launch the game as host.
3. Confirm `BepInEx\LogOutput.log` contains the plugin startup line and applied timing output.
4. Confirm `BepInEx\config\com.lncinteractive.cfg` is created and your changes affect a fresh hosted run.
5. If multiplayer behavior matters, join from a second client and confirm the host-selected settings are visible and read-only.

## Release prerequisites

- `src/MoreSettings/Packaging/manifest.json` must have the correct `version_number`, dependency list, and optional website URL.
- `src/MoreSettings/Packaging/README.md` should describe install, config, and known limits.
- `src/MoreSettings/Packaging/CHANGELOG.md` should include the release notes for the version you are publishing.
- `src/MoreSettings/Packaging/icon.png` must exist and be a 256x256 PNG.

## Build a zip for Thunderstore

Run:

```powershell
.\scripts\Pack-Thunderstore.ps1
```

That script:

1. Runs the test project unless `-SkipTests` is passed.
2. Builds the plugin in Release.
3. Stages `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, and `plugins/MoreSettings/MoreSettings.dll`.
4. Produces `artifacts/thunderstore/MoreSettings-<version>.zip`.

## Upload through the Thunderstore site

1. Create or use a Thunderstore team.
2. Open the upload page for the `gamble-with-your-friends` community.
3. Upload the generated zip.
4. Choose the `Mods` category.
5. Publish after the preview looks correct.

## Upload with CLI

This repo now includes `thunderstore.toml` and `scripts\Publish-Thunderstore.ps1` for TCLI-based publishing.

Recommended flow:

```powershell
$env:THUNDERSTORE_NAMESPACE = 'YourTeamName'
$env:TCLI_AUTH_TOKEN = 'your-token-here'
.\scripts\Publish-Thunderstore.ps1
```

What the publish script does:

1. Generates `artifacts\thunderstore\thunderstore.publish.toml` from the current `manifest.json`.
2. Runs `scripts\Pack-Thunderstore.ps1` so tests and the release zip are current.
3. Publishes the generated zip with `tcli publish --file ...`.

If `tcli` is not installed yet, either install it manually with `dotnet tool install -g tcli` or run:

```powershell
.\scripts\Publish-Thunderstore.ps1 -Namespace YourTeamName -InstallTcli
```

If you want to validate the setup without uploading anything, run:

```powershell
.\scripts\Publish-Thunderstore.ps1 -Namespace YourTeamName -DryRun
```

## GitHub Actions release workflow

The repo now includes `.github/workflows/release.yml`.

Important constraint:

- This project depends on local game assemblies from your Steam install, so the release workflow is designed for a **self-hosted Windows runner** with *Gamble With Your Friends* installed.

Runner setup:

1. Register a self-hosted Windows GitHub Actions runner on the machine that has the game installed.
2. Set repository variable `GWYF_GAME_ROOT` if the game is not installed at the default Steam path.
3. Add repository secret `THUNDERSTORE_NAMESPACE` with your Thunderstore team name.
4. Add repository secret `TCLI_AUTH_TOKEN` with your Thunderstore API token.
5. Optionally set repository variable `THUNDERSTORE_AUTO_PUBLISH=true` if tag pushes should also publish automatically.

What the workflow does:

1. On `workflow_dispatch`, it packages the mod and can optionally publish it to Thunderstore.
2. On tag pushes matching `v*`, it packages the zip, uploads it as a workflow artifact, and attaches it to the GitHub release.
3. On tag pushes, it can also publish to Thunderstore when `THUNDERSTORE_AUTO_PUBLISH=true` and the required secrets are present.

Manual dispatch notes:

- Use the `game_root` input if the runner's install path differs from the default Steam path.
- Use the `publish_to_thunderstore` input when you want the workflow to call `scripts\Publish-Thunderstore.ps1` after packaging.