# Junction diagnostics

The first checkpoint is observational: smoothing constraints are unchanged. Debug
builds emit `[NetworkTools.SmoothTrace]` JSON records to `Player.log`. Schema 2 adds
a process/domain session identifier, exact solver/adapter failure and index,
selected waypoint entities, incident edge IDs, and stored edge start/end node IDs.
The `nodes` and `edges` arrays retain cached path order. An index refers to a node
or edge according to the failure name; -1 means no individual item is identified.
Incident edges are the raw buffer, not filtered for deleted, temporary, or owned
entities. A missing buffer is null. `pinned` includes selection boundaries.

These records describe input and proposed output before the game rebuilds the
network. They do not prove rendered geometry, lane connectivity, or final Apply
results. Selections over 128 nodes omit details. Jobs skipped before transformation
do not produce a SmoothTrace. Logging is Debug-only and can grow quickly.

## Offline replay

From the repository root:

```powershell
dotnet run --project NetworkTools.Geometry.Tests --configuration Release
dotnet run --project NetworkTools.Geometry.Tests --configuration Release -- --trace-log NetworkTools.Geometry.Tests/Fixtures/junction-rejections.jsonl
```

The second command also accepts a full `Player.log`. It invokes the production
planar solver on captured inputs, handles path-count mismatches before invoking
unsafe code, prints rejection reasons, and exits nonzero for acceptance differences.
It does not reproduce Unity adapter checks or game validation. Null/nonfinite
positions and truncated JSON records are unsupported and fail parsing rather than
being silently treated as usable geometry. Historical captures may legitimately
differ from newer solver versions; investigate rather than relabeling results.

Captured examples from Sept 26:

- 1333: four nodes but two edges; rejected even at zero strength.
- 1347: interior pinned node at index 2; rejected above zero.
- 1357: zero-strength selection accepted despite the interior pin.

All 1,212 records in the preserved junction log reproduced recorded acceptance.
The three small fixtures retain only SmoothTrace records, not the rest of the log.

## Bridge review

Reviewed [agent bridge source](https://github.com/FTPAiYT/cities2-agent-bridge-ndc/tree/8c78da5e1ff4f160a032b4bdeb1d7f112af4ff64)
without installing or executing it. `src/Mod.cs` dispatches requests from a local
mailbox. Read requests can pause the simulation if control is enabled; leave control
disabled and pause manually for a read-only investigation. `RequireCity` rejects
requests without a loaded game world.

`src/NetworkCommands.cs`, `NetworkEdges`, exposes curve controls and edge endpoint
entities, excluding Temp, Deleted and Owner entities. Its approximate spatial query
is capped at 2,048 edges: check `possiblyTruncated`, use a small radius, and do not
treat it as a complete city export. `NetworkPath` reports physical adjacency, not
vehicle routing. These queries could compare the game's rebuilt network against
our proposed geometry, which offline solver replay cannot establish.

The v0.5.0-community release manifest's Game.dll SHA-256 matches the installed copy:
`AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`.
This establishes the installer's version prerequisite only, not runtime compatibility
or a security audit. No bridge code was copied into Network Tools.

## Next game session

Close the game before deploying a Debug build. On a test save, capture a short
selection with a junction at the boundary, then one in the middle; try 0, 0.001,
and 1 strength. Preserve Player.log before another launch. Compare selectedNodes,
path order, incident edges, and failure. Investigate missing path edges separately
from deciding how smoothing should treat junctions.

Bridge installation and live queries remain a separate next step. Start with
control disabled and a manually paused test save, then verify the read-only queries
before building any automation around them.
