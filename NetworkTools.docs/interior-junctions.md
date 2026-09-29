# Interior junction smoothing: development prototype

The Debug prototype allows the selected path to pass through a junction. It fits
sections on either side while constraining junction attachment positions and
leaving unselected branch curves unchanged. Junctions are not ordinary player
split points. Release retains the previous interior-junction rejection.

## Geometry and candidate search

`PlanarJunctionTarget` delegates section fitting to `PlanarSplitTarget`, then
restores the original junction attachment positions and adjusts incident handles.
The current native search uses unit handle length and common planar rotations of
both selected branches: 0, +1, -1, ... +15, -15 degrees. This preserves their
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
120 observations. Missing or ambiguous data rejects rather than guessing. A retry
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
