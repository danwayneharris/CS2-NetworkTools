# Interior junction smoothing: development prototype

The Debug prototype allows the selected path to pass through a junction. It fits
sections on either side while constraining junction attachment positions and
leaving unselected branch curves unchanged. Junctions are not ordinary player
split points. Release retains the previous interior-junction rejection.

## Geometry and candidate search

`PlanarJunctionTarget` delegates section fitting to `PlanarSplitTarget`, then
restores the original junction attachment positions and adjusts incident handles.
The current native search uses unit handle length and common planar rotations of
both selected branches. A cold search tries 0, +1, -1, ... +15, -15 degrees.
For unchanged original component copies, ordered node identities and split flags,
it tries the last native-accepted angle first, then the remaining cold candidates
without duplicates. This warm start is only a candidate hint, never cached approval. This preserves their
relative angle while permitting their angles to an unselected branch to change.
It does not reduce the requested smoothing strength. The pure fitter also exposes
a separately tested 0.5?1.5 handle-length multiplier, currently unused by the search.

Junction centers remain fixed in the authored candidate. Native rebuilding can
move the actual node center slightly; this is measured independently after Apply.
A section consisting of one edge between junctions has little remaining freedom.

## Native acceptance

`RoadShapeToolSystem.InteriorJunctions.cs` captures junctions with 3?8 incident
edges and requires exact original/candidate directed car and track connector sets,
unchanged unselected curves, unique temporary edge mappings, and one shared
temporary junction. Three fresh matching observations are required for acceptance.
Apply rechecks the original connection set and latest accepted submission.

Only a resolved connection mismatch advances the bounded candidate search, after
three identical complete observations across all interior junctions, including
temporary node identities. Missing, ambiguous or stale observations reset that
streak. Missing/ambiguous data retains a 120-observation rejection timeout rather
than triggering a guessed repair. A retry
is latched until a new correlated submission arrives. The native check uses a
restricted connector representation; roundabouts and unsupported lane ownership
remain rejected. The independent regression runner additionally checks physical
lane composition mappings, direct-join reachability and permanent output.

## Evidence and limitations

Offline tests cover constrained attachment positions, relative angles, strength
sweeps, reversed traversal, adjacent ordinary splits, and invalid input rejection.
Debug compilation, postprocessing, UI build and deployment pass. Release/Burst and
human visual quality of this interior prototype remain unverified.

- Original fixed-handle experiment: four rail connections at strengths 0 and 0.1,
  but one lost at 0.25, 0.4 and 0.5. Apply blocked.
- Length-only search: all eleven candidates failed at 0.5. Apply blocked.
- Common rotation: +2 degrees at 0.5 and +3 at 0.8 preserved all four rail
  connections. Permanent Apply at 0.8 preserved lane mappings, topology, elevations
  and unselected curves; preview/permanent control-point differences were zero.
  The strict regression still reports FAIL because the rebuilt junction center
  moved 5.17 mm, exceeding its existing 1 mm fixed-center check. No tolerance was
  changed. This is not a full pass or a vehicle-routing test.

The installed LaneSystem derives connection positions and tangents from rebuilt
EdgeGeometry boundary curves. Preserving an authored centerline tangent alone
therefore does not preserve the generated connection-space angle or curviness.
Captured composition inputs explain the missing directional rail connection; see
the current sprint session notes and captures for numeric evidence.

Junction-as-player-split is a separate follow-up: prefer preserving relative
branch angle instead of imposing an ordinary split's shared tangent. See the
feature plan. Selecting a junction as a split is currently rejected explicitly.

The four-way interior road case passed at preview strengths 0.5 and 0.8 and
permanent Apply at 0.8: 16 directed junction connections, physical lane mappings,
topology, elevations, unselected curves and strict fixed-node checks all passed.
Preview/Apply curve differences were zero. Vehicle traversal and human visual
quality remain outside this automated result.

The highway path through both the on-merge and off-merge also passed preview at
0.5/0.8 and Apply at 0.8. Both junctions retained five directed lane connections
and unchanged physical lane mappings. Fixed-node, topology, elevation, unselected
curve and preview/Apply checks passed. Three selected edges changed; the constrained
single edge between the junctions remained unchanged.

## Responsiveness checkpoint (September 30)

The previously slow full-strength alternate rail branch is now a fixture in
`scripts/fixtures/toy-interior-full-strength.json`. Run
`scripts/benchmark-interior-preview.py` with that fixture, `--save-root`, a new
`--output` and explicit `--run` for checkpointed preview-only measurements.
The script verifies the baseline and directed lane connection sets, leaves permanent
geometry unchanged, and never Applies. Native elapsed logs exclude mailbox latency.

One live run measured 1336 ms for the initial 23-retry search, then 33-50 ms for
five warm slider changes (0.8, 0.5, 1.0, 0.99, 1.0). All six previews preserved
the original four rail-merge connections. A separate full-strength Apply retained
the directed connections and matched preview curves exactly; topology, elevations
and unselected edge curves passed. The strict result remains FAIL because a fixed
node moved 0.02988 m against the existing 0.001 m tolerance. No tolerance was loosened.

The initial search still exposes trial geometry and is perceptible. This change
is not predictive fitting or atomic publication of only validated preview geometry.
Warm starts favor a stable previously successful angle over re-minimizing angular
change at each strength; the accepted angle can consequently depend on slider
history. Native validation still runs at every revision. Visual approval, vehicle
traversal and broad native road/highway regression of this latency change remain
unverified. See [the experiment record](session-notes/2026-09-30-2200-interior-preview-latency.md).
