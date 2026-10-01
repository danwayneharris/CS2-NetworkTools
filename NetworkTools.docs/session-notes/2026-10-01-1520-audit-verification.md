# Audit sprint verification and review handoff

Base: open fork PR #13, `dan/terrain-profile-sprint`, `ee9c3bc`. Sprint branch: `dan/audit-correctness-sprint`. Runtime source checkpoint: `77f8c64`; subsequent commits reconcile documentation and fixtures. Original checkout and its user changes were preserved. Bridge source was not changed.

## Implemented scope

- F03/F10: compose one incident-edge edit from both endpoint changes; preserve Node fields outside position.
- F04: path-search state includes incoming edge, retaining the existing prefab-dependent cost rule.
- F05/F06: explicit suite failures/incomplete execution, Connect preservation, and bijective preview/permanent matching.
- F02/F09: one authored-input baseline shared by calculation/submission/Apply; reject stale/deleted paths before gather or mutation; shared UI/provider gate.
- F01: Release explicitly disallows junction Smooth Curve because required native junction validation is Debug-only. Experimental surface-aware Constant Slope remains Debug-only.
- F08/F13: reject nonfinite parameter writes without state/notification changes; fail-closed generator diagnostics with source locations. Shared UI restores rejected optimistic float values.
- Real non-deploying offline entry point, current docs, historical successor links, retained diagnostics.

Every audit finding has a disposition in [audit-disposition](../audit-disposition.md). Deferred work includes cross-mod override ownership, large-city performance, broad packaging separation, universal parameter range policy, and broad refactoring. No instrumentation was removed merely for being experimental or Debug-only.

## Verification actually performed

[Compact machine-readable evidence](audit-verification-20261001.json) records revision/configuration/hash, assertion policy, fixture names, checkpoints and results. Raw query streams and build logs remain ignored under `artifacts/audit-*`.

- All **7 offline stages passed**: geometry, production-source path search (958 assertions), finite parameters (52), generator negative/configuration fixtures, compiled-production slope/incident helpers, original-input comparison (16), and Python oracle/runner suites. The initial run lacked Git metadata; the follow-up captured it successfully. These are not native UI tests.
- Full **Debug C#/postprocessing/UI/deployment passed**. Deployed DLL SHA256: `CB9213E6E829C1A50B70E243CCDCE26070BCFEDBFE168EC661211090F8DACCDA`.
- Full **Release build/Burst compilation passed (22 methods)**. Release native execution and its ordinary UI restriction remain unverified. Attempted package staging was not isolated; see the build note. The game was closed and full Debug was redeployed before launch.
- Native provider suite: **interior rail junction at full strength, two rail split points, road four-way branch, highway off-ramp all passed**, each from a verified fresh baseline. Preview/permanent authored curves agreed exactly; intended directed connections, physical lane mapping and unselected geometry checks passed. The interior fixed-node displacement was 2.99 cm, accepted by the 5 cm policy.
- Exact settled **offramp-only reversed Constant Slope repeat passed with zero node/control drift**, exact preview/Apply agreement and unchanged region fingerprint. Stale provider revision rejected.
- Native **road Connect passed**: two new segments, exact bijective preview/permanent curves, inherited prefab, preserved existing nodes/edges/topology. No traffic traversal claim.
- The first ramp repeat used the longer mainline-plus-ramp selection accidentally. Its repeat audit failed with metre-scale movement because it was a different operation; the result is retained, not relabelled a pass. Completed Apply independently preserved lanes/topology and matched preview. Reloading and testing the exact historical selection resolved this comparison error.
- Read-only review found no remaining concrete shared-Apply/oracle blocker. Sixteen changed guide files had no broken local link paths; remote links/fragments were not checked.

## Commands and effects

`bootstrap.ps1 -OfflineTest` compiles/runs the seven real suites and writes outputs; it does not deploy or contact the game. Some project preparation can rewrite the npm lockfile; inspect that diff independently. `bootstrap.ps1 -Build` builds, postprocesses, builds UI and replaces the local mod deployment; close the game first.

`run-provider-suite.py --run` checkpoints, gracefully restarts, loads a checksummed fixture baseline per case, selects/configures/previews and Applies. `exercise-tool-provider.py --run` verifies the current fixture fingerprint and checkpoint, then mutates the selected toy network. `check-terrain-reload.py` only queries. `audit-profile-repeat.py` only reads a capture and fails above the stated drift policy. See BOOTSTRAP and the runner docs for arguments; native saves are separately retained local fixtures, not bundled in Git.

## Remaining verification / why the PR is draft

Ordinary UI selection/Apply and rejected-input display were not exercised by provider calls. Native Release execution remains unqualified. F03's both-ended incident composition and F10's retained rotation have production-helper regressions, but no dedicated native two-ended/rotation fixture. Adversarial live original-entity replacement and rapidly changing external mods remain untested. The single-ended native Slope translation checks do not establish the two-ended case. Large-city search performance, vehicle traversal and human visual quality remain separate. These are explicit limits, not waived invariants.

## Dan's short review checklist

1. On `CitiesIIAgentBridge-review-surface-profile-settled-20261001-130556-32145a5a`, inspect the corrected off-ramp. Through ordinary UI, select **offramp-only**, Constant Slope, boundary smoothing off; Apply and reverse/reselect/reapply. Look for unchanged shape and absence of the prior discontinuity. Do not overwrite this baseline.
2. Load `bridge test - rail smoothing breaks merge junction highway jank roads`. Try full-strength smoothing through the interior rail junction, two split points, the road four-way branch and highway off-ramp. Check the UI Apply state, smooth slider/reselection behavior and lane continuity. The fixture coordinates are in `scripts/fixtures/toy-interior-full-strength.json` and `toy-highway-jank-splits.json`.
3. On `bridge test - terrain and elevation v1.1`, inspect the road Connect result checkpoint named in the final state below; existing roads should be unchanged. Separately exercise a Slope path with an unselected edge attached to two moving interior nodes, to supply the remaining native F03 fixture.
4. At a later supervised build switch, verify the Release junction restriction and ordinary nonjunction tools, then restore Debug before assessing the experimental ramp correction. Run actual vehicles when convenient; structural lane checks do not prove traversal.

## Final game state

Game left paused on `CitiesIIAgentBridge-review-surface-profile-settled-20261001-130556-32145a5a.cok`, city session `f8e6ac95ac7245699eecf35603657ac4`. Exact region geometry and normalized directed/physical lane mappings at all 37 shared nodes match the prior milestone after reload. No unsaved network edits followed this reload.

The passing Connect result was saved before leaving it as `CitiesIIAgentBridge-regression-before-reload-20261001-221943-30be23b5.cok`. All earlier case checkpoints are in the compact JSON. Original baseline saves were never overwritten. The deployed build is the Debug DLL hash above; no source changes followed its build.

## Publication

Draft [PR #14](https://github.com/danwayneharris/CS2-NetworkTools/pull/14) is stacked on open PR #13 (`dan/terrain-profile-sprint`). Base head was rechecked unchanged before creation. No merge or history rewrite was performed. Final handoff rechecked the same paused toy city session. Remaining native/manual checks above are the reason for draft status.
