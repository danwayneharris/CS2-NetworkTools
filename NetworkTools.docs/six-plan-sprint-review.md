# Six-plan sprint status and review index

Baseline: origin/main f36d669; planning eace043. Active worktree: nt-six-plan-sprint.
This is execution status, not a declaration of verified game behavior.

| Stage | Status | PR | Evidence / review |
| --- | --- | --- | --- |
| NT-001 | Implemented; package/offline pass; live identity verified in NT-021 | [#17](https://github.com/danwayneharris/CS2-NetworkTools/pull/17) | [Guide](development-build-identity.md) |
| NT-021 | Implemented; active-bound and fixed-height rail pass, road control failure documented | [#18](https://github.com/danwayneharris/CS2-NetworkTools/pull/18) | [Guide](junction-elevation-limits.md) |
| NT-023 | Implemented; offline and native Connect smoke pass | Draft pending | [Review](architecture-review-nt023.md) |
| NT-002 | Not started | Not opened | Pending NT-023 |
| NT-003 | Not started | Not opened | Pending NT-002 |
| NT-022 | Not started | Not opened | Pending NT-003 |

## Game and evidence

NT-021 clean Debug build `7eebfc49830a` is deployed. Terrain v1.1 toy is paused;
review checkpoint `CitiesIIAgentBridge-review-nt021-fixed-height-20261003-134806-ac400174` preserves the latest fixed-height rail result.
See compact NT-021 evidence for recovery checkpoints and the existing road-lane failure.
Human visual/UI review and vehicle traversal are pending for all changed behavior.

## Review workflow

Each stage gets a durable implementation document and reproducible build/checkpoint.
Dan reviews in dependency order after the stack is complete. Correct the affected
branch before a later authorized downstream rebase; archive old tips, rerun affected
tests, and refresh provenance. Do not equate final-build testing with stage review.