# Interior-junction elevation limits

NT-021 adds an experimental **Debug-only combined Smooth Curve + Constant Slope**
control. Offline geometry, production-transform tests, and a non-deploying Debug
package build passed during implementation on October 3, 2026. Native preview,
Apply, reload, incident-lane behavior, and visual review for these new limits are
**pending**. Existing native evidence for Unlimited is not certification of the
bounded solver. See the [plan](plans/NT-021-junction-elevation-limits.md) and
[combined smoothing review](combined-smoothing-review.md).

## Features and usage

Enable combined slope fitting in Smooth Curve, then choose:

- **Allow interior junction elevation modification**, initially enabled.
- **Unlimited**, initially enabled, preserving the existing solver path.
- With Unlimited disabled, **Maximum junction elevation change**: 0–20 meters.
  The slider advances in 0.5-meter steps; numeric entry permits finer values and
  displays three decimal places. Its initial finite value is 5 meters.

Disabling movement or entering zero holds each eligible junction at its original
Y. This fixes height alone; it does not preserve the junction's old tangent grades.
A finite value bounds upward and downward movement relative to the operation's
captured original node height. The solver chooses the height within that interval.
It is not a target elevation or a blend between two fits.

Outer endpoints and explicit split/pin constraints retain precedence. Junction XZ
stays fixed. Independent Curve preserves elevations as before. Limits can prevent
a globally constant grade. Settings survive tool changes and world recreation
within the game process, but are not written to persistent preferences; a fresh
launch starts with movement allowed and Unlimited again.

The NT provider's existing combined configuration accepts `allowJunctionElevation`,
`unlimitedJunctionElevation`, and `junctionElevationLimit`. The limit must be a
finite number in [0,20], including when its field is currently hidden. Requests
validate all supplied options before mutation. UI and provider use the same domain
predicate; parameter range labels alone do not enforce this contract.

## Architecture

[CombinedLinearProfileTransform](../NetworkTools.Mod/Systems/Tools/RoadShape/Transforms/CombinedLinearProfileTransform.cs)
constructs bounds from immutable `NodeState.OriginalPosition.y` after horizontal
fitting. [CombinedProfileInputs](../NetworkTools.Mod/Systems/Tools/RoadShape/Transforms/CombinedProfileInputs.cs)
retains the original difference between each curve endpoint and its associated
node height, and obtains station lengths from candidate horizontal curves.

Unlimited calls the original `SectionedVerticalProfile.Fit` exactly. A finite
configuration with no eligible interior junction also uses that path. Otherwise,
[BoundedVerticalProfile](../NetworkTools.Mod/Geometry/BoundedVerticalProfile.cs)
solves all selected node heights and endpoint grades together. Bounds are part of
the solve; accepted curves are never repaired by clamping their node heights.

### Objective and constraints

Let total horizontal length be T and normalized station u=s/T. Segment i has
normalized length l_i=L_i/T. Its cubic Hermite polynomial p_i interpolates
A_i=h_i+o_i,start and D_i=h_(i+1)+o_i,end, where o are the captured original offsets.
The grade unknown q=dy/du equals T times physical grade dy/ds. Minimize

```text
E = sum_i integral over segment i of (d²p_i / du²)² du
```

Two-point Gauss quadrature evaluates this quadratic exactly because each second
derivative is linear in station. Heights are translated relative to the first
anchor; the quadratic system is diagonally equilibrated before solution. This
normalizes distance and variable scaling without adding a tunable penalty weight.

Eligible junction heights have interval constraints. Ordinary free interior nodes,
including junctions with a zero-width height interval, share one incoming/outgoing
grade. Existing explicit fixed anchors keep their independent incoming/outgoing
grade-match equalities. The adapter retains the existing rejection of conflicting
grades at an ordinary explicit split; the primitive itself permits the separate
authored grades that a fixed junction legitimately has.

The bending-energy nullspace consists of affine profiles. Shared grades couple
free interior nodes, and fixed endpoint heights remove that freedom within every
span separated by explicit fixed anchors. Thus the supported problem has a unique
primary minimizer: a secondary preference toward Unlimited cannot select a
different equally optimal result. No epsilon-weighted approximation to a
tie-break is introduced. A poorly conditioned solve rejects instead of inventing
a regularized answer.

### Numerical solve and emitted curves

A deterministic feasible active-set method activates the first bound reached along
a Newton step and releases the largest wrong-sign multiplier, resolving exact ties
by variable index. Cholesky factorization rejects normalized pivots below 1e-12.
The iteration budget is 8N+32 for N free variables; exhausting it is a failure.
Acceptance requires normalized projected KKT residual at most 1e-9 and feasible,
finite heights. KKT conditions test whether free variables are stationary and
whether active bounds have the correct gradient direction.

A free-subproblem KKT check decides when a bound can be released. Using only a
small-step test was found to repeat expensive solves on amplified roundoff despite
already stationary free variables; the corrected implementation avoids that
performance failure. Outputs are staged and copied to caller buffers only after
all checks succeed.

Emitted Bézier controls use the original endpoint offsets and the candidate's
actual horizontal handle lengths:

```text
B = A + startGrade * startHandleLength
C = D - endGrade   * endHandleLength
```

This preserves shared physical endpoint grades. The station-Hermite objective is
a **surrogate** for the emitted curve's interior physical curvature when horizontal
parameter speed is nonuniform. It does not promise constant rendered grade, native
surface smoothness, or terrain conformity.

The job snapshot includes the effective permission/limit, and changing those
parameters invalidates candidate revision. Existing original-input/freshness and
native-evidence gates remain active. Bounds are checked again after surface
correction. Accepted node movement feeds the existing incident-edge adjustment,
which composes both endpoint/handle translations from the original curve rather
than overwriting one endpoint's edit with another.

## Testing and observed results

These are implementation-stage results, not native qualification:

| Check | Result |
| --- | --- |
| Standalone geometry executable | Passed; 509 bounded-profile assertions plus existing suites |
| Compiled production Slope harness | Passed; 50 combined-transform assertions, reported by the coordinating implementation run |
| Full Debug `-Build -PackageOnly` | Passed, including postprocessing and UI; no local-mod deployment |
| Live bounded preview/Apply, reload, lane and visual checks | Pending |

The 509 assertions include lower/upper bounds, zero allowance, simultaneous active
junctions, exact fixed anchors, prescribed grades, nonzero curve/node offsets,
unequal handle lengths, reversed traversal, height translation, normalized station
rescaling, deterministic evaluation, failure atomicity, and numerical/capacity
rejection. One analytic fixture constrains y(1)=1 between y(0)=y(3)=0: the coupled
minimum-bending fit has y(2)=7/8. Clamping the unrestricted flat fit would incorrectly
leave that neighboring node at zero. A sparse 64-segment fixture verifies the KKT
release correction within 10 iterations; the preceding implementation required 12.

Reproduce without deploying or accessing the game:

```powershell
dotnet run --project NetworkTools.Geometry.Tests -c Release
.\scripts\test-slope.ps1
.\scripts\bootstrap.ps1 -OfflineTest
.\scripts\bootstrap.ps1 -Build -PackageOnly
```

The geometry executable's Release configuration is an offline .NET build; it does
not establish CS2 Release/Burst compatibility. PackageOnly writes
`artifacts/packages/Debug`, including `build-manifest.json`. Identify each review
package by that manifest and its hashes, then compare against the actual loaded
build before live work. Ordinary `-Build` still deploys and requires a closed game.

### Performance evidence

After the KKT-release correction, .NET 8 Release benchmarks on the development
machine used 40 repetitions at supported path lengths. These are scalar offline
solver timings, not game Mono frame-time measurements.

| Segments / constrained junctions | Median | p95 | Allocated per solve |
| --- | ---: | ---: | ---: |
| 16 / 1 | 0.038 ms | 0.041 ms | 29 KB |
| 16 / 15 | 0.152 ms | 0.162 ms | 93 KB |
| 64 / 7 | 1.077 ms | 1.253 ms | 0.75 MB |
| 64 / 63 | 6.058 ms | 6.784 ms | 4.43 MB |

An artifact-only copy with its cap raised measured 128 segments at 12.1 ms with
sparse bounds and 83.0 ms with all junctions bounded; 256 segments required 158.7 ms
and 1,181.5 ms respectively. The latter allocated about 258 MB per solve. Before
the KKT-release fix, the sparse 128-segment case took about 1,968 ms and allocated
652 MB. These findings justify keeping the supported bounded path at 64 segments
rather than treating the fix as permission for arbitrarily large selections.

Mixed 0.05-meter/300-meter spans succeeded in the benchmark. An extreme
0.01-meter/1e12-meter fixture rejects as ill-conditioned. Local raw measurements
and the artifact-only benchmark are retained under
`artifacts/bounded-profile-benchmark/`; they are ignored local evidence, not a
committed benchmark dependency.

## Limitations and repeated operations

The bounded solver rejects paths longer than **64 segments** with an explicit
capacity reason. This cap concerns the entire selected path when the bounded
solver is needed, not the number of junctions. Unlimited and finite configurations
without eligible junctions retain the existing path. Dense managed scratch is
allocated during each solve; candidate searches and frequent slider updates can
multiply both CPU and allocation costs even below the cap. Game performance still
needs observation.

A per-operation limit is not a persistent movement budget. If the original height
is 10, an unrestricted optimum is 0, and the limit is 1, the first bounded operation
can choose 9. After Apply recaptures 9 as the next original, another operation can
choose 8. The analytical test explicitly demonstrates this accumulation. It does
not excuse numerical drift: repeated evaluation of the **same immutable inputs**
must remain deterministic, every individual displacement must satisfy its bound,
and repeat/reload evidence must distinguish intended rebasing from unintended
movement. No finite-bound repeat-Apply idempotence claim is made. Unlimited's
existing repeatability expectations remain unchanged.

The implementation does not promote combined smoothing to Release, introduce
terrain/obstacle routing, relax connectivity validation, or establish correctness
from Anarchy disabling ordinary validation.

## Dan's review

Use a disposable copy of `bridge test - trumpet combined baseline.cok`, identified
by [toy-trumpet-combined.json](../scripts/fixtures/toy-trumpet-combined.json): baseline
SHA256 `D7724AA3A84E56BCBD617CCE5D364687445C22FAE175828A426BA0C00F3FD0EF`.
The `trumpet-mainline-short` selection runs from
`(-2041.438, 610.8842, -2321.53271)` to
`(-1824.39148, 616.549438, -2350.73315)` at strength 1.0, combined mode.
Resolve nodes by those baseline positions, not stale entity IDs. Confirm eligible
interior junctions are actually present; otherwise the finite configuration
intentionally exercises only the existing unrestricted path.

1. Record the loaded informational identity and deployed DLL/UI hashes, matching
   the intended review package. Preserve the untouched named baseline. This guide
   currently records offline/package evidence only, so live review begins here.
2. Reload the baseline and inspect Unlimited preview/Apply against the preceding
   stage. Record endpoint/split positions, junction XZ/Y, authored curves, native
   surfaces, and incident directed lanes.
3. Reload again, disable junction elevation modification, and compare the same
   selection. Junction Y should remain original. Repeat with movement enabled and
   a zero limit; height behavior should agree, without assuming old grades are fixed.
4. From separate fresh baselines, try a restrictive limit such as 0.5 m and a larger
   limit such as 5 m. Record the actual chosen deltas relative to each operation's
   original heights; a restrictive result need not have constant grade. Try a
   precise numeric value such as 0.125 m and verify the slider and entry agree.
5. Change the limit while a preview is pending. A previous candidate or delayed
   native observation must not enable Apply. Check explicit split precedence and
   clear rejection for incompatible grades, unsupported capacity, or numerical failure.
6. Inspect preview versus permanent Apply for every affected selected and incident
   edge, including directed lane continuity. Repeat Apply from the resulting state,
   recording each operation's baseline and cumulative displacement separately.
   Reload the saved result and compare again. Treat unexplained drift, stale output,
   topology changes, or connectivity regression as failures.
7. Toggle tools and reload a world within the same game process to check session
   preference retention. A fresh game launch should restore allowed/Unlimited.
   Record visual and vehicle observations separately from authored-curve agreement.

Expected: bounded geometry-selected movement with fixed junction XZ, exact explicit
anchors, coherent endpoint grades, and informative rejection. Native outcomes and
Dan's observations must be added with their actual build identities before claiming
live qualification.


## Native qualification on October 3

Clean deployed build `7eebfc49830a` (`1.5.7+g7eebfc49830a.clean.Debug`)
was independently identified through the live provider. Full Debug deployment and
all seven offline suites passed. See [compact evidence](session-notes/nt021-native-evidence.json).

The terrain v1.1 `rail-high-branch` selection at strength 1.0 and 0.5 m allowance
moved its interior junction exactly 0.5 m. Authored preview/permanent curves matched
exactly, directed rail transitions remained identical, and incident endpoint-pair
translations matched independently. A second operation permitted another 0.5 m
and passed the same checks. **This is 1 m cumulative movement, not idempotence.**
The per-operation budget is explicitly renewable; persistent/cumulative limits are
not implemented. Do not interpret either pass as satisfying an idempotent-operation
policy. Dan must review that distinction before accepting this experimental UX.

The short trumpet case passed but contains no interior junction. The full mainline
failed directed road-lane preservation in both bounded and unchanged Unlimited modes:
at ordinary degree-two node `(-1534.82361,624.103,-2388.344)`, one lane target changed
from lane 3 to lane 2. This remains a recorded existing validation gap, not a tolerated
geometric error. It is not safe to claim general road-lane preservation from the
passing rail case. No baseline saves were overwritten.

Replay the checksummed terrain baseline via `scripts/launch-toy-fixture.ps1 -Fixture
scripts/fixtures/toy-terrain-v11.json -ExperimentalFinishHeightPreparation`. This
visibly launches only; it does not deploy, close another game, or prove readiness.
The flag opts into the existing hash-gated finishing experiment; it is not enabled
implicitly by the new control. Run `live-regression.py` only after verifying the toy
session, and use the bounded rail fixture choices documented in the session note.

Ordinary UI, vehicle traversal, result reload, and Release execution remain unverified.

Disabled-movement native test also passed: junction delta zero, exact preview/Apply
curves and preserved directed rail connections. Review save: `CitiesIIAgentBridge-review-nt021-fixed-height-20261003-134806-ac400174`.
Use fixture cases `rail-bounded-half` and `rail-fixed-height` in
`toy-terrain-v11.json` for reproducible fresh-baseline tests.
