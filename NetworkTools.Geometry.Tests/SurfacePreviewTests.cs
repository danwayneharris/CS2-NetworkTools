using System;
using NetworkTools.Geometry;
using R = NetworkTools.Geometry.SurfacePreviewConvergence.Result;
internal static class SurfacePreviewTests {
    private static void Check(R actual, R expected) { if (actual != expected) throw new Exception($"Surface convergence: {actual} != {expected}"); }
    internal static void Run() {
        var s = new SurfacePreviewConvergence();
        Check(s.Observe(true, double.PositiveInfinity), R.Retry);
        Check(s.Observe(true, .1241), R.Retry);
        Check(s.Observe(true, .0072), R.Retry);
        Check(s.Observe(true, .003), R.Accepted);
        s = new SurfacePreviewConvergence();
        Check(s.Observe(false, double.PositiveInfinity), R.Accepted);
        s = new SurfacePreviewConvergence();
        Check(s.Observe(true, double.PositiveInfinity), R.Retry);
        Check(s.Observe(false, 0), R.Failed);
        s = new SurfacePreviewConvergence();
        for (var i=0; i<5; i++) Check(s.Observe(true, .2), R.Retry);
        Check(s.Observe(true, .2), R.Failed);
        Check(new SurfacePreviewConvergence().Observe(true, double.NaN), R.Failed);
        Console.WriteLine("Surface preview: bounded convergence, fallback, oscillation and nonfinite tests passed.");
    }
}
