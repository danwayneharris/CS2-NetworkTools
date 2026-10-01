namespace NetworkTools.Geometry {
    using System;

    /// <summary>
    /// Fits a common edge grade while preserving endpoint/node height offsets.
    /// Junction spans retain their existing offset differences; they are not modeled
    /// as additional travel distance. Cubic controls match endpoint grades exactly,
    /// but curved horizontal segments generally have a varying interior grade.
    /// </summary>
    public static unsafe class VerticalLinearProfile {
        public struct Segment {
            public double Length, StartHandle, EndHandle, StartOffset, EndOffset;
        }
        public struct Heights {
            public double A, B, C, D;
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        /// <summary>Caller owns nonoverlapping buffers. Discard output on failure.</summary>
        public static bool Fit(Segment* segments, int count, double startHeight, double endHeight,
            bool smoothStart, double startGrade, bool smoothEnd, double endGrade,
            double* nodeHeights, Heights* curves, out double grade) {
            grade = 0;
            if (segments == null || nodeHeights == null || curves == null || count < 1
                || !Finite(startHeight) || !Finite(endHeight)
                || (smoothStart && !Finite(startGrade)) || (smoothEnd && !Finite(endGrade))) { return false; }
            var length = 0.0;
            var offsetRise = 0.0;
            for (var i = 0; i < count; i++) {
                var s = segments[i];
                if (!Finite(s.Length) || s.Length < 0.01 || !Finite(s.StartHandle)
                    || !Finite(s.EndHandle) || s.StartHandle < 1e-6 || s.EndHandle < 1e-6
                    || !Finite(s.StartOffset) || !Finite(s.EndOffset)) { return false; }
                length += s.Length;
                offsetRise += s.EndOffset - s.StartOffset;
            }
            grade = (endHeight - startHeight + offsetRise) / length;
            if (!Finite(grade) || !Finite(length)) { return false; }
            nodeHeights[0] = startHeight;
            for (var i = 0; i < count; i++) {
                var s = segments[i];
                var next = nodeHeights[i] + grade * s.Length + s.StartOffset - s.EndOffset;
                if (i == count - 1) { next = endHeight; }
                var a = nodeHeights[i] + s.StartOffset;
                var d = next + s.EndOffset;
                var b = a + (i == 0 && smoothStart ? startGrade : grade) * s.StartHandle;
                var c = d - (i == count - 1 && smoothEnd ? endGrade : grade) * s.EndHandle;
                if (!Finite(next) || !Finite(a) || !Finite(b) || !Finite(c) || !Finite(d)) { return false; }
                nodeHeights[i + 1] = next;
                curves[i] = new Heights { A = a, B = b, C = c, D = d };
            }
            return true;
        }

        /// <summary>Composite Simpson quadrature, horizontal only. Reject unresolved integration.</summary>
        public static bool TryHorizontalLength(PlanarCubic curve, out double length) {
            var previous = Integrate(curve, 32);
            length = 0;
            for (var panels = 64; panels <= 1024; panels *= 2) {
                var current = Integrate(curve, panels);
                if (!Finite(current)) { return false; }
                if (Math.Abs(current - previous) <= 1e-7 * Math.Max(1, current)) {
                    length = current;
                    return current >= 0.01;
                }
                previous = current;
            }
            return false;
        }
        private static double Integrate(PlanarCubic curve, int panels) {
            var sum = 0.0;
            for (var i = 0; i <= panels; i++) {
                var d = curve.Derivative((double)i / panels);
                var speed = Math.Sqrt(d.X * d.X + d.Z * d.Z);
                sum += (i == 0 || i == panels ? 1 : i % 2 == 0 ? 2 : 4) * speed;
            }
            return sum / (3 * panels);
        }
    }
}