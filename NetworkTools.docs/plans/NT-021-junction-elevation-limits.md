# NT-021: bounded interior-junction elevation movement

Status: reviewed planning draft; saved October 3, 2026. Not yet executing.
Order: 2 of 6. Dependency: [NT-001](NT-001-development-baseline.md).
Proposed branch: dan/nt-021-junction-elevation-limits.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Settled behavior and UX

In Debug-only combined smoothing, expose:

- **Allow interior junction elevation modification**.
- **Unlimited**, or **Maximum junction elevation change** in meters.
- A 0–20 meter slider with 0.5 meter steps and precise numeric entry within that range.

Each game launch starts with today's behavior: movement allowed and Unlimited.
Remember changes within the session only. Disabled or zero allowance fixes original
junction Y. A finite allowance bounds upward/downward movement from the operation's
original input height. The geometry chooses the elevation; this is not a user target,
a pin, interpolation between solutions, or post-fit clamping.

Outer endpoints and explicit split/pin constraints take precedence. Junction XZ stays
fixed. Independent Curve continues to preserve elevations. Finite bounds can prevent
a globally constant grade; communicate this without promising perfect rendered slopes.

## Implementation

1. Capture permission and allowance with immutable operation inputs. Include them
   in candidate identity, freshness checks, structured diagnostics, and NT provider
   parameters. UI and provider use the same domain validation.
2. Keep the existing Unlimited solver path to preserve default behavior.
3. Add an independently testable bounded profile solve for finite limits. Solve node
   heights and curve controls coherently, respecting fixed anchors, compatible grade
   constraints, and continuity. Use vertical smoothness as the primary objective
   and closeness to the unrestricted profile as a deterministic tie-break.
4. Normalize stationing; document the precise mathematical objective and numerical
   conditioning. Use deterministic constraint ordering and bounded convergence.
   Establish analytical counterexamples before integrating the solver.
5. Recalculate existing incident-branch edits from the accepted profile. Validate
   the complete affected set, preserving topology and intended directed connections.
6. Report incompatible constraints or numerical failure explicitly. Discard partial
   output on failure; never enable Apply from stale or partially computed evidence.

Current source anchors: SectionedVerticalProfile supports fixed/free anchors;
CombinedLinearProfileTransform currently frees interior junction Y. Extend the
mathematical constraint boundary rather than hiding policy inside native adaptation.

## Verification and acceptance

Test Unlimited parity, disabled/zero limits, upper/lower active bounds, multiple
junctions, reversed traversal, explicit splits, incompatible grades, degenerate spans,
parameter changes, and deterministic repeated evaluation.

For native cases inspect preview/Apply correspondence, incident edges, directed lanes,
repeat Apply, and reload. Bounds apply relative to each operation's original inputs;
separately measure repeated-operation drift rather than treating a per-operation bound
as protection against accumulation. Preserve existing repeatability expectations.

No known connectivity regression or numerical failure may be relabeled acceptable
because the visual result looks smooth. Missing live verification follows the shared
draft-PR fallback.

## Documentation and Dan's review

Create a durable junction-elevation guide with feature usage, solver architecture,
actual tests, limitations, and **Dan's review**.

The completed guide must identify a sloped-road or trumpet baseline and selections.

1. Compare Unlimited against the preceding stage's default.
2. Disable junction movement and inspect the fixed junction height.
3. Try zero, a restrictive finite allowance, and a larger allowance.
4. Inspect incident branches, Apply, repeat, and reload as instructed.

Expected: bounded geometry-selected movement, stable XZ, clear rejection when
constraints cannot be satisfied. A globally constant slope is not required.

## Delivery

One draft PR stacked on NT-001, compact solver fixtures, native evidence where
available, review checkpoints, and incremental session notes. No Release promotion.
