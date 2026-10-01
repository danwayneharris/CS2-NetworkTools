# Boundary coverage and bounded coordinated-fit research

All four native Constant Slope boundary combinations pass on the inner hill-road
selection: neither, start only, end only, both. Each enabled endpoint has exactly
one unselected neighbor and its vertical handle matches that neighbor's grade.
Topology, specific directed/physical lane connections, node XZ, fixed outer points,
and native preview/Apply agree. No unselected curve changed in these cases.

These tests deliberately use eligible pass-through endpoints, rather than checking
boxes on a dead end or multi-branch junction where they are ineligible. The maximum
sampled grade with both boundary overrides is about 7.80%; preserving a neighbor's
grade does not make the whole path constant or terrain-safe.

The reusable offline coordinated experiment takes captured Slope->Curve output,
recomputes horizontal stations and fits the new vertical profile. It predicts a
maximum interior elevation adjustment of 0.188117 m on the seven-edge ramp case.
This is an intended profile edit, not numerical error covered by the 5 cm tolerance.
It demonstrates the composition order, but is not a single native candidate test.

No combined UI mode was added. An explicit combined mode must distinguish horizontal
pins from elevation pins, and accommodate protected junction heights. Running the
whole-path Constant Slope fitter through those pins would violate their existing
meaning. Options are piecewise height profiles with continuous boundary grades, or
an explicit opt-in elevation policy; player-facing semantics remain for discussion.
The independently testable fitter and native two-stage comparisons provide a
useful foundation without inventing that choice.

Raw results: artifacts/profile-boundaries-0 through -3 and
artifacts/coordinated-profile-research. Plotting/analysis scripts are retained.
