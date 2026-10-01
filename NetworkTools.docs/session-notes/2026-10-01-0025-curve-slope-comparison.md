# Curve and Slope save comparison — October 1, 2026, 00:25

Dan supplied curve-smoothed and slope-smoothed toy saves, with mixed operation
orders. Inspect both as observed outcomes, not controlled sequential experiments.
Preserve both packages and checkpoint the current paused toy before any reload.
Current generic NT provider exposes Smooth Curve only; Slope automation needs a
separate provider extension, not an assumed mode switch. No deployment planned
for the initial comparison. Record terrain following as future policy, not a bug
in an explicitly unconstrained vertical shaping operation.

## Saved outcomes inspected

Loaded both exact named packages through the guarded lifecycle, checkpointing the
previous city before each graceful restart. No transformations, terrain edits or
simulation advance in this comparison. Left the slope-smoothed save loaded and
paused; source packages unchanged. Capture directory: captures/terrain-curve-slope-saves.

Peak sampled absolute grades (Curve save -> Slope save): hill road 27.26 -> 6.40%;
crest/dip 22.95 -> effectively 0%; high rail branch 44.84 -> 4.79%; low rail branch
44.84 -> 8.11%; mainline 27.90 -> 0.022%; ramp-in 21.93 -> 15.90%; ramp-out
37.14 -> 5.82%. These are observed saved outcomes with mixed operation history,
not controlled proof of one tool ordering or a grade safety standard.

Off-ramp path has a 3.63 percentage-point approach-grade mismatch at the junction:
level mainline followed by descending ramp. Native edge identity 51806:1 is the
three-way node in this load (rediscover on reload). The plot omits junction gaps
and native lane surfaces: do not label this measurement a proven surface crack.
Height/grade plots and per-edge signed derivatives saved in offramp-profiles.png
and profile-analysis.json. All seven paths traced successfully, but physical
adjacency is not directed lane preservation or vehicle traversal verification.

## Source leads, not yet causally isolated

RoadShapeToolSystem.PathData.cs:210-270 uses existing Curve.m_Length, 3D endpoint
and node gaps for path stationing. Core/EdgeState.cs:113-143 instead measures
horizontal straight distances from Bezier.a to handles b/c for control ratios.
SlopeLinearTransform.cs:24-31 samples the reference at those mixed ratios;
SlopeEaseInOutTransform.cs:53-64 uses their differences to set vertical handles.
This deserves an offline physical-grade continuity test on bent/sloped geometry;
it does not yet prove the cause of Dan's observed ramp defect.

More directly, PathData.cs ComputeAnchorSlope (around 290-330) only accepts exactly
one non-selected neighbor. At a three-way junction a ramp-only selection has two,
so automatic smooth-start/end anchoring is ineligible. This is a concrete scope
limitation; resolving it requires a junction-aware boundary grade policy, not just
turning on the existing checkbox. The saved history does not identify which
selections/modes/settings Dan used for each operation, so reproduce before fixing.

ProviderV1.cs exposes state/activate/clear/select/strength/split/apply for Smooth
Curve only. AutomationState/AutomationCommand also enforce CurveSmooth and its
preview submission fence. We cannot safely automate Slope by merely changing a
mode enum or bypassing the existing freshness gate. Next useful change is a
NetworkTools-owned Slope provider operation with real preview freshness/Apply
verification, then baseline Curve -> Slope and slope-only trials on the ramp.
No bridge-specific integration or manual re-creation required right now. No
runtime code or deployment changed in this session.

Terrain respect remains a future explicit policy. Flattening the crest/dip is
consistent with equal endpoint heights and an unconstrained vertical profile;
terrain-following and clearance require separate checks and design decisions.

Raw capture files referenced above are available in the
[diagnostic archive](../diagnostic-archives.md); compact summaries and replay inputs
remain tracked locally.
