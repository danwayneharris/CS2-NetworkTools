# Stateful source replay — 2026-10-02 02:06 PDT

Continuing active research from clean 3fe3621. Previous turn made executable
progress; no goal completion or native pipeline agreement is claimed.

Implementing hash-pinned local source generation for InitializeNodeGeometry,
FlattenNodeGeometry and their EdgeIterator. Arithmetic/control-flow bodies come
from the local decompile; only storage interfaces and job entry scaffolding are
adapted. Generated proprietary source remains in ignored obj/native-generated.
The tracked generator and storage adapters document this distinction from original
binary execution. Unknown component data throws; explicit absence is represented.

This is a single-threaded explicit-chunk execution seam, not an ECS scheduler or
full-world reproduction. Query membership and coherent stage inputs still need
native capture. Validation pending initial compilation and discriminating tests.

Initial compile: generated native bodies compiled through storage binding, but
three adapter errors remain: a struct lambda captured its primary-constructor
parameter, and a parameter named underscore shadowed two intended out discards.
These are harness errors; preserve this incremental attempt before correcting them.

Corrected adapter compile errors. Original generated bodies now execute against
managed world storage. Initial 17 synthetic assertions pass for weighted node
height, Updated/load retention, missing-state rejection, pairwise flattening,
mixed temporary/permanent retention and height-map output. Added a read budget
and cyclic-reference negative test to bound malformed capture execution.

Added strict projected JSON capture input with original/temp identity, explicit
absence, ordered buffers, flags and geometry. Sequential stages observe computed
node writes. Nine capture-contract cases pass; original binary seven-case seam
suite also passes after the new source-stage build. This is synthetic qualification,
not evidence that the actual native anchor inputs have been collected.

Original game source remains in ignored generated output only. No native ECS
chunk scheduling or query-membership inference is claimed. Source field projections
are explicit in WorldCapture.cs; omitted fields outside the selected stages' read
sets are not represented as complete captured native components.

Dan reported multiple dotnet modal error dialogs during the negative tests. The
CLI let rejected fixtures escape Main as unhandled exceptions, so these tests could
trigger Windows crash reporting rather than ordinary command failures. Stopped
test reruns to correct the CLI: catch exceptions at the process boundary, write a
bounded stderr diagnostic and return exit 2. Do not change Windows crash reporting
settings to hide a harness defect. No such dialogs were visible through shell tools.

Verified the correction with one missing-Temp capture: bounded stderr diagnostic,
exit 2, and no result file. Event-log query returned no confirming error entries,
so attribution of the reported dialogs remains likely rather than proven. Strengthened
negative-test checks to require exit 2 plus the handled diagnostic; arbitrary process
crashes no longer count as successful rejection tests. Final synthetic world suite
before this fix passed 18 assertions (including the cyclic-read budget) and nine
JSON contract cases; rerun results below must use the strengthened checks.

Strengthened suites pass: nine JSON world contract cases and seven original-binary
seam cases. Every negative case now returns the controlled diagnostic/exit code,
with no success artifact. Reports are in artifacts/offline-research/world-contract-controlled
and seam-tests-controlled. Stateful native differential capture remains pending.
