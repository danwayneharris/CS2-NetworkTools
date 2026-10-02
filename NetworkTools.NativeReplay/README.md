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
stages. Neither yet has native preview/permanent differential qualification.

## Stateful source replay (research increment)

`generate-native-world-stages.py` produces hash-gated, local-only copies of the
InitializeNodeGeometry/FlattenNodeGeometry bodies and their EdgeIterator, replacing
storage containers and job entry scaffolding. Read its generated adaptation ledger
in obj/native-generated. This is adapted-source execution, distinct from direct
Game.dll invocation. Original math types/helpers remain referenced from the game.

```powershell
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --world-tests artifacts/world-tests.json
python scripts/test-native-world-capture.py --dll NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --output artifacts/world-contract-tests
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --world <fixture.json> <new-report.json>
```

World schema 1 requires gameSha256, loaded, nodes (index/version pairs), stages and
entities. Each entity has id and a components object. JSON null means explicitly
absent; an omitted component means not captured and throws if queried. Present tags
are true. WorldCapture.cs defines the strict, stage-specific field projections;
fields outside the projection are not evidence of a complete original component.
Reads/writes and height-map outputs are retained in reports.

Native ECS query selection and parallel scheduling are not simulated: nodes are an
explicit input, executed as homogeneous single-node chunks. Original EdgeIterator
logic determines their edge participants using captured buffer/tag data. Missing
state is never silently treated as absent. The actual anchor is not yet captured
at this stage boundary; synthetic tests do not retire native regression cases.
