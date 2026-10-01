# Review preparation and persistence verification

The remaining native matrix completed: rail Curve->Slope under the user's 5 cm
acceptance policy, an ordinary pinned split on the sloping hill road, and off-ramp
Curve strengths 0.5 and 1 followed by Constant Slope. Three successive Constant
Slope applications completed on the strength-1 ramp. Full measurements and exact
fingerprints are being reconciled for the final review guide; no visual approval
or vehicle traversal is inferred.

Added cross-reload physical lane mapping to the existing persistence inspector,
covering ordinary shared nodes as well as multi-branch junctions. Lane indices,
lateral positions, directions, carriageways and available prefab flags/limits are
retained. World-local prefab entity IDs are excluded from cross-session signatures;
within-session regression checks still use their full original signatures. Ambiguous
geometry-based owner labels are rejected rather than conflating overlapping edges.

The README now describes this experimental branch and the current 5 cm acceptance
policy instead of presenting old millimeter-scale reports as current failures.
Named half/full ramp review checkpoints have been created without replacing any
baseline. The final full-ramp checkpoint will be reloaded and independently inspected.
