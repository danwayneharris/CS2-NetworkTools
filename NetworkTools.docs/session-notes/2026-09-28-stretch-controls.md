# Stretch controls investigation — 2026-09-28

Inspected PlanarPathTarget and path selection lifecycle. Added reproducible signed
blend counterexample and split-range research helper with two passing tests.
Negative extrapolation doubles an S-curve's excursion while length grows; length
alone is not smoothness. Proposed constrained target-length optimization instead.
Existing selection waypoints extend paths, not independent fit boundaries. Planned
explicit split-node set, shared tangents/corner policy, and atomic output publishing.
No UI/runtime changes or in-game assertions. Detailed design in smooth-curve-plan.md.

Next verification remains connected replacement-node snapshot after bridge deployment;
current installed bridge predates the resolver. No reason to change or relax the
interior-junction guard based on these research experiments.
