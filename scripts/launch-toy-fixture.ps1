# Explicit visible toy-fixture launch; does not deploy or close a running game.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Fixture,
    [string]$SaveRoot=(Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Saves'),
    [string]$BridgePath=(Join-Path $PSScriptRoot '..\..\cities2-agent-bridge-ndc'),
    [switch]$ExperimentalFinishHeightPreparation
)
$ErrorActionPreference='Stop'
if(Get-Process Cities2 -ErrorAction SilentlyContinue){throw 'Game already running; checkpoint and close gracefully first'}
if(!(Get-Process steam -ErrorAction SilentlyContinue)){throw 'Steam must be running'}
$f=Get-Content -LiteralPath $Fixture -Raw | ConvertFrom-Json
if($f.baseline -notlike '*bridge*test*.cok' -or [IO.Path]::GetFileName($f.baseline) -ne $f.baseline){throw 'Expected a named bridge test baseline'}
$saves=@(Get-ChildItem -LiteralPath $SaveRoot -Recurse -File | Where-Object {$_.Name -eq $f.baseline})
if($saves.Count -ne 1){throw 'Missing or ambiguous baseline'}
$save=$saves[0].FullName
if((Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -ne $f.baselineSha256){throw 'Baseline checksum mismatch'}
. (Join-Path $BridgePath 'save-metadata-identity.ps1')
$id=Get-SaveMetadataIdentity $save
$exe=Join-Path ([Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH','User')) 'Cities2.exe'
if(!(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Game executable missing'}
$working=Join-Path $env:TEMP ('cs2-toy-fixture-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $working | Out-Null
[IO.File]::WriteAllText((Join-Path $working 'steam_appid.txt'),'949230',[Text.Encoding]::ASCII)
$gameArgs=@('--noSplash',"--startGame=$id")
if($ExperimentalFinishHeightPreparation){$gameArgs+='--nt-experimental-finish-height-preparation'}
$p=Start-Process -FilePath $exe -WorkingDirectory $working -ArgumentList $gameArgs -WindowStyle Normal -PassThru
[pscustomobject]@{pid=$p.Id;save=$save;arguments=$gameArgs;status='Launch requested; verify loaded toy geometry, pause, controls and build independently'}
