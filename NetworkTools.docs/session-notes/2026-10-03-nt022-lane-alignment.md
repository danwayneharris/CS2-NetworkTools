# NT-022 lateral lane alignment investigation

Base PR21/a7820cc. Lane direction does not imply lateral lane correspondence. Imported independently tested pure minimax group translation (664 assertions) and common-plane intersection/projection (228 assertions) helpers. No user alignment feature claimed yet.

Source feasibility identifies a necessary output distinction: outer CoursePos must reference the original fixed node independently of the shifted curve endpoint. Native StrictNodes/StraightEdges are outside this experiment. Added explicit launch-flag `--nt-connect-fixed-anchor-probe`: Simple legacy curve only, fixed 1.5m lateral offsets on both endpoint/control pairs, fixed original node entities/positions/rotations, unconditional shared Apply rejection. This is preview-only diagnostic scaffolding, not a fake user-facing alignment control. Native experiment pending.

Full alignment additionally needs immutable unaligned native layout, explicit selected new-road groups, frame/width correspondence and pair-specific native geometry/connectivity acceptance. Pure math cannot establish those. Do not infer a general guarantee from the fixed-node probe.
Aggregate first run: pure alignment/plane suites passed, but production compilation found the missing Colossal.Entities extension import in the diagnostic adapter. Added the import; production and full packaging must pass before launch. The aggregate correctly returned failure, not a partial-green status.
