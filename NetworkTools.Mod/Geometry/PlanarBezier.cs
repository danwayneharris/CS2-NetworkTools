namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    /// <summary>Horizontal cubic reconstruction; callers keep vertical coordinates separately.</summary>
    public static class PlanarBezier {
        /// <summary>
        /// Blends a handle after translating it with its endpoint. Strength is validated
        /// by the caller. Intermediate curves may retain defects from the original.
        /// </summary>
        public static Point BlendHandle(Point original, Point endpointDelta, Point target, double strength) {
            if (strength == 0) { return original; }
            if (strength == 1) { return target; }
            var x = original.X + endpointDelta.X;
            var z = original.Z + endpointDelta.Z;
            return new Point(x + strength * (target.X - x), z + strength * (target.Z - z));
        }

        /// <summary>Returns a shared unit tangent, weighting incident chord directions equally.</summary>
        public static bool Tangent(Point previous, Point current, Point next, out Point tangent) {
            tangent = default;
            if (!Unit(current.X - previous.X, current.Z - previous.Z, out var incoming)
                || !Unit(next.X - current.X, next.Z - current.Z, out var outgoing)) { return false; }
            return Unit(incoming.X + outgoing.X, incoming.Z + outgoing.Z, out tangent);
        }

        /// <summary>
        /// Places handles along endpoint directions. Ordered chord projections prevent
        /// an individual cubic from looping back. Does not prevent inter-segment crossings.
        /// </summary>
        public static bool Handles(Point start, Point end, Point startDirection, Point endDirection,
            out Point first, out Point second) {
            first = second = default;
            var dx = end.X - start.X;
            var dz = end.Z - start.Z;
            var length = Math.Sqrt(dx * dx + dz * dz);
            if (!Finite(length) || length < 0.01
                || !Unit(startDirection.X, startDirection.Z, out var a)
                || !Unit(endDirection.X, endDirection.Z, out var b)) { return false; }
            if ((a.X * dx + a.Z * dz) / length < 0.05
                || (b.X * dx + b.Z * dz) / length < 0.05) { return false; }
            first = new Point(start.X + a.X * length / 3, start.Z + a.Z * length / 3);
            second = new Point(end.X - b.X * length / 3, end.Z - b.Z * length / 3);
            return true;
        }

        private static bool Unit(double x, double z, out Point direction) {
            direction = default;
            var length = Math.Sqrt(x * x + z * z);
            if (!Finite(length) || length < 1e-8) { return false; }
            direction = new Point(x / length, z / length);
            return true;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
