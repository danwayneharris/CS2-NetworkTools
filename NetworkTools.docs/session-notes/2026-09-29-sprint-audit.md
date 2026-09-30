# Sprint evidence audit ? 2026-09-29

This audits the requested scope; it is not a declaration that every case passes.
Detailed experiments remain in `2026-09-29-0030-autonomous-sprint.md`.

| Requirement | Evidence / status |
| --- | --- |
| Work from merged milestones | Local ancestry rechecked: NT 2180aec and bridge f0d6882 are ancestors of current development heads. Both use dan/autonomous-regressions; no publish/push performed. |
| Preserve user changes | UI/package-lock.json remains unstaged. Common submodule untouched. Bridge working tree clean at audit. |
| Reusable live runner | scripts/live-regression.py, reload-toy-baseline.py, fixtures, tests and raw captures. Baseline hash/fingerprint, fresh identity resolution, session pin, unique verified checkpoint, bounded preview polling and exact submission token. |
| Independent permanent verification | Stable permanent observations after Apply; selected preview-edge coverage; topology/elevation/fixed-node/unselected-curve checks; lane composition and directed semantic transitions. |
| Rail/road/ramp cases | Captured rail endpoint, four-way road endpoint, on-ramp and off-ramp Apply results. Later semantic oracle handles direct joins and detects lane swaps and lost U-turns. |
| Interior junction addition | Four-way road and two-interior-merge highway pass. Rail connectivity and preview/Apply pass, strict fixed-center check fails at 5.17 mm drift. |
| Split-point math and UI | PlanarSplitTarget pointer implementation and managed wrapper, CurveSmoothTransform, Splits.cs, UI list, bridge nt_split. Reversal/strength/offset/invalid tests; live rail two-pin strengths 0/0.5/1 and road two-pin full strength pass. |
| Split limitations | One-pin rail preserves the pin, G1 and connections but has 3 mm outer-junction center drift. Human UI/visual approval pending. Junction-as-split remains a deliberately separate follow-up. |
| Non-merging rail crossing | Discovery completed; train prefabs locked, bridge rejects them, no unlock command. No crossing built/tested. Need suitable toy fixture or supported unlocked setup. |
| Slip lane distinct from highway ramps | Dedicated native-built one-way bypass saved separately; toy-slip-lane.json and sprint-20260929-slip-regression report pass at 0.5/0.8 preview and 0.8 Apply. Scope is its last two edges through the exit junction, not the whole cyclic route. |
| Negative/side-bias research | explore-longer-target.py and explore-side-bias.py with plots. Clarified side bias supersedes mandatory longer route. Center semantics and orientation choices documented; not shipped. |
| Slope/combined architecture | sprint-architecture-review.md reads current transforms/pipeline and proposes independent horizontal/vertical solvers with recomputed stations. No speculative slope rewrite. |
| Incremental history | Local commits include failed length-only search, curvature evidence, successful common rotations and all captures. |
| Confidence | Offline math has executable tests; native preview and permanent results have separate captures. No vehicle traversal, human visual, broad asset or Release/Burst claim. |

Before a release, resolve Debug-only validation behavior, evaluate fixed-center
policy, and run supervised UI/visual checks. Crossing coverage remains an explicit gap. The endpoint exact-set tightening passed live rail preview and Apply in
`sprint-20260929-exact-endpoint`, with zero curve error and no fixed-node drift.
Dedicated slip construction and regression are now captured and verified, with
no claim of broad slip-lane asset or traffic coverage.
