namespace NetworkTools.Geometry {
    using System;

    /// <summary>Three-variable affine response fit; buffers are row-major and caller-owned.</summary>
    public static unsafe class SurfaceProfileResponseFit {
        public static bool Fit(double* responses, double* residual, int count, double* solution,
            double tolerance, double maxAdjustment) {
            if (count < 3 || responses == null || residual == null || solution == null
                || !(tolerance > 0) || !(maxAdjustment > 0)) { return false; }
            var normal = stackalloc double[12];
            for (var i = 0; i < 12; i++) { normal[i] = 0; }
            for (var row = 0; row < count; row++) {
                if (!Finite(residual[row])) { return false; }
                for (var a = 0; a < 3; a++) {
                    var v = responses[row * 3 + a];
                    if (!Finite(v)) { return false; }
                    for (var b = 0; b < 3; b++) { normal[a * 4 + b] += v * responses[row * 3 + b]; }
                    normal[a * 4 + 3] += v * residual[row];
                }
            }
            for (var c = 0; c < 3; c++) {
                var pivot = c;
                for (var r = c + 1; r < 3; r++) {
                    if (Math.Abs(normal[r * 4 + c]) > Math.Abs(normal[pivot * 4 + c])) { pivot = r; }
                }
                if (Math.Abs(normal[pivot * 4 + c]) < 1e-10) { return false; }
                for (var j = c; j < 4; j++) {
                    var temp = normal[c * 4 + j]; normal[c * 4 + j] = normal[pivot * 4 + j]; normal[pivot * 4 + j] = temp;
                }
                var divisor = normal[c * 4 + c];
                for (var j = c; j < 4; j++) { normal[c * 4 + j] /= divisor; }
                for (var r = 0; r < 3; r++) {
                    if (r == c) { continue; }
                    var factor = normal[r * 4 + c];
                    for (var j = c; j < 4; j++) { normal[r * 4 + j] -= factor * normal[c * 4 + j]; }
                }
            }
            for (var i = 0; i < 3; i++) {
                solution[i] = normal[i * 4 + 3];
                if (!Finite(solution[i]) || Math.Abs(solution[i]) > maxAdjustment) { return false; }
            }
            for (var r = 0; r < count; r++) {
                var predicted = 0.0;
                for (var c = 0; c < 3; c++) { predicted += responses[r * 3 + c] * solution[c]; }
                if (Math.Abs(predicted - residual[r]) > tolerance) { return false; }
            }
            return true;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
