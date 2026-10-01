# 2026-10-01 — Terrain interaction observation and sibling plugins

## User observation to retain

Dan reports that the off-ramp jank disappears when either:
- Curve strength moves the ramp farther away from the steep hillside next to the highway; or
- Pinning another ramp node produces a similar displacement away from that terrain.

He suspects terrain handling, specifically a distinction between the terrain surface
modified by the existing road and the terrain surface that would exist without it.
Retain this as an explicit future investigation, not a dismissed explanation.

The earlier centerline audit established a grade discontinuity in requested curves
and zero native/requested curve error. It did not sample terrain, generated road/lane
surfaces, retaining structures or the renderer. It therefore does not exclude a
terrain interaction contributing to the visible effect. Moving/pinning the path also
changes handle lengths and grades, so this observation alone does not isolate terrain.

Future controlled comparison: same centerline over different terrain; retain baseline
save and inspect the installed game's terrain sampling/deformation layers to determine
whether unmodified/pre-network terrain is available and how regeneration uses it.
Do not assume deleting a road reconstructs a unique original terrain surface. Couple
this with actual rendered geometry evidence, rather than equating centerline grades
with the entire visible road surface.

## Plugin installation

Used supported Codex CLI commands to refresh csmodding marketplace and add:
- coherent-gameface@csmodding 1.2.2
- unity-devtools@csmodding 1.2.0

Verified both via `codex plugin list`: installed, enabled. Existing cs2-modding
installation was not reinstalled. No hand-editing of agent configuration or plugin
manifests. Package installation is not a successful MCP/game connection test.

Remaining runtime setup:
- Gameface requires the game's UI debugging endpoint (normally --uiDeveloperMode,
  localhost:9444). Node 24 is already installed. A refreshed agent session is needed
  to expose newly installed plugin skills/tools.
- Unity server invokes `dotnet dnx UnityDevtools.Mcp --version 1.2.0 --yes` and requires
  SDK 10. Only SDK 8.0.425 is currently installed. CS2 setup record reports no debug
  player patch or debug launch options. Development-player/debugger preparation is
  still required before live use.

No game restart, game binary modification, runtime installation or live debugger
attachment was performed. No claim that either MCP server is operational yet.
