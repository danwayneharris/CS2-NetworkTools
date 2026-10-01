# 2026-10-01 0215 — Offline Constant Slope counterfactual

Replayed the last captured Curve Apply output as Constant Slope input. The endpoints
are a three-edge junction and a dead end, so neither qualifies for the optional
neighbor-tangent smoothing despite both UI boxes being checked. Used compiled
production SlopeLinearTransform, EdgeState ratio calculation and AlignEndpointHeight,
with a managed-array transcription of PathData stationing and TransformPipeline
node averaging. The historical variant substitutes the end-handle formula from
merged main 4b5fc1c and omits the newer alignment. No native ECS rebuild is emulated.

Run from root (after a non-deploying compile if production source changed):

```powershell
dotnet run --project NetworkTools.Slope.Tests -- --replay NetworkTools.docs/session-notes/captures/offramp-grade-audit/last-curve-apply.log NetworkTools.docs/session-notes/captures/offramp-grade-audit/007-get_network_edges.json NetworkTools.docs/session-notes/captures/offramp-grade-audit/offline-comparison.json
```

| Variant | First edge grade range | Grade jump into next edge |
| --- | --- | --- |
| Pre-change main equations | -7.3501% to -6.8195% | +0.5304 percentage points |
| Endpoint-handle distance correction alone | -7.1375% to -6.8195% | about -0.00018 percentage points |
| Current, with endpoint alignment | -8.0230% to -6.8195% | about -0.00018 percentage points |

Current output reproduces all five captured permanent cubics within 0.00006104 m.
That agreement is an assertion in the replay; a >1 mm discrepancy fails it. The
existing 96 production-formula assertions also pass. No game queries, mutations,
build deployment or restart during this experiment.

Conclusion: nonconstant grade predates these changes, and the handle correction
improves continuity, but our recent endpoint alignment contributes to the deeper
interior grade dip. It raises the first endpoint and its handle by about 0.52545 m
relative to the unaligned fit to retain the junction endpoint/node height relation.
The other end stays essentially fixed, forcing more descent within that cubic.
This is a demonstrated tradeoff/partial regression in vertical-profile quality,
not evidence we should disable the consistency fix. Old equations alone are not
an old-build in-game A/B: native reconstruction may have changed that old request.
We cannot certify the final old rendered result from this replay.

Deferred work is recorded in slope-improvement-plan.md. No production code changed.
Offline confidence is high for reproducing this captured profile and isolating the
alignment contribution, lower for historical native geometry, and absent for an
old-vs-new visual comparison or vehicle traversal. The user's tolerance for existing
Slope weaknesses does not mean this newly identified contribution is already accepted.
