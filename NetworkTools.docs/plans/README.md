# Detailed implementation plans

This directory will hold reviewed, executable plans linked from [the roadmap](../ROADMAP.md).
No feature implementation plans have been approved merely by creating this directory.

Name plans `{ID}-{short-topic}.md` using the stable roadmap ID. Reuse/link existing
research rather than copy it. Plan the next one or two work packages in detail;
later roadmap items may retain unresolved decisions.

An execution-ready plan records:

1. Status, roadmap ID, verified baseline and dependencies.
2. Intended behavior, explicit exclusions and settled product decisions.
3. Implementation boundaries and public interface changes, if any.
4. Acceptance criteria, discriminating tests and separate offline/live/manual evidence.
5. Autonomous permissions, protected data, checkpoints, stop conditions and blocked-work alternatives.
6. Deliverables: session notes, commits/PR policy, review artifacts and manual checklist.

Use draft -> reviewed -> approved for execution -> in progress -> completed (or
blocked/superseded) as plan states. Approval includes the stated scope and permissions;
a roadmap link alone is not approval. On completion, update the roadmap and link
actual evidence instead of turning the original plan into an unsupported success claim.

## First-six-plan delivery workflow

Dan selected these roadmap items, in this order, for detailed planning and then
an autonomous sprint: **NT-001, NT-021, NT-023, NT-002, NT-003, NT-022**.
The next step is plan-mode design, not immediate execution. Settle the material
UI/solver/permission decisions before approving the goal-mode prompt.

- Save one reviewed plan per item here. Each produces appropriate documentation
  updates/new documents and one independently reviewable PR stacked on its predecessor.
  The architecture-review item produces its map/disposition and any specifically
  scoped prerequisite changes; it need not invent a player feature to justify a PR.
- Dan will not manually validate between stages. Run meaningful automated offline
  and supported native tests between stages; report unsupported or blocked checks.
  Proceed only when the next stage's prerequisites are supported by evidence.
  Never treat lack of human review as a pass or suppress a regression to finish all six.
- Keep PRs draft pending the agreed manual review. Do not merge the stack during
  the autonomous sprint. Each description records base dependency, implementation,
  tests, known limits and the exact manual/visual checklist for that stage.
- Preserve independently reproducible review checkpoints for every PR: source
  revision, build/configuration/activation identity, baseline and result save
  identities where applicable, and rebuild/deploy/replay instructions. Later
  stages must not overwrite earlier baseline saves or remove their review evidence.
- After all six stages, Dan reviews them one by one in dependency order. For
  documentation/review-only stages, identify the documents/findings to inspect.
  Do not imply that testing only the final combined build validates each PR in isolation.
- If a stage needs correction, fix that PR branch and rebase dependent PRs,
  preserving recoverable pre-rewrite tips. Re-run affected tests and update build/
  checkpoint provenance and later review instructions. Resolve semantic changes,
  not just textual conflicts. Publishing/rewrite permissions belong in the approved
  execution plan; this workflow is not an instruction to modify current merged history.

Autonomous execution scope, toy-save/game permissions, publication details and
blocked-work alternatives must be explicit in the six plans and goal prompt.
No live game work or plan implementation was performed when recording this workflow.
