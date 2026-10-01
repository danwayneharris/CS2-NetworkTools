# Interior preview latency investigation - 2026-09-30 22:00

The captured strength-one case reached attempt 23 (+12 degrees). Each mismatch
previously consumed 120 observations; every input revision restarted at zero.
This explains visible alternating candidate geometry and an avoidable delay.

## First incremental change (not yet live verified)

- Reuse the last native-accepted angle as a first candidate only when ordered
  original component copies, node identities and split flags match. This is a hint,
  not cached acceptance; every new revision still requires native validation.
- Keep all 31 bounded angles in fallback, without duplicate retries. Zero strength
  only permits zero rotation. Changed source geometry/version invalidates the hint.
- Distinguish resolved stable mismatch from missing data: three identical complete
  connector sets, including temporary node identities and every interior junction,
  may advance search. Missing/ambiguous/stale observations reset that streak and
  retain the conservative 120-observation missing-data rejection.
- Add elapsed milliseconds and warm-start status to acceptance/retry logs.

Source grounding: installed 1.6.2f1 SystemOrder.cs:80-93 drains each modification
barrier; LaneSystem.cs:9382-9386 publishes its lane job to ModificationBarrier4.
NT_PreviewProbeSystem runs after ModificationEndBarrier (NetworkToolsMod.cs:62).
Existing revision, original component, exact selected-curve and lane-buffer checks
remain required. Repeated observations are not a universal native completion API.

Pending: offline checks, compilation, checkpoint/deployment and measured native
latency/connection regression. This is the first latency reduction, not a claim
that predictive fitting or hiding unvalidated geometry has been implemented.

## Validation so far

Offline geometry/search executable passes after restoring its missing assets file;
initial --no-restore invocation failed before running tests. Debug compile and full
bootstrap/postprocessor/UI/deployment pass (33 existing warnings).

Toy checkpoint saved and ZIP-verified before graceful close:
CitiesIIAgentBridge-regression-before-reload-20261001-050334-f239b2c7.cok.
Reloaded that checkpoint visibly; baseline geometry exactly matches the known toy
fixture. Native logs: initial 23-retry +12-degree search accepted in 1336 ms; five
subsequent strength changes accepted in 33, 33, 50, 38, 33 ms. All six independently
captured preview directed connection sets match the original. Permanent geometry
remained unchanged throughout this benchmark. Mailbox round-trip timing is slower
and is separately recorded, not presented as tool latency.

First Apply regression stopped before any transformation because the descriptive
case name generated a checkpoint label beyond the bridge limit. Renamed the case
to rail-interior-full-strength and reran with a new capture directory.

## Permanent Apply and final state

Apply completed. Independent permanent checks found identical selected/incident
preview curves (maximum error zero), preserved directed lane sets and lane
composition, topology, elevations, and unselected edge geometry. The regression
is explicitly FAIL: fixed node 75525:1 moved 0.02987677 m, above the unchanged
1 mm threshold. This is a new recorded observation for this branch/strength, not
a claim that the latency change caused or fixed native center movement.

Final result saved to
CitiesIIAgentBridge-interior-latency-applied-20261001-050923-5cbe0e28.cok.
Game remains paused, city session 31f73903cf424f6e8da32047f8abee22; the loaded input
was CitiesIIAgentBridge-regression-before-reload-20261001-050334-f239b2c7.cok.
The Apply result is checkpointed, with no subsequent network edits. Original
checksummed toy baseline remains unchanged. Bridge code was not modified.

## Confidence and remaining work

- Offline: bounded, unique cold/warm candidate enumeration, zero strength, invalid
  source/version hints and stable-set reset behavior tested alongside full geometry suite.
- Native preview: six directed-connection checks pass on this exact repro; cold
  1336 ms, warm 33-50 ms measured inside the mod, not inferred from mailbox polling.
- Permanent: lane/topology/curve/elevation checks pass, strict center position fails.
- Human visual quality and vehicle traversal: not newly validated in this session.

This does not yet meet an instantaneous first-preview goal. Predictive constraint
construction and keeping candidate-search geometry out of the visible preview
remain separate work. Warm starts deliberately favor the prior valid angle over
minimal rotation at each new strength; this introduces history dependence while
keeping exact native connection checks. Do not describe it as a stateless optimum.
No broad refactor, bridge dependency, Release interior enablement or PR push.
