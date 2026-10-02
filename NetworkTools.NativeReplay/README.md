# Offline native geometry research

This project is a local research harness, not a mod and not a production fix.
The branch starts at d03bdc5. Run from the research worktree root.

Supply the installed managed-assembly directory explicitly (normally obtained from
the toolchain owner's `CSII_MANAGEDPATH`). This standalone project does not import
the mod deployment targets and can compile while the game is open.

```powershell
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
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

Not yet supported: ECS reconstruction, participant/query selection, initial node
state, retention, flattening map generation, full finishing job, terrain and native
preview/permanent differential qualification. Original-method execution is a seam
for the larger simulation harness, not evidence that the whole pipeline matches.
