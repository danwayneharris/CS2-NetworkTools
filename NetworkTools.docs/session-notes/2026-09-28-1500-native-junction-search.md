# Native preview candidate search (experimental Debug build)

## Change

Added bounded boundary-tangent rotation to the existing full-path target fitter.
Default zero rotations preserve the existing API and behavior. Candidates retain
the original topology and vertical coordinates. At full strength the sliced target
has shared tangent direction at internal joins. Zero strength remains an exact
no-op. Angles are bounded to +/-15 degrees and non-finite input is rejected.

The Debug tool now tries 0, +1, -1, +2, -2, ... +15, -15 degrees at a selected
rail junction endpoint. Each candidate goes through native preview reconstruction.
This is a bounded feasibility search, not an optimal fitter or a direct use of the
offline 3.57-degree lane-space estimate. It keeps requested strength unchanged.
Candidates alter the whole selected target; unselected authored curves are checked
against originals at 1mm control-point tolerance.

The first scope supports one three-arm rail junction at a selection boundary.
Two junction boundaries and unsupported/missing data fail closed in the experiment.
Interior-junction rejection remains. Road-only junctions retain previous behavior.
Existing directed track connections are obligations, independent of any mainline
classification. Already missing connections are not inferred or repaired.

## Native observation and Apply

Resolve the preview junction via shared endpoints of uniquely mapped temporary
incident edges; require all three mappings and a unique temporary common endpoint.
Normalize lane path-node owners to original incident edges, retaining lane and
secondary-node identity while removing curve-position bits. Compare the required
directed connection set, not lane counts. Missing/ambiguous resolution times out
after 120 observations; original-input changes reject the search. Selection or
parameter revision changes start a new search. Candidate jobs get new submission IDs.

Require three matching connection-set observations after the existing curve and
revision checks. Apply is unavailable until acceptance and is rechecked against
the original-input snapshot and original connection set. Apply uses the same
rotation as the accepted preview. There is no automatic Apply.

This timing rule is a heuristic, not a formal completion fence for every native
system. Invalid mathematical candidates stop with Apply disabled; the current
experiment does not advance past that failure. Logs use
`[NetworkTools.JunctionSearch]` for initialization, retries, acceptance and rejection.

## Verification and confidence

The first compile caught an ambiguous Edge import (Game.Net vs Game.Pathfind);
replaced the namespace import with a PathNode alias. Non-deploying Debug compile
then passed. Geometry executable passed existing regressions plus boundary rotation,
fixed endpoints, full-target tangent continuity, zero strength and invalid-angle
checks. These validate actual linked production math, not a separate implementation.

Confidence is tracked in [offline-validation-confidence.md](../offline-validation-confidence.md).
The runtime search has no live verification yet. First supervised case: original
working merge that previously lost one connection at strength 1, selecting branch
endpoint to junction. Leave preview selected until bridge capture. Then Apply only
after review, capture again, and compare the directed connections and authored curves.

No Release runtime search is enabled. This is a Debug-only trial, not a production
connectivity guarantee. Native prefab limits guide native lane generation; the
search does not impose the offline projection's experimental 2% reserve.

## Deployment for saved-baseline comparison, 15:15

After capturing the pre-search baseline (commit40e84c9), user closed the game and
authorized experimental deployment. Full Debug bootstrap completed with 33 warnings
and zero errors; postprocessing and UI build completed (webpack three warnings).
Installed and build DLL SHA256 both:
`B3BF5DD1A958C01EC618A09414C98E2C21B5C418EFBAA2D7931FAF0662E3046D`.
New runtime behavior still awaits in-game verification. User should reload the
saved pre-Apply network, repeat branch-endpoint to junction selection at strength1,
and leave preview selected for capture without applying.

## First live preview succeeds

Reloaded saved case has new entity identities: selected53095:1 ->53107:1,
strength1. Trace input coordinates match the prior saved selection. Search accepted
submission13, revision24, attempt5 (+3 degrees authored boundary rotation), after
trying 0,+1,-1,+2,-2. Required4, observed4. User reports no visible breakage.
Independent bridge snapshots of permanent53107:1 and the resolved preview are
complete/error-free and contain the same four directed rail connections:
359709:1026 ->359710:2;359710:1 ->359709:1025;
362240:1 ->359710:2;359710:1 ->362240:2.
Captures and search trace are in `captures/comparison-save-20260928`.
This verifies one live preview search, not Apply, train traversal or broader coverage.
