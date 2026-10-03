# Detailed implementation plans

This directory will hold reviewed, executable plans linked from [the roadmap](../ROADMAP.md).
No feature implementation plans have been approved merely by creating this directory.

Name plans `{ID}-{short-topic}.md` using the stable roadmap ID. Reuse/link existing
research rather than copy it. The first six selected work packages will receive
detailed plans together; later roadmap items may retain unresolved decisions.

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
  When suitable live fixtures cannot be created within bounded effort, continue
  with meaningful offline evidence under the fallback below. Distinguish missing
  native confirmation from a known failed prerequisite.
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

## Required implementation document (all plans)

Each stage must create or update a durable feature and review document as an
implementation artifact. Session notes and the original plan do not substitute
for this document. Each draft PR must link it, and it must describe the delivered
implementation rather than merely repeat intended behavior.

The document must contain:

- **Features and usage:** what changed, how to use it, defaults, and supported scenarios.
- **Architecture:** how it works, component responsibilities, data flow, and relevant design decisions.
- **Testing:** what actually ran, reproduction instructions, results, and separate evidence for offline tests, native preview, permanent Apply, reload, and human verification.
- **Limitations:** unsupported cases, known issues, missing evidence, and deferred decisions.
- **Dan's review:** exact build/settings/save information, numbered review steps, expected results, accepted imperfections, and remaining visual, UI, or vehicle-traversal checks.

Keep this document current as fixes land. Documentation-only stages describe
their findings and architectural recommendations using the same structure where
applicable. The detailed review requirements below also apply to this document.

## Required section: Dan's review

Every plan and its resulting review documentation must include a clearly labeled
**Dan's review** section; each draft PR links it. Keep the completed handoff
current rather than leaving only the pre-execution checklist.

For each item specify:

- What Dan should evaluate: expected visual/interaction behavior, workflow clarity,
  regression signs, and any unresolved product choice. For architecture/docs-only
  work, identify the findings, ownership boundaries or explanations to review.
- The exact PR/build/configuration/opt-in flags, toy baseline/result save names,
  network/location or selection, parameter settings and short numbered actions.
  Include fixture provenance/fingerprints where available, not stale entity IDs.
- What a satisfactory result looks like, accepted limitations, and what would
  warrant a fix. Keep the accepted geometric tolerance distinct from connectivity,
  topology and accumulated drift; do not ask Dan to chase harmless millimeters.
- Which claims already have offline/native evidence and which need his visual,
  UI or vehicle-traversal confirmation. If no suitable live case exists, say so
  and give a practical scenario-creation recipe instead of inventing a save name.

## Constructing missing live test scenarios

For the planned autonomous sprint, Dan authorizes the executor to make its best
bounded effort to build a missing test scenario in-game using available controls,
or adapt networks in existing verified **toy** saves. For example, delete selected
segments in a disposable copy to create Connect endpoints. This extends fixture
preparation authority; it does not authorize touching real-city saves.

Before mutation, verify the toy scenario and a recoverable checkpoint. Preserve
unsaved toy work and save uniquely named copies; never overwrite the original
baseline. Keep simulation paused unless a specifically authorized bounded test
requires otherwise. Respect STOP/control settings and existing lifecycle rules.
Record what was constructed/removed, relevant prefabs and the resulting baseline
so Dan can reproduce the case. A successfully accepted construction request is
not proof that the intended test layout exists; inspect the result.

Each detailed plan should set a bounded live-fixture/setup effort budget. If the
required controls are unavailable, construction is unreliable, or setup takes
too long, stop that attempt, document the blocker and **continue the sprint using
meaningful offline tests**. Preserve useful partial evidence and provide Dan a
manual recipe. Do not spend the sprint fighting infrastructure or wait for Dan
between stages solely because live coverage is missing.

This fallback permits draft PRs and subsequent work without complete live testing.
It does not turn offline success into native/visual success, excuse known failing
assertions, or authorize weakening checks. If evidence establishes a defect that
invalidates a dependent step, fix it or document that dependency as blocked and
continue independent work. Clearly label unverified native behavior and relevant
dependent assumptions in every affected PR and in Dan's review checklist.
