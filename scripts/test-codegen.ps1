# Builds only the standalone .NET 8 generator; never builds/deploys the mod.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    & dotnet build NetworkTools.Codegen/NetworkTools.Codegen.csproj --source https://api.nuget.org/v3/index.json --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Standalone generator build failed.' }
    if (Get-Command uv -ErrorAction SilentlyContinue) {
        & uv run --no-project python scripts/check-codegen.py --dll NetworkTools.Codegen/bin/Debug/net8.0/NetworkTools.Codegen.dll
    } elseif (Get-Command python -ErrorAction SilentlyContinue) {
        & python scripts/check-codegen.py --dll NetworkTools.Codegen/bin/Debug/net8.0/NetworkTools.Codegen.dll
    } else {
        throw 'Python is required for codegen regression checks; install uv or Python.'
    }
    if ($LASTEXITCODE -ne 0) { throw 'Codegen regression failed.' }
} finally { Pop-Location }
