[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolved = [System.IO.Path]::GetFullPath($SourceRoot)
$required = @(
    (Join-Path $resolved '00_original_apk\trickcal.apk'),
    (Join-Path $resolved 'file_manifest.csv')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required source file is missing: $path"
    }
}

$settingsDirectory = Join-Path $ProjectRoot 'UserSettings'
$settingsPath = Join-Path $settingsDirectory 'TrickcalRecoverySettings.json'
New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null
@{ sourceRoot = $resolved } |
    ConvertTo-Json |
    Set-Content -LiteralPath $settingsPath -Encoding utf8

Write-Output "Saved the local recovery source setting: $settingsPath"
