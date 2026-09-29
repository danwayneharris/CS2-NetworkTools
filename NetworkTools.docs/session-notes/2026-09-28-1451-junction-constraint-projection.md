# Junction correction: offline feasibility experiment

User authorized offline implementation and deployment with the game closed.
This checkpoint is research tooling, not a deployed correction.

## Captured failure reproduced

`scripts/investigate-junction-loss.py` replays the permanent before/after captures
under `captures/broken-preview-20260928`. All four existing directed connections
are obligations; no through-route is inferred. The lost direction is
55025:29 -> 80305:23. Curviness increases from 0.03003605 to 0.03369665,
crossing the source prefab limit 0.0314159244. Angle increases from 36.22284
to 39.84015 degrees while span decreases from 20.69952 to 20.22213 metres.
At fixed span this needs 2.79861 degrees less angle; at fixed angle it needs
1.46808 metres more span. These are alternative connection-space bounds,
not authored control-point corrections.

The other three required directions pass the curvature gate and remain present.
The report is saved as `constraint-loss-analysis.json` beside the captures.

## Joint projection prototype

`scripts/junction-angle-projection.py` intersects the exact allowed angular
intervals for every required directed connection. Both lane directions on the
selected arm rotate together, with reconstructed lane positions held fixed.
It chooses the smallest absolute rotation in the intersection; contradictory
requirements return no solution. It has no mainline/merging-branch classifier.
This supports symmetrical merges without assigning an arbitrary priority.

On the captured applied geometry, selected arm 80305:23 admits a rotation of
3.565516 degrees with 2% curvature headroom (experimental, not a calibrated
safety margin). All four required directions then satisfy the curvature rule
in this restricted model. This is evidence of connection-space feasibility,
not proof that an authored path can realize those positions and tangents.

## Source grounding and remaining gap

Local decompile manifest: game 1.6.2f1, ILSpy 9.1.0.7988.
`src/Game/Game.Net/LaneSystem.cs:6600` and `:6615` gate target groups using
source-prefab max curviness; individual lane creation adds another check.
`src/Game/Game.Net/GeometrySystem.cs:396` through the following Cut calls
reconstruct edge boundaries before lane connection positions are calculated.
Thus preserving the authored tangent does not preserve the generated lane angle.

Do not rotate an authored handle by the prototype's reported angle and claim
the connection fixed. The missing link is a forward evaluation of candidate
authored curves through fresh geometry reconstruction. Reusing the captured
EdgeGeometry after changing a centerline would give stale, invalid evidence.
The next implementation needs either a sufficiently faithful offline forward
model or native preview candidate evaluation, followed by connection validation.
No runtime gate, slider behavior, topology, elevations, or interior-junction
rejection changed at this checkpoint. No new build was deployed.

## Verification

- `uv run python scripts/test-junction-loss.py`: 2 tests pass.
- `uv run python scripts/test-junction-angle-projection.py`: 7 tests pass.
- Existing `test-rail-junction-constraints.py`: 4 tests pass in this investigation.

Tests cover the real captured loss, all-direction projection, conflicting
constraints, unselected failures, angular wraparound, unchanged feasible inputs,
arm-order independence, and invalid data. Native connectivity, authored curve
realizability, roads, multi-track arms, and repair intent are not validated here.
The pre-existing UI package-lock modification is left untouched.
