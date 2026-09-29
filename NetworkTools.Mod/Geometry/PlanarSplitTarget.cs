namespace NetworkTools.Geometry {
    using Point = PlanarFairing.Point;

    /// <summary>Managed convenience wrapper over the job-compatible split fitter.</summary>
    public static partial class PlanarSplitTarget {
        public static unsafe bool TryFit(Point[] nodes, PlanarCubic[] curves, bool[] splits,
            out Point[] fitted, out PlanarCubic[] output, out SmoothFailure failure, out int index,
            double strength = 1) {
            fitted = null; output = null; failure = SmoothFailure.InvalidArguments; index = -1;
            if (nodes == null || curves == null || splits == null || nodes.Length < 2
                || nodes.Length > 512 || curves.Length != nodes.Length - 1 || splits.Length != nodes.Length) return false;
            var mask = new byte[nodes.Length];
            for (var i=0;i<mask.Length;i++) mask[i] = splits[i] ? (byte)1 : (byte)0;
            var candidateNodes = new Point[nodes.Length]; var candidateCurves = new PlanarCubic[curves.Length];
            var workNodes = new Point[nodes.Length]; var workCurves = new PlanarCubic[curves.Length];
            var stations = new double[nodes.Length];
            fixed (Point* n=nodes, o=candidateNodes, w=workNodes)
            fixed (PlanarCubic* c=curves, r=candidateCurves, v=workCurves)
            fixed (byte* m=mask)
            fixed (double* s=stations) {
                if (!Fit(n,c,m,nodes.Length,strength,o,r,w,v,s,out failure,out index)) return false;
            }
            fitted=candidateNodes; output=candidateCurves; return true;
        }
    }
}
