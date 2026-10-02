# Preserve preview structural metadata

Follow-up to 2026-10-01-2245-preview-structural-composition.md. User authorized
fixing the demonstrated elevated-transition preview mismatch. Preserve existing
node and edge Elevation independently; do not change permanent Apply semantics.
Working branch dan/combined-smoothing-sprint from 92c10bf. Existing package-lock
change excluded. Offline course-construction regression first, then native replay
of the checkpointed transition; broader qualification reported separately.

Implemented pure RoadShapePreviewCourse factory and used it for selected/incident definitions. Removed edge-wide clamps/force flags; exact stored elevations now survive course construction. Compiled-production offline suite passed, including 86 new structural checks and a discriminating call into the installed game's composition classifier. Native qualification pending.
