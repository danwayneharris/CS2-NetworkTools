# F04: arrival-dependent path selection

The current search settles by node although the next edge cost depends on the incoming prefab and immediate backtracking depends on the incoming edge. A cheaper arrival can therefore hide a cheaper complete route. This session changes only search bookkeeping to `(node, incoming edge)` and reconstructs parents with that same key, retaining the native heap, strict cost comparison, 9.9 prefab-change penalty, optional curve/prefab behavior and immediate-backtracking exclusion.

Validation: standalone graph harness links the actual production partial class with test doubles for ECS and collections. It verifies the algorithm but cannot qualify Unity allocation/lifetime or native heap implementation.

## Results

- `dotnet run --project NetworkTools.PathSelection.Tests`: **passed**, 958 assertions,
  including 200 deterministic graph/direction comparisons with an independent
  exhaustive simple-path cost oracle.
- Rebuilt the same harness against a captured pre-fix production file using its
  `PathSearchSource` MSBuild override: **failed as expected** at the F04
  discarded-arrival counterexample. This demonstrates that the regression
  distinguishes the actual old behavior; the corrected source was rebuilt and
  passed again afterward.
- Counterexample: direct arrival at J on prefab A costs 10, parallel arrival on
  prefab B costs 11, outgoing B edge costs 1. Old settlement returns 20.9; corrected
  arrival settlement returns 12. Both stored edge orientations and reversed
  selection order are covered. Parent reconstruction is checked edge by edge.
- Equal-cost routes must remain optimal, but native heap tie ordering is not a
  promised unique route. No new tie-breaker was added.
- Disconnected cyclic graphs terminate with empty outputs; same-node selection
  retains empty success; missing prefab retains the original no-penalty rule.

The current source evidence is
`NetworkTools.Mod/Systems/Tools/PathSelection/PathSelectionToolSystem.PathFinding.cs`:
`PathState`, arrival-keyed `visited`/`parentMap`, and state-keyed reconstruction.
A node can now settle once per incoming edge (at most two directed arrivals per
ordinary undirected edge plus the start sentinel), instead of only once per node.
This is the necessary extra state for the existing cost/backtracking rule, but
increases synchronous search work. Large-city responsiveness/profiling and native
allocation/heap behavior remain unverified; no speculative optimization was made.
The parent sprint must run the full mod compile and record native selection checks.

The exhaustive oracle covers known-prefab graphs with nonnegative lengths; it
is not an oracle for malformed network components, native container behavior,
rendered selection, or traffic routing. Existing diagnostic logging is retained.
