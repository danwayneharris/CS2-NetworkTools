namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;
    using Heights = VerticalLinearProfile.Heights;

    /// <summary>
    /// Conservative authored-to-native cubic coverage. Each authored cubic must have
    /// strictly monotone projection along its endpoint chord. This makes endpoint
    /// parameter inversion unique; unsupported looping/backtracking shapes reject
    /// rather than relying on a nearest-point guess. No game/entity types are used.
    ///
    /// Every native cubic must match exactly one authored parameter subinterval,
    /// in either direction. All four corresponding control differences must satisfy
    /// the supplied XZ and Y tolerances; the Bernstein convex-hull property then
    /// bounds the whole corresponding cubic, not just a few sampled positions.
    /// Complete nonoverlapping parameter coverage and native endpoint continuity
    /// are separate stricter checks. This does not establish lane/topology identity.
    /// </summary>
    public static unsafe class ConnectProfileCoverage {
        public const int MaximumNativeCurves = 256;
        private const double ParameterTolerance = 1e-8;
        private const double AuthoredJoinTolerance = 1e-4;
        public enum Failure { None, InvalidInput, UnsupportedHorizontalMapping, CurveMismatch, AmbiguousMapping, Gap, Overlap, DisconnectedNative }
        public struct Cubic {
            public PlanarCubic Horizontal;
            public Heights Vertical;
        }
        public struct Mapping {
            public int AuthoredIndex;
            public double StartParameter, EndParameter;
            public bool Reversed;
            public double MaximumXZControlError, MaximumYControlError;
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        private static bool Finite(Point p) => Finite(p.X) && Finite(p.Z);
        private static bool Finite(Cubic c) => Finite(c.Horizontal.A) && Finite(c.Horizontal.B) && Finite(c.Horizontal.C) && Finite(c.Horizontal.D)
            && Finite(c.Vertical.A) && Finite(c.Vertical.B) && Finite(c.Vertical.C) && Finite(c.Vertical.D);
        private static double Distance(Point a, Point b) {
            var x = a.X - b.X; var z = a.Z - b.Z; return Math.Sqrt(x * x + z * z);
        }
        private static double Projection(Point p, PlanarCubic c) {
            var x = c.D.X - c.A.X; var z = c.D.Z - c.A.Z;
            return ((p.X - c.A.X) * x + (p.Z - c.A.Z) * z) / (x * x + z * z);
        }

        /// <summary>
        /// One or two authored curves, 1..256 native curves, arbitrary native order.
        /// output has nativeCount entries in original native input order. All buffers
        /// must be disjoint. Failure leaves output untouched. Tolerances are finite,
        /// nonnegative and at most 0.05 m; they do not relax interval coverage or
        /// native join-continuity at the same spatial tolerances. Limits are conservative: a
        /// geometrically acceptable reparameterization may be rejected.
        /// </summary>
        public static bool Validate(Cubic* authored, int authoredCount, Cubic* native, int nativeCount,
            Mapping* output, out Failure failure, out int failedNativeIndex,
            double xzTolerance = .05, double yTolerance = .05) {
            failure = Failure.InvalidInput; failedNativeIndex = -1;
            if (authored == null || native == null || output == null || authoredCount < 1 || authoredCount > 2
                || nativeCount < 1 || nativeCount > MaximumNativeCurves || !Finite(xzTolerance) || !Finite(yTolerance)
                || xzTolerance < 0 || xzTolerance > .05 || yTolerance < 0 || yTolerance > .05) return false;
            for (var i = 0; i < authoredCount; i++) {
                if (!Finite(authored[i])) return false;
                if (!Monotone(authored[i].Horizontal)) { failure = Failure.UnsupportedHorizontalMapping; return false; }
                if (i > 0 && (Distance(authored[i - 1].Horizontal.D, authored[i].Horizontal.A) > AuthoredJoinTolerance
                    || Math.Abs(authored[i - 1].Vertical.D - authored[i].Vertical.A) > AuthoredJoinTolerance)) return false;
            }
            var staged = stackalloc Mapping[MaximumNativeCurves];
            var order = stackalloc int[MaximumNativeCurves];
            for (var i = 0; i < nativeCount; i++) {
                failedNativeIndex = i;
                if (!Finite(native[i])) return false;
                var found = false;
                for (var j = 0; j < authoredCount; j++) {
                    if (!Match(authored[j], native[i], xzTolerance, yTolerance, out var match)) continue;
                    if (found) { failure = Failure.AmbiguousMapping; return false; }
                    match.AuthoredIndex = j; staged[i] = match; found = true;
                }
                if (!found) { failure = Failure.CurveMismatch; return false; }
                order[i] = i;
            }
            // Stable insertion order; bounded by 256 native curves. Output remains in
            // native input order, while coverage checks follow authored path order.
            for (var i = 1; i < nativeCount; i++) {
                var value = order[i]; var j = i - 1;
                while (j >= 0 && Later(staged[order[j]], staged[value])) { order[j + 1] = order[j]; j--; }
                order[j + 1] = value;
            }
            var cursor = 0; var previousNative = -1;
            for (var a = 0; a < authoredCount; a++) {
                var end = 0.0; var covered = false;
                while (cursor < nativeCount && staged[order[cursor]].AuthoredIndex == a) {
                    var index = order[cursor++]; var map = staged[index]; failedNativeIndex = index;
                    if (map.StartParameter > end + ParameterTolerance) { failure = Failure.Gap; return false; }
                    if (map.StartParameter < end - ParameterTolerance) { failure = Failure.Overlap; return false; }
                    if (previousNative >= 0) {
                        var previous = Oriented(native[previousNative], staged[previousNative].Reversed);
                        var current = Oriented(native[index], map.Reversed);
                        if (Distance(previous.Horizontal.D, current.Horizontal.A) > xzTolerance
                            || Math.Abs(previous.Vertical.D - current.Vertical.A) > yTolerance) {
                            failure = Failure.DisconnectedNative; return false;
                        }
                    }
                    previousNative = index; end = map.EndParameter; covered = true;
                }
                if (!covered || end < 1 - ParameterTolerance) { failure = Failure.Gap; return false; }
            }
            if (cursor != nativeCount) { failure = Failure.InvalidInput; return false; }
            for (var i = 0; i < nativeCount; i++) output[i] = staged[i];
            failedNativeIndex = -1; failure = Failure.None; return true;
        }

        private static bool Later(Mapping a, Mapping b) => a.AuthoredIndex > b.AuthoredIndex
            || (a.AuthoredIndex == b.AuthoredIndex && a.StartParameter > b.StartParameter);

        /// <summary>
        /// Reconstruct an authored vertical profile on a complete native horizontal
        /// subdivision. This is a construction operation, NOT an acceptance oracle.
        /// Callers must establish definition ownership and that moving new interior
        /// course points cannot move an existing junction, then regenerate elevation
        /// classification before native node/edge generation. Validate still checks
        /// the independently generated result with its original tolerances.
        /// Failure leaves output untouched; inputs and output must be disjoint.
        /// </summary>
        public static bool RestoreHeights(Cubic* authored, int authoredCount, Cubic* native, int nativeCount,
            Cubic* output, out Failure failure, out int failedNativeIndex) {
            failure = Failure.InvalidInput; failedNativeIndex = -1;
            if (authored == null || native == null || output == null || authoredCount < 1 || authoredCount > 2
                || nativeCount < 1 || nativeCount > MaximumNativeCurves) return false;
            var planarAuthored = stackalloc Cubic[2];
            var planarNative = stackalloc Cubic[MaximumNativeCurves];
            var maps = stackalloc Mapping[MaximumNativeCurves];
            for (var i = 0; i < authoredCount; i++) {
                if (!Finite(authored[i])) return false;
                if (i > 0 && Math.Abs(authored[i-1].Vertical.D - authored[i].Vertical.A) > AuthoredJoinTolerance) return false;
                planarAuthored[i] = authored[i]; planarAuthored[i].Vertical = default;
            }
            for (var i = 0; i < nativeCount; i++) {
                if (!Finite(native[i])) { failedNativeIndex = i; return false; }
                planarNative[i] = native[i]; planarNative[i].Vertical = default;
            }
            if (!Validate(planarAuthored, authoredCount, planarNative, nativeCount, maps, out failure, out failedNativeIndex)) return false;
            var staged = stackalloc Cubic[MaximumNativeCurves];
            for (var i = 0; i < nativeCount; i++) {
                var map = maps[i];
                var h = Slice(authored[map.AuthoredIndex].Vertical, map.StartParameter, map.EndParameter);
                staged[i] = native[i]; // Preserve native XZ bit-for-bit.
                staged[i].Vertical = map.Reversed ? new Heights { A=h.D, B=h.C, C=h.B, D=h.A } : h;
            }
            // Also catches overflow in the reconstructed controls before any write.
            if (!Validate(authored, authoredCount, staged, nativeCount, maps, out failure, out failedNativeIndex)) return false;
            for (var i = 0; i < nativeCount; i++) output[i] = staged[i];
            return true;
        }
        private static bool Monotone(PlanarCubic c) {
            var chord = Distance(c.A, c.D);
            if (!Finite(chord) || chord < .01) return false;
            var b = Projection(c.B, c); var d = Projection(c.C, c);
            // Projected derivative / 3: A*t*t+B*t+C. Its exact minimum is
            // attained at an endpoint or at its interior vertex.
            var qa = 3 * b - 3 * d + 1; var qb = 2 * d - 4 * b;
            var minimum = Math.Min(b, 1 - d);
            if (qa > 0) {
                var t = -qb / (2 * qa);
                if (t > 0 && t < 1) minimum = Math.Min(minimum, (qa * t + qb) * t + b);
            }
            return Finite(b) && Finite(d) && Finite(minimum) && minimum > 1e-10;
        }
        private static double Parameter(Point point, PlanarCubic curve) {
            var target = Projection(point, curve);
            if (!Finite(target)) return double.NaN;
            // Exterior projection clamps only to an endpoint; the control-distance
            // check still enforces spatial tolerance. Interior omissions remain gaps.
            if (target <= 0) return 0;
            if (target >= 1) return 1;
            var lower = 0.0; var upper = 1.0;
            for (var i = 0; i < 60; i++) {
                var mid = .5 * (lower + upper);
                if (Projection(curve.Evaluate(mid), curve) < target) lower = mid; else upper = mid;
            }
            return .5 * (lower + upper);
        }
        private static bool Match(Cubic authored, Cubic native, double xzTolerance, double yTolerance, out Mapping mapping) {
            mapping = default;
            var start = Parameter(native.Horizontal.A, authored.Horizontal);
            var end = Parameter(native.Horizontal.D, authored.Horizontal);
            if (!Finite(start) || !Finite(end) || Math.Abs(end - start) <= ParameterTolerance) return false;
            var reversed = end < start;
            if (reversed) { var swap = start; start = end; end = swap; }
            if (!authored.Horizontal.TrySlice(start, end, out var horizontal)) return false;
            var vertical = Slice(authored.Vertical, start, end);
            var candidate = Oriented(native, reversed);
            var xzError = Math.Max(Math.Max(Distance(horizontal.A, candidate.Horizontal.A), Distance(horizontal.B, candidate.Horizontal.B)),
                Math.Max(Distance(horizontal.C, candidate.Horizontal.C), Distance(horizontal.D, candidate.Horizontal.D)));
            var yError = Math.Max(Math.Max(Math.Abs(vertical.A - candidate.Vertical.A), Math.Abs(vertical.B - candidate.Vertical.B)),
                Math.Max(Math.Abs(vertical.C - candidate.Vertical.C), Math.Abs(vertical.D - candidate.Vertical.D)));
            if (!Finite(xzError) || !Finite(yError) || xzError > xzTolerance || yError > yTolerance) return false;
            mapping = new Mapping { StartParameter = start, EndParameter = end, Reversed = reversed,
                MaximumXZControlError = xzError, MaximumYControlError = yError };
            return true;
        }
        private static Cubic Oriented(Cubic c, bool reverse) => !reverse ? c : new Cubic {
            Horizontal = new PlanarCubic(c.Horizontal.D, c.Horizontal.C, c.Horizontal.B, c.Horizontal.A),
            Vertical = new Heights { A = c.Vertical.D, B = c.Vertical.C, C = c.Vertical.B, D = c.Vertical.A }
        };
        private static double Evaluate(Heights h, double t) {
            var u = 1 - t; return u * u * u * h.A + 3 * u * u * t * h.B + 3 * u * t * t * h.C + t * t * t * h.D;
        }
        private static double Derivative(Heights h, double t) {
            var u = 1 - t; return 3 * u * u * (h.B - h.A) + 6 * u * t * (h.C - h.B) + 3 * t * t * (h.D - h.C);
        }
        private static Heights Slice(Heights h, double start, double end) {
            var a = Evaluate(h, start); var d = Evaluate(h, end); var scale = (end - start) / 3;
            return new Heights { A = a, B = a + scale * Derivative(h, start), C = d - scale * Derivative(h, end), D = d };
        }
    }
}
