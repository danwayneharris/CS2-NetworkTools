# Offline junction surface stage — 2026-10-02 04:59 PDT

Continue from 65fa660. Four computed stages already pass anchor/control/changed
arch captures within approximately 1 mm; historical failing case is still missing.
Extend into CalculateNodeGeometry using existing captures before another live run.
GeometrySystem.cs lines 1831–2090 show this stage consumes finished EdgeGeometry
and writes StartNodeGeometry/EndNodeGeometry, with two ordered iterations. This
is necessary to qualify junction surfaces, not just the incident edge surfaces.
Installed Game.dll SHA256 again matches AAEE15C4...A7536618EFFA2F86A.

Keep native query membership explicit, preserve computed upstream writes, and
reject uncaptured inputs. No live mutation or deployment in this increment.

The original CalculateNodeGeometryJob compiles with the existing storage adapters,
without additional algorithm edits. Arch replay passes both iterations with maximum
curve-control error 1.1314 mm. Junction sync parameters are unitless (1e-5 bound);
branch markers and negative-length sentinels require exact equality. Existing
3 cm spatial bounds remain, with raw errors retained.

Initial missing-input test removed SubObject buffers but passed: the arch fixture
does not take the straight-node-end branch that reads them. This is not evidence
that missing dependencies default silently. Replace the test with removal of
NetCompositionPiece buffers actually observed in the read log. Retain the failed
test attempt under artifacts/offline-research/junction-contract-01.

Corrected ten-test suite passes in junction-contract-02. Preview, permanent and
single-edge linear control also pass both junction iterations, independently of
the changed arch. All execution used existing files, with no live capture needed.

Downstream source audit (same hash-pinned GeometrySystem.cs):
- CalculateNodeGeometry reads EdgeGeometry and writes both junction-end components
  (1831–2090). Both iterations now execute offline with propagated computed state.
- CalculateIntersectionGeometry (2820 onward) reads junction ends and terrain,
  computes middle curves/bounds into an indexed IntersectionData scratch list.
- CopyNodeGeometry (3074–3101) publishes that scratch list into only m_Middle and
  m_Bounds of StartNodeGeometry/EndNodeGeometry, preserving their side curves.
- UpdateNodeGeometry (3104–3201) publishes node bounds, including connected geometry
  and optional terrain samples for orphan nodes.
- OnUpdate (3561–3575) orders these after the two junction iterations and disposes
  scratch containers only after their final reader. Those three final stages are
  not yet captured by the current schedule hook. No evidence of their native
  execution is implied by the new five-stage replay.

Remaining scope: historical failing discrepancy, ordinary observation correlation,
final middle/bounds publication, and downstream lane/render consumers. This is an
incremental research result, not completed goal acceptance or a feature correction.
