namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    /// <summary>A horizontal cubic and exact subinterval reconstruction, independent of CS2.</summary>
    public struct PlanarCubic {
        public Point A, B, C, D;

        public PlanarCubic(Point a, Point b, Point c, Point d) {
            A = a; B = b; C = c; D = d;
        }

        public Point Evaluate(double t) {
            var u = 1 - t;
            return new Point(u*u*u*A.X + 3*u*u*t*B.X + 3*u*t*t*C.X + t*t*t*D.X,
                u*u*u*A.Z + 3*u*u*t*B.Z + 3*u*t*t*C.Z + t*t*t*D.Z);
        }

        public Point Derivative(double t) {
            var u = 1 - t;
            return new Point(3*u*u*(B.X-A.X) + 6*u*t*(C.X-B.X) + 3*t*t*(D.X-C.X),
                3*u*u*(B.Z-A.Z) + 6*u*t*(C.Z-B.Z) + 3*t*t*(D.Z-C.Z));
        }

        /// <summary>
        /// Exact cubic over a parameter interval. This is parameter subdivision, not
        /// arc-length stationing. Caller supplies finite curve coordinates.
        /// </summary>
        public bool TrySlice(double start, double end, out PlanarCubic result) {
            result = default;
            if (double.IsNaN(start) || double.IsNaN(end) || start < 0 || end > 1 || start >= end) {
                return false;
            }
            var a = Evaluate(start);
            var d = Evaluate(end);
            var da = Derivative(start);
            var dd = Derivative(end);
            var scale = (end - start) / 3;
            result = new PlanarCubic(a, new Point(a.X + scale*da.X, a.Z + scale*da.Z),
                new Point(d.X - scale*dd.X, d.Z - scale*dd.Z), d);
            return true;
        }
    }
}
