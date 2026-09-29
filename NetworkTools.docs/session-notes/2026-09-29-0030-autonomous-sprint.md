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
