# Audit sprint — October 1, 2026

Base: PR #13 open, `dan/terrain-profile-sprint` at `ee9c3bcd7272e5f18f41166e2dad2a4d12744ed6`.
Worktree: `nt-audit-sprint`; new branch: `dan/audit-correctness-sprint`.

Original checkout has user lockfile edits, untracked audits and personal files. None were modified. Audit documents were copied unchanged into this worktree. The sprint targets correctness, trustworthy tests and current documentation; required instrumentation is retained. Geometric errors through 5 cm are acceptable under the current policy, but topology, semantic connections, stale inputs and accumulating drift remain independent checks.

Initial source inspection confirms F03 (whole-curve writes at two moved incident nodes can compete) and F10 (position replacement defaults Node rotation). F02's transform cache and independently captured submission inputs also remain separate. No new build or game verification yet.

Delegation: test-runner/Connect oracle fixes, path-search state, and read-only remaining findings. Only the parent controls the game and shared build/deployment.
## Incident-edge composition and node fields

F03/F10: Preview and Apply now gather each unselected incident edge once and compose both endpoint deltas from its original curve. This also removes the old sub-millimeter Preview/Apply translation threshold discrepancy. Node writes copy the original component and change position only. Native reconstruction remains independent and still needs live verification.

Non-deploying Debug compile passed with existing warnings. Compiled production helper tests passed for both-end edits, reversed storage, unchanged opposite end, preserved endpoint tangents and rotation (including unchanged position). Existing eight slope fixtures / 96 assertions also passed. These are not ECS scheduling or native-output tests.

