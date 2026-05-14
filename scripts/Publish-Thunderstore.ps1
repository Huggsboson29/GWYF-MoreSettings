#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Namespace = "",
    [string]$Token = "",
    [string]$GameRoot = "",
    [switch]$SkipTests,
    [switch]$InstallTcli,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Escape-TomlString {
    param([string]$Value)

    return ($Value ?? "").Replace('\', '\\').Replace('"', '\"')
}

function Split-DependencyString {
    param([string]$Dependency)

    $lastDash = $Dependency.LastIndexOf('-')
    if ($lastDash -lt 1 -or $lastDash -eq ($Dependency.Length - 1)) {
        throw "Dependency string '$Dependency' is not in the expected Thunderstore format."
    }

    return @{
        Id = $Dependency.Substring(0, $lastDash)
        Version = $Dependency.Substring($lastDash + 1)
    }
}

function Write-ThunderstoreConfig {
    param(
        [string]$ConfigPath,
        [pscustomobject]$Manifest,
        [string]$PackageNamespace
    )

    $dependencyLines = @()
    foreach ($dependency in $Manifest.dependencies) {
        $parts = Split-DependencyString -Dependency $dependency
        $dependencyLines += ('{0} = "{1}"' -f $parts.Id, (Escape-TomlString -Value $parts.Version))
    }

    if ($dependencyLines.Count -eq 0) {
        $dependencyLines += '# No dependencies declared'
    }

    $content = @(
        '[config]',
        'schemaVersion = "0.0.1"',
        '',
        '[package]',
        ('namespace = "{0}"' -f (Escape-TomlString -Value $PackageNamespace)),
        ('name = "{0}"' -f (Escape-TomlString -Value $Manifest.name)),
        ('versionNumber = "{0}"' -f (Escape-TomlString -Value $Manifest.version_number)),
        ('description = "{0}"' -f (Escape-TomlString -Value $Manifest.description)),
        ('websiteUrl = "{0}"' -f (Escape-TomlString -Value $Manifest.website_url)),
        'containsNsfwContent = false',
        '',
        '[package.dependencies]'
    )

    $content += $dependencyLines
    $content += @(
        '',
        '[build]',
        'icon = "./src/MoreSettings/Packaging/icon.png"',
        'readme = "./src/MoreSettings/Packaging/README.md"',
        'outdir = "./artifacts/thunderstore/tcli-build"',
        '',
        '[[build.copy]]',
        'source = "./src/MoreSettings/bin/Release/netstandard2.1/MoreSettings.dll"',
        'target = "plugins/MoreSettings/"',
        '',
        '[publish]',
        'repository = "https://thunderstore.io"',
        'communities = ["gamble-with-your-friends"]',
        '',
        '[publish.categories]',
        'gamble-with-your-friends = ["mods"]'
    )

    Set-Content -LiteralPath $ConfigPath -Value ($content -join [Environment]::NewLine) -Encoding UTF8
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$generatedConfigDirectory = Join-Path $repoRoot 'artifacts\thunderstore'
$configPath = Join-Path $generatedConfigDirectory 'thunderstore.publish.toml'
$manifestPath = Join-Path $repoRoot 'src\MoreSettings\Packaging\manifest.json'
$packScript = Join-Path $repoRoot 'scripts\Pack-Thunderstore.ps1'

if (-not (Test-Path $manifestPath)) {
    throw "Manifest file was not found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($Namespace)) {
    $Namespace = $env:THUNDERSTORE_NAMESPACE
}

if ([string]::IsNullOrWhiteSpace($Namespace)) {
    throw "Set THUNDERSTORE_NAMESPACE or pass -Namespace with your Thunderstore team name."
}

New-Item -ItemType Directory -Path $generatedConfigDirectory -Force | Out-Null
Write-ThunderstoreConfig -ConfigPath $configPath -Manifest $manifest -PackageNamespace $Namespace

$packArguments = @{}
if (-not [string]::IsNullOrWhiteSpace($GameRoot)) {
    $packArguments.GameRoot = $GameRoot
}
if ($SkipTests) {
    $packArguments.SkipTests = $true
}

& $packScript @packArguments

$zipPath = Join-Path $repoRoot ('artifacts\thunderstore\{0}-{1}.zip' -f $manifest.name, $manifest.version_number)
if (-not (Test-Path $zipPath)) {
    throw "Expected package zip was not found: $zipPath"
}

if ($DryRun) {
    Write-Host "Dry run only. Thunderstore config synced to: $configPath"
    Write-Host "Package ready for publish: $zipPath"
    Write-Host "Next command: tcli publish --config-path '$configPath' --file '$zipPath' --token <token>"
    return
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    $Token = $env:TCLI_AUTH_TOKEN
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "Set TCLI_AUTH_TOKEN or pass -Token with a Thunderstore API token."
}

$tcliCommand = Get-Command tcli -ErrorAction SilentlyContinue
$tcliPath = $tcliCommand?.Source
if ($null -eq $tcliCommand) {
    if (-not $InstallTcli) {
        throw "tcli was not found. Install it with 'dotnet tool install -g tcli' or rerun this script with -InstallTcli."
    }

    $tcliToolPath = Join-Path $generatedConfigDirectory '.tools\tcli'
    $tcliExecutable = Join-Path $tcliToolPath 'tcli.exe'
    $tcliFallbackExecutable = Join-Path $tcliToolPath 'tcli'

    New-Item -ItemType Directory -Path $tcliToolPath -Force | Out-Null

        if ((Test-Path $tcliExecutable) -or (Test-Path $tcliFallbackExecutable)) {
        & dotnet tool update tcli --tool-path $tcliToolPath
    }
    else {
        & dotnet tool install tcli --tool-path $tcliToolPath
    }

    if (Test-Path $tcliExecutable) {
        $tcliPath = $tcliExecutable
    }
    elseif (Test-Path $tcliFallbackExecutable) {
        $tcliPath = $tcliFallbackExecutable
    }
    else {
        throw "tcli installation did not produce an executable under: $tcliToolPath"
    }
}

& $tcliPath publish --config-path $configPath --file $zipPath --token $Token