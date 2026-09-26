# Windows bootstrap and local builds

Run from PowerShell as the Windows user who installed the CS2 modding toolchain.
The script supports Windows PowerShell 5.1 and PowerShell 7.

```powershell
# Check prerequisites and print the installed toolchain paths.
.\scripts\bootstrap.ps1

# Install missing command-line prerequisites with WinGet, then check again.
.\scripts\bootstrap.ps1 -Install

# Restore packages, compile, postprocess, and deploy locally (close CS2 first).
.\scripts\bootstrap.ps1 -Install -Build

# Build and run the existing test project.
.\scripts\bootstrap.ps1 -Test
```

WinGet installations may show a Windows administrator/UAC prompt. The script
does not publish to Paradox Mods, alter global NuGet sources, or change game code.
The existing build replaces the local `NetworkTools` development mod directory.
Back up any manually edited files there before building.

## One-time game toolchain setup

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
tests, Release builds, and in-game behavior remain unverified.

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
