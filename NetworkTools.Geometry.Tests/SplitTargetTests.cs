using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

internal static class SplitTargetTests {
    public static void Run() {
        var nodes = new[] { new P(0,0),new P(20,12),new P(40,10),new P(60,-8),new P(80,0) };
        var curves = new PlanarCubic[4];
        for (var i=0;i<4;i++) curves[i]=new PlanarCubic(nodes[i],
            new P(nodes[i].X+5,nodes[i].Z),new P(nodes[i+1].X-5,nodes[i+1].Z),nodes[i+1]);
        // Deliberately displaced segment endpoints at the future split.
        curves[1].D=new P(39,17); curves[2].A=new P(42,3);
        var split=new[] {false,false,true,false,false};
        if (!PlanarSplitTarget.TryFit(nodes,curves,split,out var fitted,out var result,out _,out _))
            throw new Exception("split target rejected");
        Near(fitted[2],nodes[2]); Near(result[1].D,nodes[2]); Near(result[2].A,nodes[2]);
        var a=result[1].Derivative(1); var b=result[2].Derivative(0);
        if(Math.Abs(a.X*b.Z-a.Z*b.X)>1e-8 || a.X*b.X+a.Z*b.Z<=0)
            throw new Exception("split target kink");
        Near(result[0].A,curves[0].A); Near(result[3].D,curves[3].D);
        for(var step=0;step<=20;step++) {
            if(!PlanarSplitTarget.TryFit(nodes,curves,split,out fitted,out result,out _,out _,step/20.0))
                throw new Exception("split strength rejected");
            Near(fitted[2],nodes[2]); Near(result[1].D,result[2].A);
            a=result[1].Derivative(1); b=result[2].Derivative(0);
            if(Math.Abs(a.X*b.Z-a.Z*b.X)>1e-8 || a.X*b.X+a.Z*b.Z<=0)
                throw new Exception("partial-strength split kink");
        }
        var reversedNodes=(P[])nodes.Clone(); Array.Reverse(reversedNodes);
        var reversedCurves=new PlanarCubic[curves.Length];
        for(var i=0;i<curves.Length;i++) {
            var c=curves[curves.Length-1-i]; reversedCurves[i]=new PlanarCubic(c.D,c.C,c.B,c.A);
        }
        if(!PlanarSplitTarget.TryFit(reversedNodes,reversedCurves,split,out var rn,out var rc,out _,out _))
            throw new Exception("reversed split rejected");
        for(var i=0;i<result.Length;i++) {
            Near(result[i].A,rc[rc.Length-1-i].D); Near(result[i].B,rc[rc.Length-1-i].C);
            Near(result[i].C,rc[rc.Length-1-i].B); Near(result[i].D,rc[rc.Length-1-i].A);
        }
        var twoSplits=new[] {false,true,false,true,false};
        if(!PlanarSplitTarget.TryFit(nodes,curves,twoSplits,out fitted,out result,out _,out _))
            throw new Exception("multiple split rejected");
        Near(fitted[1],nodes[1]); Near(fitted[3],nodes[3]);
        var invalid=(PlanarCubic[])curves.Clone(); invalid[1].D=new P(double.NaN,0);
        if(PlanarSplitTarget.TryFit(nodes,invalid,split,out _,out _,out _,out _))
            throw new Exception("nonfinite split endpoint accepted");
        nodes[2]=new P(40,10,true);
        if(PlanarSplitTarget.TryFit(nodes,curves,split,out fitted,out result,out var why,out var at)
            || why!=SmoothFailure.InteriorPinnedNode || at!=2 || fitted!=null || result!=null)
            throw new Exception("split bypassed junction guard or published partial result");
        Console.WriteLine("PASS: split target pin, offset endpoint reconstruction, shared planar tangent, junction rejection.");
    }
    private static void Near(P a,P b) {
        if(Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)>1e-8) throw new Exception("split position changed");
    }
}
