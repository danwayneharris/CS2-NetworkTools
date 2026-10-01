# Terrain matrix: measured successes and retained failure

Curve then Constant Slope passed on hill-road and crest/dip road. Constant Slope
alone passed on rail-high-branch, including specific directed connections, physical
lane mapping, node XZ, fixed endpoints, native preview/permanent curves and the
expected translation of the unselected rail branch at the changed junction height.
The crest/dip endpoints share an elevation: Constant Slope flattens that path,
which can cut through raised terrain. This is not terrain-following behavior.

Curve at strength 1 on rail-high-branch still fails the existing 1 mm fixed-center
assertion: junction center moves 23.5183 mm. Other captured checks pass. This is the
same magnitude documented before the sprint, and occurs before the new Slope fit.
The runner stopped; it did not silently continue into Slope or raise a tolerance.
A separate Slope-only run reloaded the original baseline first.

Replaying the installed NodeAlignSystem tangent-line center calculation predicts
23.6244 mm movement, within 0.1312 mm of native observation. This is double-precision
source transcription under same-layer/non-Standalone assumptions, not bit-exact ECS
emulation. It materially explains the center displacement; it does not excuse a
failed pin assertion or establish harmless repeated-Apply accumulation.

Generated EdgeGeometry boundaries were examined separately from authored curves.
In the five-edge ramp result, left/right half-curve middle heights meet exactly;
the largest sampled middle derivative mismatch is 0.00745 percentage points.
Those boundaries are not the final rendered mesh, and this does not exclude local
clipping/retaining-wall/terrain artifacts or discontinuities across junction spans.

Expanded the independent Slope auditor to check enabled endpoint tangents against
the actual single unselected neighboring curve. The assertion is a 1 mm vertical
handle residual, retaining the existing geometric scale rather than inventing a
large grade tolerance. New baseline-position fixtures target eligible inner road
endpoints and an ordinary split on a slope. Native boundary/split runs follow.

Raw evidence is under ignored artifacts/profile-{hill-road,crest-dip-road,
rail-high-branch,rail-slope-only}. Native prediction errors are not human visual
validation, and no vehicle traversal test has been run.

Highway on-ramp and mainline subsequently passed Curve->Slope, including the
independent physical lane mapping and incident-edge checks. Standalone rail Slope
predicts the native controls within 0.071 mm; its maximum sampled grade is 4.0222%.

Unity MCP stdio was exercised against the actual running toy game. Beacon status
reports debugger enabled with zero held suspensions; a read-only TerrainSystem
type lookup successfully attached and confirmed separate RenderTexture base and
cascade fields plus the CPU height array. The short-lived server exited normally.
This confirms live availability/type layout, not numeric base-terrain sampling.
