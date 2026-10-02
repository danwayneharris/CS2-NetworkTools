# Ramp-to-road junction surfaces: mismatch localized

Dan identified the visible discrepancy at ramp intersections and requested actual
surrounding terrain inspection. Earlier edge-only equality did not cover these
joining surfaces. No runtime fix in this increment.

Preserved the live result in regression-before-reload-20261002-071812-4e49672a,
then visibly reloaded the pre-Apply checkpoint
CitiesIIAgentBridge-surface-preview-before-apply-20261002-070428-c983f9ef.
The first read during loading rejected a city-session identity change; no mutation
was submitted. Once ready, resolved fresh nodes and verified the saved incident
curves, then recreated Combined CurveSmooth strength 1 with both boundary smoothers.
No split pins. Submission 2 stayed unchanged through detailed probes and Apply.

## Measurements

Read native StartNodeGeometry and EndNodeGeometry directly using bounded Unity
read-only eval calls. Captured four joining boundary cubics plus the middle cubic,
sync targets and middle radius for each end of five observed edges: ten probes,
69 scalar fields each. These are the junction joining surfaces, distinct from
EdgeGeometry. Applied once after another verified checkpoint:
CitiesIIAgentBridge-surface-preview-before-apply-20261002-072301-837eba82.

Four ends at the interior junction differ materially after Apply:
- selected edge 54264:1 start: maximum 0.942020m (joining boundary Y);
- selected edge 54263:1 end: 1.402050m (joining boundary Y);
- incident ramp 54261:1 start: 0.513960m (middle endpoint Y);
- incident ramp 54262:1 end: 1.844440m (joining boundary Y).
Other observed ends match exactly. These are control-point deltas, not a mesh
Hausdorff metric; nevertheless they confirm a substantial native junction-surface
mismatch in precisely the area the user identified. The prior elevated/ground
composition fix remains valid but did not resolve this independent mismatch.

Terrain sampling expanded to 21x21 grids at 3m spacing, covering 60x60m around each
selected node, plus samples along/alongside the incident curves. All 1,548 terrain
sample heights are unchanged before/after Apply in this reproduction. This does
not measure every terrain vertex or exclude terrain as an input to generation;
it does argue against *changed sampled terrain* being the immediate explanation.

## Source / next investigation

Installed Game.Net/GeometrySystem.cs generates these separate components. The
intersection stage around lines 2872-2940 reads NodeGeometry, composition and
neighboring Start/EndNodeGeometry; FindIntersectionPos around 3021 averages incident
joining endpoints and Flatten adjusts the joining curves. Terrain sampling in this
stage contributes bounds; additional native stages must be traced before assigning
causality. Next compare its node/incident inputs and preview/permanent update order,
without changing the accepted smoothing target or relaxing validation.

## Reproducibility and limits

Added junction-surface-probes.py (offline manifest generator for read-only Unity
expressions) and compare-junction-surface-probes.py (offline numeric comparison).
Expanded current-preview capture with capture-only mode, dense terrain sampling,
and combined preview recreation. The degree-one bridge discovery limitation stays
explicit; its edge capture is not relabeled as a complete junction snapshot.
Raw probes and requests remain under ignored artifacts/junction-surface-*; compact
numeric differences are in 2026-10-02-junction-surface-differences.json.

Native measurement confirms the bug, not its cause or a correction. Runtime still
04792dd, Debug. No new build, deployment, or Release testing. Game paused after Apply.

Final verified checkpoint: CitiesIIAgentBridge-junction-surface-mismatch-after-20261002-072749-74d5c33d. No subsequent permanent changes; selection cleared by Apply.

Follow-up: see [junction input investigation](2026-10-02-0050-junction-inputs.md)
for corrected full EdgeGeometry comparison, native cut/flatten replay, subsequent
unsuccessful rebuild experiments, and the exact paused-work handoff.
