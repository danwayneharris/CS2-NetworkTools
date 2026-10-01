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


## Final verification and handoff

Committed implementation: 1a0c952. Final Debug build completed compilation,
postprocessing, UI and deployment (32 existing warnings, zero errors). Deployed DLL
matches build output SHA256 469A64C4C86571AD904B9AFD59E4C4712A9B57A08BD3CA912102BB192F4763D1.
Release C# compilation passed without deployment; full Release/Burst is unqualified.
Geometry suite, convergence tests and three repeat-audit tests passed.

Fresh-baseline regressions passed for hill road, rail-high branch and highway mainline.
Rail and mainline intentionally move interior junction heights; their unselected
incident endpoints/handles follow the existing Slope translation policy. Independent
audits confirmed those expected translations and preserved directed lane movements.
Do not conflate that behavior with the terminal off-ramp correction, which preserves
all unselected authored curves. The hill check preceded the final submission-ID guard;
rail, mainline and reloaded-ramp checks used the final deployed build.

After loading the settled review checkpoint, final-build reversed Apply left the
entire region fingerprint exactly unchanged. Another graceful reload preserved exact
region geometry and normalized directed connections plus physical lane mappings at
all 37 shared nodes. Compact results:
[surface-profile-verification-20261001.json](surface-profile-verification-20261001.json).
Raw captures remain ignored under artifacts/surface-*, not added to the PR.

Read-only generated-surface capture after the final reload reports a sampled first-edge
lane peak of 17.69% magnitude (earlier captured pieces reached 27-32.5%). The shared
node-height prediction is about 617.975 m. This supports retained improvement, not
perfect constant grade, complete mesh verification or vehicle traversal.

Human confidence: Dan confirmed the earlier corrected candidate was MUCH better and
its small discontinuity gone. Final settling changed the candidate slightly; a quick
visual check of the final checkpoint remains useful. Exact fixed-point behavior is
not promised for every junction; eligibility remains deliberately narrow. A lost
correction during a preview sequence now blocks Apply instead of falling back.

Game left paused on CitiesIIAgentBridge-review-surface-profile-settled-20261001-130556-32145a5a.
Final city session: 65dfb793e5a04bdc9f3bffdffcb8fb3b. Final fingerprint:
ddd31890adfa9fbe7cb861d45eb67938c9610c893a4ed677e059e41fc313e150.
No geometry mutations after the final reload. Recovery checkpoint before it:
CitiesIIAgentBridge-regression-before-reload-20261001-131711-64f482b4.cok.
Original baseline saves and the original NetworkTools worktree were preserved.

The requested sub-agent design review is saved in
[combined smoothing options](../combined-smoothing-options.md). It recommends one
shared geometry pipeline behind Smooth-existing and Connect-new actions, beginning
with a coordinated Curve+Slope mode. No combined-tool code was added to this PR.
