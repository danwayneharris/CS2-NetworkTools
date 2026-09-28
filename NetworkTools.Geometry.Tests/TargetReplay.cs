using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

// An offline experiment, not a replacement for the game's adapter.
internal static class TargetReplay {
    private static double Distance(P a, P b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    private static P Difference(P a, P b) => new P(a.X-b.X, a.Z-b.Z);
    private static P ReadPoint(JsonElement e) => new P(e[0].GetDouble(), e[2].GetDouble());
    private static double[][] Controls(PlanarCubic c) => new[] { c.A,c.B,c.C,c.D }.Select(p => new[] { p.X,p.Z }).ToArray();
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }

    public static unsafe void Run(string[] args) {
        var curve = new PlanarCubic(new P(0,0), new P(20,40), new P(80,-30), new P(100,10));
        Check(curve.TrySlice(.13,.79,out var slice), "slice rejected");
        for (var i=0; i<=100; i++) {
            var t=i/100.0;
            Check(Distance(curve.Evaluate(.13+.66*t),slice.Evaluate(t)) < 1e-10, "subcurve differs from target");
        }
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -0.1, 1.1 }) {
            Check(!curve.TrySlice(invalid, 1, out _), "invalid interval accepted");
            Check(!curve.TrySlice(0, invalid, out _), "invalid interval accepted");
        }
        Check(!curve.TrySlice(.5,.5,out _), "empty interval accepted");
        Console.WriteLine("PASS: exact cubic subinterval reconstruction and invalid intervals.");
        if (args.Length == 0) return;
        if (args.Length != 2) throw new Exception("Expected fixture and output JSON paths");
        using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));
        var edges=doc.RootElement.GetProperty("edges").EnumerateArray().Select(e => {
            var p=e.GetProperty("input").EnumerateArray().Select(ReadPoint).ToArray();
            if (!e.GetProperty("forward").GetBoolean()) Array.Reverse(p);
            return new PlanarCubic(p[0],p[1],p[2],p[3]);
        }).ToArray();
        var nodes=doc.RootElement.GetProperty("nodes").EnumerateArray().Select(n => ReadPoint(n.GetProperty("input"))).ToArray();
        Check(edges.Length==2 && nodes.Length==3,"experiment requires two edges");
        Check(PlanarBezier.Handles(edges[0].A,edges[1].D,Difference(edges[0].B,edges[0].A),
            Difference(edges[1].D,edges[1].C),out var b,out var c),"boundary target rejected");
        var target=new PlanarCubic(edges[0].A,b,c,edges[1].D);
        var station=Distance(nodes[0],nodes[1])/(Distance(nodes[0],nodes[1])+Distance(nodes[1],nodes[2]));
        Check(target.TrySlice(0,station,out var first) && target.TrySlice(station,1,out _),"target partition rejected");
        target.TrySlice(station,1,out var second);
        var pieces=new[] { first, second };
        var maxError=0.0;
        for(var i=0;i<=1000;i++) {
            var t=i/1000.0;
            var p=t<=station ? first.Evaluate(t/station) : second.Evaluate((t-station)/(1-station));
            maxError=Math.Max(maxError,Distance(target.Evaluate(t),p));
        }
        Check(maxError<1e-9,"target reconstruction error");
        var left=first.Derivative(1); var right=second.Derivative(0);
        var cross=(left.X*right.Z-left.Z*right.X)/(Distance(left,default)*Distance(right,default));
        Check(Math.Abs(cross)<1e-12 && left.X*right.X+left.Z*right.Z>0,"join tangent mismatch");
        // Blend all four controls and node centers. No hidden switch at positive strength.
        var sweeps=new[] {0.0,.001,.1,.5,1.0}.Select(s => new {
            strength=s,
            edges=edges.Select((edge,i) => {
                var old=Controls(edge); var dst=Controls(pieces[i]);
                return old.Select((p,k) => new[] {p[0]+s*(dst[k][0]-p[0]),p[1]+s*(dst[k][1]-p[1])}).ToArray();
            }).ToArray()
        }).ToArray();
        var middle=target.Evaluate(station);
        var actualNodes=new P[3]; var actualCurves=new PlanarCubic[2]; var scratch=new double[3];
        fixed(P* inputNodes=nodes, fittedNodes=actualNodes)
        fixed(PlanarCubic* inputEdges=edges, fittedEdges=actualCurves)
        fixed(double* stations=scratch) {
            foreach(var sweep in sweeps) {
                Check(PlanarPathTarget.Fit(inputNodes,inputEdges,3,sweep.strength,fittedNodes,fittedEdges,stations),"production target rejected fixture");
                for(var j=0;j<2;j++) {
                    var controls=Controls(actualCurves[j]);
                    for(var k=0;k<4;k++) {
                        Check(Distance(new P(controls[k][0],controls[k][1]),new P(sweep.edges[j][k][0],sweep.edges[j][k][1]))<1e-9,"production target differs from replay");
                    }
                }
                Check(Distance(actualNodes[0],nodes[0])==0 && Distance(actualNodes[2],nodes[2])==0,"boundary node moved");
            }
            nodes[1].Fixed=true;
            Check(!PlanarPathTarget.Fit(inputNodes,inputEdges,3,1,fittedNodes,fittedEdges,stations),"interior junction accepted");
            nodes[1].Fixed=false;
            Check(!PlanarPathTarget.Fit(inputNodes,inputEdges,3,double.NaN,fittedNodes,fittedEdges,stations),"NaN strength accepted");
            Array.Reverse(nodes); Array.Reverse(edges);
            for(var j=0;j<2;j++) { var e=edges[j]; edges[j]=new PlanarCubic(e.D,e.C,e.B,e.A); }
            Check(PlanarPathTarget.Fit(inputNodes,inputEdges,3,1,fittedNodes,fittedEdges,stations),"reversed target rejected");
            Check(Distance(actualNodes[1],middle)<1e-9,"reversal changed middle node");
            for(var j=0;j<2;j++) {
                var q=Controls(actualCurves[j]); var p=Controls(pieces[1-j]);
                for(var k=0;k<4;k++) Check(Distance(new P(q[k][0],q[k][1]),new P(p[3-k][0],p[3-k][1]))<1e-9,"reversal changed target controls");
            }
            Array.Reverse(nodes); Array.Reverse(edges);
            for(var j=0;j<2;j++) { var e=edges[j]; edges[j]=new PlanarCubic(e.D,e.C,e.B,e.A); }
        }
        foreach (var sweep in sweeps) {
            var leftEnd=sweep.edges[0][3]; var rightStart=sweep.edges[1][0];
            var gap=Distance(new P(leftEnd[0],leftEnd[1]),new P(rightStart[0],rightStart[1]));
            Check(Math.Abs(gap-(1-sweep.strength)*Distance(edges[0].D,edges[1].A))<1e-9,
                "endpoint gap does not close continuously");
        }
        var floatPieces=pieces.Select(p => {
            var q=Controls(p).Select(v => new P((float)v[0],(float)v[1])).ToArray();
            return new PlanarCubic(q[0],q[1],q[2],q[3]);
        }).ToArray();
        var floatError=0.0;
        for (var i=0;i<=1000;i++) {
            var t=i/1000.0;
            var p=t<=station ? floatPieces[0].Evaluate(t/station) : floatPieces[1].Evaluate((t-station)/(1-station));
            floatError=Math.Max(floatError,Distance(target.Evaluate(t),p));
        }
        Check(floatError<.001,"float conversion exceeds 1 mm");
        File.WriteAllText(args[1],JsonSerializer.Serialize(new {
            target=Controls(target), station, pieces=pieces.Select(Controls),
            middle=new[] {middle.X,middle.Z}, maxError, tangentCross=cross,
            nodeMovement=Distance(nodes[1],middle), floatError, sweeps
        },new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"PASS captured target: max error={maxError:G5} m; tangent cross={cross:G5}; node movement={Distance(nodes[1],middle):F3} m");
        Console.WriteLine($"PASS endpoint-gap sweep; float-coordinate error={floatError:G5} m");
    }
}
