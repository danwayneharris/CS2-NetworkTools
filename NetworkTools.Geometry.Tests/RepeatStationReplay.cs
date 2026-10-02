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
    }
}
