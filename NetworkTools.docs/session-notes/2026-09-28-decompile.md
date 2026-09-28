# Configurable full decompile setup

User authorized a full installed-game decompile, choosing the sibling cs2-decompile
folder and explicitly requesting a configurable bootstrap path. Added opt-in
-Decompile / -DecompilePath and standalone scripts/decompile-game.ps1. Pinned ILSpy
9.1.0.7988 to stay compatible with .NET 8 and previous targeted extracts. Export
covers all 173 managed-directory DLLs, with per-assembly hashes and local Git history.

The helper rejects nonempty output and paths inside the mod/game directories,
retains partial output on failure, and updates the skill setup record only after
successful export and commit. Existing setup keys are preserved. Refresh currently
uses a new empty sibling snapshot; destructive in-place refresh is deliberately not
implemented. README, AGENTS and BOOTSTRAP document the workflow and limitations.

Verification so far: PowerShell parsing passed; protected/nonempty-path rejection
checks passed; Game.dll exported 4,416 C# files and the expected IMod source exists.
Full export completion is recorded below. No game restart or mod deployment.

Full export completed successfully: 173 assemblies, 25,812 committed files,
3,807,948 lines in local snapshot 4e41b255. Installed assembly hashes remained
unchanged. Machine record C:/Users/danwa/.cs2-modding/setup.md now points to
C:/Users/danwa/dev/cs2-mods/cs2-decompile. Game version from the current log is
1.6.2f1 (767.21d1) [6300.26419]; Unity executable reports 2022.3.71f1.
The initial commit printed an excessive file list; the reusable helper now uses
quiet commit output. Source repository has no remote and remains local.
