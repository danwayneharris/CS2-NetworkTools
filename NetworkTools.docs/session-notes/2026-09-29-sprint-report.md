# Autonomous sprint handoff ? 2026-09-29

Implemented and locally committed a reusable live regression workflow, ordinary
split-point smoothing, and Debug interior-junction smoothing. No push, PR or mod
publication occurred. Both development branches descend from the merged milestones.

## Implemented behavior

- Regression runner verifies baseline archive/hash and local geometry fingerprint,
  resolves fresh entities, checkpoints uniquely, polls bounded fresh previews and
  independently inspects permanent Apply output. It checks physical lane mappings
  and semantic directed transitions, not just connection counts.
- Ordinary split nodes stay pinned with shared planar tangent direction (G1).
  Selection/UI and guarded bridge nt_split controls are integrated. Enabling a
  hard split can change a kink even at strength zero; no-split zero remains identity.
- Interior junctions use constrained section fits and exact native connector-set
  validation. A bounded common rotation preserves the selected branches' relative
  angle while searching for a connection-preserving candidate. Unselected curves
  are not edited. Endpoint rail checks also reject added connections.
- Native fixture construction and checkpoint/reload helpers are retained in scripts.

## Observed results

| Case | Native preview / permanent Apply |
| --- | --- |
| Rail merge endpoint | Pass at 0.5/0.8 preview and 0.8 Apply, including tightened exact-set gate |
| Road four-way endpoint, highway on/off ramps | Captured successful Apply checks; later oracle adds lane/direct-join semantics |
| Rail two split points | Pass at 0, 0.5 and 1, including pin positions and tangent continuity |
| Road two split points | Pass at 1, including all 16 junction movements and U-turns |
| Interior four-way road | Pass at 0.5/0.8 preview and 0.8 Apply |
| Two interior highway merges | Pass at 0.5/0.8 preview and 0.8 Apply; five movements at each merge |
| Dedicated one-way slip lane | Native-built separate baseline; last two bypass edges through exit junction pass at 0.5/0.8 preview and 0.8 Apply |
| Interior rail merge | Connections and preview/Apply pass after +2/+3 degree rotations; strict test FAIL for 5.17 mm native node-center drift |
| Rail one split point | Pin/G1/connections pass; strict test FAIL for about 3 mm outer-junction drift |
| Non-merging rail crossing | Not tested: native bridge rejects the locked rail prefabs; no bypass added |

For passing cases, preview/permanent curve differences were zero and topology,
elevations, fixed-node thresholds, unselected curves and the implemented lane
mapping checks passed. These are bounded local observations, not vehicle routing
or human visual approval. The earlier fixed-handle and length-only interior rail
experiments failed; their commits/captures remain as evidence.

## Verification and confidence

Final executable geometry suite and 31 targeted Python tests passed. Debug build,
postprocessing, UI build and deployment passed. Bridge verify-api passed its native
adapter checks and 61-command contract. Release/Burst is unverified; interior
validation remains Debug-only. The small native center drifts were not hidden by
relaxing test tolerances. Offline NodeAlign replay predicted the earlier 3 mm drift
within 0.052 mm, but remains a limited double-precision model, not native equivalence.

Captures, tests, fixture hashes and the [scope audit](2026-09-29-sprint-audit.md)
provide the evidence. Human UI/visual validation and wider assets remain pending.

## Research and next decisions

The saved longer-route and side-bias plots distinguish length from side choice.
The centered-slider options are documented in the feature plan: coupled strength
with original geometry at center, or separate strength/bias controls. Neither is
silently shipped. Side orientation remains a UX decision.

The bounded architecture review proposes separate horizontal and vertical solvers
with recomputed arc-length stations for a future combined tool. No slope bug was
established and no speculative slope rewrite was made. Defer broad cleanup until
these regressions protect it. Junction-as-split is recorded separately: relative
branch-angle preference rather than forced shared tangent; currently rejected.

Next supervised checks: visually assess the split UI and interior results, supply
an unlocked non-merging crossing fixture, and decide the centered-slider semantics
and acceptable native center-drift policy. A production release also needs a deliberate
Release/Burst validation design and testing.

## Repository and game handoff

Key NT commits: 35e4295/ca7bc78 (split implementation), 92dae3f (interior rail
rotation), f1007c1 (interior road), 5284643 (two highway merges), de1283b (side-bias
research), 5df5084 (exact endpoint validation), 696f2f0 (slip fixture and test).
Bridge code milestone: 1afb1e1 (nt_split), with subsequent verification notes.
Full local history includes all experiments, including unsuccessful ones.

User-owned UI/package-lock.json remains unstaged; generated scripts/__pycache__
remains untracked. Common submodule unchanged. No unrelated work was discarded.

The game is paused on the toy world initially loaded from
`bridge test - rail smoothing breaks merge junction highway jank roads.cok`, now
containing the constructed and smoothed slip fixture. The final applied state is
saved as `CitiesIIAgentBridge-sprint-handoff-slip-applied-20260929-100209-cddaccc6.cok`.
Both original and pre-smoothing slip baseline hashes were reverified unchanged.
No known unsaved network changes remain after that checkpoint. Native save/load
and semantic state were used; no real city was loaded and no force-kill was used.
