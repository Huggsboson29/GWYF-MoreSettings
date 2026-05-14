#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Gamble With Your Friends',
    [string]$OutputRoot = '',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectFile = Join-Path $repoRoot 'src\MoreSettings\MoreSettings.csproj'
$testsProject = Join-Path $repoRoot 'tests\MoreSettings.Tests\MoreSettings.Tests.csproj'
$manifestPath = Join-Path $repoRoot 'src\MoreSettings\Packaging\manifest.json'
$readmePath = Join-Path $repoRoot 'src\MoreSettings\Packaging\README.md'
$changelogPath = Join-Path $repoRoot 'src\MoreSettings\Packaging\CHANGELOG.md'
$iconPath = Join-Path $repoRoot 'src\MoreSettings\Packaging\icon.png'
$gameManagedDir = Join-Path $GameRoot 'Gamble With Your Friends_Data\Managed'
$releaseDll = Join-Path $repoRoot 'src\MoreSettings\bin\Release\netstandard2.1\MoreSettings.dll'

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot 'artifacts\thunderstore'
}

foreach ($requiredPath in @($manifestPath, $readmePath, $changelogPath)) {
    if (-not (Test-Path $requiredPath)) {
        throw "Required packaging file is missing: $requiredPath"
    }
}

if (-not (Test-Path $iconPath)) {
    throw "Thunderstore requires a 256x256 icon at: $iconPath"
}

if (-not (Test-Path (Join-Path $gameManagedDir 'Assembly-CSharp.dll'))) {
    throw "Game managed assemblies were not found under: $gameManagedDir"
}

if (-not $SkipTests) {
    & dotnet test $testsProject -c Release
}

& dotnet build $projectFile -c Release "/p:GameManagedDir=$gameManagedDir"

if (-not (Test-Path $releaseDll)) {
    throw "Release build did not produce: $releaseDll"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$packageName = $manifest.name
$packageVersion = $manifest.version_number

if ([string]::IsNullOrWhiteSpace($packageName) -or [string]::IsNullOrWhiteSpace($packageVersion)) {
    throw "manifest.json must define non-empty name and version_number values."
}

$stagingDir = Join-Path $OutputRoot 'staging'
$packageDir = Join-Path $stagingDir $packageName
$pluginsDir = Join-Path $packageDir 'plugins\MoreSettings'

if (Test-Path $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}

New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $packageDir 'manifest.json') -Force
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $packageDir 'README.md') -Force
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $packageDir 'CHANGELOG.md') -Force
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $packageDir 'icon.png') -Force
Copy-Item -LiteralPath $releaseDll -Destination (Join-Path $pluginsDir 'MoreSettings.dll') -Force

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$zipPath = Join-Path $OutputRoot ("{0}-{1}.zip" -f $packageName, $packageVersion)
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageDir '*') -DestinationPath $zipPath -Force

Write-Host "Thunderstore package created: $zipPath"
Write-Host "Upload that zip through the Thunderstore web UI or CLI after verifying icon, README, and version metadata."