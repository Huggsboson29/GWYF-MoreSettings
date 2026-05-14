#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Gamble With Your Friends',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectFile = Join-Path $repoRoot 'src\MoreSettings\MoreSettings.csproj'
$fetchScript = Join-Path $repoRoot 'scripts\Fetch-DevDependencies.ps1'
$bepInExExtract = Join-Path $repoRoot '.local\BepInEx\extract'
$gameManagedDir = Join-Path $GameRoot 'Gamble With Your Friends_Data\Managed'
$gameBepInExCoreDir = Join-Path $GameRoot 'BepInEx\core'
$legacyPluginDir = Join-Path $GameRoot 'BepInEx\plugins\com.dylan.gwyf.timeconfig'
$pluginDir = Join-Path $GameRoot 'BepInEx\plugins\com.lncinteractive'
$outputDir = Join-Path $repoRoot "src\MoreSettings\bin\$Configuration\netstandard2.1"

if (-not (Test-Path $GameRoot)) {
    throw "Game root was not found: $GameRoot"
}

if (-not (Test-Path (Join-Path $gameManagedDir 'Assembly-CSharp.dll'))) {
    throw "Game managed assemblies were not found under: $gameManagedDir"
}

if (-not (Test-Path (Join-Path $bepInExExtract 'BepInEx\core\BepInEx.dll'))) {
    & $fetchScript
}

Get-ChildItem -LiteralPath $bepInExExtract -Force |
    Copy-Item -Destination $GameRoot -Recurse -Force

& dotnet build $projectFile -c $Configuration "/p:GameManagedDir=$gameManagedDir"

if (-not (Test-Path (Join-Path $outputDir 'MoreSettings.dll'))) {
    throw "Expected build output was not found under: $outputDir"
}

if ((Test-Path $legacyPluginDir) -and ($legacyPluginDir -ne $pluginDir)) {
    Remove-Item -LiteralPath $legacyPluginDir -Recurse -Force
}

New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

foreach ($legacyArtifact in @('ConfigManager.dll', 'ConfigManager.pdb', 'LobbySettings.dll', 'LobbySettings.pdb')) {
    $legacyArtifactPath = Join-Path $pluginDir $legacyArtifact
    if (Test-Path $legacyArtifactPath) {
        Remove-Item -LiteralPath $legacyArtifactPath -Force
    }
}

Copy-Item -LiteralPath (Join-Path $outputDir 'MoreSettings.dll') -Destination $pluginDir -Force

$pdbPath = Join-Path $outputDir 'MoreSettings.pdb'
if (Test-Path $pdbPath) {
    Copy-Item -LiteralPath $pdbPath -Destination $pluginDir -Force
}

Write-Host "MoreSettings deployed to: $pluginDir"
Write-Host "Expected config path after first launch: $(Join-Path $GameRoot 'BepInEx\config\com.lncinteractive.cfg')"