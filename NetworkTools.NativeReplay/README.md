# Offline native geometry research

This project is a local research harness, not a mod and not a production fix.
The branch starts at d03bdc5. Run from the research worktree root.

Supply the installed managed-assembly directory explicitly (normally obtained from
the toolchain owner's `CSII_MANAGEDPATH`). This standalone project does not import
the mod deployment targets and can compile while the game is open.

```powershell
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
python scripts/generate-native-world-stages.py --decompile <local-decompile-root>
dotnet build NetworkTools.NativeReplay "-p:ManagedPath=$managed"
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll artifacts/probe.json
python scripts/test-native-replay.py --dll NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --output artifacts/native-seam-tests
```

Create the output parent directory before the probe; evidence outputs must be new.
The test script creates its own new output directory. Local game DLLs copied into
ignored bin output must not be committed or distributed.

`--stages <fixture.json> <new-report.json>` runs actual private methods from the
hash-matched Game.dll. Schema 1 requires `gameSha256`, a nonempty `cases` array,
and per-case `id`, `stage`, `start`, `end`. Curves are four XYZ arrays. Supported:

| Stage | Additional required input |
|---|---|
| StraightenMiddleHeights | None |
| LimitMiddleHeights | maxSlope, width |
| CalculateCutOffset | startOffset, endOffset, width |
| Cut | startOffset, endOffset, cutOffset |

Missing inputs fail; the caller may not replace unknown captured state with zeros.
Current tests cover analytic controls and harness rejection behavior. A zero exit
from the feasibility probe only means its report was produced: inspect each probe's
status. NativeArray allocation is currently unavailable outside the Unity runtime.

The original-binary method path does not include ECS reconstruction, query selection,
full finishing jobs or terrain. The source-derived path below adds bounded stateful
stages. Initialize and flatten now have bounded diagnostic preview/permanent
differential evidence, described below; the complete pipeline remains unqualified.

## Stateful source replay (research increment)

`generate-native-world-stages.py` produces hash-gated, local-only copies of the
InitializeNodeGeometry/CalculateEdgeGeometry/FlattenNodeGeometry/FinishEdgeGeometry bodies and their EdgeIterator, replacing
storage containers and job entry scaffolding. Read its generated adaptation ledger
in obj/native-generated. This is adapted-source execution, distinct from direct
Game.dll invocation. Original math types/helpers remain referenced from the game.

```powershell
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --world-tests artifacts/world-tests.json
python scripts/test-native-world-capture.py --dll NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --output artifacts/world-contract-tests
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --world <fixture.json> <new-report.json>
```

World schema 2 requires gameSha256, loaded, nodes (index/version pairs), finishEdges, stages and
entities. Each entity has id and a components object. JSON null means explicitly
absent; an omitted component means not captured and throws if queried. Present tags
are true. WorldCapture.cs defines the strict, stage-specific field projections;
fields outside the projection are not evidence of a complete original component.
Reads/writes and height-map outputs are retained in reports.

FinishEdgeGeometry requires a preceding FlattenNodeGeometry invocation and consumes
its computed map; no recorded output is substituted. Supply explicit finishEdges
(empty when finishing is not requested). Reports include all four surface curves,
lengths and bounds. NetCompositionData now requires flags, width, state, heightMin
and heightMax; schema 1 is rejected instead of silently supplying missing fields.
The projected schema-2 world command does not carry terrain, so terrain-dependent
finishing fails there. The full raw pipeline below supplies captured terrain.
Other finishing branches execute the source body with
the original math types/helpers. This does not reproduce CalculateEdgeGeometry:
the initial surfaces remain explicit inputs to this bounded test.

The research target is validated reproduction and learning, including reproduction
of the preview/Apply discrepancy. A causal explanation is useful but is not an
additional prerequisite for successful differential reproduction.

Native ECS query selection and parallel scheduling are not simulated: nodes are an
explicit input, executed as homogeneous single-node chunks. Original EdgeIterator
logic determines their edge participants using captured buffer/tag data. Missing
state is never silently treated as absent. Entity.Null is a known nonexistent
identity (installed ECS rejects version zero), so optional lookups return false;
required access still fails. Other uncaptured identities still fail explicitly.

## Native scheduling capture differential

The opt-in Bridge research tracer records paired completed scheduling boundaries.
It adds synchronization and can affect timing/change versions. The first captured
preview/Apply run did **not** retain the historical surface discrepancy; no causal
attribution to instrumentation alone is established without an ordinary control.
All 11 preview and 6 permanent initialization/flatten node results, plus respective
7 and 4 height-map entries, match offline float32 output exactly. These are separate
stage tests supplied with recorded upstream inputs, not end-to-end prediction.

```powershell
python scripts/project-native-initialize.py --trace-status <completed-trace.json> --stage InitializeNodeGeometry --output <new-fixture-dir>
# Or --stage FlattenNodeGeometry. The name retains its initial-stage origin.
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --world <new-fixture-dir>/fixture.json <new-fixture-dir>/replay.json
python scripts/compare-native-initialize.py <new-fixture-dir> --output <new-fixture-dir>/differential.json
```

The projector keeps native outputs in a separate expected.json, preserves explicit
absence, and leaves unavailable cells unknown. Flatten starts with a fresh map,
as native OnUpdate does; its recorded downstream map is comparison-only. Exact
IEEE754 binary32 comparison avoids mistaking different JSON decimal renderings for
numeric errors. Tests reject a deliberately wrong output and missing identity.
Source captures, checksums, omissions and instrumented boundaries remain in the
local fixture manifests. No live regression has been retired.

## Captured edge-stage execution

```powershell
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --raw-edge <native-entry.json> <native-exit.json> <new-report.json>
python scripts/test-raw-edge-capture.py --dll NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --entry <native-entry.json> --exit <native-exit.json> --output <new-test-dir>
```

This runs the locally generated CalculateEdge body against captured component
fields and ordered buffers. Native exit data is comparison-only. The generator
also adapts the hash-pinned CalculateRoundaboutSize buffer helper; remaining value
helpers execute from installed assemblies. Two unused NetGeometryData archetype
handles are excluded explicitly and guarded against source reads. Missing other
fields/components fail. Schema, game MVID/hash, operation, pass, city and entity
membership are checked.

Exit 0 means exact compared output, 1 means a completed differential with mismatches,
2 means rejected/unexecutable input. Initial preview/permanent reports have 71/52
surface-coordinate differences up to about 1 mm. Strict exact comparison reports
these as mismatches; the user's subsequently approved spatial tolerance is applied
in the chained pipeline below. Seven contract/negative checks pass independently.
This path has no native scheduler and does not retire live tests.

## Computed pipeline with captured terrain

```powershell
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --pipeline <completed-trace.json> <new-report.json>
python scripts/test-native-pipeline.py --dll NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --trace <completed-trace.json> --output <new-test-dir>
```

The pipeline carries computed Initialize -> CalculateEdge -> Flatten -> Finish
state forward. Later captured node/edge geometry is prohibited from replacing
earlier computed outputs; the native height map is comparison-only. Initial input
assembly uses consistent, nonwritten component fields from the same bounded pass.
Terrain arrays are complete checksummed ushort grids with captured transforms;
hash-pinned native TerrainUtils methods perform sampling on managed array storage.
Reports export computed EdgeGeometry, original/temp identity mapping, terrain
sample count and stage errors. Explicit query membership is still supplied.

Dan approved millimetres and bounded accumulation into a few centimetres. Pipeline
acceptance uses 3 cm spatial scalar and curve-control distance bounds; nonspatial
markers and map/entity identities remain exact. Strict raw-edge mode still reports
all bitwise mismatches. Preview, permanent, a simpler single-edge linear control
and a held-out 6 m arch preview remain around 1 mm through finishing.
The controls exercise 144 terrain samples each. No observed amplification here is
evidence for these cases, not a guarantee for arbitrary geometry or new game patches.

Seven real-capture checks reject missing/corrupted data, wrong final output, wrong
map identities and even small errors in discrete sentinels. Poisoning recorded
intermediates leaves computed output unchanged. Historical 1.844445 m discrepancy,
ordinary scheduling equivalence, later junction processing and lanes remain outside
current qualification. See the 0420 terrain-chain session note for exact artifacts.

The arch variation changes authored control points by up to 15.23 m and finishes
within 0.949 mm of native control points. A half-strength Combined capture was
also retained, but its authored geometry matched the reference, so it is explicitly
not counted as nontrivial held-out geometry evidence.
