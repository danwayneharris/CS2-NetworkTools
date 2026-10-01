# 2026-10-01 0230 — Slope first, then Curve preview

User reports Slope-first looked good, then the Curve Smooth preview introduced a
visible kink, suspected to be terrain-related. Captured live state read-only:
citySession fb0966ce3e40492baf88cc560f9c392f, paused; CurveSmooth strength 1,
revision 345/submission 162, five ramp edges, previewReady true. Did not Apply,
clear selection, save, restart, or change parameters.

scripts/audit-current-curve-grades.py retains the matching SmoothTrace, captures
native previews at the selected nodes and checks the provider token remains stable.
All five native edge curves match requested output exactly. All node and control
point Y changes are zero. This is direct evidence against native terrain height
adjustment as the cause of this centerline kink; no terrain or rendered surface
samples were taken, so terrain/mesh interactions in general remain unassessed.

At the first ramp join:
- Permanent slope-first geometry: outgoing grade -6.40965%, incoming -6.40988%.
- Curve preview: outgoing -8.20729%, incoming -6.51159%.
- Grade discontinuity about 1.696 percentage points after horizontal smoothing.
- First segment's steepest grade changes from -7.52661% to -8.61197%.

The cause is horizontal handle changes with fixed control heights: dy/ds changes
when horizontal distance changes, even with identical height values. Planar tangent
continuity does not imply 3D tangent/grade continuity. This is distinct from the
previous Constant Slope endpoint-alignment tradeoff. CurveSmoothTransform and
NetworkTools.Geometry have no diff from merged main 4b5fc1c; WithHorizontal explicitly
preserves original.y. Thus this behavior predates this terrain-work branch, while
the deeper Constant Slope dip previously isolated remains partly attributable to
our recent alignment change. Do not combine these into a claim of no regressions.

PR checkpoint recommendation: yes, with accurate scope and explicit limitations.
Include terrain fixtures/diagnostics, guarded Slope/Connect automation, selected-asset
and editor-binding fixes, Slope preview/Apply consistency, and RoadShape node-reduction
fix. Disclose the alignment-induced vertical-profile tradeoff, existing Curve-only
3D-grade discontinuity, and deferred terrain-aware/combined smoothing. Captured
native and permanent checks are not visual or vehicle certification. User visually
approved most Curve-then-Slope toy results, with the off-ramp exception.

Evidence: captures/slope-first-curve-preview and captures/slope-first-grade-comparison.
No production changes this turn. Game left paused with user's original preview.

Raw capture files referenced above are available in the
[diagnostic archive](../diagnostic-archives.md); compact summaries and replay inputs
remain tracked locally.
