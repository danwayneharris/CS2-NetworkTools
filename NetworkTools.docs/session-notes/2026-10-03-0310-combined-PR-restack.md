# Combined feature PR restack ? 2026-10-03

PR #15 now targets the offline infrastructure branch in PR #16. Companion
Bridge capture PR: https://github.com/danwayneharris/cities2-agent-bridge-ndc/pull/7.
No feature or runtime changes are introduced. Original final tree: `3e4f947`;
complete original history retained on published archive branch
`archive/combined-before-offline-split-20261003`.

Before publishing, compared every tracked path/blob against that original tree.
All implementation, scripts, tests, configuration, submodule pins and existing
evidence are identical. Only the infrastructure publication/status doc and new
publication session notes differ. No game actions, deployment or new claims of
live verification accompany this history-only change.

The original feature worktree retains its uncommitted UI package-lock change
on an archive-named local branch; ongoing work belongs in nt-combined-stacked.
PR #15 remains draft and retains Dan's accepted tight-junction/elevation visual
limitation. Merge #16 first, then retarget/rebase #15 to main after that merge
(especially if #16 is squash-merged); do not merge the archive branch.

The base-relative whitespace check reports an existing blank line at EOF in
RoadShapeToolSystem.JunctionSearch.cs. It is inherited unchanged from the
original feature tip; not altered during this content-preserving restack. The
new documentation delta passes its whitespace check.
