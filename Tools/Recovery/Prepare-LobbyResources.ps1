[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string]$SourceRoot,
    [string]$PythonExecutable,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PythonExecutable)) {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        throw 'Python was not found. Pass -PythonExecutable with a Python 3 executable that has Pillow installed.'
    }
    $PythonExecutable = $python.Source
}

$resolver = Join-Path $PSScriptRoot 'Resolve-RecoverySource.ps1'
$resolvedSourceRoot = & $resolver -SourceRoot $SourceRoot -ProjectRoot $ProjectRoot
$recoveryScript = Join-Path $PSScriptRoot 'Recover-LobbyResources.py'
if (-not (Test-Path -LiteralPath $recoveryScript -PathType Leaf)) {
    throw "Lobby recovery script is missing: $recoveryScript"
}

$arguments = @(
    $recoveryScript,
    '--project-root', $ProjectRoot,
    '--source-root', $resolvedSourceRoot
)
if (-not $Apply) {
    $arguments += '--validate-only'
}

& $PythonExecutable @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Lobby resource recovery failed with exit code $LASTEXITCODE."
}

if ($Apply) {
    Write-Output 'Updated lobby resource copies and their manifest.'
} else {
    Write-Output 'Validation-only run completed. Pass -Apply to write files.'
}
