# NT-023 architecture review and bounded Connect prerequisite ? October 3, 2026, 06:50 PDT

Stage-three branch: `dan/nt-023-architecture-review`, after publication of NT-021 draft PR #18. Source edits for the Connect prerequisite were prepared without changing the already deployed NT-021 binary. No commit, build, deployment or game action was performed by the documentation subtask.

## Changes and findings

- Reviewed parameter/domain ownership, immutable inputs/candidates, Connect creation versus RoadShape editing, shared adapter consumers, prior audits, and NT-002/003/022 requirements. Saved the durable [architecture review](../architecture-review-nt023.md); the earlier artifact remains historical working material.
- Implemented the authorized Debug SimpleCurve-only Connect acceptance prerequisite: shared UI/provider eligibility, native Error-query check, immutable accepted configuration/resolved prefab identity, and execution-time recheck before Apply emission. Complex/Loop and Release retain their old envelope.
- Learned a specific handoff hazard: the old ControlInputs fingerprint included Phase. Reusing it during Applying would reject a legitimate unchanged request. Phase/Enabled are now lifecycle checks outside content identity.
- Guarded missing endpoint/component context so a rejected Apply cannot immediately send a vanished endpoint into preview generation. Retained existing observer scheduling and job completion fences.
- Reconciled resolved historical RoadShape/runners findings instead of repeating them as current defects. Kept structural endpoint Elevation capture, explicit approach context, Complex vertical continuity, and native lane correspondence as separately scoped upcoming work.
- Added a focused pointer/ownership description to system-architecture; no broad document rewrite or speculative code extraction.

## Verification

`dotnet run --project NetworkTools.Connect.Tests` passed **25 assertions** against the linked pure production helper. Counterexamples cover changed/missing authored inputs, pending/dirty work, stale revisions/submissions, missing/changed native observations, native errors and copied accepted configuration ownership.

Installed Game.dll SHA256 `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A` matches the separate decompile manifest. Vanilla GetAllowApply differs from NetworkTools' explicit public method; the patch reuses NT's existing Error-query policy rather than inventing a vanilla replacement.

Parent integration subsequently passed production compilation through the Slope suite and all 23/23 Python scripts. The aggregate inventory test was updated to include the newly registered Connect suite; its old expected suite set was the integration test failure, not a production failure. Native preview/Apply, request/execution mutation, reload and ordinary UI checks remain pending. Parent session owns native evidence updates. The pure policy test is not an ECS/native result, and NT-021's deployed build does not certify NT-023.
