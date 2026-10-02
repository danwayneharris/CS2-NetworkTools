# Builds only the standalone .NET 8 generator; never builds/deploys the mod.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    & dotnet build NetworkTools.Codegen/NetworkTools.Codegen.csproj --source https://api.nuget.org/v3/index.json --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Standalone generator build failed.' }
    & uv run --no-project python scripts/check-codegen.py --dll NetworkTools.Codegen/bin/Debug/net8.0/NetworkTools.Codegen.dll
    if ($LASTEXITCODE -ne 0) { throw 'Codegen regression failed.' }
} finally { Pop-Location }
