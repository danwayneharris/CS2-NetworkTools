# NT-022 lateral lane alignment investigation

Base PR21/a7820cc. Lane direction does not imply lateral lane correspondence. Imported independently tested pure minimax group translation (664 assertions) and common-plane intersection/projection (228 assertions) helpers. No user alignment feature claimed yet.

Source feasibility identifies a necessary output distinction: outer CoursePos must reference the original fixed node independently of the shifted curve endpoint. Native StrictNodes/StraightEdges are outside this experiment. Added explicit launch-flag `--nt-connect-fixed-anchor-probe`: Simple legacy curve only, fixed 1.5m lateral offsets on both endpoint/control pairs, fixed original node entities/positions/rotations, unconditional shared Apply rejection. This is preview-only diagnostic scaffolding, not a fake user-facing alignment control. Native experiment pending.

Full alignment additionally needs immutable unaligned native layout, explicit selected new-road groups, frame/width correspondence and pair-specific native geometry/connectivity acceptance. Pure math cannot establish those. Do not infer a general guarantee from the fixed-node probe.
Aggregate first run: pure alignment/plane suites passed, but production compilation found the missing Colossal.Entities extension import in the diagnostic adapter. Added the import; production and full packaging must pass before launch. The aggregate correctly returned failure, not a partial-green status.


## Native result: output-only lateral shift violates fixed-node contract

Full Debug/UI/postprocess/deploy of clean fbd91c38cdaa passed; all 14 offline suites passed after import correction. Probe script initially configured before selection (correctly rejected) and then assumed object vectors instead of provider arrays (report-only error); corrected both, retained captures. No Apply occurred in those attempts.

Completed native probe on verified terrain v1.1: authored 1.5m XZ endpoint shifts survived native reconstruction exactly, but original temporary nodes moved 0.749942m and 0.750012m. Diagnostic current-token Apply was rejected and permanent fingerprint remained baseline. See compact nt022-native-evidence.json. This is a FAILED fixed-node prerequisite, not a passing lane-alignment test.

Installed Game1.6.2f1 NodeAlignSystem.AlignNode96-169 sums compatible incident endpoints and averages them, followed by tangent-line center resolution. Query293 processes Node+Updated and does not consult Fixed, StrictNodes or CoursePos flags. ReferencesSystem.GetNodePosition503-585 computes the corresponding cached/predicted position. StrictNodes clamps shifted curve endpoints back to the node; forcing Standalone changes native road semantics. No unsupported flag retry or global native patch was attempted.

Therefore NT022 is partial: pure group fit / geometric intersection tools and bounded native diagnostic are delivered, user alignment controls and Apply remain unimplemented. A different transition/composition strategy must demonstrate fixed existing nodes and individually paired native lanes before UI completion. Do not silently allow node movement, edit neighbors, or call endpoint direction a substitute for lane alignment.


## Final normal-mode handoff

Restarted without the probe flag; normal default Connect preview is accepted. Read-only final capture confirms paused terrain v1.1 and exact baseline fingerprint, no unsaved permanent changes. Deployed build remains clean fbd91c38cdaa; subsequent changes are reusable evidence scripts and documentation. Preserved every stage's package/manifest and result checkpoints. Final 14-suite offline aggregate passed, including the new 12-case working-tree archive tests; full report artifacts/final-six-plan-offline. Local verified archive retains 3,169 raw evidence files without adding bulk captures to Git. Sixth PR remains explicitly partial/blocked for player lane alignment; all others retain their documented coverage gaps. No Bridge changes, merges, baseline overwrites or real-city operations.
