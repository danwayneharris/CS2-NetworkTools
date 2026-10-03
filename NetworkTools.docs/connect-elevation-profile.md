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

Offline: 184 pure profile assertions; 190 native-coverage assertions; integrated aggregate 10 suites passed, including production compile/tests, codegen and Python. Fixtures cover flat/sloped/opposing grades, reversal, asymmetric Complex stationing, changed handles, malformed data and atomic failure; coverage tests deliberately corrupt native curves and coverage. Full UI/package and native tests pending.

## Limitations

No terrain following, obstacle routing, surface-height guarantee, arbitrary horizontal curve matching, lane assignment guarantee, vehicle test or Release promotion. Native processing that changes the desired profile must currently be rejected, not silently accepted. Endpoint grade scalar continuity does not establish chosen lane correspondence or horizontal tangent alignment. Those are subsequent plans.

## Dan's review

1. Compare option off/on with the same simple connection; inspect endpoint slope joins.
2. Try Complex Curve and move horizontal handles/midpoint; inspect height and grade continuity.
3. At an ambiguous node choose the desired incident approach; verify the choice remains understandable.
4. Check rejection explanations on terrain/native mismatches and backtracking curves. Do not expect Apply when the desired profile cannot be certified.
5. Compare preview, Apply, surrounding terrain and reload for accepted cases. Visual and vehicle review remain yours.

Exact native/build/checkpoint evidence will be appended after testing.