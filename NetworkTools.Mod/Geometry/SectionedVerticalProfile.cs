namespace NetworkTools.Geometry {
    using System;

    /// <summary>
    /// Fits independently anchored spans using the existing offset-aware profile.
    /// Constraints are supplied by the caller; this class does not choose pin policy.
    /// </summary>
    public static unsafe class SectionedVerticalProfile {
        public struct Anchor {
            public bool Fixed;
            public double Height;
            // Grades are oriented along path traversal, not stored edge direction.
            public bool MatchIncoming, MatchOutgoing;
            public double IncomingGrade, OutgoingGrade;
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        /// <summary>
        /// Caller owns nonoverlapping buffers; anchors has count+1 entries.
        /// Discard all output on failure, including a later span failure.
        /// Endpoint handles implement the existing boundary-grade substitution;
        /// interior grades are not promised constant when boundary grades differ.
        /// </summary>
        public static bool Fit(VerticalLinearProfile.Segment* segments, int count,
            Anchor* anchors, double* nodeHeights, VerticalLinearProfile.Heights* curves,
            out int failedAnchor) {
            failedAnchor = -1;
            if (count < 1 || segments == null || anchors == null || nodeHeights == null || curves == null
                || !anchors[0].Fixed || !anchors[count].Fixed) { return false; }
            for (var i = 0; i <= count; i++) {
                var a = anchors[i];
                if ((a.Fixed && !Finite(a.Height)) || (!a.Fixed && (a.MatchIncoming || a.MatchOutgoing))
                    || (a.MatchIncoming && !Finite(a.IncomingGrade))
                    || (a.MatchOutgoing && !Finite(a.OutgoingGrade))) {
                    failedAnchor = i; return false;
                }
            }
            var start = 0;
            for (var end = 1; end <= count; end++) {
                if (!anchors[end].Fixed) continue;
                var a = anchors[start]; var b = anchors[end];
                if (!VerticalLinearProfile.Fit(segments + start, end - start, a.Height, b.Height,
                    a.MatchOutgoing, a.OutgoingGrade, b.MatchIncoming, b.IncomingGrade,
                    nodeHeights + start, curves + start, out _)) {
                    failedAnchor = end; return false;
                }
                start = end;
            }
            return true;
        }
    }
}
