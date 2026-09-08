[CmdletBinding()]
param(
    [string]$SourceRoot,
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RecoverySourceRoot {
    [CmdletBinding()]
    param(
        [string]$ExplicitRoot,
        [Parameter(Mandatory = $true)]
        [string]$UnityProjectRoot
    )

    $candidates = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $candidates.Add($ExplicitRoot)
    }
    if (-not [string]::IsNullOrWhiteSpace($env:TRICKCAL_REFERENCE_ROOT)) {
        $candidates.Add($env:TRICKCAL_REFERENCE_ROOT)
    }

    $settingsPath = Join-Path $UnityProjectRoot 'UserSettings\TrickcalRecoverySettings.json'
    if (Test-Path -LiteralPath $settingsPath -PathType Leaf) {
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($settings.sourceRoot)) {
            $candidates.Add([string]$settings.sourceRoot)
        }
    }

    foreach ($candidate in $candidates) {
        $resolved = [System.IO.Path]::GetFullPath($candidate)
        $apk = Join-Path $resolved '00_original_apk\trickcal.apk'
        $fileManifest = Join-Path $resolved 'file_manifest.csv'
        if ((Test-Path -LiteralPath $apk -PathType Leaf) -and
            (Test-Path -LiteralPath $fileManifest -PathType Leaf)) {
            return $resolved
        }
    }

    throw 'Recovery source not found. Configure TRICKCAL_REFERENCE_ROOT or UserSettings/TrickcalRecoverySettings.json.'
}

$resolvedRoot = Get-RecoverySourceRoot -ExplicitRoot $SourceRoot -UnityProjectRoot $ProjectRoot
Write-Output $resolvedRoot
