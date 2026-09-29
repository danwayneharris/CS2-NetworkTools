using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

internal static class JunctionTargetTests {
    private static unsafe bool Fit(P[] nodes, PlanarCubic[] curves, byte[] splits, double strength,
        out P[] outputNodes, out PlanarCubic[] outputCurves) {
        outputNodes = new P[nodes.Length]; outputCurves = new PlanarCubic[curves.Length];
        var workNodes = new P[nodes.Length]; var workCurves = new PlanarCubic[curves.Length];
        var stations = new double[nodes.Length];
        fixed (P* input = nodes, output = outputNodes, work = workNodes)
        fixed (PlanarCubic* original = curves, result = outputCurves, scratch = workCurves)
        fixed (byte* flags = splits)
        fixed (double* s = stations) {
            return PlanarJunctionTarget.Fit(input, original, flags, nodes.Length, strength,
                output, result, work, scratch, s, out _, out _);
        }
    }

    public static void Run() {
        var nodes = new[] { new P(0,0), new P(20,12), new P(40,-5), new P(60,0,true),
            new P(80,9), new P(100,-12), new P(120,0) };
        var curves = new PlanarCubic[nodes.Length - 1];
        for (var i = 0; i < curves.Length; i++) {
            curves[i] = new PlanarCubic(nodes[i], new P(nodes[i].X+5,nodes[i].Z),
                new P(nodes[i+1].X-5,nodes[i+1].Z), nodes[i+1]);
        }
        // Different branch attachment positions must not be collapsed to the center.
        curves[2].C = new P(55,3); curves[2].D = new P(59,2);
        curves[3].A = new P(62,-1); curves[3].B = new P(67,-1);
        var splits = new byte[nodes.Length];
        for (var step = 0; step <= 20; step++) {
            if (!Fit(nodes,curves,splits,step/20.0,out var fitted,out var result))
                throw new Exception("junction section fit rejected");
            Near(nodes[3],fitted[3]);
            Near(curves[2].C,result[2].C); Near(curves[2].D,result[2].D);
            Near(curves[3].A,result[3].A); Near(curves[3].B,result[3].B);
            if (step == 20 && Math.Abs(fitted[1].Z-nodes[1].Z)<0.01)
                throw new Exception("junction fit did not smooth the selected section");
        }
        Fit(nodes,curves,splits,1,out var forwardNodes,out var forward);
        var reverseNodes=(P[])nodes.Clone(); Array.Reverse(reverseNodes);
        var reverseCurves=new PlanarCubic[curves.Length];
        for(var i=0;i<curves.Length;i++) {
            var c=curves[curves.Length-1-i]; reverseCurves[i]=new PlanarCubic(c.D,c.C,c.B,c.A);
        }
        if(!Fit(reverseNodes,reverseCurves,splits,1,out var reversedNodes,out var reverse))
            throw new Exception("reversed junction fit rejected");
        for(var i=0;i<forward.Length;i++) {
            var c=reverse[reverse.Length-1-i];
            Near(forward[i].A,c.D); Near(forward[i].B,c.C);
            Near(forward[i].C,c.B); Near(forward[i].D,c.A);
        }
        splits[1]=1;
        if(!Fit(nodes,curves,splits,0.5,out var pinned,out var combined))
            throw new Exception("split alongside junction rejected");
        Near(nodes[1],pinned[1]);
        var left=combined[0].Derivative(1); var right=combined[1].Derivative(0);
        if(Math.Abs(left.X*right.Z-left.Z*right.X)>1e-7 || left.X*right.X+left.Z*right.Z<=0)
            throw new Exception("junction section broke ordinary split tangent");
        splits[3]=1;
        if(Fit(nodes,curves,splits,1,out _,out _)) throw new Exception("ambiguous junction/split accepted");
        splits[3]=0; curves[3].B=new P(double.NaN,0);
        if(Fit(nodes,curves,splits,1,out _,out _)) throw new Exception("nonfinite junction handle accepted");
        Console.WriteLine("PASS: isolated junction sections preserve ports, smooth interiors, reverse and combine with splits.");
    }

    private static void Near(P a,P b) {
        if(!double.IsFinite(b.X)||!double.IsFinite(b.Z)||Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)>1e-7)
            throw new Exception("junction target mismatch");
    }
}
