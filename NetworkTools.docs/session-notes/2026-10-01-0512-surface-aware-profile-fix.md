# Surface-aware Constant Slope experiment ? 2026-10-01 05:12 PDT

User authorized trying the correction after junction flattening isolation.
Working on the existing isolated sprint branch; preserve neighboring highways and
baseline saves. Start with a restricted captured-case inverse fit before deciding
whether it is sound enough for production integration. No universal terrain fix
is assumed. Geometry residuals through 5 cm remain acceptable.

Plan: model native boundary generation at unchanged horizontal geometry, preserve
the terminal junction surface anchor, fit the usable ramp profile, then independently
measure generated grades and connection preservation. Native pure-math calls use
local structs, not live ECS writes. Temporary/permanent retention behavior must be
verified separately before claiming a fix.


## First implementation

Three affine response variables adjust the virtual profile start height (tapered
to zero at the far end) and the first two vertical handles. Logical endpoint nodes
and the retained junction surface heights remain fixed. Constant grade is defined
along the ROAD CENTER, not simultaneously along inner/outer boundaries with unequal
arc lengths. The first, incorrect per-boundary target left 7.7 cm residual; the
center-profile objective gives 3.50 cm maximum control-height residual without
changing tolerance. Target grade is about -9.60%.

Pure native calls used local math only. A private value-type job method failed the
evaluator's argument binder; the probe instead expresses the short cut formula
using public math. No live edits occurred in that failed probe.

The experimental correction supports ground SmoothElevation paths, one terminal
junction and a dead end, degree-two interior nodes, zero offset/flatness, no special
node flags or boundary-smoothing options, and cutback beyond the first native half.
It normalizes reverse selection, requires generated XZ to match the model within
5 cm, bounds parameter changes to 20 m, re-evaluates the fitted response and checks
that the middle-height limiter would not move it beyond tolerance. Other cases
retain the regular Constant Slope fit. This is deliberately not a general terrain fix.

Geometry tests and non-deploying compile passed; full Debug pipeline then passed
postprocessing/UI/deployment, 32 warnings and 0 errors. Release/Burst remains
unverified. Build output DLL SHA256:
AD7BB5725B83AA1C5874449475DCF163EEE70F7D8FC4259D165CD07BAC40B059.
Recovery checkpoint before graceful close:
CitiesIIAgentBridge-regression-before-reload-20261001-121812-8112dcb2.cok.
The pre-highway full-ramp review save loaded, paused, with exact expected network
fingerprint ae11597e37fef69e37c717c9b1104d860ee6db4214f2be76ef8e40f5fb7fb1e7.


## First live candidate ? improvement, not target completion

The correction ran in Preview and Apply. Exact authored preview/Apply agreement,
unchanged topology, directed connections, physical lane mappings, horizontal node
positions and fixed endpoints; no unselected authored curves changed.
After fingerprint: 79c353d4b7a5d95ca8b78ca3b0e8882d290af9cc25377628a38849a385532657.

Dan reports the result looks MUCH better. Permanent native evidence still rejects
the fixed-anchor hypothesis: changing the first handle raised the shared derived
node height from ~616.625 to ~617.944 m. The generated ramp start rose to ~617.831 m
instead of staying ~616.428 m. The limiter still clamps the middle and one reported
lane piece peaks at 17.93% (earlier two pieces reached 27?32.5%). This is not yet
a constant-grade success or proof of repeat-operation stability. Native cut XZ
also moves ~0.1 m despite unchanged authored XZ, so observed cut locations are not
fully invariant. The favorable visual result is preserved in a unique checkpoint.

Offline fixing the junction-driving handle entirely leaves ~27 cm surface-fit
residual, exceeding tolerance. Do not relax the threshold to accept it. Next make
the shared-height/flattening feedback part of the candidate prediction and test
repeat application explicitly.

The npm-generated lockfile rewrite was inspected and backed up under artifacts
before restoration. Root dependency declarations were unchanged. An initial
unprotected restoration was blocked by automatic review; the preserved, verified
cleanup subsequently succeeded. No original-worktree edits were touched.
