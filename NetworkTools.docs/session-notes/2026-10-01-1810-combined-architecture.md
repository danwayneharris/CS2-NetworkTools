# Combined sprint: baseline and architecture

New isolated nt-combined-sprint / dan/combined-smoothing-sprint starts from verified
origin/main 5cc24c0. Original checkout has unrelated lockfile and user notes/audits;
none were modified. Common initialized at pinned d5e28d7.

Identified existing paused toy city session f4923e4b5e584ffc9fa64ebdcf16b65a and
preserved the user-edited trumpet in verified package
CitiesIIAgentBridge-combined-before-trumpet-preservation-20261002-010804-dd6b1f48.cok.
This is not an unsmoothed baseline. Dan is building a separate baseline; no further
live control while he works. Raw checkpoint responses remain ignored in artifacts.

Step 0 source review identified one-span vertical fitting, original rather than
candidate selected-incident input to the surface model, and two formerly exclusive
native feedback loops as the focused integration seams. See
[architecture checkpoint](../combined-smoothing-architecture.md). No runtime code
changed or combined behavior claimed at this checkpoint.

## Sectioned vertical primitive

Added SectionedVerticalProfile: caller supplies fixed anchors and oriented
incoming/outgoing grade constraints; each interval reuses VerticalLinearProfile.
Outputs are scratch and must all be discarded after any failure. The primitive
does not guess pin policy or change the existing independent tool paths.

60 focused assertions pass: fixed heights, offsets, nonuniform stationing, reversed
storage/traversal, equal pin grades, explicitly distinct junction grades, changed
horizontal length, repeat/deterministic calculation and invalid/late-span failures.
The full existing geometry executable also passes, including its 605 ordinary
vertical-profile assertions and native captured replay. These are not live tests.

First invocation incorrectly passed --source to dotnet run; the executable treated
it as a replay filename and failed before assertions. Corrected invocation uses
--no-restore after successful build/restore. Both logs are retained under artifacts.
No game interaction since the preservation checkpoint; user is creating a baseline.

Preparatory extraction: ordinary Constant Slope's surface-correction call now lives in one job helper, with identical arguments, result assignment and diagnostics. Non-deploying production compilation and Slope tests passed (96 assertions plus incident edits/rotation). No deployment or live mutation occurred. This creates the shared call site needed by later combined dispatch; current runtime behavior is unchanged.

## Combined input ownership regression

Added Debug-only `CombinedProfileInputs` to prepare section-fit data from the
horizontal candidate while reading endpoint/node offsets and boundary grades
from immutable authored input. Tests deliberately give the candidate stale Y
values and halve its horizontal lengths: the candidate stationing changes while
authored offsets/grades remain intact, including reversed traversal. Undefined
horizontal tangents, nonfinite candidate controls and mismatched traversal are
rejected. This helper does not yet enable combined mode.

Validation: `scripts/test-slope.ps1` passed non-deploying production compilation,
28 new production-adapter assertions, existing incident-edit regressions and 96
independent Slope assertions. Existing generated-source warnings remain. No game
mutation or deployment occurred. Output: `artifacts/combined-input-tests.log`.

Dan reports `bridge test - trumpet combined baseline` is saved, loaded and paused.
This is the intended unsmoothed toy baseline; verify its file/hash and live
identity before first automated mutation. Earlier preservation checkpoint is a
different, already edited state and must not be used as an unsmoothed baseline.

## Anchored combined vertical stage

Added Debug-only `CombinedLinearProfileTransform`, not yet dispatched by the UI
or tool job. It builds section inputs from the preceding adapter, anchors outer
nodes, explicit splits and junction pins, and publishes geometry only after the
entire fit and float-range checks succeed. Free interior Y may change; fixed XYZ
must already match authored positions. Original distinct branch grades remain
at junctions. Ordinary splits require numerically equivalent incoming/outgoing
grades (1e-6 grade equality, not a position tolerance); an incompatible split
returns a named conflict rather than inventing a transition policy. Existing
eligible outer SmoothStart/SmoothEnd policies remain available.

The pointer entry point takes disjoint caller-owned scratch buffers so offline
tests can invoke compiled production code without Unity native allocation. The
runtime wrapper owns and disposes temporary NativeArrays. No existing independent
mode dispatch changed.

First test build failed because the Slope test project did not allow unsafe code;
enabled that compiler option to exercise the production pointer entry point.
Rerun passed 15 new transform assertions, 28 input assertions, existing incident
regressions and 96 Slope assertions. Cases include changed handle lengths,
compatible split grades, explicit split-conflict rejection, fixed junction branch
grades, deterministic repeat and no partial publication on failure. This is not
native game evidence and does not yet cover the full planned combined matrix.
Output: `artifacts/combined-transform-tests-final.log`. Game remains untouched.

## Combined dispatch checkpoint (not deployable yet)

Connected the horizontal and constrained vertical stages inside one Debug job,
with an opt-in parameter defaulting false. Release configuration forces combined
off. Combined Apply is intentionally disabled until the native surface/junction
observers are coordinated; this is an intermediate checkpoint, not delivered
feature behavior. UI/provider exposure still follows. Independent dispatch is
unchanged. Surface correction cannot overwrite interior split/junction anchors;
its existing restricted terminal-ramp envelope is not expanded to those cases.

Corrected an architecture-note assumption after reading the actual predictor:
SurfaceJunctionHeightModel.Predict already substitutes selected candidate geometry
and adjacent candidate position. The outstanding issue is old native cut-reference
XZ, not substitution of the authored selected curve.

Found that SmoothPinned includes dead ends, not only junctions. Added explicit
SmoothJunction metadata from incident degree so the combined boundary policy does
not preserve dead-end grade accidentally. Two counterexamples distinguish a
height-only dead end from a terminal junction grade anchor. Non-deploying production
compile/tests pass: 28 input and 19 transform assertions plus unchanged incident
and 96 Slope assertions. `artifacts/combined-dispatch-tests-final.log`. No deployment
or live mutation. Next: current-candidate native reference priming, bounded validator
coordination, UI/provider integration, then full offline/live validation.

## Native observer coordination increment

Combined candidates now first submit the regular combined profile to collect
stable native EdgeGeometry references for their changed horizontal alignment.
Only then is the supported surface correction attempted. Junction observers wait
for the surface stage to accept that same fresh submission; dirty evidence is
not passed onward after an observer requests a retry. Changes in endpoint rotation
or interior handle scale/rotation discard surface references and convergence.
The input revision owns a 60-second combined budget, in addition to the existing
20-second surface and bounded correction/search limits. Independent Slope keeps
its original reference path. Combined Apply now requires both surface and junction
acceptance plus the existing original-input/submission gates.

Non-deploying compilation and existing production regressions pass; this does not
establish native convergence or full lifecycle coverage. UI/provider exposure,
additional stale-candidate tests and live validation remain. No deployment yet.

## Experimental combined control and provider

Added a Constant Slope opt-in inside Smooth Curve, hidden via an explicit backend
availability binding outside Debug. The explanatory text identifies fixed endpoint,
split and junction heights and incompatible split-grade rejection. New tool sessions
and ordinary provider activation reset combined mode off, preserving independent
Curve/Slope defaults. NT provider descriptor 1.2.0 adds `combined` with required
session, revision and boolean enabled; state reports combinedSlope and surface
acceptance/failure. The bridge remains unchanged. UI and provider use the same
parameter invalidation and domain Apply gates.

Production compile and existing offline Slope/combined tests passed. Generated
55 parameter bindings using the real code generator. Initial npm ci failed because
the existing lock omits optional platform packages. Installed locally without
rewriting the lock, which initially resolved newer Node declarations incompatible
with TypeScript 4.8.4. Installing exactly the lockfile's @types/node 24.9.2 locally
(no-save/no lock writes) resolved that; `tsc --noEmit` then passed. No deployment,
no in-game UI claim. Full build remains a later gate. Logs are under artifacts/
combined-ui-*; no package manifests or Common revision changed.

## Aggregate, Release compile, and first deployment

Initial aggregate: six suites passed, codegen failed because its whole-output
snapshot detected the intentional combined parameter. The updated test explicitly
asserts all four new generated lines, removes only those from the old comparison,
and retains the old complete-output hash for every existing parameter. Five codegen
tests then passed. Full aggregate rerun passed all seven suites (`artifacts/combined-offline-final/summary.json`).
Release `Compile` target passed without postprocessing/deployment; this is C#
compatibility, not Burst/native execution. Full Debug bootstrap subsequently passed
UI generation/bundling, postprocessing and deployment (32 warnings, zero errors).

Live read-only discovery verified paused population-zero Wantagh, controls enabled,
city session 23d319ec8e6042c69039b406db23a65b. Dan's unsmoothed baseline package:
`bridge test - trumpet combined baseline.cok`, SHA256
`d7724aa3a84e56bcbd617cce5d364687445c22fae175828a426ba0c00f3fd0ef`.
Package integrity and metadata verified. Before gracefully closing, preserved and
verified `CitiesIIAgentBridge-regression-before-reload-20261002-014406-27c72bcb.cok`.

Deployed Debug DLL SHA256:
`2EADDE8E2933C129B52F587E9B91FE43AA746E7C2D0A7773212A152DE1896EF6`.
Visible launch requested for the original baseline (metadata
63417f2b0b32c6769118c5d88745fd54), process 25304. Loaded-city readiness and native
combined verification still pending. Build-owned lockfile churn reviewed separately
and restored to HEAD; no intended dependency change.

## First combined native preview and Apply

Loaded verified trumpet baseline on PID25304, city session
4cf6c9b3836a45ff841a81a108171257, paused with controls enabled and provider 1.2.0.
Initial discovery used the older terrain region; corrected the reusable discovery
script to use the actual camera region (39 nodes, 40 edges). Captured identity-free
baseline fingerprint and committed a compact fixture for the three-edge mainline
section. Raw captures remain ignored under artifacts.

Preview-only test accepted submission2 at strength1 with combinedSlope true.
The loaded independent slope settings had smoothStart/smoothEnd true, so this
case exercises existing boundary grade matching, not the restricted surface
correction. Need expose those settings clearly for combined review and test the
surface-corrected ramp separately. Preview script now verifies checkpoint ZIP and
metadata before changing tool selection (the already-created checkpoint was also
independently checked).

Extended the existing runner with an explicit boolean combined case and provider
command. Only free selected interior elevations may vary in combined mode; fixed
XYZ, outside geometry, topology, preview/permanent and directed lane invariants
remain. Split tests additionally check vertical-grade agreement. Eight existing
runner guard tests passed. Further counterexamples for new policy remain to add.

`terrain-regression.py` passed the first combined Apply:
- Exactly three selected edges changed.
- No fixed-node drift or unselected curve change.
- Selected and incident preview/permanent maximum curve error: 0m.
- All watched directed lane transitions and composition identities preserved.
- Raw connector representation changed at one node, but directed transitions did
  not; the existing semantic connection oracle distinguishes these.

Evidence: artifacts/combined-trumpet-mainline-apply/report.json plus recorded
before/preview/after and terrain observations. Checkpoint:
`CitiesIIAgentBridge-regression-trumpet-mainline-short-20261002-015218-677356d9.cok`.
Game remains paused on trumpet baseline with UNSAVED mainline combined changes.
No human visual review or vehicle traversal claim. Repeat/reverse/reload, ramps,
interior junctions, splits and same-baseline operation-order comparisons remain.

## Explicit boundary controls and constraint counterexamples

Combined UI now displays the existing Smooth Start/End boundary policies. Provider
`combined` accepts optional boolean smoothStart/smoothEnd (validated before any
parameter mutation); state exposes the same rejection reason shown by UI. Split
grade conflict, failed vertical fit and exhausted surface convergence have distinct
messages. Existing defaults/independent semantics remain. These changes are not yet
deployed; the running game remains on the first combined Debug build.

Live fixtures now configure combined boundary flags explicitly (default false),
preventing persisted settings from contaminating operation-order comparisons.
Factored the existing node checks to enforce fixed-node failures immediately and
reject nonfinite positions. Ten runner tests pass, including counterexamples that
allow changed Y only for free selected interiors in combined mode, while rejecting
endpoint, pin, junction and unselected movement. Ordinary Curve still forbids all
node elevation changes. The 5cm positional policy remains; NaN is never tolerated.
Production Slope/combined tests and TypeScript checking pass after these changes.
Native first-Apply evidence above remains evidence for the earlier deployed build,
not this new UI/provider increment.


## Boundary deployment and terminal-ramp native verification

Second full Debug build/postprocess/UI/deploy passed (32 warnings, no errors).
Deployed DLL SHA256: `3FC26251F27544C9EDDF81B84758145A57414DBC19BE70BEA5AFE278F6466221`.
Prior mainline changes preserved in
`CitiesIIAgentBridge-regression-before-reload-20261002-015710-8136a541.cok`.
Reloaded unchanged checksummed `bridge test - trumpet combined baseline`.

Reversed traversal production transform test passes: now 28 assertions, including
identical world geometry under reversed traversal. Existing sequence runner now
supports combined mode using existing checkpoint/reload/native oracles. Compact
terrain-region fixture describes existing cases inside the trumpet baseline.

Terminal off-ramp combined strength 1, Smooth Start/End false: PASS. Five selected
edges changed; no fixed-node drift or outside curve change; preview/permanent curve
error 0m; directed lane transitions preserved at all five watched nodes.
Checkpoint before Apply:
`CitiesIIAgentBridge-regression-offramp-terminal-20261002-020316-aefb416a.cok`.
Evidence: artifacts/combined-terminal-ramp-apply/report.json and terrain diagnostics.
This establishes one native Apply, not repeatability, visual approval or traversal.
Same-baseline sequential-tool comparisons follow.

Automatic approval review rejected restoring the npm lockfile to HEAD because it
could not establish task ownership of every change. Left it untouched and unstaged;
review before final publication. No dependency changes intended in this feature.


## Sequential comparison: an offline prediction mismatch remains visible

Curve then Slope on the same terminal-ramp baseline completed both native Applies.
Slope report and independent audit confirm identical preview/permanent geometry,
unchanged topology and fixed endpoints, and preserved directed/physical lanes.
The additional simple offset-fit prediction failed: 5.5731243m maximum control
error (`artifacts/combined-compare-curve-slope/1-slope/profile-analysis.json`).
The sequence stopped; it is NOT recorded as an all-checks-passed comparison.

Source shows SlopeSurfaceProfileTransform adjusts the ordinary profile after the
basic offset fit. summarize-profile-experiment.py's independent fit does not model
that correction. Native provider evidence reports surfaceAccepted, which alone does
not prove correction was applied. Further provenance/model qualification is needed;
do not simply increase tolerance or discard the failed prediction. The applied toy
result was preserved before the next independent baseline reload in
`CitiesIIAgentBridge-regression-before-reload-20261002-020654-46abc52a.cok`.

Updated combined-smoothing-options.md to separate current experimental behavior
from its original generalized design proposals. Broader native testing remains.
