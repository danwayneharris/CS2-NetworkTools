# Terrain and elevation regression checkpoint

## Baseline and scope

Dan's `bridge test - terrain and elevation v1.1` is stored locally as
`bridge test - terrain and elevation v1%2E1.cok`. Its checksummed fixture is
`scripts/fixtures/toy-terrain-v11.json`. Position-based discovery survives new
entity IDs after loading; a region-wide geometry fingerprint guards every run.
The four layouts are west of the old flat examples: road hill jank, peak/valley,
rail merge and highway ramps. The highway mainline is two-way, unlike our older
one-way lane-math fixture. Preserve the original package.

## Run

Use `scripts/run-provider-suite.py --runner terrain-regression.py` with the usual
explicit `--run`, `--save-root`, fresh `--output`, and repeatable
`--case toy-terrain-v11.json:hill-road` arguments. Cases are `hill-road`,
`crest-dip-road`, `rail-high-branch`, `rail-low-branch`, `highway-mainline`,
`highway-ramp-in` and `highway-ramp-out`. The suite checkpoints and gracefully
restarts to the baseline before each case. It never force-kills or changes speed.

The terrain wrapper preserves the existing runner's strict assertions. It adds
sampled cubic grades (101 parameter samples per edge), horizontal lengths and
nine terrain samples per selected edge, both before and after. The latter is
centerline minus terrain height, NOT structure clearance or a floating-road test.
Native rebuilding may alter terrain itself. Missing/failed measurements stay errors.

`test-terrain-metrics.py` checks analytic constant/reversed grades and degenerate
horizontal tangents; `summarize-terrain-suite.py` produces JSON and optional plots.
`check-terrain-reload.py` compares region geometry and semantic junction connections
across reload using geometry-derived labels rather than entity indices.

## First live pass: October 1, 2026

Every selection reached ready previews at 0, 0.5 and 1.0. Independent permanent
inspection followed Apply at 1.0. The detailed independent connection/curve
comparison is at that final strength, not a claim of full oracle coverage for
every intermediate slider position.

| Selection | Strict result | Horizontal length, m | Peak sampled grade, % |
|---|---|---|---|
| Hill road | PASS | 576.00 -> 507.57 | 24.46 -> 27.26 |
| Crest/dip road | PASS | 720.00 -> 664.13 | 20.93 -> 22.95 |
| Rail high branch through merge | Fixed-center mismatch | 507.99 -> 489.87 | 47.12 -> 46.07 |
| Rail low branch through merge | PASS | 488.62 -> 481.83 | 47.12 -> 46.07 |
| Highway mainline through both junctions | PASS | 570.50 -> 556.03 | 26.93 -> 27.90 |
| Highway ramp-in path | PASS | 352.94 -> 345.03 | 21.46 -> 22.27 |
| Highway ramp-out path | PASS | 494.62 -> 466.45 | 31.09 -> 37.14 |

All seven preserve captured directed connections/lane composition, topology, node
elevations and unselected curves; selected/incident preview and permanent curves
match exactly. High rail's fixed junction center moved 23.5183 mm, exceeding the
unchanged 1 mm assertion. It remains a strict FAIL, distinguished from connection
loss. Existing extreme grades precede smoothing and are not newly invented failures.
Crest/dip maximum sampled centerline-ground offset grows 3.411 -> 5.373 m; human
visual inspection is needed to determine practical appearance and support.

Evidence: `session-notes/captures/terrain-v11-first-suite` and
`terrain-v11-branches-suite`; each has JSON summaries, plots and raw captures.
These are observed local invariants, not a universal safety certificate.

## What we learned / next work

Preserving elevations can steepen a shortened horizontal route. Curve smoothing
alone is not slope smoothing, terrain following or grade-limited routing. These
captures motivate an explicit future vertical-profile policy, without changing
current semantics during this testing pass.

For centimeter drift, prioritize cause and consequences over magnitude alone.
Installed NodeAlignSystem recalculates centers from incident curves. Earlier
3 mm alignment was reproduced offline; the larger 24/30 mm cases still need replay
and accumulation checks. Keep functional connection/topology checks strict. Do not
apply a blanket under-5-cm tolerance to authored pins, heights or clearance.

Still unverified: human visual quality and actual vehicle traversal, reverse
selection on these new fixtures, ordinary splits on these slopes, bridges/tunnels,
non-merging crossings, native structure classification and broad terrain-clearance
coverage. This pass uses the deployed Debug prototype, not Release/Burst execution.
Bridge implementation and mod geometry code were not changed by this testing work.

## Persistence and deferred tolerance option

The final ramp-out result was saved to the unique checkpoint
`CitiesIIAgentBridge-terrain-v11-ramp-out-tested-20261001-070913-7f660e0f.cok`.
After graceful reload, the region geometry fingerprint and semantic directed
connections at all three junctions exactly match the pre-save capture. Evidence:
`session-notes/captures/terrain-v11-final-before-save` and
`terrain-v11-final-after-reload`. This verifies persistence of this result, not
repeated-Apply accumulation of the high-rail center drift. The game is paused on
that checkpoint; other layouts retain their original baseline geometry.

Dan suggested a future user-facing tolerance with a proposed 5 cm default. Keep
this as an option for discussion after defining which error it controls. It has
not been implemented and did not alter any assertion in this pass. In particular,
it must not silently permit lost connections or conflate native center alignment
with movement of authored pins or elevations.
