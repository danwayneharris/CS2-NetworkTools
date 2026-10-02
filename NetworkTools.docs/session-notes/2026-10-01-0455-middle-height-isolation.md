# Isolating the native middle-height limiter

Installed Game 1.6.2f1 GeometrySystem.FinishEdgeGeometryJob.LimitMiddleHeights,
lines 1809-1827, computes endpoint-derived middle-height bounds using each half's
horizontal length and the prefab slope limit. If the two constraints conflict,
it replaces the interval with the average of its inverted endpoints. Otherwise,
it contracts the feasible interval toward its midpoint using road width. It clamps
the current shared middle Y to that interval and translates the neighboring handles
by the same vertical amount. This is not a bound on every cubic derivative.

Read-only live prefab 16071:1 NetGeometryData confirms m_MaxSlopeSteepness=0.2,
width=8 and the captured SmoothElevation/ground flags. The inspected ground branch
calls LimitMiddleHeights, not StraightenMiddleHeights. No debugger writes/breakpoints
or simulation steps were needed. No native game source is copied into the repo;
the small mathematical bounds model is retained as an analysis script.

Before highway slope, left/right generated ramp boundaries have horizontal half
lengths about 3.04+23.54 m and 2.99+22.60 m. Outer drops are 5.45/5.47 m, implying
average grades 20.52%/21.38%. Even the full remaining boundary length cannot meet
the 20% prefab setting. The left middle interval is inverted: lower 615.821154,
upper 615.683206 m. Native fallback fixes it to 615.752180 m; capture is 615.7522 m.
Right predicts 615.672130 m; capture is 615.6721 m. Because the interval collapses,
this prediction is independent of the uncaptured pre-limiter middle height.

After highway slope, outer average grades are 7.05%/7.30% and the interval is
feasible. Width-contracted lower bounds predict 612.471367/612.478286 m; captured
middle heights are 612.4714/612.4783 m. These match the active lower boundary;
the pre-limiter values were not captured, so the exact amount moved at this stage
is not claimed. All numerical residuals are insignificant under the 5 cm policy.

This isolates a decisive downstream stage reproducing the bad middle heights.
It does NOT fully attribute the upstream high cut-end anchors to node averaging,
FlattenNodeGeometryJob or retained endpoint heights. The large junction cutback
and high boundary start leave too little usable edge for the authored target.
Removing the limiter would not establish a safe junction or a correct surface.
A correction needs compatible generated-boundary conditions, usable length and
lane-grade verification, not solely an authored Curve fit or a terrain sample.

Validation: five independent analytic tests pass (incompatible bounds, feasible
contraction, traversal reversal, elevation translation, horizontal-only length).
Both native captures replay successfully. Raw reports: artifacts/native-middle-
before.json and native-middle-after.json. No production code or live network changed.
Game remains on the previously verified paused post-highway checkpoint.
