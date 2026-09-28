[CmdletBinding()]
param([string]$BridgePath = (Join-Path $PSScriptRoot '../../cities2-agent-bridge-ndc'))
$ErrorActionPreference = 'Stop'
foreach ($file in @('bridge.ps1', 'commands.json', 'rebuilt/build-manifest.json', 'rebuilt/CitiesIIAgentBridge.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $BridgePath $file))) {
        throw "Missing bridge prerequisite: $file under $BridgePath. Follow BOOTSTRAP.md local bridge setup."
    }
}
$commands = Get-Content -LiteralPath (Join-Path $BridgePath 'commands.json') -Raw | ConvertFrom-Json
foreach ($command in @('get_junction_snapshot', 'get_junction_preview')) {
    if ($command -notin $commands) { throw "Bridge lacks $command; upstream alone does not contain our local extensions." }
}
$manifest = Get-Content -LiteralPath (Join-Path $BridgePath 'rebuilt/build-manifest.json') -Raw | ConvertFrom-Json
$source = Join-Path $BridgePath 'rebuilt/CitiesIIAgentBridge.dll'
if ((Get-FileHash -LiteralPath $source).Hash -ne $manifest.dllSha256) { throw 'Bridge build manifest mismatch.' }
$game = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User')
$mods = [Environment]::GetEnvironmentVariable('CSII_LOCALMODSPATH', 'User')
if (!$game -or !$mods) { throw 'Missing user-scoped CSII_INSTALLATIONPATH or CSII_LOCALMODSPATH.' }
if ((Get-FileHash -LiteralPath (Join-Path $game 'Cities2_Data/Managed/Game.dll')).Hash -ne $manifest.gameAssemblySha256) {
    throw 'Bridge was built against different game assemblies; rebuild it.'
}
$installed = Join-Path $mods 'CitiesIIAgentBridge/CitiesIIAgentBridge.dll'
if (!(Test-Path -LiteralPath $installed)) { throw "Bridge not installed: $installed. See BOOTSTRAP.md." }
if ((Get-FileHash -LiteralPath $installed).Hash -ne $manifest.dllSha256) { throw 'Installed bridge differs from rebuilt DLL; deploy deliberately with the game closed.' }
Write-Host 'Bridge files and installed hashes match. This does not establish loading, playset enablement, or live API compatibility.'
