# First bounded offline geometry harness

Research completed against CS2 1.6.2f1 on October 2, 2026. This is a simulation
research result, not a Combined Smooth Curve fix. The eight implicated geometry
stages execute offline from captured initial state and carry computed outputs
forward. The failing preview/Apply surface difference is reproduced.

## Evidence and commands

The immutable local fixture inventory is
[native-qualified-cohort.json](session-notes/2026-10-02-native-qualified-cohort.json).
It hashes 168 input/capture/evidence files, references the original source/assembly
inventory, and records native stage measurements. Bulk world state and proprietary
source remain outside Git. Paths are local; preserve their contents before moving
or cleaning the research worktrees/mailbox. This is not a distributable game bundle.

Run from `CS2-NetworkTools/Temp/offline-game-execution`:

```powershell
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')
python scripts/generate-native-world-stages.py --decompile C:/Users/danwa/dev/cs2-mods/cs2-decompile
dotnet build NetworkTools.NativeReplay "-p:ManagedPath=$managed"
$dll = 'NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll'
$burst = 'C:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/lib_burst_generated.dll'
python scripts/validate-native-cohort.py --manifest NetworkTools.docs/session-notes/2026-10-02-native-qualified-cohort.json --dll $dll --burst-binary $burst --output artifacts/offline-research/my-new-cohort
python scripts/test-native-pipeline.py --dll $dll --burst-binary $burst --trace artifacts/offline-research/map-apply-01/trace-completed.json --output artifacts/offline-research/my-new-contract-tests
```

These commands do not deploy or contact the game. Use new output directories.
The standalone project is independent of the mod bootstrap's deployment targets.
Initial source generation requires the hash-matched local decompile. Consult
`%USERPROFILE%/.cs2-modding/setup.md` when relocating the sources/install.

Latest evidence: `artifacts/offline-research/qualified-cohort-01/summary.json` and
`native-pipeline-contract-01/summary.json`. Five cases took 1.4–2.2 seconds each,
8.5 seconds total execution, plus manifest verification. This is a measured local
run, not a benchmark promise or a comparison against an entire live suite.
The [committed results](session-notes/2026-10-02-native-qualified-results.json)
preserve per-stage measurements, test outcomes, harness/adaptation hashes and the
verified final checkpoint. Installed and offline copied dependency hashes were
rechecked against the original assembly inventory at handoff.

| Captured case | Offline result through publication | Largest surface-control residual |
|---|---|---:|
| Burst anchor preview | Pass | 0.200 mm |
| Burst anchor permanent | All compared stage values exact | 0 |
| Simpler single-edge linear preview | All compared stage values exact | 0 |
| Held-out 6 m arch preview | Pass | 1.774 mm |
| Independent permanent capture with map storage | All compared stage values exact | 0 |

The held-out arch changed authored controls materially (about 15 m); it is not the
half-strength experiment whose geometry happened to remain unchanged. These
captures predate the lookup adaptation and were not edited to make it pass.

Mapped ramp 54262:1 has computed preview/permanent maximum Y difference
**1.844440 m**, versus native **1.844445 m**: 0.005 mm residual. The other four
mapped edges have zero native and predicted difference. Discrete identities,
membership, map keys, negative-length and middle markers remain exact.
Spatial acceptance is 3 cm, consistent with Dan's accepted bounded accumulation;
actual errors are reported rather than rounded up to the threshold. Unitless
vertex-sync parameters use 1e-5 with exact zero/one endpoints. No observed
amplification in this cohort establishes a universal stability guarantee.

Sixteen contract checks pass: real anchor execution, poisoned recorded
intermediates leaving the computation unchanged, missing/corrupted terrain,
missing composition buffers, wrong final/junction/scratch/published outputs,
wrong map identities, small discrete-marker errors, changed Burst binary,
wrong execution context/allocation, and the managed-only failing counterfactual.
CLI errors are caught and returned as exit 2 with stderr; numerical failure is
exit 1. Negative tests do not rely on unhandled .NET exceptions/modal dialogs.

## What was learned

The original Python flatten replay was useful but bounded: approximately 0.1 mm
agreement using native cut inputs, a 100-iteration cap, and no complete ECS,
retention, height-map publication or finishing execution. Its agreement was not
evidence for complete native equivalence. It is preserved as historical evidence.

A plain .NET host can invoke original Game.dll value-only methods, but native
array allocation fails with an ECall SecurityException. Consequently the harness
uses hash-pinned generated algorithm bodies with explicit managed storage, while
retaining original game/Mathematics value types and helpers. It does not pretend
that unmodified ECS ComponentLookup can be backed by a dictionary.

The important new dependency was **compiled lookup semantics**. Initialization,
edge cutting and flattening matched the permanent capture exactly. Native map
capture showed all four keys in well-formed buckets; managed native TryGetValue
found them at Finish entry and exit. A dictionary with the correct entries still
predicted the wrong finishing output.

Static inspection of installed `lib_burst_generated.dll` found endpoint-1 hash
instructions using OR where managed `int2` hashing uses addition. For ramp 54262,
the producer bucket is 2 and the native consumer bucket is 26. The native lookup
therefore misses a value that exists. This is a derived execution rule, not an
entity-specific correction or an imported recorded lookup result.

`BurstFinishLookup.cs` reads the PE without loading/executing it, pins its full SHA,
checks/decodes the same instructions in SSE2/SSE4/AVX/AVX2 variants, derives map
capacity from edge count, and interprets their bucket selection. Original exact
key equality means another bucket cannot produce a false matching key. Every
value still comes from computed Flatten. This is an instruction-derived bounded
adaptation, not execution of the entire native job or proof of a general compiler
defect. Why those instructions were emitted remains open.

Native SHA:
`C907D1A8E74368513756FE860853DAF0B032D59DC5E30A12EA236A6469CDC2EB`.
Export family `853da17896382c2aa97461e4792dcf61`; decoded RVAs/constants appear in
each report. Game/Mathematics binaries and source bodies are independently pinned.
See [map-storage investigation](session-notes/2026-10-02-0534-map-storage.md).

**Entity indices are behaviorally relevant in this build.** Preserve aliasing and
actual indices during execution; use original/temp mappings only for comparison.
Arbitrary identity renaming can change hash buckets even when topology is identical.

An ordinary untraced repeat had accepted preview and matching Apply surfaces, with
unchanged terrain and authored curves. Its temporary ramp was 52710:9 rather than
anchor 52711:3; its predicted lookup misses too. Permanent heights match the traced
case. This supports the need to capture identity and execution context, but is not
a full new pipeline differential or proof that instrumentation never matters.
Two heavily instrumented previews exceeded roughly the deployed 20-second preview
validation clock and were rejected; neither was applied. An ordinary recreation
was accepted before the next checkpointed Apply.

## Dependency and execution boundary

Source references below are in the pinned local `src/Game/Game.Net/GeometrySystem.cs`.
`OnUpdate` at line 3402 chooses all-versus-updated queries using loaded state,
obtains terrain, allocates scratch, and schedules dependencies. Query results and
ordered roots are explicit harness inputs; query discovery and scheduling are not
predicted. The harness checks entry/exit membership, operation/city/pass identities
and ordered junction iterations.

| Stage / source line | Principal reads and predicates | Computed writes / handoff |
|---|---|---|
| InitializeNodeGeometry / 25 | Node, PrefabRef, Temp/Original, Updated, ownership, connected edges, curves/compositions, prefab geometry; presence controls branches | NodeGeometry, including original/temp and retention state |
| AllocateBuffers / 1451 | Ordered edge query count | Height map capacity=2×edge count; intersection scratch length=edge count |
| CalculateEdgeGeometry / 186 | Curves, endpoint NodeGeometry, composition/prefab geometry, ordered incident participants via EdgeIterator, elevation/upgrade/roundabout and optional tags | Cut/retained EdgeGeometry and initial StartNodeGeometry/EndNodeGeometry |
| FlattenNodeGeometry / 1468 | Computed node/edge geometry, EdgeIterator participants, original/temp relationships, shared map | Updated NodeGeometry and computed endpoint float4 height-map values |
| FinishEdgeGeometry / 1688 | Computed edges/map, compiled lookup behavior, terrain grids/transforms, composition state/width/height bounds | Finished edge surfaces, lengths and bounds |
| CalculateNodeGeometry / 1831, iterations 0 then 1 | Finished edges, nodes, composition pieces, ordered participants, previous iteration outputs | Junction-end curves, synchronization parameters, markers and bounds |
| CalculateIntersectionGeometry / 2820 | Computed edge/junction geometry, compositions and participants | Every intersection scratch slot written |
| CopyNodeGeometry / 3074 | Computed intersection scratch and junction data | Published middle curves and bounds |
| UpdateNodeGeometry / 3104 | Computed connected geometry, terrain/composition inputs and component presence | Published node bounds |

This table is a guide, not a substitute for the exact generated job fields and
capture contract. `RawPipelineCapture.cs` assembles unchanged upstream cells across
stage entries and rejects inconsistent values. It forbids importing recorded
geometry after the corresponding earlier stage has computed it. The generated
EdgeIterator preserves original buffer order and participant filtering. Stage
reports expose reads/writes, exact scalar differences and maximum metric errors.

Capture contract: native job schema 1; explicit `presence: present|absent`; omitted
or unavailable data means unknown and fails on required reads. Preserve complete
field values, ordered buffers, entity index/version, original aliases, roots/chunk
membership, loaded state, operation/pass/city identities, Burst context and source
versions. Projected `--world` schema 2 is a narrower legacy stage interface.

Terrain storage contract `TerrainHeightData-ushort-arrays-v1` retains checksummed
little-endian ushort arrays, resolution, transforms and backdrop state. Full arrays
are input here, sampled by adapted native TerrainUtils; unchanged terrain samples
never implied terrain was irrelevant. This boundary does not compute terrain writes.
Map lookup-storage v1 retains capacity/mask, occupied slots/next links/buckets and
managed lookup observations for auditing; these do not replace computed map values.

Capture occurs around completed original dependency/job handles before native
containers are disposed. Completion barriers and read handles can change timing
or versions. It is coherent at those boundaries, not a claim of atomic capture
across arbitrary systems. Flatten's write-only map wrapper is an explicitly known
capture omission; its values are observed at Finish. Uninitialized intersection
scratch may contain nonfinite data; its entry bytes are excluded because all slots
are overwritten before use. Captured exit scratch is comparison-only.

Adaptation ledger: generated `obj/native-generated/adaptations.json`, source
generator, `ReplayStorage.cs`, `TerrainCapture.cs` and `BurstFinishLookup.cs`.
Storage/job scaffolding changes, removed unused sorting, original helper calls,
terrain scale constant and native lookup interpretation are explicit. Generated
proprietary bodies remain ignored. This is not a general Unity mock framework.

## Which live tests can move offline

Use this cohort as the first gate for changes to these harness adapters and for
repeated geometry investigations within its recorded dependency/identity boundary.
Rerun all native stages offline instead of reopening the same captures in-game.
Use direct value tests, missing-data tests and poisoned-output checks before any
live experiment. Do not repeatedly collect known fixtures just to verify reports.

No existing product live suite was deleted or disabled. Retain live checks for
new operations/input construction, query membership, job scheduling, preview
acceptance and Apply transactions, lanes/connectivity, rendered meshes, walls,
retaining/quay behavior, terrain deformation, arbitrary prefabs/topology, mod
interactions and game patches. Surfaces/terrain/walls can be inputs as well as
outputs; adding an output requires following its actual writers and dependencies.
This result validates bounded captured transitions, not arbitrary newly changed
feature geometry or the whole game. The capability registry states that distinction.

Recommended ordering: static/pure tests → this offline cohort → unsupported-boundary
assessment → one targeted live capture/ordinary control for new behavior. Capture
complete coherent inputs and intermediate/final outputs once, then perform the
expensive analysis/iteration offline. Do not increase captures without a dependency
question; instrumentation timeouts are themselves measured behavior.

## Expansion and patch refresh

When feature work encounters an unknown read, new branch or unexplained residual,
pause that feature's equivalence claim and open a separate research fixture. Keep
the old validated capability usable. Identify the first divergent stage, inspect
its source and compiled context, expand capture by dependency closure, and retain
the failure. Implement only the necessary adapter/stage; never fill unknown data
with zero or native output. Validate anchor, simpler control and a held-out changed
case; add missing-input and deliberately wrong-output tests. Record numerical
growth and exact branch/topology differences, then version the capability. Feature
work resumes with a documented supported boundary rather than an implicit assumption.

For a game patch:

1. Freeze old captures, saves, manifests, reports and local source before updating.
   Never overwrite historical fixtures with newly exported results.
2. Compare Game, Mathematics, Collections, Entities, Colossal helpers and native
   Burst hashes/MVIDs, Unity/runtime versions, decompile manifest, stage/helper
   source hashes, schemas and captured prefab/composition/terrain asset data.
3. Invalidate affected capabilities. Review source and native instruction changes;
   do not merely replace hash constants to bypass rejection. Recheck all eight
   scheduler signatures, field presence and allocation/lookup assumptions.
4. Create a new patch cohort on disposable fixture copies. Record migration/save
   identity, actual loaded mod hashes and execution flags. Capture both outcomes,
   intermediate decisions, a simple control, changed geometry and failure/boundary
   cases; include an ordinary scheduling comparison.
5. Differentially revalidate from initial inputs, run contract/negative tests, and
   compare against the old cohort to classify intentional game changes versus
   harness/capture errors. Publish a new capability version only for passing scope.

A patch refresh is deliberately a research task independent of any one feature.
Global automation remains future work; the hashes, immutable cohorts and fail-closed
entry points are implemented foundations for it.

## Completion and handoff

The initial bounded goal meets its acceptance criteria: executable stages,
reproduced failing anchor, simpler control, nontrivial held-out variation,
meaningful negative tests and explicit scope. No production discrepancy fix,
lane simulation or general ECS runtime is claimed. Original feature work remains
paused. Research lives on NT `dan/offline-game-execution` and Bridge
`dan/offline-geometry-capture`; no push/publication was performed.
