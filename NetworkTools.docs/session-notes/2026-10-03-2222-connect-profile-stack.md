# 2026-10-03 22:22 — Connect profile acceptance and stack reconciliation

Dan confirmed the Connect elevation-profile fix behaves as expected. Complex Curve previews can be obscured by terrain, but Apply preserves the obscured road and adjusts terrain. Recorded as BUG-002, explicitly low priority and non-blocking. No new runtime change or deployment is needed for this observation.

Authorized sequence: publish the existing tested fixes, update and squash-merge PR #20, archive the original downstream tips, then rebase PR #21 on the squash result and PR #22 on the rebased #21. Preserve unrelated worktrees and the original-checkout user stash. Remaining PRs stay draft pending their individual review.

Evidence inherited from the preceding session: ten offline suites passed; bounded native preview/permanent checks passed on the recorded Debug build. Dan now supplies visual acceptance for this case. Save/reload, vehicle traversal, Release/Burst and broad prefab coverage remain separate qualifications.

## Rebase result

PR #20 merged as `d892fb2`; its tree exactly matched the reviewed branch tip `a24ee85`. Archived original PR #21 and #22 tips locally under `archive/pr21-before-profile-rebase-20261003` and `archive/pr22-before-profile-rebase-20261003`.

PR #21's three commits rebased cleanly. PR #22 required combining its fixed-node probe before definition emission with the restored-profile tagging after emission, and keeping both the current restoration documentation and later alignment findings. No validation was removed. Range-diff confirmed the remaining feature changes were retained. A documentation encoding artifact from conflict resolution was corrected before publication.

All 14 offline aggregate suites passed on the combined rebased stack (`artifacts/profile-rebase-offline/summary.json`), including production Slope compilation/tests and restoration/coverage tests. This does not qualify the interaction of lane directions, profile restoration and the optional alignment probe in-game. No deployment, restart, save or live mutation occurred during this task; the user's running game is untouched. PRs #21 and #22 remain draft for their separate review.