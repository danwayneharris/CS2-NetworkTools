# User clarification: small geometric errors are acceptable

Dan explicitly clarified during this sprint that millimeter-scale errors through
5 cm are trivial, should not be chased, and should not count as failures. The
future user-facing parameter remains deferred. This supersedes the sprint prompt's
earlier instruction not to relax the old geometric threshold.

Live regression acceptance now uses a named 0.05 m world-space tolerance. Measured
values remain recorded. The 23.5183 mm rail-center discrepancy is an acceptable
observation under this policy, not a failed case. Historical reports keep their
original pass/fail result and threshold context; they are not rewritten.

This applies to world-space geometry comparisons, including fixed centers, heights,
preview/Apply controls and small outside-selection adjustments. It does not excuse
missing/added lane connections, changed physical lane identities, altered topology,
invalid data, stale candidates or uncertain Apply. Analytical math/unit tests retain
their numerical precision checks: that is test determinism, not a demand to hunt
sub-centimeter artifacts in the game. Runtime native validation and UI settings
are not broadly rewritten as part of this acceptance-policy update.

The ongoing four boundary-option cases started with the old threshold; new cases
will report geometryToleranceMeters explicitly. We will repeat the rail sequence
under the newly authorized policy rather than continuing an uncertain operation.
