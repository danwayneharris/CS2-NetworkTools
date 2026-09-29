# 2026-09-29 0030 - Autonomous regression and split-point sprint

Verified merged main checkpoints: NT 2180aec, bridge f0d6882. New local dan/autonomous-regressions branches start from these. Existing NT UI package-lock change remains user-owned and unstaged. No publishing authorized.

Priorities: reusable live regression runner; fresh baseline discovery and independent Apply verification; representative toy networks; tangent-continuous pinned split nodes; bounded longer-route and slope architecture investigations. Preserve baseline saves, use unique checkpoints, stay paused, no force killing. Record failures as well as successes and separate offline/native/permanent/visual confidence.

## Runner increment and first live blocker

Implemented scripts/live-regression.py: explicit mutation opt-in, fresh geometry-based endpoint discovery, identity-independent baseline fingerprint, fixed city-session check, request/response capture, no mutation replay, bounded polling, verified unique checkpoint package, strength sweep and independent permanent topology/elevation/connection/incident-curve comparisons. Four offline guard tests pass. Fixture has rail, four-way road branch, highway on/off ramp cases.

First discovery failed because get_network_edges includes long edges with far nodes outside get_network radius. Fingerprint now explicitly marks absent far-node positions; scope remains bounded, not whole-map validation. Discovery found 37 nodes and 39 edges. First live attempt saved a valid checkpoint then stopped at nt_activate: Debug adapter unavailable. No smoothing occurred. Added earlier adapter preflight and baseline save hash guard.

Environment evidence: local mod folder is .NetworkTools (disabled); content_load.json lists the large normal playset. User-specified reduced test playset is not currently active. Gracefully closed PID 49932 after preserving checkpoint; no forced termination. Asked for reduced playset name while continuing offline. Current runner remains a prototype: broader polling stability, complete selected-path preview comparison and forbidden crossing fixtures still pending. Existing connection comparison uses directed lane endpoint identities, not counts.

## Split target research increment

Added managed PlanarSplitTarget research builder and executable tests. It partitions at explicit split flags, derives one shared tangent from adjacent anchor chords, reconstructs split curve endpoints at the pinned node (rather than preserving a dogleg offset), calls the existing target fitter per section, and publishes only after every section succeeds. Interior junction pins still reject even when also selected as splits. Full-strength target test checks positions, shared positive tangent direction, outside endpoints and no partial result on failure. Existing geometry suite passes.

Not integrated or deployed. Managed arrays are unsuitable inside the current Burst job without adaptation. Partial-strength source/target blending can reintroduce source tangent mismatch: this must be addressed explicitly before claiming split continuity throughout the slider range. At strength zero, identity and correction of an existing kink cannot both hold. Current test establishes planar target continuity only, not curvature or 3D grade continuity.

## Longer-route and architecture review

Added sprint-architecture-review.md with source-grounded boundaries, runner readiness gaps, slope test recommendations and a combined horizontal/vertical candidate design. No slope bug claimed or runtime slope changes made.

Added scripts/explore-longer-target.py and retained PNG/SVG/JSON plots. A sextic single-bulge profile preserves straight boundary tangents and zero endpoint curvature, with explicit +/-20 m corridor. Numerically solving sampled arc length gives +2 m with 8.74 m amplitude and +5 m with 13.99 m amplitude. +10/+20 m are infeasible within this particular target family and corridor (not universally impossible). Mirrored routes have equal length, exposing the missing side-selection policy. No negative slider implementation: length bias and smoothing strength remain separate concepts. Plot generation succeeded through isolated uv matplotlib environment.

## Runtime split wiring (not deployed)

Replaced the managed-only target with a shared pointer implementation and managed test wrapper. Added per-node split flags, selection-owned identity set, UI list, and shared mutation validation. Split constraints are hard at every strength including zero; paths without splits retain prior behavior. Offline sweep/reversal/multiple-split/nonfinite tests pass, C# Compile and TypeScript --noEmit pass. Bridge nt_split compiled and 61-command catalog check passed. Full deployment and live tests still pending while user manually launches corrected playset.

User reported local mod absent. Verified original DLL remained intact in Mods/.NetworkTools with SHA C093D6FB7005F2AEB97663318F6E8E1FE993835FF69967BB850A3D13F780BB41. With no game process running, renamed only that directory to NetworkTools; destination absent, parent paths checked, DLL hash unchanged. This restored the milestone binary, not sprint code. A new game process 37508 appeared afterward; no city loaded at last observation.

Expanded runner now traces the selected path, captures every selected preview edge through shared nodes, rechecks the exact submission token before Apply, and checks fixed/unselected geometry afterward. It rejects missing preview coverage. Successful response duplication (.txt plus .json) removed for future captures. No claim that these new checks have passed live yet.

## New user baseline and first successful live runner case (01:20)

The previous baseline guard correctly rejected the user's newly edited toy layout before any mutations. User confirmed the new save is `bridge test - rail smoothing breaks merge junction highway jank roads`; capture found 42 nodes and 39 edges in the region. Registered a separate fixture `toy-highway-jank.json`, verified the local save ZIP and SHA256, and explicitly updated moved highway endpoints from the observed graph. Older fixture/save untouched. This does not claim that no assets exist outside the query region.

Rail regression on the restored milestone deployment passed at strengths 0.5 then 0.8. Unique pre-mutation checkpoint: `CitiesIIAgentBridge-regression-rail-merge-20260929-082016-27c19dcb`. Three selected edges changed. All selected preview/permanent control points agreed within the runner's 1 mm tolerance; reported incident maximum errors were zero. Three watched shared nodes preserved directed lane-pair sets (4, 2, 2 connections). Captured topology, elevations, fixed/unselected nodes and unselected edge curves passed. Requests/responses retained in `captures/sprint-20260929-jank-rail`.

Five offline runner tests pass, including equal-count/different-pair rejection. Added split automation and permanent pinned-position/tangent checks; those branches remain unverified live because sprint split binaries are not deployed yet. No traffic traversal or new human visual approval. Native rebuild stability across delayed samples remains an open runner improvement. Game left paused in the new toy city, with the rail Apply unsaved; baseline and checkpoint preserved.

User clarified the lower-priority negative-strength idea as a centered side-bias
control, midpoint equivalent to zero; extra length is not the defining requirement.
Recorded in smooth-curve-plan.md. Existing longer-route research remains historical,
not an implementation commitment. Continue regression/split work first.

## Road lane connectivity requirement and stability sampling

Dan explicitly requires road lane connections to be preserved just as rail
connections are: a ramp feeding the outermost highway lane must still feed that
lane after smoothing. Equal counts are insufficient. The runner already compares
directed owner/lane-index/secondary endpoint pairs, but mapping lane indices to
physical lanes must be checked against actual snapshots/source before claiming
outermost-lane semantic coverage. Rail crossings likewise preserve absent pairs.

Added bounded permanent-result sampling: require three equal observations separated
by at least one second, reject incomplete snapshots and wait while owners carry
Updated/Created. Compare network geometry/identities plus watched owner geometry
and directed connection sets; ignore timestamp-only changes. Six offline guard
tests pass. This is observed stability, not a native job-completion fence.

Live read-only stability check on the already-applied rail case passed: three
matching observations, 42 nodes/39 edges, directed connection sets of 4/2/2.
Captures: sprint-20260929-rail-stability. No additional game mutation occurred.

## Guarded baseline reload and road regression

Added scripts/reload-toy-baseline.py: requires an independently identified live
citySession, paused empty toy city and enabled controls; verifies baseline checksum
and ZIP metadata; saves and verifies a unique checkpoint before shutdown. Checks
STOP and exact heartbeat PID before CloseMainWindow; waits at most 45 seconds,
never kills, then uses existing visible launch helper. Launch acceptance is not
readiness: subsequent runner checks session, pause and baseline fingerprint.
Two offline package-guard tests pass. Two live graceful reload cycles succeeded.

Road-four-way-branch passed strength 0.5 then 0.8 after a fresh baseline load:
three selected edges changed, watched directed car-lane sets remained 4/16/4,
incident preview/permanent errors were zero, and three permanent observations
settled. Captured topology/elevations/unselected geometry passed. No traffic or
human visual claim. Capture: sprint-20260929-jank-road.

Source follow-up: installed Game.Pathfind/PathNode.cs packs owner in upper 32 bits,
composition lane byte plus segment byte in lower 16; GetLaneIndex returns both.
LaneSystem.cs:4147-4149 constructs edge PathNodes from composition lane index and
segment indices. Lines 2649-2658 associate composition lane indices with lateral
m_Position.x. Thus owner/lane keys carry more meaning than lane entity IDs, but
unchanged composition/order still needs verification for physical outermost-lane
claims. The current native corrective junction search is TrackLane-only
(RoadShapeToolSystem.JunctionSearch.cs:81); road regression checks must not be
misrepresented as an implemented road connection-preserving search.

Highway-on-ramp passed 0.5 then 0.8: two selected edges changed, unchanged directed
car-lane pair sets at the three watched nodes (1/0/5), zero incident curve errors,
three stable observations and unchanged captured topology/elevations/outside
geometry. Zero pairs at one watched node is a recorded empty set, not a claim of
traffic traversal through that node. Capture: sprint-20260929-jank-onramp.

## Off-ramp pass and split deployment

Highway-off-ramp passed 0.5 then 0.8 on a fresh baseline: two selected edges changed,
unchanged directed sets (1/5/1), zero incident curve errors, three stable permanent
observations, topology/elevation/outside-selection checks passed. Saved final test
state to a unique checkpoint and gracefully closed the game using --close-only.

Full Debug bootstrap build passed including IL postprocessing, webpack and local
deployment (33 existing C# warnings, webpack size warnings). NT deployed SHA256:
96AAFE46716BBEE2DA7BFB0617FBAFEE577D3A80FC551B52F96DECD5473DED1A.
Matching bridge rebuilt DLL copied with timestamped backup and verified SHA256:
F765BC3714CB7409B99E668EE8F7523932A93203804ED5296BCC22061C89FBBB.
Visible baseline launch started PID 33860; split behavior still awaiting live test.
Added separate one/two-split rail fixture definitions using captured interior-node
positions; strengths 0/0.5/1. Baseline save untouched. Existing user lockfile remains
unstaged; Release/Burst is not established by this Debug deployment.

## First live split run failed strict regression

rail-one-split completed preview sweeps 0/0.5/1 and Apply, but runner rejected a
fixed endpoint movement of 0.003008 m. Split node 350911 stayed at its original
position. End junction 350934 moved from (-74.86412,617.6969,-1925.09436) to
(-74.86254,617.6969,-1925.0918). Native preview already contained the shifted
position, so this is not solely Apply disagreement. No tolerance relaxed.

Further capture inspection: three-way merge directed track pairs preserved, but
junction-owned pairs at degree-two nodes 350909 and 350911 disappeared (two each).
Need distinguish actual loss from native direct-join representation before fixing
or revising the oracle. Do not call this split test a pass. Captures retained in
sprint-20260929-one-split; game paused with failed-test Apply unsaved and pre-test
checkpoint intact.

Installed NodeAlignSystem.cs:87-169 recomputes non-standalone node centers from
incident endpoints and tangent-line interactions; assigning a fixed Node position
alone is insufficient. It explains a mechanism consistent with observed native
preview drift, but exact replay/causality still needs verification. Follow-up must
address native center constraints and independently trace degree-two connectivity.

## Direct-join graph oracle and node-center replay

New lane-connectivity.py reconstructs directed transitions between incident edge
composition lanes through full snapshot-local PathNode equality. This covers both
junction-owned connector lanes and shared-node direct joins. It stops upon reaching
another edge rather than inferring remote paths. Tests on actual one-split captures
show identical transitions before/after at all three watched nodes; injected broken
shared endpoint and same-count destination-lane swap are detected. Current runner
now compares these transitions and records raw connector-pair changes separately.
This is native graph reachability, not full traffic/access restrictions or a proof
of physical lane ordering under arbitrary composition changes.

replay-node-center.py implements bounded same-layer, non-Standalone XZ alignment
from installed source. Predicted 2.9648 mm shift versus observed 3.0083 mm, error
0.0517 mm (double diagnostic versus native floats). This supports native alignment
as cause. Dan regards 3 mm as trivial visually; retain it diagnostically rather than
claim a player-visible defect or silently remove the check.

Added replay-regression.py to run current assertions on recorded requests/results
without contacting the game. One-split replay passes topology, elevations,
unselected curves, selected preview/permanent curves, actual split pin and tangent
checks, and semantic lane transitions. It still fails the unchanged strict 1 mm
boundary-center check and reports that drift explicitly. Six runner tests and three
graph tests pass. No further live mutation in this investigation.

Dan added interior-junction smoothing to this sprint explicitly. Current split
implementation intentionally rejects it. Finish current split validation, then
prioritize this addition with a tested replacement for the guard and coverage of
selected/unselected incident branches; recorded in the feature plan.

## Two-split native pass and physical lane mapping

Two pinned interior rail nodes passed strength sweep 0/0.5/1 and permanent Apply
at 1.0, including strict 1 mm fixed-node check, pinned curve endpoints and shared
planar tangent directions. Three selected edges changed; directed transitions
remain 4/2/2. Native preview/permanent curves match exactly in captured output.
No fixed-node drift reported in this case. Capture: sprint-20260929-two-splits.

Added edge composition comparisons (lane index, lateral position, direction flags,
carriageway/group, prefab and width), so unchanged numeric lane transitions cannot
hide swapped lane positions. All stored road/on-ramp/off-ramp and one-split
snapshots retain the same composition mappings. Four lane-graph tests now include
a deliberate lateral lane swap and pass; six runner tests pass.

Observed stale splitChoices in Idle after Apply: path data cache outlives active
selection. Added Phase.Ready gate to SplitChoicesJson; no geometry changes. This
small UI-state fix is not deployed yet. Starting a separate partial-strength Apply
case rather than claiming that preview readiness alone proves its permanent result.

Partial-strength permanent Apply at 0.5 also passed for two rail splits, including
strict node-position tolerance, tangent joins, semantic lane transitions and stable
composition mapping. Native preview/permanent incident curve errors were zero.
Capture: sprint-20260929-half-splits. Stale split-choice UI fix compiled without
deployment. Game currently paused in the named highway-jank toy baseline with the
half-strength two-split Apply unsaved; its pre-test checkpoint is preserved.

Zero-strength two-split permanent Apply passed: hard pin/join constraints apply
at zero as designed, with positions, tangents, composition mappings, directed
transitions and preview/Apply agreement verified. Capture: sprint-20260929-zero-splits.
A separate road split fixture is prepared, and its baseline reload is in progress.

Road two-split full-strength Apply passed geometry/pins/tangents and lane checks.
Review caught that the semantic graph initially omitted same-edge U-turns. Fixed
traversal to retain transitions to another lane on the same edge and transitions
back through a connector. Added captured four-way U-turn deletion test; all five
graph tests pass. Offline road-capture replay with corrected oracle still passes,
with 4/16/4 transitions and zero curve errors, no fixed-node drift. Both original
report and corrected replay retained. This was an oracle coverage bug, not a game
change. Game remains paused with road split Apply unsaved and checkpoint intact.

## Interior-junction section prototype (offline only)

Added PlanarJunctionTarget, deliberately not called by the game transform. It
partitions at fixed interior junction nodes, fits sections using the split target,
and restores each selected junction port's original endpoint and adjacent handle.
This preserves existing incident tangent lines and handle lengths rather than
forcing all branches into a shared ordinary-split tangent. The selected path gives
the route; no arbitrary main/merging branch classification is introduced.

Tests pass across 21 strengths: exact center/port/handle preservation, real movement
inside sections, reverse traversal, split point alongside junction, ambiguous
junction+ordinary-split rejection, and nonfinite input rejection. Full geometry
executable suite passes. This does not prove native curviness/lane connectivity or
visual quality. A one-edge section between two junctions can have no free controls
under these constraints; do not claim smoothing where none is possible.

Runtime interior guard remains intact. Next integration requirement is repeated,
fresh native validation for every affected junction, including unselected branches
and exact intended/forbidden lane transitions. Current endpoint search supports only
one rail junction, so simply invoking this fitter would be insufficient.

Dan added a later junction-as-split case: preserve/prefer the relative selected-branch
tangent angle rather than force a common tangent, while pinning the junction and
preserving all connections. Recorded after interior functionality in the feature
plan. Current runtime work continues; combined junction+ordinary split remains
explicitly unsupported rather than silently collapsing the angle.

## Guarded interior-junction development integration

Added a Debug-only native gate for each interior junction (3-8 incident edges).
Missing connectivity, repeated edges, unknown endpoint ownership, missing/ambiguous
temporary mappings and changed unselected curves reject or wait boundedly. Baseline
car/track directed pairs must match exactly (both missing and extra connections
reject), through three observations of the current fresh submission. Original
input freshness still comes from the existing probe; Apply rechecks baseline pairs.
This gate does not claim support for roundabout connector ownership or degree-two
direct-join rewrites at a junction. Unsupported cases stay rejected.

Only successfully captured interior baselines enable the new section fitter in
Debug job config. Failed/unknown baselines and Release retain the old rejection.
C# Compile and the full geometry executable tests pass. New rail-through-merge and
road-through-four-way fixture definitions prepared; deployment/live verification
are next. This is an experimental guarded replacement, not a claim of proven
native behavior from compilation.
