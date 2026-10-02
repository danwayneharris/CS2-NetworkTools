# Native edge execution — 2026-10-02 03:58 PDT

Use the complete scheduling-boundary CalculateEdgeGeometry captures to extend the
offline native source boundary. Preserve the successful initialize/flatten result
in d377842. No further live mutation is needed for the initial execution attempt.
Keep generated source local and hash pinned; record adapter/compiler failures.

First generation/compile attempt fails on two namespace ambiguities introduced
by moving the original Game.Net-namespaced job: OutsideConnection and SubNet also
exist in Game.Prefabs. Need explicit aliases preserving the original resolution.
No native algorithm body was modified. Last successful executable remains in bin.
