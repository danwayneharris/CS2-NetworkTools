namespace NetworkTools.Geometry {
    using System;

    /// <summary>Game-independent horizontal path fitting with caller-owned storage.</summary>
    public static unsafe class PlanarFairing {
        /// <summary>Horizontal coordinates in metres and an optional positional constraint.</summary>
        public struct Point {
            public double X, Z;
            public bool Fixed;
            public Point(double x, double z, bool pinned = false) { X = x; Z = z; Fixed = pinned; }
        }

        /// <summary>
        /// Fits a path using weighted position fidelity and squared second differences
        /// on original chord-length stations. Endpoints and marked points are fixed.
        /// Requires count output points and 5*count scratch doubles, all non-overlapping.
        /// Returns false for invalid input or collapsed/reversed output chords.
        /// Output is usable only on success. No allocations or game types are used.
        /// </summary>
        /// <param name="input">Read-only input points; first and last are always fixed.</param>
        /// <param name="output">Caller-owned output buffer with at least count elements.</param>
        /// <param name="count">Number of path-ordered points, at least two.</param>
        /// <param name="strength">Dimensionless fitting strength from zero to one.</param>
        /// <param name="scratch">Caller-owned workspace of at least 5*count doubles.</param>
        public static bool Fit(Point* input, Point* output, int count, double strength, double* scratch) {
            if (input == null || output == null || scratch == null || count < 2 || count > int.MaxValue / 5
                || !Finite(strength) || strength < 0 || strength > 1) { return false; }
            var diagonal = scratch;
            var lower1 = scratch + count;
            var lower2 = scratch + count * 2;
            var rhsX = scratch + count * 3;
            var rhsZ = scratch + count * 4;
            for (var i = 0; i < count; i++) {
                if (!Finite(input[i].X) || !Finite(input[i].Z)) { return false; }
                output[i] = input[i];
                diagonal[i] = lower1[i] = lower2[i] = rhsX[i] = rhsZ[i] = 0;
                if (i > 0) {
                    var length = Distance(input[i - 1], input[i]);
                    if (!Finite(length) || length < 0.01) { return false; }
                }
            }
            if (strength == 0) { return true; }

            // Integrated fidelity weights; reference length is 50 metres.
            // Effective smoothing length = 50 * sqrt(strength); see curve-geometry.md.
            const double referenceLength = 50.0;
            var lambda = referenceLength * referenceLength * referenceLength * referenceLength * strength * strength;
            for (var i = 0; i < count - 1; i++) {
                var h = Distance(input[i], input[i + 1]);
                diagonal[i] += h * 0.5;
                diagonal[i + 1] += h * 0.5;
            }
            for (var i = 1; i < count - 1; i++) {
                var left = Distance(input[i - 1], input[i]);
                var right = Distance(input[i], input[i + 1]);
                var a = 1.0 / left;
                var c = 1.0 / right;
                var b = -a - c;
                var weight = lambda * 2.0 / (left + right);
                diagonal[i - 1] += weight * a * a;
                diagonal[i] += weight * b * b;
                diagonal[i + 1] += weight * c * c;
                lower1[i] += weight * a * b;
                lower1[i + 1] += weight * b * c;
                lower2[i + 1] += weight * a * c;
                // Solve displacements, avoiding large absolute-world-coordinate RHS values.
                var dx = (input[i + 1].X - input[i].X) * c - (input[i].X - input[i - 1].X) * a;
                var dz = (input[i + 1].Z - input[i].Z) * c - (input[i].Z - input[i - 1].Z) * a;
                rhsX[i - 1] -= weight * a * dx; rhsZ[i - 1] -= weight * a * dz;
                rhsX[i] -= weight * b * dx; rhsZ[i] -= weight * b * dz;
                rhsX[i + 1] -= weight * c * dx; rhsZ[i + 1] -= weight * c * dz;
            }
            for (var i = 0; i < count; i++) {
                if (!Pinned(input, i, count)) { continue; }
                diagonal[i] = 1;
                rhsX[i] = rhsZ[i] = 0;
                lower1[i] = lower2[i] = 0;
                if (i + 1 < count) { lower1[i + 1] = 0; }
                if (i + 2 < count) { lower2[i + 2] = 0; }
            }
            // Banded Cholesky: linear time and storage for this pentadiagonal system.
            for (var i = 0; i < count; i++) {
                if (i > 1) { lower2[i] /= diagonal[i - 2]; }
                if (i > 0) {
                    lower1[i] = (lower1[i] - (i > 1 ? lower2[i] * lower1[i - 1] : 0)) / diagonal[i - 1];
                }
                var pivot = diagonal[i] - lower1[i] * lower1[i] - lower2[i] * lower2[i];
                if (!Finite(pivot) || pivot <= 0) { return false; }
                diagonal[i] = Math.Sqrt(pivot);
                if (i > 0) { rhsX[i] -= lower1[i] * rhsX[i - 1]; rhsZ[i] -= lower1[i] * rhsZ[i - 1]; }
                if (i > 1) { rhsX[i] -= lower2[i] * rhsX[i - 2]; rhsZ[i] -= lower2[i] * rhsZ[i - 2]; }
                rhsX[i] /= diagonal[i]; rhsZ[i] /= diagonal[i];
            }
            for (var i = count - 1; i >= 0; i--) {
                if (i + 1 < count) { rhsX[i] -= lower1[i + 1] * rhsX[i + 1]; rhsZ[i] -= lower1[i + 1] * rhsZ[i + 1]; }
                if (i + 2 < count) { rhsX[i] -= lower2[i + 2] * rhsX[i + 2]; rhsZ[i] -= lower2[i + 2] * rhsZ[i + 2]; }
                rhsX[i] /= diagonal[i]; rhsZ[i] /= diagonal[i];
                if (!Pinned(input, i, count)) { output[i].X += rhsX[i]; output[i].Z += rhsZ[i]; }
                if (!Finite(output[i].X) || !Finite(output[i].Z)) { return false; }
            }
            for (var i = 1; i < count; i++) {
                var length = Distance(output[i - 1], output[i]);
                if (!Finite(length) || length < 0.01) { return false; }
                var dot = (output[i].X - output[i - 1].X) * (input[i].X - input[i - 1].X)
                    + (output[i].Z - output[i - 1].Z) * (input[i].Z - input[i - 1].Z);
                if (!Finite(dot) || dot <= 0) { return false; }
            }
            return true;
        }

        private static bool Pinned(Point* points, int i, int count) { return i == 0 || i == count - 1 || points[i].Fixed; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static double Distance(Point a, Point b) {
            var x = a.X - b.X; var z = a.Z - b.Z;
            return Math.Sqrt(x * x + z * z);
        }
    }
}
