# NT-003: explicit approach lanes and direction-aware Connect

Status: Debug implementation at `fdbf10d`; 12 offline suites passed. Native acceptance and human review pending (October 3, 2026). See [implemented behavior, testing and review guide](../connect-lane-direction.md).
Order: 5 of 6. Dependency: [NT-002](NT-002-connect-elevation-profile.md).
Proposed branch: dan/nt-003-connect-lane-direction.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Settled UX and scope

For Simple/Complex Connect, add optional lane-aware endpoint controls using lane
diagrams. Identify the incident approach edge and show lane travel direction.
The player chooses a lane or contiguous same-direction group.

Start with ordinary road/highway vehicle lanes, including one-way and two-way roads.
Explicit rail/tram/shared-lane mapping is deferred; existing support remains.
Legacy direction behavior stays selectable and remains the default unless lane-aware
mode is enabled. Keep the new experimental controls Debug-only.

This changes directional constraints, not lateral alignment (NT-022) or arbitrary
lane-connector editing.

## Implementation

1. Read actual lane geometry, prefab/composition data, and travel direction.
   Do not infer intent from lane count or node degree alone.
2. Resolve departure/arrival tangents with consistent orientation under reversed
   edge storage and reversed selection. For a group use its supported common
   approach direction; reject materially conflicting directions rather than
   averaging unrelated branches.
3. Apply chosen direction constraints to curve initialization and endpoint handles.
   Explicit lane choices replace the degree-two perpendicular heuristic for that
   endpoint. Preserve legacy initialization when the feature is off.
4. Retain selections only while approach-edge, composition, and lane identities
   remain valid. Clear stale choices visibly and invalidate candidate evidence.
5. Expose equivalent NT provider choices and diagnostic identities through the
   generic interface. UI and automation use the same domain policy.
6. Inspect intended native directed connections after rebuilding. Lane selection
   does not promise exclusive lane use that the native result cannot establish.

The current source counterexample is Connect's degree-two node direction rule:
it deliberately projects toward the other node perpendicular to the through direction.
Test the added-lane offramp case against this behavior before changing it.

## Verification and acceptance

Test added lane-math offramps, onramps, one-/two-way approaches, reversed traversal,
ambiguous junctions, stale choices, conflicting groups, and feature-off parity.
Check direction against actual source lane geometry and permanent lane identities,
not counts or appearance alone. Exercise interaction with NT-002's vertical profile.

## Documentation and Dan's review

Create a durable lane-direction guide covering usage, geometry/native boundaries,
testing, limitations, and **Dan's review**.

1. Compare legacy perpendicular departure with a chosen outer-lane departure.
2. Check approach-edge selection, lane-diagram orientation, and travel arrows.
3. Adjust the endpoint handle and verify the documented direction constraint.
4. Apply and inspect ramp flow; assess actual vehicle use separately.

Expected: an understandable explicit approach choice and intended ramp direction.
Do not claim exclusive routing or traffic qualification from provider tests.

## Delivery

One draft PR stacked on NT-002, lane-direction fixtures and evidence, and a
stage-specific review build/checkpoint. Leave lateral group alignment to NT-022.

