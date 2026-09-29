# Working in this repository

This is a fork of Luca Rager's CS2 Network Tools. Keep changes incremental and
explain the relevant systems concepts: the maintainer is an experienced audio/DSP
engineer learning CS2 modding. Ground implementation claims in the actual source
or installed assemblies, with file and line references when useful.

## Read first

At the start of work, read:

1. [README.md](README.md) — fork motivation and broad roadmap.
2. [BOOTSTRAP.md](BOOTSTRAP.md) — Windows setup, commands, deployment, and smoke tests.
3. [Build system](NetworkTools.docs/build-system.md) — build stages and known issues.
4. [System architecture](NetworkTools.docs/system-architecture.md) — runtime structure
   and source-reading path.

For Smooth Curve work, also read [the feature plan](NetworkTools.docs/smooth-curve-plan.md).
Keep proposed behavior in that plan and implemented behavior in the architecture
guide. Update the relevant docs when behavior or build requirements change.
The current source and project files take precedence over stale descriptive text.

## Build and verification

Run from the repository root in PowerShell:

```powershell
.\scripts\bootstrap.ps1                         # Check prerequisites
.\scripts\bootstrap.ps1 -Build                  # Debug build and local deployment
.\scripts\bootstrap.ps1 -Build -Configuration Release
.\scripts\bootstrap.ps1 -Test                   # Build and invoke existing tests
```

- Close CS2 before building. A build replaces the local development mod directory;
  it is not just compilation. Do not terminate the user's game without permission.
- Run as the Windows user who installed the toolchain. MSBuild reads persistent
  User-scoped `CSII_*` settings; the UI reads process settings. The bootstrap
  refreshes the process environment. A sandbox account may have different settings.
  Do not hardcode one developer's paths to work around this.
- Use the bootstrap's framework-reference overrides and explicit NuGet source.
  See the build guide before changing reference resolution to fix compiler errors.
- The mod targets .NET Framework 4.8 with C# 11; the generator targets .NET 8.
  The installed postprocessor may require a separate runtime, checked by the script.
- Release enables Burst. Debug success alone does not verify Release compatibility.
- Postprocessing, UI generation/build, and deployment must succeed for a full build.
  Report automated tests and in-game verification separately. Do not infer test
  success from the existence of a test project or claim game behavior from compilation.
- The bootstrap's `-Install` option installs missing prerequisites. It does not
  change Windows security policies. Diagnose postprocessor signing blocks using
  the build guide; do not automate disabling Smart App Control.

As of Sept 26, 2026, Debug builds and deployment passed, and the maintainer confirmed
the modified tooltip and a successful Connect operation in-game. Automated tests,
Release/Burst, and other tool behavior remain unverified in this fork. This is a
baseline record, not evidence that subsequent changes work.

## Source and working-tree care

- Inspect the working tree before editing. Preserve user changes and unrelated files;
  `prompt-scratch/`, if present, contains user-owned notes.
- `NetworkTools.Mod/Common` is a pinned Git submodule. Do not advance its revision or
  edit shared code incidentally while fixing the parent project.
- The existing npm prebuild runs `npm install` and can rewrite `package-lock.json`.
  Review that diff separately; do not bundle accidental dependency changes or discard
  pre-existing user edits. React dependencies are project-local, not a global install.
- `UI/src/generated/parameters.generated.ts` under the mod is generated and ignored.
  Change the C# parameter declarations/code generator rather than hand-editing output.
- Follow surrounding code style: four-space C# indentation, same-line opening braces,
  `m_` private fields, and existing `NT_` system/component naming. Preserve partial-class
  organization. [Existing style notes](NetworkTools.Mod/.agents/instructions.md) provide
  examples, but their C# 9 version and repository map are outdated.
- Prefer focused changes and meaningful verification. Do not publish to Paradox Mods
  as part of a local build or test task.

## Geometry work

- Full local source setup is optional via `bootstrap.ps1 -Decompile -DecompilePath`.
  Read `%USERPROFILE%/.cs2-modding/setup.md` to locate it and inspect its
  `source-manifest.json` before relying on version-sensitive code. Keep decompiled
  source in that separate local repository; do not add it to this repository.

- Smooth Curve has an integration prototype. Consult the geometry guide and
  session notes for current behavior, known limitations, and verification results.
- The agreed prototype moves existing nodes horizontally, preserves node elevations
  and topology, reuses selection/preview/Apply, and isolates out-of-game-testable math.
  Node removal, resegmentation, and obstacle/terrain routing are later explorations.
- Account for reversed edge traversal, intersection-center versus Bézier-endpoint
  offsets, and effects on connections outside the selection.
- Preview and Apply use different game integration paths; verify both. Preserve
  Unity job dependencies and native-container lifetimes, and consider Burst constraints
  when choosing the geometry module's API and implementation.
- The tool's built-in Anarchy option disables validation. Test geometry explicitly;
  disabled validation does not establish correct curves or external-mod compatibility.

## General Operating Philosophy

- At the beginning of each session, create a new .md doc in NetworkTools.docs/session-notes/ that includes the \ date and time in the filename, and for each small incremental change during that session, document what the change was, whether it did or didn't work, or what was learned from testing it. Then commit that change to git along with the updated doc EVEN IF THE CHANGE DIDN'T WORK.  If the change was bad, manually revert (not git revert) and update the doc accordingly.  This is to ensure that all lessons are learned and we have a clear picture of our potentially messy journey.

## Bridge and agent-plugin setup

For junction diagnostics, read BOOTSTRAP.md's local bridge and coding-agent plugin
sections and the sibling bridge's AGENTS.md, INSTALL.md and docs/JUNCTION-SNAPSHOTS.md.
The bridge is a diagnostic-workflow dependency, not a NetworkTools assembly dependency.
Use scripts/check-bridge.ps1 or bootstrap.ps1 -CheckBridge for read-only file/hash
checks. Never equate installed files with live query support. Keep bridge code/local
commits in its own repository; origin is danwayneharris/cities2-agent-bridge-ndc and upstream is FTPAiYT/cities2-agent-bridge-ndc. Changes go through PRs to our fork. The cs2-modding
plugin provides source-reading guidance, not game access or mutation authorization.
