# Windows bootstrap and local builds

> October 3 successor: see [development build identity](NetworkTools.docs/development-build-identity.md) and [six-plan sprint status](NetworkTools.docs/six-plan-sprint-review.md). Historical verification statements below apply only to their recorded revisions; later bounded native/Release evidence is linked in the audit disposition. Broad Release qualification and vehicle traversal remain separate.

Run from PowerShell as the Windows user who installed the CS2 modding toolchain.
The script supports Windows PowerShell 5.1 and PowerShell 7.

```powershell
# Check prerequisites and print the installed toolchain paths.
.\scripts\bootstrap.ps1

# Install missing command-line prerequisites with WinGet, then check again.
.\scripts\bootstrap.ps1 -Install

# Restore packages, compile, postprocess, and deploy locally (close CS2 first).
.\scripts\bootstrap.ps1 -Install -Build

# Full Debug package, including postprocessing/UI, without installing into CS2.
.\scripts\bootstrap.ps1 -Build -PackageOnly

# Run the actual offline suites; no deployment or game access.
.\scripts\bootstrap.ps1 -OfflineTest

# Legacy path: BUILDS/DEPLOYS, then invokes the legacy test project.
# This does not execute the actual aggregate above.
.\scripts\bootstrap.ps1 -Test
```

WinGet installations may show a Windows administrator/UAC prompt. The script
does not publish to Paradox Mods, alter global NuGet sources, or change game code.
The existing build replaces the local `NetworkTools` development mod directory.
Back up any manually edited files there before building.

## Verification commands and side effects

| Command | What it does | Game/deployment effect |
| --- | --- | --- |
| `bootstrap.ps1` | Inspect prerequisites and refresh this process environment | None |
| `bootstrap.ps1 -OfflineTest` | Geometry, path-selection, parameters, codegen, compiled-production Slope, original-input and Python suites; JSON/log summary under `artifacts/offline-tests` | No deployment or game connection; writes local build/test artifacts |
| `uv run --no-project python scripts/run-offline-tests.py --output artifacts/offline-tests` | Same aggregate directly; needs tools on PATH | Same non-deploying scope |
| `scripts/test-slope.ps1` | Compile Debug production code using `Compile`, then exercise it with game mathematics assemblies | No postprocessing, UI build or deployment; no ECS world |
| `bootstrap.ps1 -Build` | Full SDK build, postprocessing, UI and local deployment | Replaces local mod; close CS2 first |
| `bootstrap.ps1 -Test` | Legacy full build/deployment plus legacy test project | Deploys; does not certify current suites |
| Live regression scripts | Checkpoint/reload/preview/Apply according to the selected script | May visibly restart and modify toy networks; read [runbook](NetworkTools.docs/live-regression-runner.md) first |

`-OfflineTest` is standalone and Debug-only: do not combine it with `-Build`,
`-Test`, `-Install`, `-Decompile`, `-CheckBridge`, or Release. It prefers uv and
otherwise uses Python. Slope needs the installed CS2 toolchain and initialized
Common submodule; .NET 8 and PowerShell are required. Missing prerequisites are
blocked results, not passes. Required suites must emit execution evidence; every
failed/blocked suite makes the aggregate exit nonzero. Optional trace/replay modes,
native behavior, Release/Burst execution and visual/vehicle review are not implied.

Normal builds still couple packaging and deployment through SDK targets. There
is no new standalone package-only command in this sprint; do not assume that
`dotnet build` is safe for an open game. Compile-only verification is a distinct,
limited stage. Current audit work and configuration limits are tracked in the
[audit disposition](NetworkTools.docs/audit-disposition.md).

## Optional: full local game source for investigation

Use a separate directory outside this mod repository and the game installation:

```powershell
.\scripts\bootstrap.ps1 -Decompile -DecompilePath 'C:\Users\danwa\dev\cs2-mods\cs2-decompile'
# Any other empty destination is supported through -DecompilePath.
# Standalone export, without checking UI/postprocessor prerequisites:
.\scripts\decompile-game.ps1 -Destination 'D:\source\cs2-decompile'
```

`-Decompile` is opt-in and requires an explicit configurable path. It neither
builds nor deploys a mod unless you also specify `-Build`/`-Test`. The game may
remain open: export only reads its installed assemblies. Avoid updating the game
while it runs; the helper checks assembly hashes again before recording success.

The helper installs **ILSpy 9.1.0.7988** as a pinned local .NET tool (compatible
with our .NET 8 SDK), exports every DLL in `CSII_MANAGEDPATH` into
`src/<Assembly>/<Namespace>/<Type>.cs`, and creates a separate local Git repository
with the source, tool manifest, and per-assembly SHA256 manifest committed. Expect
many files and several minutes. No Git remote is configured or source uploaded.
The generated projects are for source inspection; export does not promise that
the game can be rebuilt from them.

After success it updates `%USERPROFILE%\.cs2-modding\setup.md`, preserving unrelated
settings. This lets the modding skills find the source across projects. Game version
comes from `Player.log` when available, Unity version from `Cities2.exe`, and assembly
hashes identify the actual input even if the log is stale. ILSpy's version stays
pinned so a decompiler change does not masquerade as a game-code change.

The first implementation refuses nonempty destinations rather than deleting or
overwriting local source. After a game update, export into a new sibling directory
(for example `cs2-decompile-next`), compare its manifest/source with the previous
snapshot, and retain both local Git histories. The setup record switches only after
the new export and commit succeed. In-place refresh is not yet automated. A failed
export retains its partial files for diagnosis and does not write a success record;
use a new empty destination for a retry. Do not push proprietary decompiled source
into either mod repository.

## One-time game toolchain setup (in game)

1. Install and launch Cities: Skylines II.
2. Open **Options > Modding** and install the modding toolchain.
3. When prompted, open Unity Hub and sign in. Under **Settings > Licenses**, activate
   your license. Eligible users can select **Add license > Get a free personal license**.
4. Return to the game and finish installation until the checks pass. Close the game.
5. Run the bootstrap script again.

Use the Unity version requested by CS2. This version must match the game's tooling.
The bootstrap does not automate Unity account login or license acceptance. We have
not verified a supported standalone installer for the complete CS2 toolchain.
Skyve's documented mod/playset management is not a replacement for this developer setup.

Sources: [CS2 toolchain overview](https://colossalorder.fi/news/development-diary-code-modding/),
[Unity license activation](https://docs.unity.com/en-us/hub/manage-license),
[Skyve features](https://github.com/JadHajjar/Skyve/blob/main/README.md).

## Dependencies

| Dependency | How it is handled |
| --- | --- |
| Git and `NetworkTools.Mod/Common` | Check the pinned submodule commit; `-Install` initializes missing submodules without updating their revisions. |
| .NET 8 SDK | Required for `NetworkTools.Codegen`; install with `Microsoft.DotNet.SDK.8`. |
| Node.js and npm | UI declares Node >=18; `-Install` uses `OpenJS.NodeJS.LTS` when missing or too old. |
| React and other UI packages | Project-local npm dependencies, installed by the existing build's `prebuild` step. No global React installation. |
| .NET Framework 4.8 reference assemblies | Restored by the .NET SDK through NuGet when no targeting pack is installed. |
| CS2/Unity assemblies and generators | Installed by the in-game toolchain. |
| Mod postprocessor runtime | Read from `ModPostProcessor.runtimeconfig.json`; the installed toolchain currently requires .NET 6, separate from the .NET 8 SDK. |

Installing Visual Studio Build Tools alone does not guarantee these SDKs or runtimes
are present. The bootstrap uses `dotnet build`; a full Visual Studio IDE is not required.

## Environment and installation locations

The projects explicitly read **User-scoped** `CSII_*` Windows environment variables.
Setting `$env:CSII_TOOLPATH` only in a shell is insufficient for those imports.
The UI, however, reads its current process environment. The script reads the user
variables and exports them into the build process, and refreshes PATH from both
user and machine settings. Restarting the terminal is normally unnecessary.

For comparison:

```powershell
# Current shell and its future child processes only (like Bash export):
$env:PATH = "C:\Program Files\dotnet;$env:PATH"

# Inspect the persistent setting used by MSBuild:
[Environment]::GetEnvironmentVariable('CSII_TOOLPATH', 'User')
```

Separate agent shell calls may have separate process environments. Also, a sandbox
account has different User-scoped settings from your interactive Windows account.
Build as the toolchain owner; do not copy account-specific paths into project files.

Locations observed on this machine (the script prints the actual user settings):

| Component | Location |
| --- | --- |
| .NET SDK/runtimes | `C:\Program Files\dotnet` |
| CS2 install | `C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II` |
| Toolchain `Mod.props` / `Mod.targets` | `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Modding` |
| Unity mod project/package cache | `UnityModsProject` under that toolchain directory |
| Mod postprocessor | `Cities2_Data\Content\Game\.ModdingToolchain\ModPostProcessor` under the game install |
| Local development mod | `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\NetworkTools` |

## Findings from the initial unmodified build

The first build stopped because `dotnet` was unavailable. After installing SDK
8.0.425, it stopped at missing `Mod.props`. Installing the CS2 toolchain resolved that.

NuGet had no configured sources. The script passes nuget.org explicitly rather
than changing machine or user configuration.

Compilation then failed on `ReadOnlySpan<T>` and dictionary entry deconstruction.
MSBuild was choosing NuGet's stock .NET Framework 4.8 `mscorlib.dll` instead of the
game's core library. These command-line settings resolved both errors without
editing the mod or the Common submodule:

```powershell
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
dotnet build CS2-NetworkTools.sln --configuration Debug `
    --source https://api.nuget.org/v3/index.json `
    "-p:FrameworkPathOverride=$managed" `
    -p:AdditionalExplicitAssemblyReferences=netstandard
```

Use the bootstrap for environment refresh; the command above assumes PATH and the
process-level CSII variables are already available. The `netstandard` reference
is essential when using the game's framework directory.

The full Debug build now succeeds, including postprocessing, code generation, UI
build, and local deployment. .NET 6.0.36 resolved the missing runtime; the user then
disabled Smart App Control to resolve the Windows block on the unsigned postprocessor
(`0x800711C7`). See the [build-system diagnosis](NetworkTools.docs/build-system.md#windows-application-control-blocker).
Both the original-source build and a subsequent UI-marker build passed. Automated
tests and Release builds remain unverified. As of Sept 26, 2026, the maintainer
confirmed the modified tooltip appeared in-game and Connect successfully joined
two road segments. Other tool behavior remains unverified in this fork.

## In-game smoke test after a successful build

1. Before starting CS2, disable the subscribed Network Tools in the playset used
   for testing (Skyve can manage the playset). Keep the local development build;
   avoid loading both copies together.
2. Launch CS2 and load a disposable city or a copy of a save.
3. Hover over the Network Tools button near the upper-left UI. Its tooltip should
   contain **`[Dan local]`** after the translated mod name. The marker is also added
   to the editor button tooltip. Its presence verifies the edited UI bundle loaded.
4. Open and close the panel. Select an existing tool and verify selection/preview
   feedback. Try a small Add Node operation on a disposable road, then inspect it.
5. Check `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs`
   for the NetworkTools log and `Player.log` in the parent directory for load errors.
   A marker alone does not establish that every C# system or geometry operation works.
6. Close the game before the next build, which replaces the local mod output.

Build success cannot establish in-game behavior or Anarchy compatibility. Record
the marker, panel, tool operation, and log results separately. Smooth Curve work
starts after this baseline is confirmed.

## Junction development dependencies

Our junction diagnostic workflow depends on the locally extended Cities II Agent
Bridge, maintained in sibling `../cities2-agent-bridge-ndc`. Network Tools itself
has no bridge assembly/runtime dependency: normal players do not need it.
Use the merged fork main: `git clone https://github.com/danwayneharris/cities2-agent-bridge-ndc.git ../cities2-agent-bridge-ndc`. Follow that checkout's current INSTALL.md and docs/MOD-PROVIDERS.md; the historical junction-snapshot branch is not a setup requirement. Discover live generic provider capabilities rather than inferring them from installed files.

From the NetworkTools root, build the available local checkout without deploying:

```powershell
$bridge = Resolve-Path ../cities2-agent-bridge-ndc
$game = [Environment]::GetEnvironmentVariable('CSII_INSTALLATIONPATH', 'User')
& "$bridge/build.ps1" -GamePath $game -OutputDirectory "$bridge/rebuilt" -CommunityRelease
& "$bridge/tests/JunctionApiTests.ps1" -GamePath $game
```

To install this local build, save and close CS2, confirm no Cities2 process remains,
then use the following explicit copy procedure. This is separate from ordinary
bootstrap checks; `-Install` does not install or enable the bridge.

```powershell
if (Get-Process Cities2 -ErrorAction SilentlyContinue) { throw 'Close CS2 first' }
$manifest = Get-Content "$bridge/rebuilt/build-manifest.json" -Raw | ConvertFrom-Json
$source = "$bridge/rebuilt/CitiesIIAgentBridge.dll"
if ((Get-FileHash $source).Hash -ne $manifest.dllSha256) { throw 'DLL mismatch' }
if ((Get-FileHash "$game/Cities2_Data/Managed/Game.dll").Hash -ne $manifest.gameAssemblySha256) { throw 'Game mismatch' }
$mods = [Environment]::GetEnvironmentVariable('CSII_LOCALMODSPATH', 'User')
if (!$mods) { throw 'Missing CSII_LOCALMODSPATH' }
$destination = Join-Path $mods 'CitiesIIAgentBridge'
New-Item -ItemType Directory -Force $destination | Out-Null
$dll = Join-Path $destination 'CitiesIIAgentBridge.dll'
if (Test-Path $dll) { Copy-Item $dll "$dll.$([guid]::NewGuid().ToString('N')).bak" }
Copy-Item $source $dll
if ((Get-FileHash $dll).Hash -ne $manifest.dllSha256) { throw 'Copy mismatch' }
.\scripts\check-bridge.ps1 -BridgePath $bridge
```

Or add `-CheckBridge` (and optional `-BridgePath`) to `bootstrap.ps1` to run the
same read-only diagnostic dependency checks alongside prerequisites. Checks do not
launch the game or send mailbox commands. A stale installed DLL is an error, not an
automatic deployment request. Enable the local bridge and local Network Tools in
your disposable test playset using Skyve; avoid duplicate published versions.
Load the toy save, pause manually, and leave **Allow local bridge controls off**
for snapshot queries. See the bridge's `docs/JUNCTION-SNAPSHOTS.md` and `INSTALL.md`.

## Coding-agent plugin

We use `cs2-modding@csmodding`, a knowledge/skills plugin rather than an in-game mod.
It is recommended for source-grounded development, not required to compile or play.
The marketplace's README and the installed Codex CLI document:

```powershell
codex plugin marketplace add CitiesSkylinesModding/agents-plugins
codex plugin add cs2-modding@csmodding
codex plugin list
```

Start a new agent session and verify the cs2-modding skills are available. This
plugin has no MCP server, so absence from `/mcp` is not a failure. Installing it
does not install Unity, the CS2 SDK, the bridge, or a game-debugging patch. The
marketplace's unity-devtools/coherent-gameface plugins are separate optional tools.
See [marketplace instructions](https://github.com/CitiesSkylinesModding/agents-plugins#install)
and [OpenAI marketplace guidance](https://developers.openai.com/plugins/build/plugins).

### Optional live-debugging plugins

See [live-debugging setup](../cities2-agent-bridge-ndc/docs/DEBUGGING-PLUGINS.md) for the
coherent-gameface and unity-devtools prerequisites, reversible development-player
patch, and separate server/live-connection verification. These are optional
developer tools and are not dependencies of the released mod.


## Combined smoothing experiment

Debug builds expose an opt-in Constant Slope option within Smooth Curve. Building
with `-Build` deploys as described above; the offline test command does not.
See [combined review and exact toy saves](NetworkTools.docs/combined-smoothing-review.md)
for verification scope and manual steps. Release does not expose this experiment.


## Experimental native finishing compatibility (Debug only)

Current candidate requires explicit game launch argument
`--nt-experimental-finish-height-preparation`. It changes native geometry rebuilds,
including save loading, not only the active tool. Ordinary launches leave it disabled.
Read [current review and exact saves](NetworkTools.docs/combined-smoothing-review.md)
before enabling; game binary fingerprints and patch-conflict guards can refuse it.
No agent settings, Steam settings, global Burst settings or persistent user options
are modified by this argument. Disabling it may reproduce native surface discrepancies
on later rebuilding. Release promotion and broad performance testing remain deferred.

The offline Python aggregate reports parameterized native research CLIs separately
as not run. See `NetworkTools.NativeReplay/README.md` for their explicit captured-input
commands; an aggregate pass does not imply those commands or live tests ran.
