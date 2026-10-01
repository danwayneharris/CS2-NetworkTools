# Terrain first-batch regressions

Authorized by Dan after reviewing labeled terrain/elevation v1.1. Preserve baseline
and use reduced toy playset, paused. Prepared position-based fixtures for the two
isolated roads, both rail branches and the highway mainline/ramps. Every suite case
uses the existing checkpoint/graceful-reload workflow and discovers new identities.

New terrain-regression.py wraps existing strict assertions without changing their
thresholds. It reports sampled grade, horizontal length and centerline-ground offset
before/after, including failures. These diagnostics are not pass/fail grade policy,
clearance certification or claims about unchanged terrain. Added analytic tests for
constant grade, reversed traversal and degenerate horizontal tangent handling.

No mod or bridge source changes or deployment. Pending live results below.

## First four selections completed

Each case reloads the checksummed v1.1 package and runs strengths 0, 0.5 and 1,
then independently inspects full-strength permanent Apply. Strict passes: hill
road, crest/dip road, highway mainline across both junctions. Rail high branch
retains all directed transitions and exactly matching preview/permanent curves,
but fails the unchanged 1 mm fixed-center threshold: 23.5183 mm drift. Elevations,
topology and unselected curves pass; no thresholds were changed.

Hill road horizontal length 576.00 -> 507.57 m, sampled peak grade 24.459 -> 27.264%.
Crest/dip road length 720.00 -> 664.13 m, grade 20.929 -> 22.954%. Sampled maximum
centerline-minus-terrain offset there rises 3.411 -> 5.373 m. This is not a floating
road verdict: current queries do not establish rendered support/terrain alignment.
The source-grade extremes are already present before smoothing. Grade measurements
are diagnostics, not a universal road/rail acceptance policy.

Eight existing regression guard tests and three analytic metric tests pass.
No mod changes/build/deploy. Native tests exercise the previously deployed latency
build. Bridge remains unchanged. Additional branch/ramp selections underway.

## Additional branch selections

Other rail branch and both ramp paths strictly pass. Seven total cases: six PASS,
one fixed-center FAIL. Preserve all detailed reports; do not aggregate that into
a generic all-pass label. Each begins from saved original v1.1, not the previous
case's modified city. Terrain metrics are in terrain-regressions.md and suite plots.

Dan explicitly requested a subagent assessment of centimeter-scale error while
testing continued. The read-only assessment found deliberate native NodeAlignSystem
realignment is a plausible cause, established for the earlier 3 mm case but not yet
replayed for the 24/30 mm cases. Low player priority if derived-center-only and
nonaccumulating; retain measurements and strict functional invariants. No global
tolerance change or precision-focused detour was made.

Final ramp-out result checkpointed as
CitiesIIAgentBridge-terrain-v11-ramp-out-tested-20261001-070913-7f660e0f.cok.
Independent comparison after graceful reload passed: exact region geometry fingerprint
and directed semantic connections at all three junctions match the pre-save capture.
The game is left paused on this ramp-out checkpoint; other layouts are baseline,
because each case began from a fresh baseline. No further network edits after reload.
Original v1.1 hash remains protected by the existing save checks.


Dan proposed a future user-facing error tolerance, temporarily defaulting to 5 cm.
Record as a design option, not an implemented setting or changed acceptance criterion.
Define its scope first (derived junction-center alignment versus authored pins,
elevations, geometry or connections). Continue the current testing trajectory;
no threshold was changed for this proposal.
