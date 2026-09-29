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
