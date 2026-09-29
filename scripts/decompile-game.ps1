[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Destination,
    [string]$ManagedPath = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User'),
    [string]$GamePath = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User')
)
$ErrorActionPreference = 'Stop'
$version = '9.1.0.7988' # .NET 8 compatible; change deliberately, separately from game updates.
function Run([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe failed ($LASTEXITCODE). Partial output retained; no success record written." }
}
if (!$ManagedPath -or !(Test-Path -LiteralPath (Join-Path $ManagedPath 'Game.dll'))) { throw 'Missing installed Game.dll' }
if (!$GamePath -or !(Test-Path -LiteralPath (Join-Path $GamePath 'Cities2.exe'))) { throw 'Missing installed Cities2.exe' }
$target = [IO.Path]::GetFullPath($Destination)
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach ($protected in @($repo, [IO.Path]::GetFullPath($GamePath))) {
    if ($target -eq $protected -or $target.StartsWith($protected.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Decompile destination must be outside the mod repository and game installation.'
    }
}
if ((Test-Path -LiteralPath $target) -and @(Get-ChildItem -LiteralPath $target -Force).Count) {
    throw 'Use an empty destination. For a game update, export into a new sibling directory first; see BOOTSTRAP.md.'
}
Get-Command dotnet,git -ErrorAction Stop | Out-Null
$assemblies = @(Get-ChildItem -LiteralPath $ManagedPath -Filter *.dll | Sort-Object Name)
$hashes = @($assemblies | ForEach-Object { @{name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
New-Item -ItemType Directory -Force -Path $target | Out-Null
Push-Location $target
try {
    Run git @('init')
    Run dotnet @('new', 'tool-manifest')
    Run dotnet @('tool', 'install', 'ilspycmd', '--version', $version, '--add-source', 'https://api.nuget.org/v3/index.json')
    # Export separately to keep src/<Assembly>/<Namespace>/<Type>.cs predictable.
    foreach ($assembly in $assemblies) {
        Write-Host "Decompiling $($assembly.Name)"
        Run dotnet @('tool', 'run', 'ilspycmd', '--disable-updatecheck', '-p', '-o',
            (Join-Path 'src' $assembly.BaseName), '-r', $ManagedPath, $assembly.FullName)
    }
    if (!(Test-Path 'src/Game/Game.Modding/IMod.cs')) { throw 'Expected Game source missing' }
    foreach ($entry in $hashes) {
        if ((Get-FileHash (Join-Path $ManagedPath $entry.name)).Hash -ne $entry.sha256) {
            throw 'Installed assemblies changed during decompilation; output is not a consistent snapshot.'
        }
    }
    $userData = [Environment]::GetEnvironmentVariable('CSII_USERDATAPATH', 'User')
    $gameVersion = 'unknown (check Player.log)'
    if ($userData -and (Test-Path (Join-Path $userData 'Player.log'))) {
        $line = Select-String -Path (Join-Path $userData 'Player.log') -Pattern '^Game version:' | Select-Object -Last 1
        if ($line) { $gameVersion = ($line.Line -replace '^Game version:\s*','').Trim() }
    }
    $unity = (Get-Item (Join-Path $GamePath 'Cities2.exe')).VersionInfo.ProductVersion
    @{ decompiler=$version; gameVersion=$gameVersion; unityVersion=$unity;
        createdUtc=[DateTime]::UtcNow.ToString('o'); assemblies=$hashes } |
        ConvertTo-Json -Depth 5 | Set-Content source-manifest.json -Encoding UTF8
    Run git @('add', '.config', 'src', 'source-manifest.json')
    Run git @('commit', '--quiet', '-m', "Decompile CS2 $gameVersion with ILSpy $version")
    $recordDir = Join-Path $env:USERPROFILE '.cs2-modding'
    New-Item -ItemType Directory -Force $recordDir | Out-Null
    $record = Join-Path $recordDir 'setup.md'
    $text = if (Test-Path $record) { Get-Content $record -Raw } else {
        "# cs2-modding setup`nDebug patch: (none)`nLaunch options: (none)`nMod corpus root: (none)`nUI bundle copy: (none)`nUI bundle lines: (none)`n"
    }
    $fields = [ordered]@{'Game install'=$GamePath; 'Game version'=$gameVersion; 'Unity version'=$unity;
        'Decompile root'=$target; 'Decompiled'=(Get-Date -Format yyyy-MM-dd)}
    foreach ($key in $fields.Keys) {
        $pattern = '(?m)^' + [regex]::Escape($key) + ':.*$'
        $replacement = $key + ': ' + $fields[$key]
        if ([regex]::IsMatch($text, $pattern)) {
            $text = [regex]::Replace($text, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement })
        } else { $text += "`n$replacement" }
    }
    [IO.File]::WriteAllText($record, $text, (New-Object Text.UTF8Encoding($false)))
    Write-Host "Source ready: $target"
    Write-Host "Machine source record: $record"
} finally { Pop-Location }
