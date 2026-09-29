# Interior junction smoothing: development prototype

The selected path can pass through a junction, but the junction is not treated as
an ordinary movable path node or an ordinary split. The current experimental
geometry keeps its center and the selected branches' incident endpoints and
adjacent handles fixed. It smooths the sections between junctions. Unselected
branches are not candidates for modification.

`PlanarJunctionTarget` delegates section fitting to `PlanarSplitTarget`, then
restores the constrained junction controls. A one-edge section bounded by two
junctions can have no remaining free controls. This limitation is reported as a
consequence of the current constraints, not a general inability to smooth it.

The Debug integration captures every interior junction with 3–8 incident edges.
`RoadShapeToolSystem.InteriorJunctions.cs` requires exact original and candidate
directed car/track connector sets, unchanged unselected curve controls, unique
temporary edge mappings and one shared temporary node. It waits for three matching
observations of the current fresh submission. Changed original inputs invalidate
the existing preview correlation; Apply also rechecks original connection sets.
Missing/ambiguous results are rejected after a bounded wait. Unsupported connector
ownership is not interpreted as an empty connection set.

The native check is currently a restricted connector representation, not the full
offline semantic lane graph oracle. Roundabouts and other unsupported ownership
patterns remain rejected. The independent regression runner additionally checks
physical lane composition mappings, direct-join reachability and permanent output.
Release does not enable this prototype and retains interior-junction rejection.

Offline tests pass for constrained controls, strength sweep, reversed selection,
ordinary split points in adjacent sections and invalid input rejection. Debug
compilation, postprocessing and deployment passed. Native validation is pending;
build success does not prove game behavior. The first captured interior rail case
preserved four directed track connections at strengths 0 and 0.1, but lost one at
0.25, 0.4 and 0.5; the native gate blocked Apply. No interior Apply was performed.

The offline fitter now exposes an experimental handle-length multiplier bounded
to 0.5–1.5, defaulting to 1. It preserves incident endpoint positions and original
handle directions, hence the relative branch angle. Tests cover derivative scaling,
reversed traversal and invalid bounds. The Debug native search tries 1, 1.1, 0.9, ... through 1.5 and 0.5 at the
requested strength. A resolved connection mismatch waits up to 120 observations
before advancing; missing or ambiguous mappings reject instead. A pending retry
cannot advance again until a new submission arrives. Apply still requires exact
connection preservation and three fresh matching observations. The bounds are exploratory, not a native safety guarantee; keeping
the direction alone has already proved insufficient to guarantee connectivity.

Junction-as-player-split is a separate follow-up. Prefer preserving the relative
branch angle rather than forcing one shared tangent; see the feature plan.

The first live handle-length search exhausted all eleven candidates at strength
0.5 on the captured rail-through-merge case. None passed exact native connectivity,
and Apply remained blocked. Length-only adjustment is insufficient for this case;
there is no verified interior-junction Apply result yet.

## Common-rotation live experiment

The current Debug search uses unit-length handles and common rotations of both
selected incident tangents: 0, +1, -1, ... +15, -15 degrees. This preserves their
relative angle, but permits changing their angle to an unselected branch. Exact
native connector preservation still gates Apply. Length-only search remains a
recorded failed experiment; the pure fitter still exposes its tested multiplier.

The rail-through-merge case accepted +2 degrees at strength 0.5 and +3 at 0.8.
Permanent Apply at 0.8 preserved all four junction connections and the physical
lane mappings, topology, elevations and unselected curves. Preview/permanent
control-point differences were zero. The strict regression result remains FAIL:
native rebuilding moved the interior junction center 5.17 mm, exceeding the 1 mm
fixed-center check. No human visual approval or vehicle traversal test exists.
