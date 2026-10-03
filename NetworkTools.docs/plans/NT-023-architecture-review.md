# NT-023: architecture review and focused prerequisites

Status: reviewed planning draft; saved October 3, 2026. Not yet executing.
Order: 3 of 6. Dependency: [NT-021](NT-021-junction-elevation-limits.md).
Proposed branch: dan/nt-023-architecture-review.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Intended result

Clarify ownership and reusable boundaries before Connect expansion. Review broadly;
remediate narrowly. This is not a framework rewrite or automatic implementation of
every historical audit recommendation.

## Review method

Reconcile prior audits against current source and NT-021. Map:

- Original inputs, constraints, candidates, native evidence, acceptance, and Apply.
- Pure geometry, native adaptation, job dependencies, and native-container ownership.
- Curve/Slope editing versus Connect creation.
- UI/provider parameter handling and shared domain validation.
- Diagnostics, offline replay, regression runners, and fixture ownership.
- Duplication, readability, upstream-sensitive files, and merge/rebase hotspots.

Produce an architecture diagram and finding-by-finding disposition with source
evidence, benefit, risk, prerequisites, and status: implement now, already resolved,
defer, or disagree.

## Bounded remediation

Implement only demonstrated correctness fixes or direct prerequisites for NT-002,
NT-003, and NT-022 with an identifiable consumer and regression protection.

Prefer explicit input/candidate ownership, shared acceptance policy, small geometry
adapters, and removal of demonstrated duplicated logic. Do not generalize differing
edit/create semantics merely because implementations look similar.

Separate mechanical moves/renames from behavior changes. Preserve instrumentation,
independent native-result checks, and native lifetime guarantees. Do not introduce
a generalized graph-edit framework, a new runtime dependency, broad API churn, or
speculative abstractions. Measure before making optimization claims.

For a larger finding, document the minimal necessary prerequisite and defer the rest.
Unknown audit outcomes are handled through this selection rule rather than invented
refactors. A genuine blocking defect stays explicit.

## Interfaces and verification

Internal extractions should preserve externally observable behavior. If a confirmed
defect requires a public/provider change, document it and add a discriminating test.

Capture behavior before each refactor, run affected suites afterward, and run the
aggregate on the final stage. Add counterexamples for correctness changes. Do not
remove defensive completion calls without proving replacement dependency handling.

## Documentation and Dan's review

The durable architecture-review document includes findings/features, architecture,
testing, limitations, and **Dan's review**. Link session evidence and mark deferred
findings with concrete reasons.

1. Review the responsibility map and geometry/edit/create boundaries.
2. Review implemented findings and why they were prerequisites.
3. Review deferred decisions and maintenance/merge tradeoffs.
4. Where behavior-sensitive code moved, follow the provided existing-tool smoke test.

Expected: clearer ownership and protected behavior, not a player-facing feature
invented to justify a PR. Missing human smoke coverage remains pending.

## Delivery

One draft PR stacked on NT-021 with the architecture map, disposition, separately
scoped prerequisites, regression evidence, and review instructions.
Schedule a focused follow-up in NT-022 rather than repeating this full audit there.
