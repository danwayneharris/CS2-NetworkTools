# Add interior preview latency work to milestone PR

The maintainer requested adding the measured improvement to the existing PR.
Verified NetworkTools PR #11 remains open against main, with its previous head
109a7a9. Fast-forwarded dan/provider-regression-sprint to include investigation
2221b50 and implementation a4407c7; no cherry-pick or history rewrite required.

The PR description now includes the native 1336 ms cold / 33-50 ms warm timing,
six connection-preserving previews, matching permanent Apply, and the additional
29.877 mm fixed-center failure against the unchanged 1 mm tolerance. Initial
candidate visibility and warm-start history dependence remain explicit limitations.
This publication changes no code or deployment; prior verification still applies.
Bridge and the original NetworkTools worktree remain untouched.
