# Working junction loses one preview connection

User created two failing examples; first already applied, second left unapplied at
1.0. Captured full diagnostic trace plus permanent/preview for junction54998:29 in
citySession38eff1866678419193aa0bd38cd3a889. Latest SmoothTrace184 valid=true,
selected55008:41 ->54998:29, strength1. Probe revision269 matches, originalInputs
matches, all four selected curves match and edgeTrackLanes=8.

Despite those healthy diagnostic checks, permanent junction has FOUR directed
track lanes while resolved preview55332:71 has THREE. Missing normalized connection:
55025 lane1 ->80305 lane2. Other three remain (55023:1026 ->55025:2,
55025:1 ->55023:1025,80305:1 ->55025:2). Both snapshots complete/error-free.
This is direct evidence that successful geometry/revision/edge-lane checks do not
establish junction connectivity preservation. Capture saved before user Apply.
Next supervised Apply will test whether this predicted loss occurs in the permanent
network too. First already-applied example is not yet separately identified/captured.

## Apply confirms predicted loss; desired behavior clarified

User applied and reports identical visible break. After snapshot confirms the same
three remaining directed track lanes and loss of55025:1 ->80305:2. All three
incident edge curves exactly match preview at serialized precision. Snapshot complete,
no errors, same citySession. Saved54998-applied.json.

User prioritizes adjusting target geometry and recalculating curve coefficients to
preserve connections, over merely highlighting failures. Treat working directed
connections as required constraints; already-broken layouts need explicit repair
intent rather than silently accepting their missing links as the target. Candidate
fitting should consider boundary tangent/handle choices and distribute corrections
along the selected path, respecting topology/elevations. Native reconstruction must
verify candidate connections; source-derived curvature limits can guide the search
but do not replace native validation. A safe lower-strength candidate can be a
fallback if verified, not an assumption of monotonic feasibility. If no feasible
candidate is found, explain the failure and block Apply for that future mode.
Current runtime behavior remains unchanged; this is design direction, not implemented.
