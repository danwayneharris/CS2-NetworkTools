namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    /// <summary>Managed research target builder. Does not yet run inside the Burst job.</summary>
    public static class PlanarSplitTarget {
        // Produces a full-strength planar target. Partial-strength blending is a separate
        // policy: blending two kinked source handles does not preserve target continuity.
        public static unsafe bool TryFit(Point[] nodes, PlanarCubic[] curves, bool[] splits,
            out Point[] fitted, out PlanarCubic[] output, out SmoothFailure failure, out int index) {
            fitted = null; output = null; failure = SmoothFailure.InvalidArguments; index = -1;
            if (nodes == null || curves == null || splits == null || nodes.Length < 2
                || nodes.Length > 512 || curves.Length != nodes.Length - 1 || splits.Length != nodes.Length
                || splits[0] || splits[nodes.Length - 1]) return false;
            var directions = new Point[nodes.Length];
            for (var i = 1; i < nodes.Length - 1; i++) {
                // A player split never bypasses the interior-junction guard.
                if (nodes[i].Fixed) return PlanarPathTarget.Fail(SmoothFailure.InteriorPinnedNode, i, out failure, out index);
                if (!splits[i]) continue;
                var previous = i - 1; while (previous > 0 && !splits[previous]) previous--;
                var next = i + 1; while (next < nodes.Length - 1 && !splits[next]) next++;
                if (!PlanarBezier.Tangent(nodes[previous], nodes[i], nodes[next], out directions[i]))
                    return PlanarPathTarget.Fail(SmoothFailure.BoundaryTangents, i, out failure, out index);
            }
            var candidateNodes = new Point[nodes.Length];
            var candidateCurves = new PlanarCubic[curves.Length];
            for (var start = 0; start < nodes.Length - 1;) {
                var end = start + 1; while (end < nodes.Length - 1 && !splits[end]) end++;
                var n = end - start + 1;
                var sectionNodes = new Point[n]; var sectionCurves = new PlanarCubic[n - 1];
                Array.Copy(nodes, start, sectionNodes, 0, n);
                Array.Copy(curves, start, sectionCurves, 0, n - 1);
                if (start > 0) {
                    var c = sectionCurves[0]; c.A = nodes[start];
                    c.B = new Point(c.A.X + directions[start].X, c.A.Z + directions[start].Z);
                    sectionCurves[0] = c;
                }
                if (end < nodes.Length - 1) {
                    var c = sectionCurves[n - 2]; c.D = nodes[end];
                    c.C = new Point(c.D.X - directions[end].X, c.D.Z - directions[end].Z);
                    sectionCurves[n - 2] = c;
                }
                var sectionOutput = new Point[n]; var curveOutput = new PlanarCubic[n - 1];
                var stations = new double[n];
                fixed (Point* a = sectionNodes, b = sectionOutput)
                fixed (PlanarCubic* c = sectionCurves, d = curveOutput)
                fixed (double* s = stations) {
                    if (!PlanarPathTarget.Fit(a, c, n, 1, b, d, s, out failure, out index)) {
                        if (index >= 0) index += start;
                        return false;
                    }
                }
                Array.Copy(sectionOutput, 0, candidateNodes, start, n);
                Array.Copy(curveOutput, 0, candidateCurves, start, n - 1);
                start = end;
            }
            fitted = candidateNodes; output = candidateCurves;
            failure = SmoothFailure.None; index = -1; return true;
        }
    }
}
