[CmdletBinding()]
param(
    [ValidateSet('Identity', 'Manifest')][string]$Mode = 'Identity',
    [string]$RepositoryRoot,
    [string]$ReleaseVersion,
    [string]$Configuration,
    [Parameter(Mandatory = $true)][string]$IdentityPath,
    [string]$ArtifactDirectory,
    [string]$AssemblyName = 'NetworkTools',
    [string]$GitExecutable = 'git'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Utf8IfChanged([string]$Path, [string]$Text) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($fullPath)) | Out-Null
    if (!(Test-Path -LiteralPath $fullPath) -or [IO.File]::ReadAllText($fullPath) -cne $Text) {
        [IO.File]::WriteAllText($fullPath, $Text, (New-Object Text.UTF8Encoding($false)))
    }
}

if ($Mode -eq 'Identity') {
    if ($ReleaseVersion -notmatch '^\d+\.\d+\.\d+$' -or $Configuration -notmatch '^[A-Za-z0-9-]+$') {
        throw 'Identity requires a numeric release version and an alphanumeric configuration.'
    }
    $revision = 'unknown'
    $dirty = 'unknown'
    try {
        # A source archive inside another checkout must not inherit that checkout's identity.
        if (!(Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git'))) { throw 'No repository metadata.' }
        $candidate = & $GitExecutable -C $RepositoryRoot rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -ne 0 -or $candidate -notmatch '^[a-fA-F0-9]{40,64}$') { throw 'Cannot read revision.' }
        $status = @(& $GitExecutable -C $RepositoryRoot status --porcelain --untracked-files=normal --ignore-submodules=none 2>$null)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read working tree.' }
        $revision = $candidate.ToLowerInvariant()
        $dirty = if ($status.Count -gt 0) { 'dirty' } else { 'clean' }
    } catch {
        Write-Warning 'Git identity unavailable; revision and working-tree state are unknown.'
    }
    $revisionLabel = if ($revision -eq 'unknown') { 'unknown' } else { 'g' + $revision.Substring(0, 12) }
    $identity = [ordered]@{
        schemaVersion = 1
        releaseVersion = $ReleaseVersion
        informationalVersion = "$ReleaseVersion+$revisionLabel.$dirty.$Configuration"
        sourceRevision = $revision
        workingTree = $dirty
        configuration = $Configuration
    }
    Write-Utf8IfChanged $IdentityPath ($identity | ConvertTo-Json)
    Write-Utf8IfChanged "$IdentityPath.version" $identity.informationalVersion
    Write-Output $identity.informationalVersion
} else {
    $identity = Get-Content -LiteralPath $IdentityPath -Raw | ConvertFrom-Json
    $uiMod = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../NetworkTools.Mod/UI/mod.json') -Raw | ConvertFrom-Json
    if ($uiMod.id -cne $AssemblyName) { throw 'UI mod id must match the assembly name.' }
    # Webpack outputModule emits .mjs; its filename is explicit in webpack.config.js.
    foreach ($required in @("$AssemblyName.dll", "$($uiMod.id).mjs", "$($uiMod.id).css")) {
        if (!(Test-Path -LiteralPath (Join-Path $ArtifactDirectory $required) -PathType Leaf)) {
            throw "Cannot certify incomplete package: missing $required"
        }
    }
    $directory = (Resolve-Path -LiteralPath $ArtifactDirectory).Path.TrimEnd('\', '/')
    $files = @(Get-ChildItem -LiteralPath $directory -File -Recurse | Where-Object { $_.Name -ne 'build-manifest.json' } | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = $_.FullName.Substring($directory.Length + 1).Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            bytes = $_.Length
        }
    })
    $manifest = [ordered]@{ schemaVersion = 1; build = $identity; artifacts = $files }
    Write-Utf8IfChanged (Join-Path $directory 'build-manifest.json') ($manifest | ConvertTo-Json -Depth 8)
}
