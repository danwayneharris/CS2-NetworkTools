# Connect lane alignment: partial implementation and native blocker

NT-022 / stage 6 is **PARTIAL / BLOCKED for the full feature**. The delivered work is tested pure geometry helpers and an opt-in, preview-only fixed-anchor diagnostic. **No Align selected lanes checkbox, explicit new-road group workflow, or user-applicable lane-alignment feature is delivered.**

The native experiment failed the fixed-existing-node prerequisite: a 1.5 m authored lateral endpoint offset moved the temporary counterparts of the existing nodes by approximately 0.75 m. Apply was rejected and the permanent network remained unchanged. See the [compact native evidence](session-notes/nt022-native-evidence.json) and [investigation log](session-notes/2026-10-03-nt022-lane-alignment.md).

## Features and usage

Normal launches do not enable the diagnostic. Existing tool behavior remains on its existing path without `--nt-connect-fixed-anchor-probe`. The pure helpers do not add a player-facing alignment control. [Lane-aware direction](connect-lane-direction.md) is a separate feature and must not be described as lateral alignment.

The Debug launch flag enables a deliberately narrow experiment: Simple Connect, lane-aware direction off, smooth elevation profile off, ordinary geometry without StrictNodes or StraightEdges. It offsets both endpoint/control pairs laterally by 1.5 m while emitting the original outer node entities, positions and rotations separately as CoursePos anchors. All **Connect** Apply paths/modes are disabled for the flagged process, including provider requests, with `alignment_probe_preview_only`. This is not a guard on unrelated tools.

Review package: `artifacts/review/NT-022/NetworkTools`. Tested runtime identity: `1.5.7+gfbd91c38cdaa.clean.Debug`, assembly version `1.5.7.0`. The package/build identity and installed/live assembly identity must be checked independently before reproducing the diagnostic; merely finding a package directory does not establish what the game loaded.

### Reproduce the fixed-anchor diagnostic

Use the disposable terrain/elevation v1.1 fixture, never a working city. Close the game gracefully first; the launcher refuses an already-running game and does not deploy. Have Steam running and the reviewed Debug package installed using the normal documented deployment workflow. From the repository root in PowerShell:

```powershell
.\scripts\launch-toy-fixture.ps1 -Fixture .\scripts\fixtures\toy-terrain-v11.json -ConnectFixedAnchorProbe
```

Wait for the baseline to load, independently confirm its geometry/build identity, pause simulation, and ensure bridge controls are enabled. Then run:

```powershell
uv run --python 3.13 python scripts/probe-connect-fixed-anchors.py --fixture scripts/fixtures/toy-terrain-v11.json --bridge ../cities2-agent-bridge-ndc --save-root "$env:USERPROFILE\AppData\LocalLow\Colossal Order\Cities Skylines II\Saves" --output artifacts/nt022/review-fixed-anchor --run
```

The script requires the exact baseline geometry fingerprint, verifies the save package, creates and verifies a checkpoint, selects the fixture endpoints, prepares the preview and captures native temporary nodes/curves. It also sends a current-token Apply request **to verify rejection**; it does not successfully apply geometry. Its final permanent-network fingerprint must equal the baseline. Read `report.json`: a completed script with `blocked_fixed_node_invariant` records the expected failed prerequisite, not a passing alignment feature.

Recorded baseline: `bridge test - terrain and elevation v1%2E1.cok`, baseline save SHA256 `6EF6B6E1DF56A8E834EC00313F478F64F47202507A857AEBFC65E87F62220301`. Recorded checkpoint: `CitiesIIAgentBridge-nt022-fixed-anchor-probe-20261003-153148-edb0e810`. A reproduction creates a new checkpoint identity; preserve its report rather than attributing it to the recorded run.

To return to ordinary operation, close gracefully and relaunch without the probe switch:

```powershell
.\scripts\launch-toy-fixture.ps1 -Fixture .\scripts\fixtures\toy-terrain-v11.json
```

## Architecture

[ConnectLaneAlignment.cs](../NetworkTools.Mod/Geometry/ConnectLaneAlignment.cs) fits an explicitly ordered correspondence using actual signed lateral offsets. For differences `d_i = reference_i - candidate_i`, it minimizes the largest absolute residual with `shift = (min(d_i) + max(d_i))/2`. Maximum position residual must be at most 0.05 m. Width compatibility is a separate explicit caller tolerance; it cannot relax position tolerance. The helper rejects invalid/nonfinite input, duplicate identities, noncontiguous or mixed-direction groups, incompatible width/spacing and capacity overflow. It does not choose a group, guess which side gains a lane, move individual lanes, or scale widths. Maximum group size is 64.

[LanePlaneIntersection.cs](../NetworkTools.Mod/Geometry/LanePlaneIntersection.cs) separates a tangent-line **proposal** from actual cubic/plane intersection. Proposal projection has explicit crossing-angle and maximum horizontal-extrapolation bounds. Actual cubic intersection projects its control points onto a horizontal plane normal, solves the derivative polynomial, partitions the entire parameter interval at derivative roots, and isolates roots on the resulting monotone intervals. Tangency, ambiguous/multiple intersections and ill-conditioned cases reject. This avoids a sampled-sign search that could miss an even-multiplicity tangent or closely spaced crossings. The returned cubic point includes its interpolated Y; selecting the physical comparison plane remains adapter policy.

These are ordinary double-precision numerical helpers, not interval-arithmetic formal certificates. The cubic helper explicitly rejects normalized tolerances below `1e-12`, near-repeated derivative roots and near-plane extrema/endpoints; conservative false rejection is possible. Its root-isolation tolerance is distinct from the final 5 cm lane-alignment criterion. Tangent extrapolation cannot substitute for a missing intersection of an actual native lane curve, especially at curved/setback ports.

[ConnectToolSystem.AlignmentProbe.cs](../NetworkTools.Mod/Systems/Tools/Connect/ConnectToolSystem.AlignmentProbe.cs) exercises the lower-level distinction between authored curve endpoint XZ and fixed original CoursePos node anchors. This initially appeared feasible from GenerateEdgesSystem, which can preserve authored endpoint XZ for non-StrictNodes geometry. Native reconstruction has a later position update that invalidates that assumption.

### Native blocker

In the installed Game 1.6.2f1 source, `Game.Net/NodeAlignSystem.cs:96-169` accumulates compatible incident curve endpoints, averages them, and resolves tangent-line centers. Its update query at line 293 processes `Node + Updated`; it does not honor a CoursePos fixed-position contract or consult Fixed/StrictNodes as a bypass. `Game.Net/ReferencesSystem.cs:503-585` computes the corresponding cached/predicted node position.

The probe observed exactly this problem: the authored shifted curve endpoints survived, but the existing nodes' temporary counterparts moved. Supplying original node entities and anchor positions therefore does not constrain the final native node positions. StrictNodes clamps the shifted endpoints back to nodes and defeats the proposed translation. Forcing Standalone changes native road semantics; it is not an acceptable workaround. No unsupported flag retry, neighbor move, or global native patch was used.

The next step needs a **different transition/composition strategy** that demonstrates fixed existing nodes before building the full interaction. It must also freeze an immutable unaligned native layout, expose explicit chosen new-road groups, retain composition/layout fingerprints and indices across rebuilds rather than stale temporary entity references, and independently validate each requested native lane pair. Existing node positions and neighboring authored curves/topology must remain fixed. An aligned preview must never feed another correction back into its own baseline.

### Focused architecture follow-up

NT-002, NT-003 and this investigation exposed concrete shared boundaries:

- **Endpoint context:** approach selection, orientation, original node anchors, actual lane ports and freshness are related but distinct. NT-002 owns vertical grade/offset policy; NT-003 adds exact lane identity and travel role; NT-022 requires a fixed physical comparison station and explicit new-road correspondence. Reuse the existing endpoint/context machinery without treating these constraints as interchangeable.
- **Candidate preparation:** preserve an immutable authored candidate and source revision. Direction, lateral transition and vertical profile are separate transformations. Future lateral geometry changes must precede vertical fitting because horizontal spans change. Keep stage-6 baseline capture separate from aligned-preview observation to avoid cumulative feedback.
- **Native observation:** curve-profile coverage, directed lane reachability, lateral pair agreement and fixed-node invariants are separate acceptance checks. NT-003's selected-to-some-new-edge witness cannot establish NT-022's explicit pair correspondence. Reuse graph/identity extraction where accurate, but do not reuse a weaker acceptance result under a stronger name.
- **Emission:** separating CoursePos entity/anchor state from curve endpoints is useful diagnostic structure, but it is not native authority over node position. The failed probe demonstrates why emitter output alone cannot certify final geometry.
- **UI/provider state:** retain the shared policy and revision gate. A future alignment baseline needs its own immutable token and invalidation reasons, not additional ad-hoc mutable fields that silently survive a rebuild.

These are targeted integration recommendations, not a broad refactor proposal. Keep the pure helpers separate, preserve existing opt-in behavior and add only the adapter boundaries required by a demonstrated native strategy.

## Testing and recorded evidence

The corrected aggregate passed **14 offline suites**, including **664 lateral-group fit assertions** and **228 plane/projection assertions**. The first aggregate failed on a missing `Colossal.Entities` extension import in the diagnostic adapter; it correctly reported failure. After that correction, the full Debug build, postprocessing, UI build and deployment of `fbd91c38cdaa` passed. Release/Burst qualification is not established by these results.

The native fixed-anchor experiment used the verified terrain v1.1 baseline and recorded checkpoint above:

| Measurement | Departure | Arrival |
| --- | ---: | ---: |
| Authored lateral offset | 1.5 m | 1.5 m |
| Temporary node displacement from original | 0.749942 m | 0.750012 m |
| Native curve endpoint error from authored endpoint | 0 m | 0 m |

The fixed-node invariant **failed** against the 5 cm policy. The diagnostic Apply request was rejected; no successful Apply occurred. The permanent network fingerprint remained equal to baseline. These observations establish a blocker in the tested output-only strategy, not successful selected-lane alignment.

The capture does not establish individual lane correspondence, rendered-surface/marking quality, permanent aligned output, reload stability or vehicle use. Full 2-to-3/3-to-2, each extra-lane side, differing widths, reversed selection, curved approaches, two-way carriageways and smooth-profile interaction acceptance remain incomplete.

## Limitations

- No user alignment workflow or checkbox exists. Full NT-022 remains blocked, not completed by delivering helpers.
- The diagnostic is fixed-offset, Simple-only, Debug-only and preview-only. It does not select lane groups or exercise the full intended feature.
- The recorded result violates the fixed-existing-node requirement; it must not be relaxed to make the experiment pass.
- No neighboring network movement, forced Standalone configuration, direct lane-routing edit, or global native-system change is an accepted fallback.
- Pure mathematical feasibility does not prove native composition, lane identity, geometry or traffic behavior.
- Native shared-node ports, explicit junction lanes, longitudinal setbacks and changes across temporary rebuilds require exact adapter handling. Raw prefab lane count is not the effective native layout.
- With the launch flag present, Connect Apply stays unavailable regardless of whether a particular preview appears acceptable. Without the flag, the diagnostic is inactive.

## Dan's review

This is a review of an explicitly failed prerequisite and the bounded diagnostic, not a request to approve a completed alignment feature.

1. Verify the loaded Debug identity against the recorded package and open the disposable terrain v1.1 baseline. Confirm that normal operation exposes no new Align selected lanes checkbox.
2. Use the flagged launch and probe commands above. Inspect the report/checkpoint identities and confirm `FixedNodeAlignmentProbe`, `alignment_probe_preview_only`, `applyBlocked: true`, and `permanentUnchanged: true`.
3. Compare original node positions with native temporary counterparts. Expect approximately 0.75 m movement in the recorded fixture despite 1.5 m shifted curve endpoints and original CoursePos anchors. This fails the fixed-node requirement.
4. Inspect preview shape separately if useful, but do not interpret an attractive taper or surviving curve endpoint as lane correspondence or fixed-node success. Do not try to bypass the diagnostic Apply block.
5. Relaunch without the flag and verify ordinary tool availability. Record any regression independently; the diagnostic run does not qualify all existing tools.
6. Before a future full-feature review, require a replacement strategy to pass fixed-node invariants and explicit paired-lane geometry/connectivity on 2-to-3/3-to-2 fixtures. Only then review either side gaining a lane, markings, Apply/reload and vehicle traversal separately.

Human visual review and full lane-alignment acceptance remain pending. The current evidence supports retaining the blocker and pursuing a different native transition strategy.

