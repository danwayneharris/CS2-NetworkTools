$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
if (!$managed -or !(Test-Path -LiteralPath $managed)) { throw 'Run bootstrap to configure CSII_MANAGEDPATH first.' }
Push-Location $repo
try {
    & dotnet msbuild NetworkTools.Mod/NetworkTools.csproj -t:Compile -p:Configuration=Debug "-p:FrameworkPathOverride=$managed" -p:AdditionalExplicitAssemblyReferences=netstandard -verbosity:quiet
    if ($LASTEXITCODE -ne 0) { throw 'Non-deploying compile failed.' }
    & dotnet run --project NetworkTools.Slope.Tests
    if ($LASTEXITCODE -ne 0) { throw 'Slope regression failed.' }
} finally { Pop-Location }
