# Combined smoothing architecture checkpoint

Baseline: main 5cc24c0 (PRs 13 and 14). Experimental Debug-only feature; this note
records the implementation plan, not delivered functionality.

## Existing ownership and concrete gaps

RoadShapeToolSystem.PathData captures original EdgeState/NodeState and authored
comparison values. JobMethods waits for readers, refreshes changed preview inputs,
and schedules a job with copied configuration. Jobs creates temporary mutable
arrays; Preview/Apply re-evaluate from originals. Update.CandidateAllowsApply and
execution compare original/revision/submission identity. Retain those boundaries.

CurveSmoothTransform fits XZ and retains Y. SlopeLinearProfileTransform measures
current cubic XZ arc lengths and fits one outer-endpoint vertical span. Combining
those steps inside one job is feasible, but the one-span fitter would move pinned
interior heights. A section-aware vertical wrapper is required, without changing
ordinary Constant Slope semantics.

SurfaceProfileTransform reads the selected junction incident from CurveLookup
(original authored curve). In combined mode that must be the candidate curve and
candidate adjacent node position. Native reference cut positions can also be from
old XZ. Failed initial correction must not be mistaken for proof that no supported
correction is needed after horizontal movement; collect current native references
before making that decision. Existing correction deliberately supports only a
restricted terminal ground ramp, not arbitrary pinned/interior-junction paths.

PreviewProbe currently calls surface, endpoint-junction and interior-junction
observers independently. Their modes were mutually exclusive. Combined mode needs
coordinated ordering: settle surface for the current horizontal candidate, validate
junctions on that complete candidate, and if junction search changes horizontal
parameters discard dependent surface references/convergence. Every accepted stage
must carry current input revision and final submission. A new submission invalidates
old native acceptance even with identical user inputs.

## Proposed pipeline and memory

Immutable authored snapshot -> temporary horizontal candidate -> recomputed XZ
station distances -> constrained vertical sections -> supported surface correction
-> complete candidate trace/probe -> native observation -> accepted submission.
Each job owns and disposes its temporary arrays. Native reference storage remains
system-owned and fenced by the existing job completion. No temporary entity IDs
survive native rebuilding. Apply reconstructs from the same originals/configuration
and accepted reference values; independent output inspection remains mandatory.

Configuration changes increment the input revision. Horizontal candidate-search
changes invalidate vertical/surface dependencies. Native surface iterations retain
horizontal parameters and originals. Neither feedback loop may accept evidence
from a previous complete candidate. Use a global bounded attempt/time budget in
addition to individual observer limits; report exhaustion rather than continue
indefinitely. Do not remove the existing completion fences.

## Pin and grade contract

Outer nodes, explicit splits and interior junction centers remain XYZ-fixed.
Unpinned interior Y may move. Selected/unselected topology and all unselected
authored curves remain fixed. Degree-two split tangents retain existing planar
continuity. Vertical sections use the existing offset-aware constant-grade fitter.
Boundary handle substitution is the existing means of grade transition; no new
transition length policy is invented. At a pin preserve an existing common grade
when available. If incoming/outgoing original grades conflict and no established
policy resolves them, reject explicitly rather than average by guess. Junction
selected attachments retain their individual original grades (relative directions
are not forced to match across a branch junction). Validate native connectivity.

The constrained fitter must take explicit anchors and endpoint-grade constraints;
its API must not decide what a user pin means. Test invalid anchors, reversal,
nonuniform lengths, pinned sections and equal-grade continuity before integration.
Ordinary independent Curve/Slope continue using their original paths.

## Focused preparatory work

1. Factor the current ordinary surface fit call into one job helper, unchanged
   inputs/outputs, only when needed by combined dispatch. Existing regressions
   establish behavior preservation before the new branch is enabled.
2. Add a pure constrained-section profile helper and tests, using VerticalLinearProfile
   for each span. Caller-owned scratch/output; discard all output on any failure.
3. Add explicit combined candidate/dependency state only as needed to coordinate
   existing observers; test stale revision/submission and retry invalidation.
4. Preserve stage-specific traces and expose an opt-in provider/UI configuration.

Defer global override ownership, broad parameter policy, file rearrangement,
performance optimization, generalized graph editing and Release promotion. None is
required to express the new candidate pipeline. If surface/junction coordination
cannot be bounded without a major prerequisite, retain tested independent work
and report that boundary; do not claim a combined native success.

## Validation and evidence

Offline calculations, native preview, permanent Apply, repeat/reload, Release
compilation, human visuals and vehicle traversal remain distinct. Geometry position
tolerance is 5 cm; topology, lane identity, freshness and material accumulating drift
remain strict. Constant grade everywhere on generated surfaces is not promised.
