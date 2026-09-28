# Autonomous CS2 launch, load, and save: source investigation

Date: 2026-09-28. Target: installed Windows Steam CS2 1.6.2f1 (767.21d1) [6300.26419], Unity 2022.3.71f1 (c9bf13b0b844).

## Findings

An unattended launch/load/save loop is technically plausible using existing game entry points, without changing Network Tools. It is **not yet a verified capability of this installation**. The remaining work is an external launch orchestrator plus reliable in-game control/readiness/save acknowledgements.

The most consequential findings are:

1. **Cities2.exe is the native Unity game entry point, not the Paradox Play-button launcher.** The installed launcher bootstrap is `Launcher/dowser.exe`; its configuration points to `../Cities2.exe`. Steam platform initialization inside the game can redirect a bare executable launch back through Steam, explaining how starting the game executable can still lead to a launcher.
2. **The installed game supports `--startGame=<asset GUID>`.** This can dispatch directly to saved-game loading after boot. It accepts an asset identity, not a filesystem path or display name.
3. **The existing bridge startup helper selects the wrong kind of identity for normal packaged saves.** It reads the outer `.cok.cid`, which identifies the package. The loader accepts `SaveGameMetadata` or `SaveGameData`, not `PackageAsset`. Read the metadata CID *inside* the ZIP-format `.cok` instead. This is a concrete source-supported defect in the helper, not merely an argument-forwarding suspicion.
4. **Saving already has an in-process API and a bridge command.** However, the game's `Save()` task can return `true` while reporting save failure through its completion event. The bridge currently trusts the task result. A robust harness must verify more than `get_operation` reporting `complete`.
5. **Current bridge controls reset off on every load.** Fully unattended restarts require either authorized UI activation or a deliberately scoped automation authorization design. The existing bridge alone does not implement that lifecycle.

This session did not launch, close, kill, pause, load, save, query the live bridge, change Steam settings, or modify either mod. The user explicitly requested no launch/kill because another agent is working concurrently. Existing runtime experiments below are historical records, not tests performed here.

## Evidence and reproducibility

The local setup record is `C:/Users/danwa/.cs2-modding/setup.md`. It points to:

- Installation: `C:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II`.
- Decompiled source: `C:/Users/danwa/dev/cs2-mods/cs2-decompile/src`.
- Manifest: `../cs2-decompile/source-manifest.json` relative to the repository root.
- Export tool: ILSpy 9.1.0.7988; export date 2026-09-28.

Throughout this report, `D/` means that decompiled `src` directory, `I/` means the installation, and `B/` means sibling `../cities2-agent-bridge-ndc`. Line references are to this local snapshot, not upstream source releases. Proprietary decompiled code is not copied into this repository.

SHA256 values were recomputed from installed files. All five managed assemblies below match their decompile-manifest entries:

| File | SHA256 |
| --- | --- |
| Game.dll | `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A` |
| Colossal.Core.dll | `B434BCDCEDCD5473A3545CC9D207B3FEC56257027BD5EA5C67B75EB920DEE116` |
| Colossal.IO.AssetDatabase.dll | `8D5571FECE816C4E19237A3D8A0A6F477B36B812D3C86EA05B41CD7EFAA65D54` |
| Colossal.PSI.Steamworks.dll | `9209E63A41A59F125F338F039A47FD52949FF40F43E0EE791EE9ACC8ED7CAEF5` |
| Colossal.TestFramework.dll | `D67FDAF990FB397E555359DE95E7545454C72E181E7DD6768E007B6E0A6E466E` |

This is static analysis of matching assemblies plus read-only binary/package inspection. No new full decompile was necessary. It does not establish successful runtime behavior, active playset identity, installed bridge revision, or absence of dialogs during the next boot.

## 1. What the executable actually is

Read-only PE-header inspection found AMD64 (`0x8664`) binaries with a zero CLR directory RVA for all three files below. They are native executables/libraries, not managed C# assemblies suitable for ILSpy project export.

| File | Bytes | SHA256 |
| --- | ---: | --- |
| Cities2.exe | 666,624 | `150110372AC36D0FEF7370613F56BAA0A83B2C41228F2731F4D937C437737919` |
| UnityPlayer.dll | 31,068,592 | `1DA82D19C9B19B8B923E6018F2CAAB0D416E84916EFBA02EB58DE9171B40529F` |
| Launcher/dowser.exe | 7,855,168 | `FAC69493292F2E09B2D9C128B4B287D97B4C60F03076002177C621F7B3D7EBCD` |

`Cities2.exe` contains `UnityPlayer.dll`, `UnityMain2`, and a Unity WindowsPlayer Mono x64 build-path string. Its file product version and UnityPlayer's are both 2022.3.71f1. Dowser contains Go-style symbols and `github.com/paradox-interactive/launcher-v2-dowser` strings. These are binary strings and PE metadata; no native disassembler was run and this is not an exhaustive import/call-graph audit.

`I/Launcher/launcher-settings.json` is more directly useful than native disassembly:

- `exePath`: `../Cities2.exe`.
- Standard `exeArgs`: empty array.
- `distPlatform`: `steam`.
- `gameId`: `cities_skylines_2`.
- Alternate launch mode: same executable with `--disableCodeModding`.
- Reported game version: `1.6.2f1`.

Thus the useful architecture is Steam -> launcher bootstrap/launcher -> Unity game process -> managed Game.dll. Most launch/load/save policy is readable C# in Game.dll and Colossal assemblies. Reconstructing native UnityPlayer or the launcher's UI is unnecessary for discovering these operations.

## 2. Why direct execution can reopen the launcher or fail

`D/Game/Game.PSI/PlatformSupport.cs:11` selects Steam app ID **949230**. `D/Colossal.PSI.Steamworks/Colossal.PSI.Steamworks/SteamworksPlatform.cs:499` initializes Steam:

1. Run platform binary checks.
2. Call `SteamAPI.RestartAppIfNecessary` at line 516; return failure from platform initialization when it requests a relaunch.
3. Call `SteamAPI.Init()` at line 526.
4. Validate the returned app ID at line 532.

`D/Game/Game.SceneFlow/GameManager.cs:1671` initializes the platform manager. On failure it logs an error and calls `QuitGame()`; code then continues into asset-database registration. That ordering is consistent with the historical report of platform failure followed by asset/world errors. Static inspection cannot tell whether the original failure was a relaunch request, app-ID/context failure, or another platform initialization problem.

Valve documents that `RestartAppIfNecessary` may relaunch through Steam rather than restart the calling executable. API initialization also depends on a running, licensed Steam session and the correct OS user/elevation context. This makes Steam-context launching the first candidate to test. [Valve Steamworks API overview](https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown).

Historical evidence:

- [Earlier direct-launch failure](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-28-direct-launch-failed.md): bare Cities2.exe startup failed platform initialization; no new bridge session. The helper's launch action was disabled afterward.
- [Earlier Steam auto-load attempt](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-28-0531-handoff.md): `Steam.exe -applaunch 949230 --startGame=...` still opened the launcher; after Play, no auto-load was observed. Argument forwarding was not established.

### Candidate approaches for a later authorized experiment

**A. Steam launch-option executable override.** Test an override that executes Cities2.exe while Steam supplies the launch context, preserving existing options. Candidate Steam Launch Options text:

```text
"C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2.exe" --noSplash --startGame=<METADATA_ASSET_GUID> %command%
```

This is a proposed experiment, **not a command executed or verified here**. `%command%` is a Steam launch-option placeholder, not a PowerShell expression. The exact client substitution and trailing arguments must be checked at runtime. The installed launcher configuration establishes the target executable; it does not establish this Steam override's behavior. First validate menu-only launching and expected mods, then add explicit save loading. Plain `-applaunch` alone has already failed to bypass this installation's launcher.

**B. Direct launch with a developer Steam app-ID hint.** Valve documents a development `steam_appid.txt` containing the app ID, with lookup affected by the working directory. It also suppresses the restart check. This is another possible experiment, not a fix proven on this game; it still needs the correct user, running Steam, and ownership. No hint file was created. [Valve documentation](https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown).

**C. UI automation of the existing launcher.** A future native UI controller could launch through Steam and click Play using observed UI state. That avoids having to solve an executable override, but adds a launcher-version/focus dependency. This session's computer-use surface offers browser automation and explicitly disables native app control, so such automation is not currently demonstrated here.

Do not use `--disablePdxSdk` or `--disableModding` as a substitute for fixing Steam context: they change the mod environment being tested and do not remove the Steam initialization path described above. No evidence was found in the inspected GameManager option parser for a launcher-bypass switch. That is a bounded search result, not a claim about every possible launcher option.

## 3. Loading a save: native support and the identity mistake

### Startup flow

`D/Game/Game.SceneFlow/GameManager.cs` provides:

| Entry | Source | Meaning |
| --- | --- | --- |
| `--startGame=<hash>` | 409-412 | Parse a Colossal.Hash128 asset ID. |
| `--continuelastsave` | 438-441 | Request the UI's continue-last-save flow. |
| `--noSplash` | 425-428 | Skip startup splash screens. |
| `runOnce.txt` | 445-454 | Read additional command-line text from the user-data directory and delete the file before parsing. |
| Boot dispatch | 639-660 | Benchmark takes precedence; then startGame, editor, continue-last-save, otherwise main menu. |
| `AutoLoad` | 1241-1263 | Resolve the ID and dispatch supported map/save asset types. |

Boot initializes platforms, caches assets, loads settings/mods/prefabs, and reaches WorldReady before startup dispatch (`GameManager.cs:591-638`). A `Boot completed` log is therefore not proof that the requested save has loaded.

`AutoLoad` accepts maps for new-game creation and `SaveGameData`/`SaveGameMetadata` for `Purpose.LoadGame`. It rejects other asset types, including a package wrapper. Failure falls back to the menu. Prefer metadata: the metadata load path preserves the session GUID and obtains the referenced data descriptor (`GameManager.cs:1182-1207`). Loading raw data instead explicitly logs that session GUID information is lost.

### Outer package GUID versus inner saved-game GUID

A `.cok` is a package. `PackageAsset.cs:7` declares that extension. `PackageAsset.cs:24-35` writes contained assets through a package writer; `ZipPackageWriter.cs:52-75` writes each member and its own `.cid` containing that member's asset GUID. The outer file also has a separate identity.

Global GUID lookup iterates databases (`AssetDatabase.cs:427-437`); local lookup is a dictionary lookup by that exact GUID (`AssetDatabase.cs:1632-1635`). It does not automatically unwrap a package into its metadata. `SaveGameMetadata.cs:53-67` separately associates metadata with its containing package for display-name purposes.

Read-only ZIP inspection of the existing **bridge test - rail merges, road intersections, highway slip lanes.cok** found:

| Identity | Observed value |
| --- | --- |
| Adjacent outer `.cok.cid` | `192da9684338e4c559215bbcd44b7f94` |
| Internal `.SaveGameMetadata.cid` | `cd611e887a35c51adcc38f2a7617bec7` |

The source-supported candidate for that observed file is:

```text
--startGame=cd611e887a35c51adcc38f2a7617bec7
```

This is **not runtime-tested**, and should be re-derived if the save changes. The file must be indexed by the game's asset database. A GUID string matching 32 hex digits establishes syntax only, not correct asset type or availability.

The bridge's `start-game.ps1` reads `SavePath + '.cid'` and passes it directly. For this package layout that is wrong. The older failed attempt used a different GUID; this session did not establish what that historical GUID referenced. Thus the package-ID defect is real, but it does not conclusively diagnose that particular attempt or prove the launcher forwarded its arguments.

A future extractor should open the selected package read-only, find exactly one metadata CID member, validate it and its associated metadata, and reject ambiguity. Do not guess from filename or choose whichever save was most recently modified. No extractor or helper change was made here.

### Continue-last-save and runOnce caveats

`AppBindings.cs:215-218` shows `LauncherContinueGame()` only triggers the frontend `checkContinueGamePrerequisites` event and immediately returns true. It is not an awaitable acknowledgement of successful loading. The last save comes from user-state settings (`AppBindings.cs:232-240`). It is less deterministic than an explicitly selected metadata identity and can involve frontend prerequisite handling.

`Game.PSI.PdxSdk/Launcher.cs:44-46,70-84` writes `continue_game.json` with display information: title, description, date and version. It is not a save-load RPC or the authoritative asset identity.

`runOnce.txt` could pass `--startGame` directly to the game even if a launcher drops command-line arguments. It neither bypasses the launcher nor survives consumption on a failed boot. This is a shared user-data file, so a future orchestrator must coordinate ownership and preserve any existing content. It was not created or modified here.

### Pause and readiness

`Game.Simulation/SimulationSystem.cs:203-218` sets speed after loading to 0 when `GameplaySettings.pausedAfterLoading` is enabled, otherwise 1. The settings default is false (`Game.Settings/GameplaySettings.cs:53`). Do not assume loading a previously paused save leaves simulation paused.

`GameManager.cs:1136-1137` invokes `onGameLoadingComplete` before assigning WorldReady. A readiness check inside that callback may need to defer to a later update. Verify the requested save identity, game mode, loading state, simulation pause, expected mod versions and a fresh city/session signal; process existence alone is insufficient.

## 4. Saving and reliable completion

### Native API

`GameManager.Save(string, SaveInfo, ILocalAssetDatabase, Texture)` and the async-preview overload are public (`GameManager.cs:945-950`). The implementation:

1. Announces save start and waits for a GPU frame.
2. Builds a transient database containing simulation data, prefab references, metadata, and optional preview.
3. Serializes through the game's SaveGameSystem (`SaveSimulationData`, line 934).
4. Checks storage quota, replaces an existing same-name package if present, writes a package, and updates last-save state.
5. Emits a completion event with `saveSuccess` and logs completed/failed.

Use a unique checkpoint name; the overwrite path deletes the old package before adding the replacement (`GameManager.cs:987-992`). This is not an atomic rollback guarantee. Copying an existing `.cok` file only copies an old checkpoint; it does not serialize current in-memory ECS state.

The ordinary menu uses `TaskManager.EnqueueTask("SaveLoadGame", ...)` to serialize its operations (`MenuUISystem.cs:784-786,886-900`). `GetSaveInfo(false)` supplies current city/options/mod metadata (`MenuUISystem.cs:856-883`). A future control bridge should call through the game update context and coordinate with other save/load operations, including autosave, rather than invoke Unity/ECS APIs from an arbitrary external worker thread.

### Existing bridge support and limits

`B/src/Workflow.cs:28-46` implements `save_checkpoint`: require a city and enabled controls, generate a unique name, capture a preview, obtain SaveInfo, and call GameManager.Save targeting `AssetDatabase.user`. It returns an operation ID; `WorkflowTick` later checks the task (`Workflow.cs:97-105`). This provides a useful existing starting point, not a verified durable-save contract.

**Important correctness gap:** `GameManager.cs:978-1006` initializes `saveSuccess=false`, performs writing only when quota permits, emits the actual result, and then returns `true` unconditionally on the normal non-exception path. Insufficient quota is a concrete counterexample: the task can complete successfully with true while no package was written. `WorkflowTick` currently maps that result to operation `complete`.

For unattended use, require all of:

- Task termination without exception, plus a matching end-of-save event with `success=true`.
- The unique output package exists, is nonempty, and can be opened; expected metadata/data members and IDs are present.
- Record output identity, size/hash, and game/mod fingerprints.
- During acceptance testing, reload that exact output and verify a deliberately observable state change survived.

Event and file checks improve confidence; a reload test establishes substantially more than file existence. Correlate by unique save name/session and allow only one lifecycle operation at a time. A timeout means outcome unknown; inspect before retrying or exiting.

### Authorization and shutdown

`B/src/Settings.cs:15` defaults `AllowControl=false`. `B/src/Mod.cs:60-71` resets it and changes citySession during preload. `SaveCheckpoint` calls `RequireControl`. The current bridge has no supported automated load/quit lifecycle command in the inspected dispatch/control list, and no scoped authorization persistence.

For future unattended runs, use an explicit test-session authorization tied to disposable save identity, duration and operations, honoring STOP and rejecting unexpected transitions; or operate the existing settings UI with authorized native/UI control. This is proposed design, not code supplied by this report. Do not silently remove the reset.

`GameManager.cs:794` waits for the named `SaveLoadGame` task group during graceful termination. The current bridge calls Save directly, so do not assume its save task participates in that menu task group. Finish and independently verify the bridge save before requesting exit. Termination saves settings but is not an implicit checkpoint of the city.

## 5. Alternative in-game control surfaces

### Existing frontend bindings

`MenuUISystem.cs:473-486` exposes menu triggers for continueGame, loadGame, saveGame, quicksave, quickload and exitToMainMenu. `AppBindings.cs:201` exposes exitApplication. A controller attached to the game's Coherent UI could invoke the existing flow without adding a Network Tools assembly change.

However, loadGame takes a structured argument (`MenuUISystem.cs:108-137`): saveId, cityName, options and gameMode, plus a separate dismiss boolean. It is not a one-string filepath operation. Save uses selected storage/cloud settings and may request overwrite confirmation. The normal load path also applies options and achievements handling (`MenuUISystem.cs:789-809`), which the simpler startup AutoLoad route does not explicitly duplicate.

The UI library defaults debuggerPort to 9444 and forces background execution when debugging is enabled (`D/Colossal.UI/Colossal.UI/UIManager.cs:93`; `UISystem.cs:99-113`). GameManager parses `--uiDeveloperMode`. The bridge setup record currently says launch options are not provisioned. No debugger endpoint, frontend attachment or trigger invocation was tested; frontend automation remains a candidate, not an available verified service.

### Shipped QA automation

`D/Colossal.TestFramework/Colossal.TestFramework/AutomationClientSystem.cs` contains a real automation client:

- `--automation=<IP[:port]>` or `--automation=local`.
- Default loopback port 27070.
- `--categoryFilter=...` for test scenarios.
- A DryDock client that connects outward to an automation server.
- Local-server startup referring to a developer `Tools/Automation/co` project and an internal build path.

`TestScenarioSystem.cs:70-79` initializes this client, and GameManager calls the test system during boot. GameManager also parses `--qaDeveloperMode`. These findings do **not** imply that port 27070 is a ready-made game RPC listener or that enabling QA mode gives an agent load/save commands. The inspected system queues registered test scenarios and reports results; provisioning/reimplementing its expected server would be a separate project. It is a less direct starting point than the known save API and an intentionally small bridge lifecycle extension.

No headless dedicated-server workflow was established. The inspected save path includes GPU-frame waits and screenshot handling; a desktop unattended harness is a much better-supported first target than `-batchmode -nographics`.

## 6. Proposed implementation and acceptance sequence

Keep lifecycle orchestration outside Network Tools. A small external controller owns launch arguments, logs, timeouts, build/deploy sequencing and output files. An in-game adapter owns Unity-thread commands and state acknowledgements. This is a control-plane/state-machine separation: issuing a request and observing its completion are separate states.

Suggested states:

```text
Stopped -> Starting -> BootReady -> Loading -> PausedReady
        -> TestOperation -> Saving -> SaveVerified -> Exiting -> Stopped
```

Each transition should have a timeout, explicit error state and request/session correlation. A timeout must not automatically trigger force-kill or repeat a mutation.

Later, when the other agent is finished and runtime testing is authorized:

1. Preserve current Steam launch settings and select a disposable save/playset. Confirm no lifecycle operation is owned by another controller.
2. Test launcher bypass to the main menu only, under the interactive Steam user's context. Confirm platform/mod readiness.
3. Derive the internal metadata GUID read-only; add `--startGame` and verify it reaches the game and resolves the intended asset. Preserve a redacted argument record; never copy launcher authentication tokens.
4. Verify pause-after-load behavior and the loaded save identity. Check mod/playset fingerprints rather than assuming direct launch retains them.
5. Exercise one uniquely named checkpoint, verifying the completion event and package contents. Reload it and compare observable city state.
6. Exercise graceful exit, then repeat the loop. Build/deploy only after process exit; the repository build replaces installed mod files.
7. Test failure handling for invalid asset ID, missing mods, unresolved dialog, failed save, and startup timeout on disposable data.

**Acceptance criterion:** repeated start -> exact save loaded and paused -> test -> independently verified checkpoint -> graceful exit, with zero manual clicks during the agreed run. None of these end-to-end transitions were exercised in this documentation session.

## 7. Work performed and remaining uncertainty

Performed: prerequisite/project reading; matching manifest/hash verification; native PE/strings inspection; installed launcher JSON inspection; source tracing across game, asset database, Steam integration, QA automation and bridge; read-only inspection of one existing save ZIP's metadata IDs; official Steamworks documentation check.

Not performed: new decompilation, native disassembly, executable patching, builds, installation/deployment, Steam configuration changes, UI/debugger attachment, live bridge requests, process launch/termination, save writes, or gameplay. No claim of live success is made. No Network Tools or bridge source file was changed.

The strongest next experiment is Steam-context menu-only launch, followed by explicit loading with the **internal metadata GUID**. The strongest prerequisite for trustworthy saving is fixing/working around the discrepancy between Save's task result and its actual completion status. Both can be addressed independently of Network Tools geometry code.
