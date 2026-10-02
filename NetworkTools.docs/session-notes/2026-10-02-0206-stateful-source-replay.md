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
