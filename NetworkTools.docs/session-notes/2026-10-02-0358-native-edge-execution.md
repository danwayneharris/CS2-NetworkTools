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
