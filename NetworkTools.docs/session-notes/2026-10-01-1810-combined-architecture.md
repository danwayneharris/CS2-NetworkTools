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
