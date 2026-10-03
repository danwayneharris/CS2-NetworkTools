using System;
using NetworkTools.Geometry;
using R = NetworkTools.Geometry.SurfacePreviewConvergence.Result;
internal static class SurfacePreviewTests {
    private static void Check(R actual, R expected) { if (actual != expected) throw new Exception($"Surface convergence: {actual} != {expected}"); }
    internal static void Run() {
        if (!SurfacePreviewIdentity.AllowsApply(4,4,8,8,8,true,false))
            throw new Exception("Current complete candidate rejected");
        if (SurfacePreviewIdentity.AllowsApply(5,4,8,8,8,true,false)
            || SurfacePreviewIdentity.AllowsApply(4,4,9,8,9,true,false)
            || SurfacePreviewIdentity.AllowsApply(4,4,9,9,8,true,false)
            || SurfacePreviewIdentity.AllowsApply(4,4,9,8,8,true,false)
            || SurfacePreviewIdentity.AllowsApply(4,4,8,8,8,false,false)
            || SurfacePreviewIdentity.AllowsApply(4,4,8,8,8,true,true))
            throw new Exception("Stale, cross-submission, pending, failed acceptance");
        Console.WriteLine("PASS: surface and native validators require the same current complete candidate.");
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
