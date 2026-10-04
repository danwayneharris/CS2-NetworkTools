namespace NetworkTools.Geometry {
    using System;

    /// <summary>
    /// Fits one explicit ordered lane correspondence by rigid lateral translation.
    /// All offsets are metres in the SAME path-oriented physical cross-section frame.
    /// The adapter owns frame extraction, port station, native lane identity, carriageway,
    /// supported lane flags, prefab selection and freshness. A successful fit is not
    /// proof that native lane generation produces the requested directed connections.
    /// No tangent, vertical, endpoint-node or structural-elevation policy lives here.
    /// </summary>
    public static class ConnectLaneAlignment {
        public const double PositionTolerance = 0.05;
        public const int MaximumLanes = 64;

        public struct Lane {
            public long Id;
            // Rank in the complete eligible lane layout, increasing in lateral offset.
            // Selected groups must not omit ranks. Caller must establish these ranks;
            // array indices alone do not establish native contiguity.
            public int PortOrder;
            // +1/-1 travel relative to the shared connection-traversal frame.
            public int Direction;
            public double Offset;
            public double Width;
            public Lane(long id, int portOrder, int direction, double offset, double width) {
                Id = id; PortOrder = portOrder; Direction = direction; Offset = offset; Width = width;
            }
        }

        public enum Failure {
            None, InvalidInput, CapacityExceeded, UnequalCount, DuplicateIdentity,
            NoncontiguousGroup, MixedDirection, DirectionMismatch, IncompatibleWidth,
            IncompatibleSpacing, NonfiniteOutput
        }

        public struct Result {
            public int Count;
            public double Shift;
            public double MaximumPositionResidual;
            public double MaximumWidthDifference;
        }

        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private static Failure Validate(Lane[] lanes) {
            for (int i = 0; i < lanes.Length; i++) {
                Lane lane = lanes[i];
                if (!Finite(lane.Offset) || !Finite(lane.Width) || lane.Width <= 0
                    || (lane.Direction != 1 && lane.Direction != -1)) return Failure.InvalidInput;
                for (int j = 0; j < i; j++) {
                    if (lane.Id == lanes[j].Id) return Failure.DuplicateIdentity;
                }
                if (i == 0) continue;
                if (lane.Direction != lanes[0].Direction) return Failure.MixedDirection;
                if ((long)lane.PortOrder != (long)lanes[i - 1].PortOrder + 1
                    || lane.Offset <= lanes[i - 1].Offset) return Failure.NoncontiguousGroup;
            }
            return Failure.None;
        }

        /// <summary>
        /// Minimize max_i |shift + candidate[i].Offset - reference[i].Offset|.
        /// If d_i = reference_i - candidate_i, the deterministic minimax solution is
        /// (min d_i + max d_i)/2, with residual (max d_i - min d_i)/2.
        /// This also tests intersection of [d_i-.05, d_i+.05]. No individual lane
        /// warping, width scaling, group search or side selection occurs.
        /// Width compatibility is separate caller policy: absolute pairwise width
        /// differences must not exceed the explicitly supplied widthTolerance.
        /// WidthTolerance never relaxes the fixed 5 cm positional tolerance.
        /// On spacing/width failure Result contains diagnostics, NOT an accepted fit.
        /// On invalid input Result is default. Inputs are never modified; no allocations.
        /// Reverse orientation by reversing each group and negating offsets, direction
        /// and port ranks (the adapter must handle unrepresentable rank negation).
        /// </summary>
        public static bool Fit(Lane[] reference, Lane[] candidate, double widthTolerance,
            out Result result, out Failure failure) {
            result = default; failure = Failure.InvalidInput;
            if (reference == null || candidate == null || reference.Length == 0 || candidate.Length == 0
                || !Finite(widthTolerance) || widthTolerance < 0) return false;
            if (reference.Length > MaximumLanes || candidate.Length > MaximumLanes) {
                failure = Failure.CapacityExceeded; return false;
            }
            if (reference.Length != candidate.Length) { failure = Failure.UnequalCount; return false; }
            failure = Validate(reference);
            if (failure != Failure.None) return false;
            failure = Validate(candidate);
            if (failure != Failure.None) return false;
            if (reference[0].Direction != candidate[0].Direction) {
                failure = Failure.DirectionMismatch; return false;
            }
            double min = double.PositiveInfinity, max = double.NegativeInfinity, widthDifference = 0;
            for (int i = 0; i < reference.Length; i++) {
                double d = reference[i].Offset - candidate[i].Offset;
                if (!Finite(d)) { failure = Failure.NonfiniteOutput; return false; }
                min = Math.Min(min, d); max = Math.Max(max, d);
                widthDifference = Math.Max(widthDifference, Math.Abs(reference[i].Width - candidate[i].Width));
            }
            // Halving before addition/subtraction avoids avoidable overflow.
            double shift = min * 0.5 + max * 0.5;
            double residual = max * 0.5 - min * 0.5;
            if (!Finite(shift) || !Finite(residual) || !Finite(widthDifference)) {
                failure = Failure.NonfiniteOutput; return false;
            }
            result = new Result { Count = reference.Length, Shift = shift,
                MaximumPositionResidual = residual, MaximumWidthDifference = widthDifference };
            if (widthDifference > widthTolerance) { failure = Failure.IncompatibleWidth; return false; }
            if (residual > PositionTolerance) { failure = Failure.IncompatibleSpacing; return false; }
            failure = Failure.None; return true;
        }
    }
}
