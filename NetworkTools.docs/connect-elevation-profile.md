# Optional Connect elevation profile (NT-002)

Experimental Debug-only work, stacked on NT-023. Existing Connect defaults remain unchanged.

## Features and usage

Select two nodes in Connect Simple Curve or Complex Curve and enable **Smooth elevation profile**. At ambiguous endpoints choose the existing approach edge explicitly. The profile joins node heights and approach grades; horizontal handles remain the player's controls. Loop is unsupported and explains that in the panel. Controls are session-only, default off. Provider `connect_configure` exposes the same option, mode, approach choices and horizontal controls.

## Architecture

`ConnectVerticalProfile` fits a cubic Hermite target over total horizontal station, then emits vertical controls using the actual horizontal handle lengths. Complex mode shares one midpoint height and grade. This is not an exact arc-length reparameterization of every emitted cubic. It guarantees authored endpoint/join grades, not constant rendered slope.

The adapter reads explicit incident-edge context and rejects unsupported node/curve endpoint height offsets above 5 cm. World height is not a structural elevation flag. Structural course policy remains legacy; neither automatic bridge/tunnel classification nor the separate finishing compatibility experiment is activated by this option.

Manual/provider Apply share a frozen solved candidate, input revision, native observation and native-error gate. `ConnectProfileCoverage` independently maps native split cubics onto complete, unique authored intervals (including reversal). Four-control XZ/Y bounds imply whole-cubic error bounds. Missing, duplicate, overlapping, ambiguous or materially different output rejects Apply. The conservative mapper requires strictly monotone projection along each authored chord; looping/backtracking shapes are explicitly unsupported. Native terrain processing can rewrite authored controls, so successful fitting does not imply native acceptance.

Diagnostics retain raw parameters, authored candidate, endpoint context, native observation and rejection reason. Optional logging is not required for validation.

## Testing

Offline: 184 pure profile assertions; 190 native-coverage assertions; integrated aggregate 10 suites passed, including production compile/tests, codegen and Python. Fixtures cover flat/sloped/opposing grades, reversal, asymmetric Complex stationing, changed handles, malformed data and atomic failure; coverage tests deliberately corrupt native curves and coverage. Full Debug UI/package/deployment passed. See native qualification below.

## Limitations

No terrain following, obstacle routing, surface-height guarantee, arbitrary horizontal curve matching, lane assignment guarantee, vehicle test or Release promotion. Native processing that changes the desired profile must currently be rejected, not silently accepted. Endpoint grade scalar continuity does not establish chosen lane correspondence or horizontal tangent alignment. Those are subsequent plans.

## Dan's review

1. Compare option off/on with the same simple connection; inspect endpoint slope joins.
2. Try Complex Curve and move horizontal handles/midpoint; inspect height and grade continuity.
3. At an ambiguous node choose the desired incident approach; verify the choice remains understandable.
4. Check rejection explanations on terrain/native mismatches and backtracking curves. Do not expect Apply when the desired profile cannot be certified.
5. Compare preview, Apply, surrounding terrain and reload for accepted cases. Visual and vehicle review remain yours.

## Native qualification and review saves

Clean deployed source `3611214` (`1.5.7+g3611214.clean.Debug`; full hash in [evidence](session-notes/nt002-native-evidence.json)). Simple and Complex straight-control connections between equal-height terrain-toy endpoints passed: native profile accepted, Apply matched every preview cubic exactly, existing nodes/edge curves unchanged, expected prefab inherited, graph connection established. Stale revision Apply rejected. The legacy default (profile off, original curved handles) also passed the same independent preservation and exact preview/Apply checks. This is a modest near-flat-height case, not broad slope qualification.

The different-height hill-start to crest-start connection was rejected with `profile_native_CurveMismatch_0`. A current-token Apply attempt was blocked and network fingerprint stayed unchanged. **Native course sampling currently prevents this case from realizing the authored smooth profile.** This stage is a guarded prototype with a real native limitation, not complete terrain-aware Connect. Do not relax the gate or silently enable global finishing changes to claim support.

The default doubling-back connection is also explicitly unsupported by the conservative horizontal mapper. Initial native-ID reuse left preview pending; an empty-preview boundary now precedes guarded replacement. Changed controls subsequently reached accepted preview and Apply.

Review saves (preserved, never baseline-overwritten):
- Simple result: `CitiesIIAgentBridge-regression-before-reload-20261003-143533-4d66a61a`
- Complex result: `CitiesIIAgentBridge-regression-before-reload-20261003-143916-1054d6f2`
- Different-height pre-test checkpoint: `CitiesIIAgentBridge-provider-connect-20261003-143734-6555fa26`

Baseline: `bridge test - terrain and elevation v1.1`. Reproduce with `scripts/exercise-tool-provider.py --stage connect --connect-profile --connect-straight-controls` plus explicit baseline fingerprint, save root, output and `--run`. This command checkpoints then mutates through Apply. Add `--preview-only` for preview plus a rejected-Apply negative test when rejected; `--connect-mode ComplexCurve` chooses the two-section case; `--connect-start-endpoint start --preview-only` reproduces the different-height rejection. All scripts preserve baseline saves. They do not certify rendered terrain, selected lane correspondence, vehicle use or reload equivalence.

Successor: [course-height correction and replay](connect-course-height-replay.md).
The October 3 `connect repro` case now passes native preview and Apply with exact
curve agreement on deployed `02b899b89f9c`. The historical different-height result
above remains evidence for its earlier revision, not a rerun of that fixture.
The new adapter restores authored heights on qualified native subdivisions and
recomputes ordinary-course elevation classification; see its narrower supported
envelope, replay methods, limitations and Dan's review checklist.
