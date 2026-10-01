# Neighbor highway Apply changes the ramp and its surroundings

Dan reports the full ramp checkpoint still has visible grade variation and a small
kink. Previewing Constant Slope on the highway makes the ramp look better; Apply
also changes the surrounding terrain, which the preview does not reflect. Dan had
already tried Apply and reloaded to recreate the case before this observation.

Captured the paused toy session d79396bc922d4417822d2985be7165bf. Permanent curves
before Apply exactly matched the final review save; the highway was in ready
Constant Slope preview, both boundary smoothing options enabled. Four selected
highway edges, five path nodes. The Gameface endpoint and native computer-use pipe
were unavailable, so these are bridge measurements plus Dan's visual observations,
not a fresh assistant screenshot assessment.

Saved and verified unique checkpoint
CitiesIIAgentBridge-before-highway-neighbor-slope-20261001-113259-a13873a0,
then applied the selected highway operation once. The initial attempt used the
Smooth Curve apply alias and was explicitly rejected with smooth_tool_not_active,
before mutation. The corrected slope_apply invocation completed; the game remains
paused, selection cleared, with the highway edit unsaved. Inspected directed lane
connections were preserved. Raw evidence: artifacts/highway-neighbor-apply-slope/.

The highway edit lowers the shared off-ramp junction by 3.8208 m. On first ramp edge
57080, curve controls A and B both move down 3.8208 m; C and D stay unchanged.
Horizontal coordinates stay unchanged. The start/end grades remain approximately
-7.153%, but the midpoint changes from -7.150% to -0.607%. Four mainline edges and
one other incident ramp edge also change. This is an actual off-selection geometry
change through shared-node/adjacent-handle movement, not just a terrain effect.

Consequently the reported improvement in visual constantness does not mean the
stored ramp profile became more constant. Both authored network changes and the
terrain/rendered surface response must be inspected. This strengthens the need to
compare the generated road surface and network-adjusted terrain; it does not yet
prove pristine-vs-adjusted terrain is the specific cause. Preview terrain appearance
must not be conflated with permanent terrain appearance.

Next controlled experiment: preserve this state; compare authored centerline,
generated road/lane surface and terrain samples at fixed ramp stations before and
after the neighboring edit. Then re-fit the ramp in the changed surroundings as a
separate operation. Do not silently treat moving an incident endpoint as a complete
profile refit, or infer terrain causality from this coupled edit alone.

Retained a reusable explicit-Apply capture script, with fresh preview-token checks,
checkpoint verification and no uncertain mutation retries. Subsequent invocations
require expected city session, expected geometry fingerprint and save root. No
production code changed. The previous experimental implementation was already
committed at 2978a6b, with final documentation at 5e35b0b.
