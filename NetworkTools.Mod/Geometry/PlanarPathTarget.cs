namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    /// <summary>Experimental single-target fit for a simple, forward-going path.</summary>
    public static class PlanarPathTarget {
        private static bool Finite(Point p) => !double.IsNaN(p.X) && !double.IsInfinity(p.X)
            && !double.IsNaN(p.Z) && !double.IsInfinity(p.Z);
        private static Point Difference(Point a, Point b) => new Point(a.X-b.X, a.Z-b.Z);
        private static double Length(Point p) => Math.Sqrt(p.X*p.X+p.Z*p.Z);
        private static Point Blend(Point a, Point b, double s) => s == 0 ? a : s == 1 ? b
            : new Point(a.X+s*(b.X-a.X), a.Z+s*(b.Z-a.Z));

        /// <summary>
        /// Inputs and outputs must not overlap; count nodes, count-1 path-ordered curves,
        /// and count doubles of scratch are required. Discard all outputs on rejection.
        /// Interior junction pins and paths that turn back along the overall chord are
        /// unsupported. Endpoint nodes and outer curve endpoints stay fixed.
        /// </summary>
        public static unsafe bool Fit(Point* nodes, PlanarCubic* curves, int count, double strength,
            Point* outputNodes, PlanarCubic* outputCurves, double* stations) {
            return Fit(nodes, curves, count, strength, outputNodes, outputCurves, stations, out _, out _);
        }

        public static unsafe bool Fit(Point* nodes, PlanarCubic* curves, int count, double strength,
            Point* outputNodes, PlanarCubic* outputCurves, double* stations,
            out SmoothFailure failure, out int failureIndex) {
            return Fit(nodes, curves, count, strength, outputNodes, outputCurves, stations,
                out failure, out failureIndex, 0, 0);
        }

        public static unsafe bool Fit(Point* nodes, PlanarCubic* curves, int count, double strength,
            Point* outputNodes, PlanarCubic* outputCurves, double* stations,
            out SmoothFailure failure, out int failureIndex, double startRotation, double endRotation) {
            failure = SmoothFailure.None;
            failureIndex = -1;
            if (double.IsNaN(startRotation) || double.IsInfinity(startRotation)
                || double.IsNaN(endRotation) || double.IsInfinity(endRotation)
                || Math.Abs(startRotation) > Math.PI / 12 || Math.Abs(endRotation) > Math.PI / 12)
                return Fail(SmoothFailure.InvalidArguments, -1, out failure, out failureIndex);
            if (nodes == null || curves == null || outputNodes == null || outputCurves == null
                || stations == null || count < 2 || double.IsNaN(strength) || strength < 0 || strength > 1) return Fail(SmoothFailure.InvalidArguments, -1, out failure, out failureIndex);
            stations[0] = 0;
            for (var i=0; i<count; i++) {
                if (!Finite(nodes[i])) return Fail(SmoothFailure.NonFiniteNode, i, out failure, out failureIndex);
                if (i == 0) continue;
                var length = Length(Difference(nodes[i], nodes[i-1]));
                if (double.IsInfinity(length) || length < .01) return Fail(SmoothFailure.DegenerateNodeChord, i, out failure, out failureIndex);
                stations[i] = stations[i-1] + length;
                var c = curves[i-1];
                if (!Finite(c.A) || !Finite(c.B) || !Finite(c.C) || !Finite(c.D)) return Fail(SmoothFailure.NonFiniteCurve, i-1, out failure, out failureIndex);
            }
            if (double.IsInfinity(stations[count-1])) return Fail(SmoothFailure.PathLengthOverflow, -1, out failure, out failureIndex);
            if (strength == 0) {
                for (var i=0;i<count;i++) outputNodes[i]=nodes[i];
                for (var i=0;i<count-1;i++) outputCurves[i]=curves[i];
                return true;
            }
            var first=curves[0]; var last=curves[count-2];
            var chord=Difference(last.D,first.A);
            var chordLength=Length(chord);
            if (chordLength < .01 || double.IsInfinity(chordLength)) return Fail(SmoothFailure.DegenerateBoundaryChord, -1, out failure, out failureIndex);
            for (var i=1;i<count;i++) {
                if (i<count-1 && nodes[i].Fixed) return Fail(SmoothFailure.InteriorPinnedNode, i, out failure, out failureIndex);
                var delta=Difference(nodes[i],nodes[i-1]);
                if ((delta.X*chord.X+delta.Z*chord.Z)/chordLength < .01) return Fail(SmoothFailure.BackwardNodeChord, i, out failure, out failureIndex);
            }
            if (!PlanarBezier.Handles(first.A,last.D,Rotate(Difference(first.B,first.A), startRotation),
                Rotate(Difference(last.D,last.C), endRotation),out var b,out var c2)) return Fail(SmoothFailure.BoundaryTangents, -1, out failure, out failureIndex);
            var target=new PlanarCubic(first.A,b,c2,last.D);
            var total=stations[count-1];
            for (var i=0;i<count;i++) {
                stations[i] /= total;
                outputNodes[i] = i==0 || i==count-1 ? nodes[i] : Blend(nodes[i],target.Evaluate(stations[i]),strength);
            }
            for (var i=0;i<count-1;i++) {
                if (!target.TrySlice(stations[i],stations[i+1],out var part)) return Fail(SmoothFailure.InvalidSlice, i, out failure, out failureIndex);
                var old=curves[i];
                // Preserve outer curve endpoints exactly, independently of node offsets.
                outputCurves[i]=new PlanarCubic(i==0 ? old.A : Blend(old.A,part.A,strength),
                    Blend(old.B,part.B,strength),Blend(old.C,part.C,strength),
                    i==count-2 ? old.D : Blend(old.D,part.D,strength));
            }
            return true;
        }

        private static Point Rotate(Point p, double angle) => new Point(
            p.X * Math.Cos(angle) - p.Z * Math.Sin(angle),
            p.X * Math.Sin(angle) + p.Z * Math.Cos(angle));

        public static bool Fail(SmoothFailure reason, int index, out SmoothFailure failure, out int failureIndex) {
            failure = reason; failureIndex = index; return false;
        }
    }
}
