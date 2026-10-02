# Provider acceptance oracles: F05/F06

Audited current sprint base after PR #13, not the older audit checkout.

## Changes

- F05 remains reproducible in source: a completed child failure was recorded but
  the suite returned zero. The suite now returns 1 for completed assertion failures,
  2 for incomplete/uncertain execution (including empty case sets), and 0 only for
  explicit passing reports. Summary rows include passed, assertion-failed,
  execution-incomplete, and not-run. Exceptions and missing/malformed reports stop
  further cases; even a nonzero exit following a passing report is uncertain.
  A completed assertion failure may continue only through the existing fresh-
  baseline reload workflow. Historical reports are not reclassified or overwritten.
- F06 remains present: Connect reported existing-edge preservation without requiring
  it. Existing edge identities/topology/prefabs are now required, and existing node
  positions and all four controls must remain within the explicit 5 cm policy.
  Nonfinite errors fail. Added nodes may extend the graph; existing node incident
  lists are intentionally not required to remain unchanged because Connect adds edges.
- New-edge preview/permanent comparison uses bottleneck bipartite matching rather
  than nearest-neighbor reuse. Counts must agree, permanent identities must be
  unique, controls finite and complete, and each cubic is used once (storage reversal
  is permitted). ReadControlPreview emits Temp.m_Original before each cubic;
  only null-original rows describe new edges. Existing-edge preview rows are
  excluded from new-edge matching and permanent preservation is checked separately.
- Slope observation, directed/physical lane checks, incident-edge translation
  diagnostics, and raw captures remain unchanged. This does not claim new native
  Connect or slope verification or vehicle traversal coverage.

## Verification and limitations

`uv run --no-project python scripts/test-provider-oracles.py`: 9 offline tests pass.
Negative cases cover aggregate child failures, missing/invalid reports, uncertain
exceptions, not-run remainder, empty suites, many-to-one curve reuse, extra/missing
curves, duplicate permanent identities, nonfinite controls/errors, preservation
failures, and matching that requires reassigning a previous candidate. Reversed
curves/permutations and unchanged existing preview rows are accepted correctly.

`uv run --no-project python scripts/test-live-regression.py`: 8 existing tests pass.

No game calls, builds, deployment, or full-suite run in this subtask. Parent will
coordinate integration verification. Connect tests still establish authored cubic
coverage and existing-entity preservation, not all possible lane behavior or terrain
surface quality. The suite consumes the existing child `passed` completion contract;
a report file's mere existence is no longer sufficient evidence of completion.
