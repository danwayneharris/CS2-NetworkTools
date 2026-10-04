# Offline native geometry research

This standalone local harness executes eight bounded GeometrySystem stages and
reproduces the captured ramp preview/Apply surface discrepancy. It is not a mod,
a feature fix, a general Unity host or a replay of recorded Bridge responses.

Start with the [research results and reproducible commands](../NetworkTools.docs/offline-game-execution-results.md).
The [capability registry](capabilities.json) distinguishes validated captured
transitions from unsupported game systems. Historical progress and failures remain
in the timestamped session notes; current qualification supersedes earlier status.

## Build and run

Run from the research worktree root. Use the local decompile location in
`%USERPROFILE%/.cs2-modding/setup.md`. No mod deployment occurs.

```powershell
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
python scripts/generate-native-world-stages.py --decompile <local-decompile-root>
dotnet build NetworkTools.NativeReplay "-p:ManagedPath=$managed"
```

Generated game bodies live only in ignored `obj/native-generated`, with
`adaptations.json`. Local DLLs copied to ignored bin output must not be distributed.
The generator pins source hashes. The CLI pins Game.dll; native mode additionally
pins Mathematics and the installed native Burst binary and decodes its lookup
instructions without executing the native DLL.

| CLI mode (arguments follow the mode) | Boundary |
|---|---|
| `<new-report>` | Original binary/runtime feasibility probe |
| `--course-height-tests <new-report>` | Pinned CourseSplit height sampling methods with explicit synthetic buffers; excludes height-buffer construction, ECS splitting and classification |
| `--stages <fixture> <new-report>` | Original private value-only methods |
| `--world-tests <new-report>` | Synthetic storage/stage assertions |
| `--world <fixture> <new-report>` | Projected schema-2 initialization/flatten/finishing, no terrain |
| `--raw-edge <entry> <exit> <new-report>` | Captured CalculateEdge stage, exact comparison |
| `--pipeline <trace> <new-report>` | Four computed source-driven stages including terrain |
| `--pipeline-junction <trace> <new-report>` | Adds CalculateNode iterations 0/1 |
| `--pipeline-full <trace> <new-report>` | Adds intersection, copy and node publication; managed lookup semantics |
| `--pipeline-native <trace> <BurstDLL> <new-report>` | All eight stages with pinned instruction-derived Finish lookup |
| `--pipeline-prepared <trace> <BurstDLL> <new-report>` | Offline correction experiment: prefill computed height-map values, then run native lookup model; old observed mismatches remain failures |

Use `scripts/validate-native-cohort.py` for the five-case native qualification and
paired discrepancy check. Use `scripts/test-native-pipeline.py --burst-binary ...`
for the 18 contract/negative checks. Full commands and local manifests are in the
research report. The source-only full pipeline intentionally fails the recovered
Burst permanent case; do not loosen tolerance to make that counterfactual pass.

All output paths must be new. Exit 0 accepts the bounded differential, 1 records
an executed mismatch, and 2 rejects invalid/unexecutable input. The feasibility
probe's exit alone is not evidence that every probed runtime capability works.

## Contracts and limits

Native captures use job schema 1; explicit component presence differs from absence
and unknown. Unknown required reads fail. Entity aliases and buffer order are
preserved. Original entity indices affect native bucket behavior and cannot be
freely renamed. Later native geometry, map values or scratch never replace earlier
computed outputs. Query membership and execution context are explicit inputs.

Terrain uses complete checksummed ushort grids with captured transforms. Native
sampling is source-adapted; terrain deformation is not simulated. Spatial scalars
and curve controls use a 3 cm acceptance bound with measured errors reported;
discrete markers/identities remain exact, and sync parameters use a separate bound.

The harness does not qualify scheduling, UI acceptance, Apply transactions, lanes,
rendering, retaining/quay walls, arbitrary geometry/prefabs, or future game patches.
No product live test has been disabled. Use offline replay first, then targeted
live checks for unsupported behavior. The research report defines the expansion
and global patch-refresh process.


The offline preparation experiment is described in
[finishing preparation](../NetworkTools.docs/session-notes/2026-10-03-0100-finishing-preparation.md).
`scripts/test-finish-height-preparation.py --manifest <cohort.json> --dll <harness.dll>
--burst-binary <installed-Burst.dll> --output <new-directory>` compares it with managed
finishing across the captured cohort. This reads local proprietary inputs and writes
reports; it does not deploy, contact the game, or install a persistent correction.
A prepared replay can correctly exit 1 because old native observations contain the
fault: the separate cohort comparison requires exact corrected/managed agreement,
original native reproduction and a discriminating counterexample.

### Disabled runtime candidate (October 3)

A Debug-only, explicitly opt-in finishing compatibility candidate now lives in
`NetworkTools.Mod/Compatibility`; see
[runtime candidate and gates](../NetworkTools.docs/session-notes/2026-10-03-0115-finishing-runtime-candidate.md).
NativeReplay links the same height assignment helper. `--finish-compatibility-tests
<compiled-NT.dll> <managed-directory> <new-report>` verifies the actual transpiler
against installed call operands, fingerprints and default-off activation. This is
an offline test; it does not prove runtime scheduling, performance or persistence.
