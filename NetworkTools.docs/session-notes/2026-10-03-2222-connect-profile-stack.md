# 2026-10-03 22:22 — Connect profile acceptance and stack reconciliation

Dan confirmed the Connect elevation-profile fix behaves as expected. Complex Curve previews can be obscured by terrain, but Apply preserves the obscured road and adjusts terrain. Recorded as BUG-002, explicitly low priority and non-blocking. No new runtime change or deployment is needed for this observation.

Authorized sequence: publish the existing tested fixes, update and squash-merge PR #20, archive the original downstream tips, then rebase PR #21 on the squash result and PR #22 on the rebased #21. Preserve unrelated worktrees and the original-checkout user stash. Remaining PRs stay draft pending their individual review.

Evidence inherited from the preceding session: ten offline suites passed; bounded native preview/permanent checks passed on the recorded Debug build. Dan now supplies visual acceptance for this case. Save/reload, vehicle traversal, Release/Burst and broad prefab coverage remain separate qualifications.
