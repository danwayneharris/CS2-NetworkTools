using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

// Dependency-free executable tests: a failing assertion exits nonzero.
internal static class Program {
    private static unsafe P[] Fit(P[] input, double strength = 0.5) {
        var output = new P[input.Length];
        var scratch = new double[input.Length * 5];
        fixed (P* p = input, q = output)
        fixed (double* s = scratch) {
            if (!PlanarFairing.Fit(p, q, input.Length, strength, s)) { throw new Exception("Fit rejected test path"); }
        }
        return output;
    }
    private static void Near(double expected, double actual, string name) {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > 1e-7) { throw new Exception(name + ": " + expected + " != " + actual); }
    }
    private static void Same(P[] a, P[] b, string name) {
        for (var i = 0; i < a.Length; i++) { Near(a[i].X, b[i].X, name); Near(a[i].Z, b[i].Z, name); }
    }
    private static unsafe void Main(string[] args) {
        if (args.Length == 2 && args[0] == "--trace-log") { TraceReplay.Run(args[1]); return; }
        TargetReplay.Run(args);
        VerticalProfileTests.Run();
        SurfaceResponseTests.Run();
        NativeProfileReplayTests.Run();
        FailureTests.Run();
        JunctionSearchTests.Run();
        BoundaryRotationTests.Run();
        SplitTargetTests.Run();
        JunctionTargetTests.Run();
        CheckBezier();
        CheckCapturedHandleBlend();
        // Analytic three-point solution: equal chords h, only the middle Z is free.
        // E(z) = h*(z-z0)^2 + lambda*4*z^2/h^3, so z=z0/(1+4*lambda/h^4).
        var three = new[] { new P(0, 0), new P(20, 15), new P(40, 0) };
        Near(15.0 / (1.0 + 4.0 * 6250000.0 * 0.25 / Math.Pow(25, 4)), Fit(three)[1].Z, "analytic solution");
        var straight = new[] { new P(0, 0), new P(1, 2), new P(30, 60), new P(100, 200) };
        Same(straight, Fit(straight), "nonuniform straight line");
        var zigzag = new[] { new P(0, 0), new P(20, 12), new P(40, -12), new P(60, 12), new P(80, 0) };
        Same(zigzag, Fit(zigzag, 0), "zero strength");
        var smooth = Fit(zigzag);
        Near(0, smooth[0].Z, "start pinned"); Near(0, smooth[4].Z, "end pinned");
        for (var i = 1; i < 4; i++) {
            if (Math.Abs(smooth[i].Z) >= Math.Abs(zigzag[i].Z)) { throw new Exception("zigzag not reduced"); }
        }
        var reversed = (P[])zigzag.Clone(); Array.Reverse(reversed);
        var reversedFit = Fit(reversed); Array.Reverse(reversedFit);
        Same(smooth, reversedFit, "reversal invariance");
        var translated = (P[])zigzag.Clone();
        for (var i = 0; i < translated.Length; i++) { translated[i].X += 10000; translated[i].Z -= 10000; }
        var translatedFit = Fit(translated);
        for (var i = 0; i < translated.Length; i++) { translatedFit[i].X -= 10000; translatedFit[i].Z += 10000; }
        Same(smooth, translatedFit, "translation invariance");
        var rotated = (P[])zigzag.Clone();
        var angle = 0.73;
        for (var i = 0; i < rotated.Length; i++) {
            rotated[i] = new P(zigzag[i].X * Math.Cos(angle) - zigzag[i].Z * Math.Sin(angle),
                zigzag[i].X * Math.Sin(angle) + zigzag[i].Z * Math.Cos(angle));
        }
        var rotatedFit = Fit(rotated);
        for (var i = 0; i < rotated.Length; i++) {
            Near(smooth[i].X * Math.Cos(angle) - smooth[i].Z * Math.Sin(angle), rotatedFit[i].X, "rotation X");
            Near(smooth[i].X * Math.Sin(angle) + smooth[i].Z * Math.Cos(angle), rotatedFit[i].Z, "rotation Z");
        }
        // Nonuniform, long input exercises both off-diagonals and multiple interior pins.
        var longPath = new P[257];
        for (var i = 0; i < longPath.Length; i++) {
            longPath[i] = new P(i * 8 + (i % 3), Math.Sin(i * 0.71) * 2, i % 31 == 0);
        }
        var longFit = Fit(longPath, 1);
        CheckStationarity(longPath, longFit, 1);
        for (var i = 0; i < longPath.Length; i++) {
            if (longPath[i].Fixed) { Near(longPath[i].Z, longFit[i].Z, "multiple pins"); }
        }
        zigzag[2].Fixed = true;
        var pinned = Fit(zigzag); Near(zigzag[2].X, pinned[2].X, "junction X"); Near(zigzag[2].Z, pinned[2].Z, "junction Z");
        var pair = new[] { new P(3, 4), new P(30, 40) }; Same(pair, Fit(pair), "two nodes");
        var invalid = new[] { new P(0, 0), new P(0, 0) };
        var result = new P[2]; var workspace = new double[10];
        fixed (P* p = invalid, q = result)
        fixed (double* s = workspace) {
            if (PlanarFairing.Fit(p, q, 2, 0.5, s)) { throw new Exception("duplicate accepted"); }
            invalid[1] = new P(double.NaN, 0);
            if (PlanarFairing.Fit(p, q, 2, 0.5, s)) { throw new Exception("NaN accepted"); }
            invalid[1] = new P(double.PositiveInfinity, 0);
            if (PlanarFairing.Fit(p, q, 2, 0.5, s)) { throw new Exception("infinity accepted"); }
            invalid[1] = new P(double.MaxValue, 0);
            if (PlanarFairing.Fit(p, q, 2, 0, s)) { throw new Exception("overflow accepted at zero strength"); }
            invalid[1] = new P(10, 0);
            foreach (var strength in new[] { -1.0, 1.1, double.NaN, double.PositiveInfinity }) {
                if (PlanarFairing.Fit(p, q, 2, strength, s)) { throw new Exception("invalid strength accepted"); }
            }
            if (PlanarFairing.Fit(null, q, 2, 0.5, s) || PlanarFairing.Fit(p, q, 1, 0.5, s)) {
                throw new Exception("invalid buffer/count accepted");
            }
        }
        Console.WriteLine("PASS: analytic solution, nonuniform straight line, zero strength, zigzag reduction, endpoint/junction pins, reversal/translation/rotation invariance, long-path stationarity, two nodes, invalid inputs.");
    }

    private static void CheckCapturedHandleBlend() {
        // Player.log trace 566, Sept 26: second handle on edge 111496:1.
        // At strength .001 the old adapter jumped 81.68 m with stationary nodes.
        var original = new P(-1559.512, -1867.44067);
        var target = new P(-1533.36365, -1944.82434);
        var zero = PlanarBezier.BlendHandle(original, default, target, 0);
        Near(original.X, zero.X, "captured zero X"); Near(original.Z, zero.Z, "captured zero Z");
        var previous = 0.0;
        foreach (var strength in new[] { 0.000001, 0.001, 0.1, 0.49, 0.5, 0.51, 1.0 }) {
            var point = PlanarBezier.BlendHandle(original, default, target, strength);
            var distance = Math.Sqrt(Math.Pow(point.X - original.X, 2) + Math.Pow(point.Z - original.Z, 2));
            var fullDistance = Math.Sqrt(Math.Pow(target.X - original.X, 2) + Math.Pow(target.Z - original.Z, 2));
            Near(fullDistance * strength, distance, "captured displacement scales with strength");
            if (distance < previous || (strength <= 0.001 && distance > 0.082)) {
                throw new Exception("captured handle jumps or reverses during sweep");
            }
            previous = distance;
        }
        var delta = new P(7, -3);
        var moved = PlanarBezier.BlendHandle(original, delta, new P(target.X + 7, target.Z - 3), 0.5);
        var stationary = PlanarBezier.BlendHandle(original, default, target, 0.5);
        Near(stationary.X + 7, moved.X, "endpoint translation X");
        Near(stationary.Z - 3, moved.Z, "endpoint translation Z");
        Console.WriteLine("PASS: captured handle continuity, strength sweep, endpoint translation.");
    }

    private static void CheckBezier() {
        var a = new P(0, 0); var d = new P(30, 0);
        if (!PlanarBezier.Handles(a, d, new P(1, 0), new P(1, 0), out var b, out var c)) {
            throw new Exception("straight cubic rejected");
        }
        Near(10, b.X, "straight handle B"); Near(20, c.X, "straight handle C");
        var middle = new P(30, 10); var end = new P(60, 0);
        if (!PlanarBezier.Tangent(a, middle, end, out var tangent)) { throw new Exception("tangent rejected"); }
        if (!PlanarBezier.Handles(a, middle, new P(1, 0), tangent, out b, out c)
            || !PlanarBezier.Handles(middle, end, tangent, new P(1, 0), out var e, out var f)) {
            throw new Exception("bend rejected");
        }
        Near(0, (middle.X - c.X) * (e.Z - middle.Z) - (middle.Z - c.Z) * (e.X - middle.X), "shared tangent cross product");
        if (!PlanarBezier.Handles(middle, a, new P(-tangent.X, -tangent.Z), new P(-1, 0), out var rb, out var rc)) {
            throw new Exception("reversed cubic rejected");
        }
        Near(c.X, rb.X, "reverse handle"); Near(c.Z, rb.Z, "reverse handle");
        Near(b.X, rc.X, "reverse handle"); Near(b.Z, rc.Z, "reverse handle");
        if (PlanarBezier.Handles(a, d, new P(-1, 0), new P(1, 0), out _, out _)
            || PlanarBezier.Tangent(a, d, a, out _)) { throw new Exception("backward/cusp accepted"); }
        Console.WriteLine("PASS: cubic handles, shared planar tangents, reversal, backward/cusp rejection.");
    }

    // Independently differentiate the documented objective at the fitted points.
    // This checks optimality, including both axes and uneven spacing, not just appearance.
    private static void CheckStationarity(P[] original, P[] fitted, double strength) {
        var gradientX = new double[original.Length];
        var gradientZ = new double[original.Length];
        var h = new double[original.Length - 1];
        for (var i = 0; i < h.Length; i++) {
            h[i] = Math.Sqrt(Math.Pow(original[i + 1].X - original[i].X, 2) + Math.Pow(original[i + 1].Z - original[i].Z, 2));
            foreach (var j in new[] { i, i + 1 }) {
                gradientX[j] += h[i] * (fitted[j].X - original[j].X);
                gradientZ[j] += h[i] * (fitted[j].Z - original[j].Z);
            }
        }
        for (var i = 1; i < original.Length - 1; i++) {
            var dx = (fitted[i + 1].X - fitted[i].X) / h[i] - (fitted[i].X - fitted[i - 1].X) / h[i - 1];
            var dz = (fitted[i + 1].Z - fitted[i].Z) / h[i] - (fitted[i].Z - fitted[i - 1].Z) / h[i - 1];
            var multiplier = 4 * Math.Pow(50, 4) * strength * strength / (h[i - 1] + h[i]);
            var coefficients = new[] { 1 / h[i - 1], -1 / h[i - 1] - 1 / h[i], 1 / h[i] };
            for (var k = 0; k < 3; k++) {
                gradientX[i - 1 + k] += multiplier * dx * coefficients[k];
                gradientZ[i - 1 + k] += multiplier * dz * coefficients[k];
            }
        }
        for (var i = 1; i < original.Length - 1; i++) {
            if (original[i].Fixed) { continue; }
            if (Math.Abs(gradientX[i]) > 1e-5 || Math.Abs(gradientZ[i]) > 1e-5) {
                throw new Exception("objective gradient not zero at " + i);
            }
        }
    }
}
