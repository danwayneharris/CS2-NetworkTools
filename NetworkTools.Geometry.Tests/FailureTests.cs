using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

internal static class FailureTests {
    public static unsafe void Run() {
        var nodes = new[] { new P(0,0), new P(50,10,true), new P(100,0) };
        var curves = new[] {
            new PlanarCubic(new P(0,0),new P(15,0),new P(35,10),new P(50,10)),
            new PlanarCubic(new P(50,10),new P(65,10),new P(85,0),new P(100,0))
        };
        var output = new P[3]; var outputCurves = new PlanarCubic[2]; var scratch = new double[3];
        fixed(P* n=nodes, o=output)
        fixed(PlanarCubic* c=curves, co=outputCurves)
        fixed(double* s=scratch) {
            if (PlanarPathTarget.Fit(n,c,3,.001,o,co,s,out var failure,out var index)
                || failure!=SmoothFailure.InteriorPinnedNode || index!=1) throw new Exception("Missing interior-pin reason");
            if (!PlanarPathTarget.Fit(n,c,3,0,o,co,s,out failure,out index)
                || failure!=SmoothFailure.None || index!=-1) throw new Exception("Zero-strength pin rejected");
            nodes[1] = new P(0,0);
            if (PlanarPathTarget.Fit(n,c,3,0,o,co,s,out failure,out index)
                || failure!=SmoothFailure.DegenerateNodeChord || index!=1) throw new Exception("Missing degenerate-chord reason");
            nodes[1] = new P(double.NaN,10);
            if (PlanarPathTarget.Fit(n,c,3,1,o,co,s,out failure,out index)
                || failure!=SmoothFailure.NonFiniteNode || index!=1) throw new Exception("Missing nonfinite reason");
        }
        Console.WriteLine("PASS: rejection reasons, offending indices, and zero-strength junction acceptance.");
    }
}
