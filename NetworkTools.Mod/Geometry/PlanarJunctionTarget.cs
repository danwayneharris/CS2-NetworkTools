namespace NetworkTools.Geometry {
    using Point = PlanarFairing.Point;

    /// <summary>
    /// Experimental section fit around fixed junction ports. Not wired into the
    /// game transform: native connection validation is required before integration.
    /// A junction keeps its original incident endpoint and adjacent handle, rather
    /// than imposing an ordinary split's shared tangent across different branches.
    /// </summary>
    public static class PlanarJunctionTarget {
        public static unsafe bool Fit(Point* nodes, PlanarCubic* curves, byte* splits, int count,
            double strength, Point* outputNodes, PlanarCubic* outputCurves,
            Point* workNodes, PlanarCubic* workCurves, double* stations,
            out SmoothFailure failure, out int index,
            double startRotation = 0, double endRotation = 0) {
            failure = SmoothFailure.InvalidArguments;
            index = -1;
            if (nodes == null || curves == null || splits == null || outputNodes == null
                || outputCurves == null || workNodes == null || workCurves == null
                || stations == null || count < 2 || count > 512) return false;
            // Junction pins and player splits have distinct meanings. Reject an
            // ambiguous request rather than silently changing its constraint type.
            for (var i = 1; i < count - 1; i++) {
                if (nodes[i].Fixed && splits[i] != 0)
                    return PlanarPathTarget.Fail(SmoothFailure.InteriorPinnedNode, i, out failure, out index);
            }
            for (var start = 0; start < count - 1;) {
                var end = start + 1;
                while (end < count - 1 && !nodes[end].Fixed) end++;
                if (!PlanarSplitTarget.Fit(nodes + start, curves + start, splits + start, end - start + 1,
                    strength, outputNodes + start, outputCurves + start, workNodes, workCurves,
                    stations, out failure, out index,
                    start == 0 ? startRotation : 0, end == count - 1 ? endRotation : 0)) {
                    if (index >= 0) index += start;
                    return false;
                }
                if (start > 0) {
                    var edge = outputCurves[start];
                    edge.A = curves[start].A;
                    edge.B = curves[start].B;
                    outputCurves[start] = edge;
                    outputNodes[start] = nodes[start];
                }
                if (end < count - 1) {
                    var edge = outputCurves[end - 1];
                    edge.C = curves[end - 1].C;
                    edge.D = curves[end - 1].D;
                    outputCurves[end - 1] = edge;
                    outputNodes[end] = nodes[end];
                }
                start = end;
            }
            failure = SmoothFailure.None;
            index = -1;
            return true;
        }
    }
}
