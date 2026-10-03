# Finishing persistence verification ? 2026-10-03 00:45 Pacific

Dan visually confirmed preview and Apply match in the bounded managed-finishing experiment.
This is human visual evidence for that case, not proof of persistence or general correctness.

Continuing the feature goal: verify ordinary reload/rebuild before deciding whether
persistent native compatibility changes are necessary. The previous turn clarified scope;
this session resumes evidence collection. No persistent patch is assumed approved by silence.

Fresh read-only city query: Wantagh, population 0, paused, controls enabled, same citySession
as the experiment. NT is idle with submission 44 (previous recorded Apply was 4), so preserve
intervening toy work before restarting. Do not assume it equals the captured corrected result.
The supplied trace-status command was unsupported; this produced no mutation. Use documented
commands. Reload the checksummed, separately preserved successful experiment checkpoint.

## Reload result: persistence failed

Graceful restart preserved intervening user toy work in
`CitiesIIAgentBridge-regression-before-reload-20261003-074414-87335976.cok`.
Loaded the successful corrected checkpoint
`CitiesIIAgentBridge-managed-finish-experiment-completed-20261002-142424-207b8a77.cok`,
SHA256 `2f9276aca68f47d9c07f8aa8e209fc4ca9f0db263edac3cfd40a09e9beedb9a8`,
using visible research launch with Burst enabled, no armed managed-finishing intervention.
Process 55940; fresh citySession `73b8b5e4330f4314a7d96280cb434f3f`; Wantagh paused,
population 0. Fresh nodes resolved uniquely from captured XYZ positions and directed
edges matched by those endpoints (not old IDs or expected surface shapes).

All five authored cubics remain exactly equal. Three generated EdgeGeometry surfaces
remain equal, but original edge 54263 -> fresh 59201 differs by 2.955469 m and
original ramp 54262 -> fresh 59192 differs by 1.844445 m. A second later observation
reproduces the same result. These are generated cubic-control distances, not rendered
mesh-distance measurements. This is an actual reload failure, not a speculative risk.
Entity identities changed on reload, consistent with the prior identity-dependent
lookup finding, but this observation alone does not establish every changed surface's
specific lookup path. Do not claim a fresh eight-stage replay of this load.

Reusable read-only comparator: scripts/compare-reloaded-surfaces.py. It rejects
missing/ambiguous endpoint matches, duplicate/missing/extra edge coverage, nonfinite
geometry, and incomplete snapshots; exits 1 for geometric differences above 5 cm.
Four offline comparison contracts passed. The live comparator correctly exits 1.
Raw captures remain ignored in artifacts/managed-finish-reloaded-01 and -02; compact
results are retained beside this note. No lane-identity or vehicle claim is made here.

Current game: the successful experiment checkpoint has been loaded but native
reconstruction no longer matches its previously observed surfaces. Paused; no edits
or Apply after reload, no new save needed. Baseline/checkpoint files untouched.
Next: persistent finishing compatibility needs scope resolution and implementation;
one-pass managed finishing is insufficient. Ordinary edit-induced rebuild and durable
fix save/reload qualification remain pending. No publish in this increment.
