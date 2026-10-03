# Preserve preview structural metadata

Follow-up to 2026-10-01-2245-preview-structural-composition.md. User authorized
fixing the demonstrated elevated-transition preview mismatch. Preserve existing
node and edge Elevation independently; do not change permanent Apply semantics.
Working branch dan/combined-smoothing-sprint from 92c10bf. Existing package-lock
change excluded. Offline course-construction regression first, then native replay
of the checkpointed transition; broader qualification reported separately.

Implemented pure RoadShapePreviewCourse factory and used it for selected/incident definitions. Removed edge-wide clamps/force flags; exact stored elevations now survive course construction. Compiled-production offline suite passed, including 86 new structural checks and a discriminating call into the installed game's composition classifier. Native qualification pending.

## Native result (Debug 04792dd)

Full build/postprocessing/UI/deployment succeeded. All seven offline aggregate
suites passed, artifacts/preview-structure-aggregate/summary.json. Deployed DLL
SHA256 48EF3D8D36626D73C61F80133850509F6E8688337F05CF185533225636D0AEE9.

Gracefully closed only after checkpoint ZIP verification:
CitiesIIAgentBridge-regression-before-reload-20261002-060831-6a79ee38.
Visibly launched original pre-Apply toy checkpoint:
CitiesIIAgentBridge-surface-preview-before-apply-20261002-045548-ef974c79.
Resolved fresh endpoint identities by positions and verified all five original
incident authored curves before recreating the captured SlopeEaseInOut settings.
This is an authored-geometry-matched replay, not proof that save/reload preserves
identical transient terrain buffers.

Checkpoint before the new Apply:
CitiesIIAgentBridge-surface-preview-before-apply-20261002-061146-6c4414a6.
Applied once after bounded readiness and exact session/revision/submission checks.

- All five observed edges now match preview/permanent composition (edge and both
  end compositions), width and state. Ground road remains width 12; adjacent
  elevated segment retains its own elevated composition.
- All five authored curves and generated EdgeGeometry surface control points
  match exactly between preview and Apply. Previous selected-edge maximum
  corresponding-control discrepancy was 2.656899m, now zero.
- Surface results remained identical immediately, +3sec and +8sec after Apply.
- Directed car-lane transitions unchanged at both watched nodes: four and fourteen
  respectively; compared identities/pairs, not just counts. No added/removed pair.
- Terrain samples still changed after Apply, maximum 2.687565m across 225 samples.
  This differs from the earlier live session's 0.414m; save/reload/native terrain
  context is not claimed identical. Terrain deformation remains a separate limit.

Raw captures: artifacts/structure-fix-native; compact composition/geometry report
2026-10-01-preview-structure-fixed-comparison.json. Reusable preparation script
prepare-surface-preview-replay.py changes selection/parameters only after toy and
saved authored-geometry checks; compare-current-slope-preview.py explicitly
checkpoints and Applies; summarizer is offline only.

Final permanent result saved and ZIP verified:
CitiesIIAgentBridge-surface-preview-structure-fixed-20261002-061253-afdb4c8f.
Game remains paused, Wantagh toy, citySession 965ff4a044d64f0b931509325672a2ae.
Same segment reselected for human inspection; no permanent edits after final save.

## Qualification boundaries

High confidence for this ground/elevated-transition native regression. Tunnel,
portal, asymmetric elevation and reversal have compiled-production offline
coverage; no new native tunnel test, Release/Burst execution, vehicle traversal
or human visual signoff performed. The broader combined-mode/junction and terrain
limitations remain; this fix changes shared preview course construction only.

Native source clarification: CourseSplitSystem's collection path skips original-
linked edge courses (line 132), so the previous note's force-flag elevation stage
is general context, not a demonstrated executed stage in this reproduction. The
mod's direct endpoint elevation clamps are sufficient to explain the observed
new temporary node Elevation. Removing both the clamps and edge-wide forcing is
consistent with preserving original metadata, and native comparison confirms the
result here.
