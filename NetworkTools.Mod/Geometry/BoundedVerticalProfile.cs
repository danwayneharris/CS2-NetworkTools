namespace NetworkTools.Geometry {
    using System;

    /// <summary>
    /// Debug profile solver: box-constrained cubic-Hermite bending energy in normalized
    /// station u=s/totalLength. On each interval l, endpoint values are node height plus
    /// the ORIGINAL curve/node offsets; q=dy/du. Minimize sum integral (p''(u))^2 du.
    /// Two-point Gauss quadrature evaluates this quadratic exactly. Translation of Y and
    /// diagonal equilibration improve conditioning. Fixed outer heights remove its affine
    /// nullspace, so the primary objective has a unique minimizer (no epsilon tie penalty).
    /// Fixed anchors separate incoming/outgoing grades; their match flags are equalities.
    /// Other nodes share a grade, including a node whose permitted height interval is zero.
    ///
    /// Emitted controls use the actual horizontal handle lengths, preserving endpoint
    /// grades and offsets. Station-Hermite energy is a surrogate for the emitted curve's
    /// interior physical curvature when its horizontal parameter speed is nonuniform.
    /// No output is written until a feasible solution passes its projected KKT residual.
    /// Managed scratch and dense algebra are intentionally bounded to Debug-sized paths.
    /// </summary>
    public static unsafe class BoundedVerticalProfile {
        public enum Failure { None, InvalidInput, CapacityExceeded, IllConditioned, IterationLimit, ResidualFailure }
        // Dense active-set solves allocate per iteration. Offline Debug qualification found
        // a pre-free-KKT-fix ~2-second / 652-MB case at 128 segments; do not expose that
        // unbounded interactive cost through the slider. Longer finite-bound paths
        // reject explicitly; Unlimited retains its separate existing linear solver.
        public const int MaximumSegments = 64;
        private const double ResidualTolerance = 1e-9;
        private const double PivotTolerance = 1e-12;
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private struct Value {
            public int Index;
            public double Constant;
            public double Read(double[] x) => Index < 0 ? Constant : x[Index];
        }

        /// <summary>
        /// Bounds use absolute node Y. +/-Infinity means no bound; NaN is rejected.
        /// Existing fixed anchors take precedence over intervals. Nonfixed anchors cannot
        /// specify grade matches, matching SectionedVerticalProfile's input contract.
        /// Buffers must be disjoint. Failure leaves both output buffers unchanged.
        /// Rebased bounds in a NEW operation may permit further movement; this is not
        /// a cumulative movement budget or a promise of repeat-Apply idempotence.
        /// </summary>
        public static bool Fit(VerticalLinearProfile.Segment* segments, int count,
            SectionedVerticalProfile.Anchor* anchors, double* lower, double* upper,
            double* nodeHeights, VerticalLinearProfile.Heights* curves,
            out Failure failure, out double kktResidual, int maxIterations = 0) {
            failure = Failure.InvalidInput; kktResidual = double.PositiveInfinity;
            if (count < 1 || segments == null || anchors == null || lower == null || upper == null
                || nodeHeights == null || curves == null || !anchors[0].Fixed || !anchors[count].Fixed
                || maxIterations < 0) return false;
            if (count > MaximumSegments) { failure = Failure.CapacityExceeded; return false; }
            var total = 0.0;
            for (var i = 0; i < count; i++) {
                var s = segments[i];
                if (!Finite(s.Length) || s.Length < .01 || !Finite(s.StartHandle) || s.StartHandle < 1e-6
                    || !Finite(s.EndHandle) || s.EndHandle < 1e-6 || !Finite(s.StartOffset) || !Finite(s.EndOffset)) return false;
                total += s.Length;
            }
            if (!Finite(total)) return false;
            for (var i = 0; i <= count; i++) {
                var a = anchors[i];
                if ((a.Fixed && !Finite(a.Height)) || (!a.Fixed && (a.MatchIncoming || a.MatchOutgoing))
                    || (a.MatchIncoming && !Finite(a.IncomingGrade)) || (a.MatchOutgoing && !Finite(a.OutgoingGrade))
                    || double.IsNaN(lower[i]) || double.IsNaN(upper[i])
                    || (!a.Fixed && (lower[i] > upper[i] || lower[i] == double.PositiveInfinity || upper[i] == double.NegativeInfinity))) return false;
            }
            var capacity = 3 * count + 3;
            var lo = new double[capacity]; var hi = new double[capacity];
            var heights = new Value[count + 1]; var incoming = new Value[count + 1]; var outgoing = new Value[count + 1];
            var size = 0; var baseline = anchors[0].Height;
            for (var i = 0; i <= count; i++) {
                heights[i] = anchors[i].Fixed ? Constant(anchors[i].Height - baseline)
                    : AddValue(lower[i] - baseline, upper[i] - baseline, lo, hi, ref size);
                if (i > 0) incoming[i] = anchors[i].MatchIncoming ? Constant(anchors[i].IncomingGrade * total)
                    : AddValue(double.NegativeInfinity, double.PositiveInfinity, lo, hi, ref size);
                if (i < count) outgoing[i] = anchors[i].MatchOutgoing ? Constant(anchors[i].OutgoingGrade * total)
                    : i > 0 && !anchors[i].Fixed ? incoming[i]
                    : AddValue(double.NegativeInfinity, double.PositiveInfinity, lo, hi, ref size);
            }
            var matrix = new double[size, size]; var linear = new double[size];
            var values = new Value[4]; var coefficients = new double[4];
            for (var i = 0; i < count; i++) {
                var s = segments[i]; var length = s.Length / total;
                values[0] = heights[i]; values[1] = heights[i + 1];
                values[2] = outgoing[i]; values[3] = incoming[i + 1];
                for (var sample = 0; sample < 2; sample++) {
                    var t = .5 + (sample == 0 ? -1 : 1) / (2 * Math.Sqrt(3));
                    var delta = (6 - 12 * t) / (length * length);
                    coefficients[0] = -delta; coefficients[1] = delta;
                    coefficients[2] = (6 * t - 4) / length; coefficients[3] = (6 * t - 2) / length;
                    var constant = delta * (s.EndOffset - s.StartOffset);
                    for (var j = 0; j < 4; j++) if (values[j].Index < 0) constant += coefficients[j] * values[j].Constant;
                    var weight = length / 2;
                    for (var j = 0; j < 4; j++) {
                        var row = values[j].Index;
                        if (row < 0) continue;
                        linear[row] += weight * coefficients[j] * constant;
                        for (var k = 0; k < 4; k++) if (values[k].Index >= 0)
                            matrix[row, values[k].Index] += weight * coefficients[j] * coefficients[k];
                    }
                }
            }
            var scale = new double[size];
            for (var i = 0; i < size; i++) {
                if (!Finite(matrix[i, i]) || matrix[i, i] <= 0 || !Finite(linear[i])) { failure = Failure.IllConditioned; return false; }
                scale[i] = Math.Sqrt(matrix[i, i]);
            }
            for (var i = 0; i < size; i++) {
                linear[i] /= scale[i]; lo[i] *= scale[i]; hi[i] *= scale[i];
                for (var j = 0; j < size; j++) {
                    matrix[i, j] = matrix[i, j] / scale[i] / scale[j];
                    if (!Finite(matrix[i, j])) { failure = Failure.IllConditioned; return false; }
                }
            }
            var x = new double[size];
            if (!Solve(matrix, linear, lo, hi, x, maxIterations == 0 ? 8 * size + 32 : maxIterations,
                out failure, out kktResidual)) return false;
            for (var i = 0; i < size; i++) x[i] /= scale[i];
            var outputHeights = new double[count + 1];
            var outputCurves = new VerticalLinearProfile.Heights[count];
            for (var i = 0; i <= count; i++) {
                outputHeights[i] = anchors[i].Fixed ? anchors[i].Height : heights[i].Read(x) + baseline;
                if (!Finite(outputHeights[i]) || (!anchors[i].Fixed && (outputHeights[i] < lower[i] - 1e-8 || outputHeights[i] > upper[i] + 1e-8))) {
                    failure = Failure.ResidualFailure; return false;
                }
            }
            for (var i = 0; i < count; i++) {
                var s = segments[i]; var a = outputHeights[i] + s.StartOffset; var d = outputHeights[i + 1] + s.EndOffset;
                var b = a + outgoing[i].Read(x) / total * s.StartHandle;
                var c = d - incoming[i + 1].Read(x) / total * s.EndHandle;
                if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d)) { failure = Failure.ResidualFailure; return false; }
                outputCurves[i] = new VerticalLinearProfile.Heights { A = a, B = b, C = c, D = d };
            }
            for (var i = 0; i <= count; i++) nodeHeights[i] = outputHeights[i];
            for (var i = 0; i < count; i++) curves[i] = outputCurves[i];
            failure = Failure.None; return true;
        }

        private static Value Constant(double value) => new Value { Index = -1, Constant = value };
        private static Value AddValue(double lower, double upper, double[] lo, double[] hi, ref int size) {
            if (lower == upper) return Constant(lower);
            lo[size] = lower; hi[size] = upper;
            return new Value { Index = size++ };
        }

        // Primal feasible active set. Enter the first bound reached along a Newton step;
        // leave the largest KKT-violating bound, resolving exact ties by variable index.
        private static bool Solve(double[,] q, double[] c, double[] lo, double[] hi, double[] x,
            int maxIterations, out Failure failure, out double residual) {
            var n = x.Length; var active = new int[n]; var gradient = new double[n]; var free = new int[n];
            failure = Failure.IterationLimit; residual = double.PositiveInfinity;
            for (var i = 0; i < n; i++) {
                x[i] = Math.Max(lo[i], Math.Min(hi[i], 0));
                if (!Finite(x[i])) { failure = Failure.IllConditioned; return false; }
                active[i] = x[i] == lo[i] ? -1 : x[i] == hi[i] ? 1 : 0;
            }
            for (var iteration = 0; iteration < maxIterations; iteration++) {
                residual = 0; var freeResidual = 0.0; var release = -1; var violation = ResidualTolerance;
                for (var i = 0; i < n; i++) {
                    var g = c[i]; var norm = 1 + Math.Abs(c[i]);
                    for (var j = 0; j < n; j++) { var term = q[i, j] * x[j]; g += term; norm += Math.Abs(term); }
                    gradient[i] = g;
                    var r = active[i] < 0 ? Math.Max(0, -g) / norm : active[i] > 0 ? Math.Max(0, g) / norm : Math.Abs(g) / norm;
                    residual = Math.Max(residual, r);
                    if (active[i] == 0) freeResidual = Math.Max(freeResidual, r);
                    if (active[i] != 0 && r > violation) { violation = r; release = i; }
                }
                if (!Finite(residual)) { failure = Failure.ResidualFailure; return false; }
                if (residual <= ResidualTolerance) { failure = Failure.None; return true; }
                // The free subproblem's KKT residual, not tiny Newton movement, decides
                // when to release a wrong-sign active multiplier. Ill-conditioning can
                // amplify roundoff into nontrivial steps despite an already solved free
                // subproblem; repeating those solves causes severe allocation/time spikes.
                if (freeResidual <= ResidualTolerance && release >= 0) {
                    active[release] = 0; continue;
                }
                var freeCount = 0;
                for (var i = 0; i < n; i++) if (active[i] == 0) free[freeCount++] = i;
                var direction = new double[n];
                if (!Direction(q, gradient, free, freeCount, direction)) { failure = Failure.IllConditioned; return false; }
                var movement = 0.0;
                for (var i = 0; i < n; i++) movement = Math.Max(movement, Math.Abs(direction[i]) / (1 + Math.Abs(x[i])));
                if (movement <= 1e-12) {
                    if (release < 0) { failure = Failure.ResidualFailure; return false; }
                    active[release] = 0; continue;
                }
                var step = 1.0; var hit = -1; var side = 0;
                for (var i = 0; i < n; i++) {
                    var p = direction[i];
                    var candidate = p < 0 ? (lo[i] - x[i]) / p : p > 0 ? (hi[i] - x[i]) / p : double.PositiveInfinity;
                    if (candidate < step) { step = candidate; hit = i; side = p < 0 ? -1 : 1; }
                }
                if (!Finite(step) || step < 0) { failure = Failure.ResidualFailure; return false; }
                for (var i = 0; i < n; i++) x[i] += step * direction[i];
                if (hit >= 0) { x[hit] = side < 0 ? lo[hit] : hi[hit]; active[hit] = side; }
            }
            return false;
        }

        private static bool Direction(double[,] q, double[] gradient, int[] free, int count, double[] direction) {
            var l = new double[count, count]; var rhs = new double[count];
            for (var i = 0; i < count; i++) {
                for (var j = 0; j <= i; j++) {
                    var value = q[free[i], free[j]];
                    for (var k = 0; k < j; k++) value -= l[i, k] * l[j, k];
                    if (i == j) {
                        if (!Finite(value) || value < PivotTolerance) return false;
                        l[i, j] = Math.Sqrt(value);
                    } else l[i, j] = value / l[j, j];
                }
                var b = -gradient[free[i]];
                for (var k = 0; k < i; k++) b -= l[i, k] * rhs[k];
                rhs[i] = b / l[i, i];
            }
            for (var i = count - 1; i >= 0; i--) {
                var b = rhs[i];
                for (var k = i + 1; k < count; k++) b -= l[k, i] * direction[free[k]];
                direction[free[i]] = b / l[i, i];
                if (!Finite(direction[free[i]])) return false;
            }
            return true;
        }
    }
}
