# Junction heights and generated car-lane grades

Read the installed Game 1.6.2f1 GeometrySystem and LaneSystem rather than assuming
terrain directly raises the authored curve. The following native stages matter:

- GeometrySystem 119-160 chooses eligible incident edges and derives a common
  NodeGeometry height. With at least two SmoothElevation edges, it blends the
  authored node height halfway toward an inverse-horizontal-handle-length-weighted
  average of neighboring handle heights. This is a shared junction calculation.
- Lines 480-524 split the authored curve, replace applicable endpoint heights and
  adjust the vertical handles/middle. Lines 375-407 cut boundaries back for the
  junction; lines 713-733 limit height deltas under the relevant flatness rules.
- FlattenNodeGeometryJob 1505-1671 can move boundary endpoints to satisfy pairwise
  slope constraints. Its mixed temporary/permanent branch can preserve original
  boundary heights. FinishEdgeGeometryJob 1725-1756 applies those results and then
  limits/straightens middle heights. These are candidate causes, not all proven
  active in this case.
- LaneSystem 2657-2700 derives edge lane anchors/segments from EdgeGeometry, not
  directly from the authored Curve. A smooth authored Curve therefore does not
  guarantee a near-constant generated car-lane grade.

New offline lane measurements from saved captures: the first ramp edge's two car
lane pieces reach about 32.5% and 27.2% before highway slope, versus about 17.5% and
7.9% after. The previous 7.15% authored-curve figure cannot stand in for those
actual generated lanes. These are sampled derivative grades, not vehicle traversal.
The first generated boundary half is only about 3 m long after the edit; the large
junction consumes much of the authored segment, concentrating grade variation in
what remains. This establishes the discrepancy, not its single responsible stage.

Read-only Unity debugger inspection of restored node 53534:1 reports
NodeGeometry.m_Position=612.99, flatness=0, offset=0. Replaying the eligible weighted
height formula using captured incident handles predicts 612.989984993 m. The
15-micrometre discrepancy is trivial and not a failure. The authored node is
613.5095 m. Earlier scratch computation incorrectly compared decorated endpoint
dictionaries and reversed edges; corrected identity comparison uses index/version.
The before-edit source prediction is 616.624804748 m; that historical intermediate
component was not captured and is not claimed as native-verified.

The live debugger has no held suspension or armed breakpoint. No network edits,
reloads or production code changes in this step. Same post-highway checkpoint is
loaded and paused. Reusable script analyzes lane grades and the restricted weighted
height branch, rejects unsupported special-node cases, and saves raw reports under
ignored artifacts. Native debugging confirmed only one stage; exact attribution of
the residual metre-scale boundary uplift still needs stage-level observation.

Next implementation direction: account for native junction surface boundary
conditions and shortened usable edge span, and validate actual generated lane
profiles. Do not counter-warp authored curves or patch vanilla constraints merely
to hide the error before that mapping is understood. Terrain remains a separate
measured neighbor interaction; this is not evidence that terrain is irrelevant.
