using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

internal static class JunctionTargetTests {
    private static unsafe bool Fit(P[] nodes, PlanarCubic[] curves, byte[] splits, double strength,
        out P[] outputNodes, out PlanarCubic[] outputCurves, double handleScale = 1, double rotation = 0) {
        outputNodes = new P[nodes.Length]; outputCurves = new PlanarCubic[curves.Length];
        var workNodes = new P[nodes.Length]; var workCurves = new PlanarCubic[curves.Length];
        var stations = new double[nodes.Length];
        fixed (P* input = nodes, output = outputNodes, work = workNodes)
        fixed (PlanarCubic* original = curves, result = outputCurves, scratch = workCurves)
        fixed (byte* flags = splits)
        fixed (double* s = stations) {
            return PlanarJunctionTarget.Fit(input, original, flags, nodes.Length, strength,
                output, result, work, scratch, s, out _, out _, junctionHandleScale: handleScale, junctionRotation: rotation);
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
        foreach (var scale in new[] { 0.5, 0.8, 1.2, 1.5 }) {
            if (!Fit(nodes,curves,splits,1,out var scaledNodes,out var scaled,scale)
                || !Fit(reverseNodes,reverseCurves,splits,1,out _,out var scaledReverse,scale))
                throw new Exception("bounded junction handle candidate rejected");
            Near(nodes[3],scaledNodes[3]);
            Near(curves[2].D,scaled[2].D); Near(curves[3].A,scaled[3].A);
            var before = curves[2].Derivative(1); var after = scaled[2].Derivative(1);
            Near(new P(before.X*scale,before.Z*scale),after);
            before = curves[3].Derivative(0); after = scaled[3].Derivative(0);
            Near(new P(before.X*scale,before.Z*scale),after);
            for (var i=0;i<scaled.Length;i++) {
                var c=scaledReverse[scaledReverse.Length-1-i];
                Near(scaled[i].A,c.D); Near(scaled[i].B,c.C);
                Near(scaled[i].C,c.B); Near(scaled[i].D,c.A);
            }
        }
        foreach (var scale in new[] { double.NaN, double.PositiveInfinity, -1, 0, 0.49, 1.51 })
            if (Fit(nodes,curves,splits,1,out _,out _,scale))
                throw new Exception("invalid junction handle scale accepted");
        foreach (var rotation in new[] { -Math.PI/12, -0.05, 0.05, Math.PI/12 }) {
            if (!Fit(nodes,curves,splits,0.8,out var rotatedNodes,out var rotated,1,rotation)
                || !Fit(reverseNodes,reverseCurves,splits,0.8,out _,out var reverseRotated,1,rotation))
                throw new Exception("junction rotation rejected");
            Near(nodes[3],rotatedNodes[3]);
            Near(curves[2].D,rotated[2].D); Near(curves[3].A,rotated[3].A);
            var a=curves[2].Derivative(1); var b=curves[3].Derivative(0);
            var c=rotated[2].Derivative(1); var d=rotated[3].Derivative(0);
            if(Math.Abs(a.X*b.X+a.Z*b.Z-c.X*d.X-c.Z*d.Z)>1e-7
                || Math.Abs(a.X*b.Z-a.Z*b.X-c.X*d.Z+c.Z*d.X)>1e-7)
                throw new Exception("common rotation changed relative tangent angle");
            for(var j=0;j<rotated.Length;j++) {
                var v=reverseRotated[reverseRotated.Length-1-j];
                Near(rotated[j].A,v.D); Near(rotated[j].B,v.C);
                Near(rotated[j].C,v.B); Near(rotated[j].D,v.A);
            }
        }
        foreach(var rotation in new[] { double.NaN, double.PositiveInfinity, -0.27, 0.27 })
            if(Fit(nodes,curves,splits,1,out _,out _,1,rotation))
                throw new Exception("unbounded junction rotation accepted");
        splits[1]=1;
        if(!Fit(nodes,curves,splits,0.5,out var pinned,out var combined))
            throw new Exception("split alongside junction rejected");
        Near(nodes[1],pinned[1]);
        var left=combined[0].Derivative(1); var right=combined[1].Derivative(0);
        if(Math.Abs(left.X*right.Z-left.Z*right.X)>1e-7 || left.X*right.X+left.Z*right.Z<=0)
            throw new Exception("junction section broke ordinary split tangent");
        splits[3]=1;
        if(Fit(nodes,curves,splits,1,out _,out _)) throw new Exception("ambiguous junction/split accepted");
        splits[3]=0;
        var multiple=(P[])nodes.Clone(); multiple[4]=new P(nodes[4].X,nodes[4].Z,true);
        if(!Fit(multiple,curves,splits,1,out var multiNodes,out var multiCurves))
            throw new Exception("multiple junctions rejected");
        Near(multiple[3],multiNodes[3]); Near(multiple[4],multiNodes[4]);
        Near(curves[3].A,multiCurves[3].A); Near(curves[3].B,multiCurves[3].B);
        Near(curves[3].C,multiCurves[3].C); Near(curves[3].D,multiCurves[3].D);
        curves[3].B=new P(double.NaN,0);
        if(Fit(nodes,curves,splits,1,out _,out _)) throw new Exception("nonfinite junction handle accepted");
        Console.WriteLine("PASS: isolated junction sections preserve ports, smooth interiors, reverse and combine with splits.");
    }

    private static void Near(P a,P b) {
        if(!double.IsFinite(b.X)||!double.IsFinite(b.Z)||Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)>1e-7)
            throw new Exception("junction target mismatch");
    }
}
