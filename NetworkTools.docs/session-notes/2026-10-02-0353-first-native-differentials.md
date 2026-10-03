# First native stage differentials — 2026-10-02 03:53 PDT

Research continues on isolated NT branch; production feature remains paused.
Bridge research commit 6f1139a deployed after a verified unique checkpoint and
graceful close. Local Bridge `artifacts/schedule-live` records checkpoint, hashes,
baseline launch, fresh sessions, commands, paired captures and post-Apply snapshots.
NT runtime remains historical 04792dd, not this research branch.

## Observations and limitations

Preview capture produced 16 files, Apply 28, with no tracer fault. Both patches
were explicitly removed after capture. Initialize, CalculateEdge and CalculateNode
captures report complete. Flatten lacks only the ParallelWriter representation;
its produced map is observable at the immediate Finish entry. Finish explicitly
lacks terrain NativeArray<ushort>. No absent terrain is inferred.

Both native initialize and flatten stages now reproduce captured diagnostic
preview/permanent inputs with exact float32 outputs:

| Stage | Preview | Permanent |
|---|---|---|
| Initialize | 11 nodes, 44 scalar fields exact | 6 nodes, 24 fields exact |
| Flatten node state | 11 nodes, 44 fields exact | 6 nodes, 24 fields exact |
| Flatten height map | 7 keys, 28 values exact | 4 keys, 16 values exact |

Each stage test starts from its own recorded upstream inputs. Computation is real
adapted native source; these results are not a chained end-to-end prediction.
The comparator rejects deliberately wrong output and missing identity. Float32
comparison resolves tiny differences between Newtonsoft and System.Text.Json's
decimal renderings without introducing a metric tolerance.

**This instrumented run does not reproduce the historical discrepancy.** All four
central incident authored curves and surfaces match preview/Apply exactly in
Bridge `anchor-surface-differential.json`, including original ramp 54262:1.
The earlier ~1.844445 m discrepancy remains historical valid evidence. Completion
barriers/read handles, managed launch, and reproduction path must be controlled
before attributing this result to any one cause. Initial commentary suggesting
synchronization caused the change was stronger than this evidence supports.

## What failed and changed

First initialization replay rejected uncaptured Entity.Null/ConnectedEdge[].
Installed Unity.Entities BufferLookup.cs:50-59 returns false when Exists is false;
EntityComponentStore.cs:660-673 rejects version zero. Implemented only Entity.Null
as known nonexistent, preserving unknown failures for other uncaptured identities.
Required null access still throws. Synthetic source suite increased to 40 passing
assertions. No game source was copied into tracked files.

Projection is explicit and hash/MVID gated. Unknown cells stay omitted/unknown;
native outputs go only into expected.json. README documents executable commands.
Local NT fixture/report directories: native-initialize-preview-01,
native-initialize-apply-01, native-flatten-preview-01, native-flatten-apply-01
under artifacts/offline-research. Source capture files and hashes are recorded in
each expected.json. Capture data stays local; registry now distinguishes this
bounded evidence from synthetic finishing and the unreproduced failing anchor.

Next: exploit the already complete CalculateEdge captures to execute its native
source offline; capture terrain by its actual NativeArray/transform dependencies;
obtain ordinary-execution control and held-out data. No live checks are retired.
