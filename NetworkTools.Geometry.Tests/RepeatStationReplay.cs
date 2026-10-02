using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;
internal static class RepeatStationReplay {
    static P Read(JsonElement e) => new P(e.GetProperty("x").GetDouble(),e.GetProperty("z").GetDouble());
    static double Distance(P a,P b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    public static unsafe void Run(string path) {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var root=doc.RootElement;
        var nodes=root.GetProperty("nodes").EnumerateArray().Select(Read).ToArray();
        var second=root.GetProperty("secondNodes").EnumerateArray().Select(Read).ToArray();
        var curves=root.GetProperty("curves").EnumerateArray().Select(e=>{var q=e.EnumerateArray().Select(Read).ToArray();return new PlanarCubic(q[0],q[1],q[2],q[3]);}).ToArray();
        var output=new P[nodes.Length];var fitted=new PlanarCubic[curves.Length];var stations=new double[nodes.Length];
        fixed(P* n=nodes,o=output) fixed(PlanarCubic* c=curves,f=fitted) fixed(double* s=stations) {
            if(!PlanarPathTarget.Fit(n,c,nodes.Length,1,o,f,s))throw new Exception("Captured path rejected");
        }
        var displacement=nodes.Select((n,i)=>Distance(n,output[i])).Max();
        var nativeError=second.Select((n,i)=>Distance(n,output[i])).Max();
        Console.WriteLine(JsonSerializer.Serialize(new {displacement,nativeError,stations}));
        if(displacement<1)throw new Exception("Counterexample no longer demonstrates substantial restationing; update experiment");
        fixed(P* n=nodes,o=output) fixed(PlanarCubic* c=curves,f=fitted) fixed(double* s=stations) {
            if(!PlanarPathTarget.Fit(n,c,nodes.Length,1,o,f,s,out _,out _,0,0,true))throw new Exception("Stable station fit rejected");
        }
        var stableDrift=nodes.Select((n,i)=>Distance(n,output[i])).Max();
        Console.WriteLine(JsonSerializer.Serialize(new {stableDrift}));
        if(stableDrift>.001)throw new Exception("Stable station replay moved already fitted nodes");
        var again=new P[nodes.Length];var againCurves=new PlanarCubic[curves.Length];
        fixed(P* n=output,o=again) fixed(PlanarCubic* c=fitted,f=againCurves) fixed(double* s=stations) {
            if(!PlanarPathTarget.Fit(n,c,nodes.Length,1,o,f,s,out _,out _,0,0,true))throw new Exception("Repeated stable fit rejected");
        }
        if(output.Select((n,i)=>Distance(n,again[i])).Max()>1e-8)throw new Exception("Stable fit not idempotent");
        for(var i=0;i<fitted.Length;i++) {
            var x=fitted[i];var y=againCurves[i];
            if(new[]{Distance(x.A,y.A),Distance(x.B,y.B),Distance(x.C,y.C),Distance(x.D,y.D)}.Max()>1e-8)
                throw new Exception("Repeated controls drifted");
        }
        var reverseNodes=nodes.Reverse().ToArray();
        var reverseCurves=curves.Reverse().Select(c=>new PlanarCubic(c.D,c.C,c.B,c.A)).ToArray();
        fixed(P* n=reverseNodes,o=again) fixed(PlanarCubic* c=reverseCurves,f=againCurves) fixed(double* s=stations) {
            if(!PlanarPathTarget.Fit(n,c,nodes.Length,1,o,f,s,out _,out _,0,0,true))throw new Exception("Reverse stable fit rejected");
        }
        if(output.Select((n,i)=>Distance(n,again[nodes.Length-1-i])).Max()>1e-8)throw new Exception("Reversed stations differ");
        Console.WriteLine("PASS: captured station drift, stable repeated controls and reversed traversal.");
    }
}
