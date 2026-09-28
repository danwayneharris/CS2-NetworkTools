[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$Build,
    [switch]$Test,
    [switch]$CheckBridge,
    [switch]$Decompile,
    [string]$DecompilePath,
    [string]$BridgePath = (Join-Path $PSScriptRoot '../../cities2-agent-bridge-ndc'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

function Refresh-Environment {
    # Preserve launcher-provided paths while picking up newly installed tools.
    $env:Path = (@($env:Path, [Environment]::GetEnvironmentVariable('Path', 'Machine'),
        [Environment]::GetEnvironmentVariable('Path', 'User')) -join ';')
    if (Test-Path "$env:ProgramFiles\dotnet\dotnet.exe") {
        $env:Path = "$env:ProgramFiles\dotnet;$env:Path"
    }
    foreach ($entry in [Environment]::GetEnvironmentVariables('User').GetEnumerator()) {
        if ($entry.Key -like 'CSII_*') {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
        }
    }
}

function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}

function Install-Package {
    param([string]$Id)
    if (-not $Install) { throw "Missing prerequisite: $Id. Run again with -Install to install it through WinGet." }
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw 'WinGet is required for automatic installation. Install Windows App Installer, then retry.'
    }
    Invoke-Checked winget @('install', '-e', '--id', $Id, '--accept-source-agreements',
        '--accept-package-agreements', '--silent')
    Refresh-Environment
    Write-Host 'Junction diagnostics require our local bridge extension; ordinary NetworkTools builds do not. See BOOTSTRAP.md.'
    if ($CheckBridge) {
        & (Join-Path $PSScriptRoot 'check-bridge.ps1') -BridgePath $BridgePath
    }
}

Push-Location $repo
try {
    Refresh-Environment
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Install-Package 'Git.Git' }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Install-Package 'Microsoft.DotNet.SDK.8' }
    if (-not ((& dotnet --list-sdks) -match '^8\.0\.')) { Install-Package 'Microsoft.DotNet.SDK.8' }
    if (-not ((& dotnet --list-sdks) -match '^8\.0\.')) { throw '.NET 8 SDK is still unavailable.' }
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { Install-Package 'OpenJS.NodeJS.LTS' }
    if ([int]((& node --version) -replace '^v', '').Split('.')[0] -lt 18) {
        Install-Package 'OpenJS.NodeJS.LTS'
    }
    if ([int]((& node --version) -replace '^v', '').Split('.')[0] -lt 18) { throw 'Node.js 18 or newer is required.' }
    if (-not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) { throw 'npm.cmd is missing; repair your Node.js installation.' }

    $submodule = & git submodule status -- NetworkTools.Mod/Common
    if ($LASTEXITCODE -ne 0 -or -not $submodule) { throw 'Unable to inspect the Common submodule.' }
    if ($submodule.StartsWith('-')) {
        if (-not $Install) { throw 'Common is uninitialized. Run with -Install or run git submodule update --init --recursive.' }
        Invoke-Checked git @('submodule', 'update', '--init', '--recursive', '--', 'NetworkTools.Mod/Common')
    } elseif ($submodule.StartsWith('+') -or $submodule.StartsWith('U')) {
        throw 'Common differs from the recorded commit or has conflicts. Resolve this before bootstrapping.'
    }

    # The project reads User-scoped variables explicitly. Process-only values do not suffice.
    $required = @('CSII_TOOLPATH', 'CSII_MANAGEDPATH', 'CSII_USERDATAPATH',
        'CSII_UNITYMODPROJECTPATH', 'CSII_MODPOSTPROCESSORPATH', 'CSII_MSCORLIBPATH', 'CSII_LOCALMODSPATH')
    foreach ($name in $required) {
        $value = [Environment]::GetEnvironmentVariable($name, 'User')
        if (-not $value) { throw "$name is unset for this Windows user. Complete CS2 Options > Modding toolchain installation and Unity license activation first." }
        Write-Host "$name=$value"
        if ($name -ne 'CSII_LOCALMODSPATH' -and -not (Test-Path -LiteralPath $value)) {
            throw "$name points to a missing path: $value. Repair the toolchain in CS2."
        }
    }
    foreach ($path in @((Join-Path $env:CSII_TOOLPATH 'Mod.props'),
        (Join-Path $env:CSII_TOOLPATH 'Mod.targets'), (Join-Path $env:CSII_MANAGEDPATH 'Game.dll'),
        (Join-Path $env:CSII_MANAGEDPATH 'netstandard.dll'))) {
        if (-not (Test-Path -LiteralPath $path)) { throw "Missing toolchain file: $path" }
    }
    $entitiesVersion = [Environment]::GetEnvironmentVariable('CSII_ENTITIESVERSION', 'User')
    $generators = Join-Path $env:CSII_UNITYMODPROJECTPATH "Library\PackageCache\com.unity.entities@$entitiesVersion\Unity.Entities\SourceGenerators"
    if (-not (Test-Path (Join-Path $generators 'SystemGenerator.dll'))) { throw "Missing ECS source generators: $generators" }

    $runtimeConfig = [IO.Path]::ChangeExtension($env:CSII_MODPOSTPROCESSORPATH, 'runtimeconfig.json')
    $runtime = (Get-Content -LiteralPath $runtimeConfig -Raw | ConvertFrom-Json).runtimeOptions.framework
    if ($runtime.name -ne 'Microsoft.NETCore.App') { throw "Unexpected postprocessor runtime. Inspect $runtimeConfig" }
    $runtimeVersion = [version]$runtime.version
    $runtimePattern = '^Microsoft\.NETCore\.App ' + $runtimeVersion.Major + '\.' + $runtimeVersion.Minor + '\.'
    if (-not ((& dotnet --list-runtimes) -match $runtimePattern)) {
        Install-Package "Microsoft.DotNet.Runtime.$($runtimeVersion.Major)"
    }
    if (-not ((& dotnet --list-runtimes) -match $runtimePattern)) { throw 'The postprocessor runtime is still unavailable.' }
    Write-Host 'Prerequisite checks passed.'

    if ($Decompile) {
        if (-not $DecompilePath) { throw 'Supply -DecompilePath for the separate, local source repository.' }
        & (Join-Path $PSScriptRoot 'decompile-game.ps1') -Destination $DecompilePath
    }

    if ($Build -or $Test) {
        $deploy = [IO.Path]::GetFullPath((Join-Path $env:CSII_LOCALMODSPATH 'NetworkTools'))
        Write-Host "Build replaces the local development mod at: $deploy"
        Write-Host 'Close CS2 before building. This does not publish to Paradox Mods.'
        # Use the game/Unity framework, including its netstandard facade, for net48.
        # NuGet's stock net48 core library lacks APIs exposed by current game assemblies.
        $buildArgs = @('build', 'CS2-NetworkTools.sln', '--configuration', $Configuration,
            '--source', 'https://api.nuget.org/v3/index.json',
            "-p:FrameworkPathOverride=$env:CSII_MANAGEDPATH",
            '-p:AdditionalExplicitAssemblyReferences=netstandard', '--verbosity', 'minimal')
        Invoke-Checked dotnet $buildArgs
    }
    if ($Test) {
        Invoke-Checked dotnet @('test', 'NetworkTools.Tests/NetworkTools.Tests.csproj',
            '--configuration', $Configuration, '--no-build', '--no-restore')
    }
} finally {
    Pop-Location
}
