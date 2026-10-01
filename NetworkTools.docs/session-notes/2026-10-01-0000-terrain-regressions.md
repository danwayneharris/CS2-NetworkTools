# Terrain first-batch regressions

Authorized by Dan after reviewing labeled terrain/elevation v1.1. Preserve baseline
and use reduced toy playset, paused. Prepared position-based fixtures for the two
isolated roads, both rail branches and the highway mainline/ramps. Every suite case
uses the existing checkpoint/graceful-reload workflow and discovers new identities.

New terrain-regression.py wraps existing strict assertions without changing their
thresholds. It reports sampled grade, horizontal length and centerline-ground offset
before/after, including failures. These diagnostics are not pass/fail grade policy,
clearance certification or claims about unchanged terrain. Added analytic tests for
constant grade, reversed traversal and degenerate horizontal tangent handling.

No mod or bridge source changes or deployment. Pending live results below.
