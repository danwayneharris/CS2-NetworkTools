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
