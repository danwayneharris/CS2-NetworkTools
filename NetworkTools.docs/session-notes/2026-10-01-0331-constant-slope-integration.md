# Constant Slope integration — 2026-10-01 03:31

The shared ShapeTransformJob now calls SlopeLinearProfileTransform for Constant
Slope. It computes horizontal lengths from unchanged XZ cubics, passes original
endpoint/node Y offsets to the pure fitter, checks all outputs before publishing,
and writes interior node heights directly. It bypasses the later alignment step
because the fit already satisfies those constraints. Ease and Arch retain their
existing code and alignment; ordinary Curve elevation semantics are unchanged.

CanApply now requires a completed valid numerical result for Constant Slope as
well as Curve. Failed fits emit no candidate and cannot Apply. Existing Debug
native probe/automation revision guards remain in place. Junction Curve search
is consulted only for Curve, not incorrectly inherited by the new Slope result.

Non-deploying Debug compilation succeeds; existing 96 Slope assertions pass.
The independent geometry suite passes 338 profile assertions. These do not yet
execute the NativeArray adapter or certify native reconstruction. Next: unique
checkpoint of the baseline experiment, graceful shutdown, full Debug deployment,
then exact-baseline native testing. No success claim for the integrated fix yet.