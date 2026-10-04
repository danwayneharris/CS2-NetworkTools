namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    /// <summary>
    /// Pure optional Connect profile for one or two horizontal cubics. A global cubic
    /// Hermite polynomial P(u), u=horizontalStation/totalLength, matches raw desired
    /// endpoint heights and path-oriented physical grades. A two-curve path samples
    /// P and P'/totalLength once at its actual arc-length join station.
    ///
    /// Returned Y controls preserve physical endpoint grades using each actual XZ
    /// handle length. They do not exactly represent P(s(t)) inside a curved horizontal
    /// cubic: arc length is generally nonlinear in its parameter. This is a join-grade
    /// construction, not a guarantee about native surfaces or terrain processing.
    /// Endpoint/native/structural offsets and chosen approach orientation are explicit
    /// caller policy; this helper never infers offsets, elevation flags or terrain Y.
    /// </summary>
    public static unsafe class ConnectVerticalProfile {
        public enum Failure { None, InvalidInput, UnsupportedSegmentCount, DegenerateHandle, HorizontalLength, DisconnectedJoin, HorizontalKink, NonfiniteOutput }
        public struct Result {
            public double TotalHorizontalLength;
            // For one curve these describe the middle station of the target profile,
            // not an extra emitted node. For two curves this is their existing join.
            public double JoinStationFraction;
            public double JoinHeight;
            public double JoinGrade;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Finite(Point point) => Finite(point.X) && Finite(point.Z);
        private static double Distance(Point a, Point b) {
            var x = a.X - b.X; var z = a.Z - b.Z;
            return Math.Sqrt(x * x + z * z);
        }

        /// <summary>
        /// Path-ordered input; reversing requires reversing each cubic and swapping /
        /// negating endpoint grades. Caller supplies count disjoint output entries.
        /// Returns only Y controls, never changes the horizontal input, and leaves the
        /// entire output buffer untouched on failure. No heap allocations or game types.
        /// </summary>
        public static bool Fit(PlanarCubic* horizontal, int count,
            double startHeight, double endHeight, double startGrade, double endGrade,
            VerticalLinearProfile.Heights* output, out Result result, out Failure failure) {
            result = default; failure = Failure.InvalidInput;
            if (horizontal == null || output == null || !Finite(startHeight) || !Finite(endHeight)
                || !Finite(startGrade) || !Finite(endGrade)) return false;
            if (count < 1 || count > 2) { failure = Failure.UnsupportedSegmentCount; return false; }
            var lengths = stackalloc double[2];
            var startHandles = stackalloc double[2]; var endHandles = stackalloc double[2];
            var staged = stackalloc VerticalLinearProfile.Heights[2];
            var total = 0.0;
            for (var i = 0; i < count; i++) {
                var curve = horizontal[i];
                if (!Finite(curve.A) || !Finite(curve.B) || !Finite(curve.C) || !Finite(curve.D)) return false;
                startHandles[i] = Distance(curve.A, curve.B); endHandles[i] = Distance(curve.C, curve.D);
                if (!Finite(startHandles[i]) || !Finite(endHandles[i]) || startHandles[i] < 1e-6 || endHandles[i] < 1e-6) {
                    failure = Failure.DegenerateHandle; return false;
                }
                if (!VerticalLinearProfile.TryHorizontalLength(curve, out lengths[i])) {
                    failure = Failure.HorizontalLength; return false;
                }
                total += lengths[i];
            }
            if (!Finite(total)) { failure = Failure.HorizontalLength; return false; }
            if (count == 2) {
                var left = horizontal[0]; var right = horizontal[1];
                if (Distance(left.D, right.A) > 1e-6) { failure = Failure.DisconnectedJoin; return false; }
                var inX = (left.D.X - left.C.X) / endHandles[0]; var inZ = (left.D.Z - left.C.Z) / endHandles[0];
                var outX = (right.B.X - right.A.X) / startHandles[1]; var outZ = (right.B.Z - right.A.Z) / startHandles[1];
                // A Y-only fit cannot repair a horizontal kink. Tolerance accommodates
                // float-origin handles; it is a dimensionless tangent-dot tolerance.
                if (inX * outX + inZ * outZ < 1 - 1e-5) { failure = Failure.HorizontalKink; return false; }
            }
            var rise = endHeight - startHeight;
            var m0 = total * startGrade; var m1 = total * endGrade;
            // P(u)=startHeight + ((a*u+b)*u+c)*u, evaluated about its height origin.
            var a = -2 * rise + m0 + m1;
            var b = 3 * rise - 2 * m0 - m1;
            var c = m0;
            var u = count == 2 ? lengths[0] / total : .5;
            var joinHeight = startHeight + ((a * u + b) * u + c) * u;
            var joinGrade = ((3 * a * u + 2 * b) * u + c) / total;
            if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(joinHeight) || !Finite(joinGrade)) {
                failure = Failure.NonfiniteOutput; return false;
            }
            for (var i = 0; i < count; i++) {
                var y0 = i == 0 ? startHeight : joinHeight;
                var y1 = i == count - 1 ? endHeight : joinHeight;
                var g0 = i == 0 ? startGrade : joinGrade;
                var g1 = i == count - 1 ? endGrade : joinGrade;
                var h = new VerticalLinearProfile.Heights {
                    A = y0, B = y0 + g0 * startHandles[i], C = y1 - g1 * endHandles[i], D = y1
                };
                if (!Finite(h.A) || !Finite(h.B) || !Finite(h.C) || !Finite(h.D)) {
                    failure = Failure.NonfiniteOutput; return false;
                }
                staged[i] = h;
            }
            for (var i = 0; i < count; i++) output[i] = staged[i];
            result = new Result { TotalHorizontalLength = total, JoinStationFraction = u, JoinHeight = joinHeight, JoinGrade = joinGrade };
            failure = Failure.None; return true;
        }
    }
}
