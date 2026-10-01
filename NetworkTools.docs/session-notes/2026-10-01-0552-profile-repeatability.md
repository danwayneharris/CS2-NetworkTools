# Surface profile repeatability and PR preparation ? October 1, 2026 05:52 PDT

Continue the isolated terrain-profile sprint. User authorized a PR after verification;
original working tree and baseline saves remain untouched. Perfect constant generated
grade is deferred. The default 5 cm geometry tolerance remains unchanged.

The stable-reference candidate reproduces the visually approved first result to
0.1 mm, but the first reversed repeat moves controls 12.41 cm and nodes 7.019 cm.
The following forward repeat moves controls 7.2 mm and nodes 2.38 mm. All three
preserve authored preview/Apply geometry, topology, directed/physical lane mappings,
fixed endpoints and unselected authored curves.

A third reversed repeat unexpectedly falls back to ordinary fitting, returning to
the pre-correction fingerprint. Logs reject at base Model (stage 1), before the
least-squares solve. This is not acceptable as a small-tolerance waiver. The user
asked about a narrow exception; do not globally loosen tests or claim idempotence.

Source: GeometrySystem.FlattenNodeGeometryJob iterates at most 100 times and uses
the resulting heights even when the pair inequalities never become false. Our
restricted model instead rejected any non-converged result. Candidate fix recognizes
an exactly unchanged floating-point height vector as stationary, even if an
unrepresentable sub-ULP correction is still requested. It retains the iteration cap
and rejects actual continued movement; live verification remains required.


## Stationary-height correction verified

Reloaded the exact checkpoint that caused the fallback. With unchanged-float-state
recognition, the correction remained active. Repeat movement: 2.455 mm maximum
control and 0.123 mm maximum node. Topology, directed/physical lanes, horizontal
positions, fixed endpoints, unselected authored curves and preview/Apply checks pass.
Evidence: artifacts/surface-stationary-repro. This fixes the fallback, not the first
12.4 cm repeat drift.

## Bounded native preview feedback

Add a Debug-only preview loop for the experimental surface correction. Each
candidate starts from the same authored baseline; copy native generated boundary
components from a fresh, unambiguous temporary mapping into the next candidate.
Wait for three matching geometry observations, require two successive candidate
changes within 5 cm, cap at six candidates and 20 seconds, and retain the final
reference for Apply. A correction that becomes unavailable after an earlier
successful candidate fails closed instead of silently reverting to ordinary slope.
Unsupported initial cases retain ordinary Constant Slope.

The experimental surface correction is now Debug-only because this gate depends
on the Debug native observation infrastructure. The regular offset-aware Constant
Slope fit remains in Release. Release/Burst is not qualified here.
Offline convergence tests and complete geometry suite passed. Non-deploying compile
and full Debug postprocess/UI/deployment passed. Native settling verification is
in progress; do not infer live success from these results.

A multi-file edit stopped on an existing UTF-8 source decoded with the Windows
codepage; no game action occurred. The remaining edits used explicit UTF-8 and
compiled successfully. npm lockfile churn was backed up with its diff and only
restored after confirming unchanged declared dependencies.


## Native preview settling passed

From the pre-correction review save, the preview loop observed candidate differences
Infinity (first candidate), 0.1240845 m, 0.0072021 m and 0.0024414 m, then accepted.
Apply matched the accepted authored preview exactly. Reversed repeat moved controls
1.155 mm and nodes 0.143 mm. The following forward repeat left the entire region
fingerprint exactly unchanged. All recorded directed and physical lane mappings,
topology, endpoint/horizontal and unselected authored-curve checks passed.
No tolerance relaxation was required. The result is saved separately as
CitiesIIAgentBridge-review-surface-profile-settled-20261001-130556-32145a5a.cok.

Review also tightened Apply readiness to the current verified submission, including
same-input rebuilds. Live persistence and other-network checks remain in progress.
A proposed chained regression batch was rejected by automatic review because it
would carry state across cases. Nothing ran; use the existing per-case baseline
reload runner instead. An earlier review-service usage-limit failure prevented a
checkpoint attempt; after the user's continuation, the same authorized action ran.
