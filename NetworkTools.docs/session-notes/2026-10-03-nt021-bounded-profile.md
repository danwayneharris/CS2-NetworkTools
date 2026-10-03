# NT-021: bounded junction profile integration

Stage 2 starts from NT-001 8eb7b28 / draft PR #17. Pure solver and UI/domain
parameter wiring have nonoverlapping agent ownership; coordinator integrates the
production transform, validates after native surface corrections, and owns tests/docs.

Unlimited retains the existing exact profile path. Finite bounds constrain original
junction Y while XZ stays fixed. Zero fixes only height; an explicit split/pin still
retains its existing stronger grade constraints. No post-fit curve clamping.

Important mathematical limitation under investigation: re-baselining after Apply
changes the origin of a per-operation displacement interval. Repeated bounded
operations can intentionally approach a distant optimum in steps even with a fully
deterministic solver. This must be tested and distinguished from numerical drift;
no claim of finite-bound repeated-Apply idempotence is made yet.
## Pure solver, analytical verification, and performance follow-up

Implemented `Geometry/BoundedVerticalProfile.cs` and standalone tests. The solve
minimizes the exact integrated squared second derivative of station-Hermite cubics,
with station normalized by total horizontal length, translated height origin, and
diagonal equilibration. Shared grades couple free nodes; explicit anchors preserve
their independent grade matches. Height-only zero bounds do not inherit authored
grade constraints. Fixed endpoint heights remove the affine nullspace, yielding a
unique primary minimizer; no epsilon tie penalty or post-fit height clamp is used.
Outputs remain untouched on failure. Actual Bézier handles preserve physical
endpoint grade using their horizontal lengths; the objective remains a station
surrogate for interior emitted/native curvature.

The first implementation passed 173 assertions, but benchmark work found a severe
performance problem outside a prudent interactive envelope: sparse bounds on 128
segments took about 1,968 ms and allocated 652 MB per solve. Releasing constraints
only when the Newton step became tiny repeatedly solved already-stationary free
subproblems because conditioning amplified roundoff. The fix tests free-variable
KKT residual before releasing a wrong-sign active multiplier. A dedicated regression
converges in 10 iterations where the prior code needed 12. The supported bounded
path cap is now 64 segments, with distinct capacity rejection rather than an
unfinished candidate. No game process was touched; the long task-owned benchmark
process was stopped and an artifact-only uncapped copy was used for later measurements.

Latest standalone geometry run passed **509 bounded assertions**, including atomic
failure/capacity outputs and the KKT regression. Analytical cases cover active
upper/lower and coupled bounds, zero height intervals, fixed anchors and grades,
nonzero offsets, reversed traversal, unequal handles, uniform station scaling,
invalid inputs and ill-conditioning. The coupled three-segment test proves that
refitting the adjacent free height to 7/8 is necessary; clamping an unrestricted
flat profile would leave it incorrectly at zero. Rebased-operation bounds explicitly
allow a junction to move 10 -> 9 -> 8 under separate 1-meter budgets; this is not a
promise of idempotence or an excuse for unexplained numerical/native drift.

Forty-run offline .NET 8 Release benchmarks after the fix measured:

| Segments / bounded junctions | Median | p95 | Allocation per solve |
| --- | ---: | ---: | ---: |
| 16 / 1 | 0.038 ms | 0.041 ms | 29 KB |
| 16 / 15 | 0.152 ms | 0.162 ms | 93 KB |
| 64 / 7 | 1.077 ms | 1.253 ms | 0.75 MB |
| 64 / 63 | 6.058 ms | 6.784 ms | 4.43 MB |

Artifact-only uncapped measurements: 128 sparse/all-junction medians 12.1/83.0 ms;
256 sparse/all-junction medians 158.7/1,181.5 ms, with ~258 MB for the latter.
All these post-fix benchmark solves succeeded. Mixed 0.05/300-meter lengths also
succeeded; the test's extreme 0.01/1e12-meter case rejects ill-conditioning. These
are not game Mono timings, and searches may multiply solve costs. Local raw JSONL
results remain in `artifacts/bounded-profile-benchmark/`.

The coordinator reported 50 production combined-transform assertions passing and
a complete non-deploying Debug `-Build -PackageOnly` build passing, including
postprocessing and UI. Native bounded preview/Apply, directed-lane checks, repeated
Apply/reload, and Dan's visual review remain pending. The new durable
[junction-elevation guide](../junction-elevation-limits.md) describes usage,
mathematical constraints, numerical limits, actual evidence, and numbered review
steps on the checksummed unsmoothed trumpet baseline. Unlimited and finite settings
with no eligible junction continue through the existing solver unchanged.
