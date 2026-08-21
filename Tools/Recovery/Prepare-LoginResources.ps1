[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string]$SourceRoot,
    [string]$PythonExecutable,
    [switch]$FinalizeUnityMeta,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PythonExecutable)) {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        throw 'Python 3 was not found. Pass -PythonExecutable with a Python 3 executable that has Pillow installed.'
    }
    $PythonExecutable = $python.Source
}

$resolvedSourceRoot = & (Join-Path $PSScriptRoot 'Resolve-RecoverySource.ps1') `
    -SourceRoot $SourceRoot `
    -ProjectRoot $ProjectRoot

$arguments = @(
    (Join-Path $PSScriptRoot 'Recover-LoginResources.py'),
    '--project-root', $ProjectRoot,
    '--source-root', $resolvedSourceRoot
)

if ($Apply) {
    $arguments += '--apply'
}
if ($FinalizeUnityMeta) {
    $arguments += '--finalize-unity-meta'
}

& $PythonExecutable @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Login resource recovery failed with exit code $LASTEXITCODE."
}

if ($Apply) {
    Write-Output 'Updated login Title Spine copies and their manifest.'
} else {
    Write-Output 'Validation-only run completed. Pass -Apply to write files.'
}
