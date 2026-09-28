# Preview validation path — 2026-09-28

Source inspection, no runtime modifications or live game queries.

- RoadShapeToolSystem.Jobs.cs:85 sets SmoothResult from CurveSmoothTransform.Execute,
  before OutputPreview at line 109. This reports mathematical fit success only.
- RoadShapeToolSystem.Update.cs:113 gates CanApply on SmoothPreviewResult == 1;
  the getter waits for the shape job, not for native lane reconstruction.
- RoadShapeToolSystem.JobMethods.cs:77 emits new preview definitions through the
  tool barrier. Apply at line 92 clears definitions, schedules OutputApply, then
  resets selection. Apply does not simply promote the inspected temporary network.
- Installed LaneSystem extraction: OnCreate at about 9250 queries updated/deleted
  SubLane owners without excluding Temp. UpdateLanesJob reads ownerTemp.m_Original
  and original SubLane buffers (around 624 and 706). Temporary lane generation is
  a real source path, but its availability/timing for our preview is not live-tested.
- Bridge src/JunctionSnapshot.cs:127 explicitly excludes Temp from eligible entities.
  The existing query cannot establish preview connectivity, by design.

Next bounded change: a separate read-only preview snapshot contract, preserving the
existing permanent-network query. Resolve temporary owners via Temp.m_Original;
report both identities and flags. Distinguish missing, ambiguous and pending data
from a completed negative connectivity result. Include the selected transform's
revision so old preview data cannot approve a new slider value. Determine the
native update/barrier observation point before wiring this into CanApply.

Start by observing a currently permitted endpoint-junction preview; do not remove
interior-junction rejection just to make the diagnostic possible. Compare captured
preview and applied results later on the toy save, since those output paths differ.
A native preview snapshot is evidence, not yet a feasibility solver or permission
to change the user's city. No build/deploy is needed for this research record.
