[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string]$SourceRoot,
    [string]$PythonExecutable,
    [string[]]$Targets,
    [switch]$FinalizeUnityMeta,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PythonExecutable)) {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        throw 'Python 3 was not found. Pass -PythonExecutable with a Python 3 executable.'
    }
    $PythonExecutable = $python.Source
}

$resolvedSourceRoot = & (Join-Path $PSScriptRoot 'Resolve-RecoverySource.ps1') `
    -SourceRoot $SourceRoot `
    -ProjectRoot $ProjectRoot

$arguments = @(
    (Join-Path $PSScriptRoot 'Recover-CharacterResources.py'),
    '--project-root', $ProjectRoot,
    '--source-root', $resolvedSourceRoot
)

foreach ($target in $Targets) {
    if (-not [string]::IsNullOrWhiteSpace($target)) {
        $arguments += @('--target', $target)
    }
}

if ($Apply) {
    $arguments += '--apply'
}
if ($FinalizeUnityMeta) {
    $arguments += '--finalize-unity-meta'
}

& $PythonExecutable @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Character resource recovery failed with exit code $LASTEXITCODE."
}

if ($Apply) {
    Write-Output 'Updated character resource copies and their manifest.'
} else {
    Write-Output 'Validation-only run completed. Pass -Apply to write files.'
}
