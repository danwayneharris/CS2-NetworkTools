# Junction flattening isolation ? 2026-10-01 05:05 PDT

## Question and result

Continue isolating the cutting/height-limiting stage before designing a correction.
The captured pre-highway ramp distortion is reproduced by native cutting/offset
math, pairwise junction flattening, and the already isolated middle-height limiter.
No production change or deployment was made. No live network mutation was needed.

## Source and method

Installed Game 1.6.2f1, `Game.Net/GeometrySystem.cs`: shared-height generation
119?160; split/endpoint substitution 480?524; CutCurve 641?675;
FlattenNodeGeometryJob 1505?1671; height-map application 1725?1756;
LimitMiddleHeights 1809?1827. Source lives in the separate cs2-decompile repository.
Game.dll SHA256: AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.

`scripts/prepare-native-cut-replay.py` builds pure native-math debugger expressions
from saved captures. They construct local curves and call installed math helpers;
there are no EntityManager writes or live identity dependencies. They reproduce
node projection/cutting, derived-height substitution, middle adjustment and lateral
offsets, then locate the captured boundary start in XZ. This last step deliberately
uses observed cut locations: it is NOT an independent prediction of junction cutback.
Initial expressions used compound assignments unsupported by the evaluator; they
were rejected before execution. Explicit assignments worked.

Scope is the verified SmoothElevation highway family, zero middle offset/node
flatness and the observed permanent same-layer junction. Supplemental endpoint
snapshots use the full captured network and the verified highway height branch.
The generator is an experiment helper, not a general native-geometry emulator;
Temp retention, mixed merge layers and other prefab flags are not covered.
Unconditional cut endpoint reset is harmless in this fixture's untrimmed case
because the original endpoint already matches the node height.

Read-only live prefab checks confirmed all three incident slope limits are 0.2.
Native pure-math results matched captured endpoint XZ within 0.00023 m, allowing
height differences to be attributed to subsequent stages at the same locations.

## Observations

Before highway slope, native cut/offset math gives ramp start heights
612.8344/612.8525 m; permanent boundaries are 616.428467/616.446533 m.
The approximately 3.594 m rise is therefore downstream of cutting alone.
Outgoing highway initial heights 619.735/619.8087 m fall to
616.7718/616.8455 m; incoming highway stays around 615.7352 m.
After highway slope, the equivalent ramp rise is only about 0.763 m.

The new restricted `scripts/replay-junction-flattening.py` uses the pre-flatten
heights, observed XZ, shared node height and actual prefab limits. One relaxation
pass predicts all six permanent boundary start heights within 0.00004 m of capture.
The fixture is `NetworkTools.Geometry.Tests/Fixtures/junction-height-stage.json`.
Labels are historical entity identifiers, not identities to reuse in a new world.
The 5 cm tolerance remains the acceptance threshold; tiny replay residuals are
not bugs to chase.

Combined with the previous limiter replay: the large junction leaves roughly
26 m of usable ramp boundary, flattening raises its start, and the required
20.5?21.4% average drop exceeds the 20% prefab setting. The native middle-height
bounds collapse to a fallback. That does not bound the cubic's maximum derivative,
so captured generated lane peaks of 27?32.5% are consistent with this mechanism.

## Reproduction and verification

```powershell
uv run --no-project python scripts/replay-junction-flattening.py NetworkTools.Geometry.Tests/Fixtures/junction-height-stage.json --output artifacts/junction-flattening-replay.json
uv run --no-project python scripts/test-native-middle-height.py
```

The first is a captured-case replay; the second supplies analytic limiter checks.
For native expression regeneration (requires ignored local raw evidence):

```powershell
uv run --no-project python scripts/prepare-native-cut-replay.py artifacts/ramp-layers-highway-before/layers.json --output artifacts/native-cut-before-expressions.json
```

Use `--edge-index 57041` for the outgoing highway and `--edge-index 59232 --reverse`
for the incoming highway in this historical capture. Expressions do not themselves
execute; they need the installed Unity debugger and the matching game version.
Raw generated expressions and reports remain in ignored artifacts.

Confidence: strong captured-case attribution for permanent boundary heights and
middle-height constraints; no general prediction of arbitrary junction cutback,
no newly tested preview equivalence, no vehicle traversal or fresh human visual
validation. Terrain deformation and rendered mesh details are not ruled out as
additional causes of visible jank.

## Implication and preserved state

Prototype feasibility against generated cut-boundary conditions and remaining
span before changing the fitter. An exact authored constant grade need not yield
an exact generated constant grade when neighboring roads impose different heights.
Do not disable native constraints or infer a terrain-sampling feedback fix.

Game remains on the post-highway checkpoint
`CitiesIIAgentBridge-regression-before-reload-20261001-113726-dcd2c49b.cok`.
No game modifications this experiment. The preceding verified state was paused,
with geometry fingerprint
`61f84c10529ae1239f04b5240d37b22d58775ce9e887cdb96673183b6f12ff22`.
Debugger has no held suspension or breakpoints and remains attached. A prior
attempt to detach was rejected by automatic approval review as potentially
resuming the game; it was not retried or bypassed.

Final verification: captured flattening replay passed; all five analytic limiter
checks passed; `git diff --check` passed. Read-only bridge query confirmed the same
citySession `9a5a41524a764d25a68481fbf5d34e6c`, population 0 and selectedSpeed 0.
