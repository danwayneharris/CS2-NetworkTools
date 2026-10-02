# Native edge execution — 2026-10-02 03:58 PDT

Use the complete scheduling-boundary CalculateEdgeGeometry captures to extend the
offline native source boundary. Preserve the successful initialize/flatten result
in d377842. No further live mutation is needed for the initial execution attempt.
Keep generated source local and hash pinned; record adapter/compiler failures.

First generation/compile attempt fails on two namespace ambiguities introduced
by moving the original Game.Net-namespaced job: OutsideConnection and SubNet also
exist in Game.Prefabs. Need explicit aliases preserving the original resolution.
No native algorithm body was modified. Last successful executable remains in bin.

Aliases resolved those errors. The next compile exposed three calls to
NetCompositionHelpers.CalculateRoundaboutSize requiring a native DynamicBuffer.
Extend the local source closure with only that hash-pinned helper, adapting its
buffer type. Other helper calls remain actual installed binary implementations.
Compilation does not establish executable edge-stage capture support yet; it needs
the fuller component-field contract than the current initialize/flatten projection.

Expanded source closure compiles with zero warnings/errors. CalculateEdge's native
body and roundabout helper are generated locally; no hand-rewritten geometry math.
Next executable work is raw-to-stage field reconstruction and differential replay
against the already captured 8-edge preview and permanent scheduling lists. Do not
call this stage validated merely because it compiles.

## Raw captured execution

Added --raw-edge entry.json exit.json new-report.json. It decodes all captured
value fields for the job's component/buffer types, enforcing field counts and
presence. The two NetGeometryData archetype handles are intentionally outside the
read set; generator refuses source referencing them. Their capture descriptions
are required and the exclusions reported. No other unknown field gets a default.
Expected outputs never enter the ReplayWorld. Original captured entity order is
executed sequentially; native parallel scheduling is not reproduced.

First preview replay executes 8 edges, comparing all 1,728 scalar fields across
EdgeGeometry/StartNodeGeometry/EndNodeGeometry. It returns differential failure:
71 surface-coordinate mismatches, maximum approximately 1 mm. Permanent capture
executes 5 edges (correcting the earlier estimate of 8), 1,080 fields, 52 mismatches.
All reported differences are EdgeGeometry coordinates. Junction-end fields match,
but many are initialized to zero at this stage; this is not downstream junction
processing validation. Disabling .NET hardware intrinsics leaves the preview
difference list unchanged. These residual errors remain unexplained and unaccepted.

Evidence: artifacts/offline-research/native-edge-preview-01.json,
native-edge-preview-nointrinsics-01.json, native-edge-apply-01.json. Next discriminate
source adaptation from runtime arithmetic with binary/source pure-helper comparison
and native intermediate captures; do not hide these errors under a loose tolerance.

Build passes without warnings. Seven raw-capture checks pass, covering actual
execution/report discrimination, incomplete/version-changed capture, missing bound,
missing Curve field, missing Curve component, and deliberately incorrect output.
Rejected execution uses handled stderr/exit 2; mismatches use report/exit 1.
Reports in raw-edge-contract-01 explicitly retain the native mismatch count.
