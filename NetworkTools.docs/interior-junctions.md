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
build success does not prove game behavior.

Junction-as-player-split is a separate follow-up. Prefer preserving the relative
branch angle rather than forcing one shared tangent; see the feature plan.
