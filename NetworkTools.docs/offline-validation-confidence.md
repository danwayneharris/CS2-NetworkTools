# Offline validation confidence

Track evidence by claim, not a single confidence percentage. Update this table
when live observations support or contradict the model; preserve counterexamples.

| Claim | Evidence / confidence | Remaining gap |
|---|---|---|
| Boundary-rotated target math is internally consistent | High for tested cases: executable links actual C# source; endpoint, tangent, zero-strength and invalid-input checks plus existing regressions pass | Broader adversarial shapes and intermediate-strength self-intersections |
| Curvature rule explains captured rail loss | Strong for this fixture: before/after composed-lane inputs predict all four required directions, including the lost one | Other prefabs, grouping, target sorting and other native gates |
| Joint lane-direction correction is feasible | High for the fixed-position mathematical model; all constraints tested simultaneously | Does not predict authored handle changes or reconstructed positions |
| Native candidate search finds a useful correction | One live saved regression passes: +3 degrees accepted; independent bridge confirms all four directed connections; user sees no break | Other cases, exhaustion and rapid user edits; not general validation |
| Repeated fresh observations establish final preview | Limited: revision/control checks already exercised; three-observation policy is new | Native rebuild completion is not proven by frame count |
| Accepted preview survives Apply | One new-search round trip confirmed: same four directed rail connections and exact serialized curves on all three incident edges; user sees no break | Broader layouts, repeated edits and other prefabs |
| Connections remain operational | Not established by geometry or connector presence alone | Train traversal and save/reload |

## Opportunities to reduce manual work

1. Capture every candidate's result and retain failures as replay fixtures.
2. Extract and regression-test the native search controller after the first live
   trial settles its observation assumptions; do not model guessed timing as fact.
3. Compare predicted versus native lane position/tangent changes to build a local
   response model that proposes fewer candidates. Keep native checks as final authority.
4. Expand the case matrix incrementally: reversed selection, symmetric Y, second
   rail prefab, then roads. Avoid asking the player to repeat cases already covered
   by unchanged math unless the integration changes.

## October 1 non-flat checkpoint

Seven position-discovered v1.1 selections reached native preview and permanent
Apply. Six strictly pass; high rail fails only the 23.5183 mm center displacement
assertion. Captured connections, topology, elevations, unselected curves and
preview/Apply agreement pass all seven. Three analytic diagnostic tests pass;
these do not validate terrain support, grade suitability or vehicle traversal.
See [terrain results](terrain-regressions.md). The source of the larger native
center drift remains a bounded follow-up rather than an established float error.

The terrain ramp-out result also survived a saved-checkpoint reload with exact
region geometry and normalized directed junction connections (three junctions).
This adds native persistence evidence for that case only; it does not establish
that the separate high-rail center drift cannot accumulate under repeated Apply.

## October 1 Slope and prefab-default validation

- Offline: 96 assertions against compiled Slope code across four formula terrains,
  both directions. Strong for the tested handle metric/height-offset invariants;
  not a native terrain or lane model.
- Native preview: previously blocked curve-then-ease off-ramp now passes the same
  1 mm guard. No threshold was relaxed.
- Permanent Apply: exact captured preview/control-point agreement, unchanged
  topology and node XZ, and identical directed lane-transition sets at six watched
  nodes. One side edge translates vertically at the moved junction as designed;
  that junction descends approximately 9.982 m. Terrain suitability is unverified.
- Connect: road and rail both inherit the expected prefab, preserve existing edge
  geometry, reject stale Apply revisions, and produce exact preview/Apply curves.
  This establishes physical connectivity, not vehicle route availability.
- Human visual and vehicle validation: pending for this build. Editor toolbar and
  mixed-prefab Parallel behavior are compiled/UI-type-checked, not native-tested.

## October 1 offset-profile sprint: current acceptance and persistence

The later user-authorized geometric tolerance is 5 cm. Earlier sections recording
23.5183 mm as a failure describe the prior policy, not a current failed case.
Functional connection/topology requirements remain independent of this tolerance.

The offset-aware Constant Slope candidate passed offline math and native preview/Apply
audits across the terrain matrix. Its final full-ramp checkpoint survived reload with
exact geometry and normalized directed/physical lane mappings at all 37 shared nodes.
These establish stronger numerical/integration/persistence confidence; they do not
establish visual terrain quality, vehicle traversal or Release/Burst compatibility.
See [review handoff](terrain-profile-review.md) for exact checkpoints and limitations.


## Surface-aware ramp experiment

Offline response-fit and convergence-state tests pass. They establish bounded
numerical behavior, rejection of invalid fits, and the policy for failed/oscillating
preview candidates; they do not model all native cut locations or surface heights.
Native evidence was essential: it exposed 1.17 m repeat drift, a smaller cut-position
feedback, and a stationary-float rejection that the initial offline tests missed.

The revised Debug correction settles against fresh native boundaries before Apply.
From the captured review baseline it used four candidates; reversed repeat moved
controls 1.155 mm and nodes 0.143 mm, then forward repeat was exactly unchanged.
This raises confidence for this particular supported off-ramp, not every junction.
Dan's favorable visual verdict applies to the first corrected candidate; the final
settled candidate still merits a quick visual confirmation. Vehicle traversal and
full Release/Burst remain unverified. Details and persistence checks are in the
[repeatability record](session-notes/2026-10-01-0552-profile-repeatability.md).

Final-build persistence follow-up: the settled checkpoint reloaded with exact geometry;
reversed Apply was unchanged, and a second reload preserved normalized directed and
physical lane mappings at all 37 shared nodes. This is native persistence evidence,
not vehicle traversal. Release C# compilation passed; Burst/postprocessing remains
unqualified. Final settled lane sampling still reports about 17.69% peak grade, so
"perfect constant slope" would be an incorrect claim.
