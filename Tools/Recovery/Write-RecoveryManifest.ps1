[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MappingPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$SourceRoot,
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourceResolver = Join-Path $PSScriptRoot 'Resolve-RecoverySource.ps1'
$resolvedSourceRoot = & $sourceResolver -SourceRoot $SourceRoot -ProjectRoot $ProjectRoot
$mapping = Get-Content -LiteralPath $MappingPath -Raw | ConvertFrom-Json
$mappingEntries = if ($null -ne $mapping.entries) { $mapping.entries } else { $mapping }
$allowedStatuses = @('confirmed', 'inferred', 'unconfirmed', 'synthetic', 'recreated')

$entries = foreach ($entry in $mappingEntries) {
    $sourceRelative = ([string]$entry.sourceRelativePath).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $outputRelative = ([string]$entry.outputRelativePath).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    if ([System.IO.Path]::IsPathRooted($sourceRelative) -or [System.IO.Path]::IsPathRooted($outputRelative)) {
        throw 'Manifest paths must be relative.'
    }
    if ($allowedStatuses -notcontains [string]$entry.evidenceStatus) {
        throw "Unsupported evidence status: $($entry.evidenceStatus)"
    }

    $sourcePath = Join-Path $resolvedSourceRoot $sourceRelative
    $outputFilePath = Join-Path $ProjectRoot $outputRelative
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Source file is missing: $sourceRelative"
    }
    if (-not (Test-Path -LiteralPath $outputFilePath -PathType Leaf)) {
        throw "Generated file is missing: $outputRelative"
    }

    $sourceFile = Get-Item -LiteralPath $sourcePath
    $outputFile = Get-Item -LiteralPath $outputFilePath
    $supportingSources = @()
    if ($entry.PSObject.Properties.Name -contains 'supportingSources') {
        $supportingSources = @($entry.supportingSources | ForEach-Object {
            $supportingRelative = ([string]$_.relativePath).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
            if ([System.IO.Path]::IsPathRooted($supportingRelative)) {
                throw 'Supporting source paths must be relative.'
            }
            $supportingPath = Join-Path $resolvedSourceRoot $supportingRelative
            if (-not (Test-Path -LiteralPath $supportingPath -PathType Leaf)) {
                throw "Supporting source file is missing: $supportingRelative"
            }
            $supportingFile = Get-Item -LiteralPath $supportingPath
            [ordered]@{
                role = [string]$_.role
                relativePath = ([string]$_.relativePath).Replace('\', '/')
                bytes = $supportingFile.Length
                sha256 = (Get-FileHash -LiteralPath $supportingPath -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        })
    }
    [ordered]@{
        category = [string]$entry.category
        purpose = if ($entry.PSObject.Properties.Name -contains 'purpose') { [string]$entry.purpose } else { [string]$entry.category }
        evidenceStatus = [string]$entry.evidenceStatus
        sourceRelativePath = ([string]$entry.sourceRelativePath).Replace('\', '/')
        sourceBytes = $sourceFile.Length
        sourceSha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
        supportingSources = $supportingSources
        outputRelativePath = ([string]$entry.outputRelativePath).Replace('\', '/')
        outputBytes = $outputFile.Length
        outputSha256 = (Get-FileHash -LiteralPath $outputFilePath -Algorithm SHA256).Hash.ToLowerInvariant()
        transformation = [string]$entry.transformation
        evidence = if ($null -ne $entry.evidence) { [string]$entry.evidence } else { '' }
        notes = if ($null -ne $entry.notes) { [string]$entry.notes } else { '' }
    }
}

$identityPath = Join-Path $ProjectRoot 'Recovery\Manifests\source-identity.json'
$manifest = [ordered]@{
    schemaVersion = 1
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    sourceIdentity = [ordered]@{
        manifestRelativePath = 'Recovery/Manifests/source-identity.json'
        sha256 = (Get-FileHash -LiteralPath $identityPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    entries = @($entries | Sort-Object outputRelativePath)
}

$parent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output "Generated manifest: $OutputPath"
