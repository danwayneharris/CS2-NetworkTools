using System;
using Colossal.Mathematics;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Mathematics;

internal static class CombinedProfileInputTests {
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static void Near(double a, double b, string message) => Check(Math.Abs(a-b)<1e-6, message);
    public static void Run() {
        foreach (var forward in new[]{true,false}) {
            var curve=new Bezier4x3(new float3(0,10.25f,0),new float3(10,12.25f,0),
                new float3(30,15.5f,0),new float3(40,17.5f,0));
            var original=new EdgeState{Bezier=curve,IsForward=forward};
            var candidate=original;
            candidate.Bezier.b.x=5; candidate.Bezier.c.x=15; candidate.Bezier.d.x=20;
            // A stale/intermediate candidate Y must not become an authored endpoint offset.
            candidate.Bezier.a.y=100; candidate.Bezier.d.y=200;
            var start=new NodeState{OriginalPosition=new float3(0,forward?10:18,0)};
            var end=new NodeState{OriginalPosition=new float3(0,forward?18:10,0)};
            Check(CombinedProfileInputs.TrySegment(original,candidate,start,end,out var s),"candidate input");
            Near(s.Length,20,"candidate length instead of original 40");
            Near(s.StartHandle,5,"candidate start handle");Near(s.EndHandle,5,"candidate end handle");
            Near(s.StartOffset,forward?.25:-.5,"authored start offset");
            Near(s.EndOffset,forward?-.5:.25,"authored end offset");
            Check(CombinedProfileInputs.TryGrade(original,true,out var gs),"original start grade");
            Check(CombinedProfileInputs.TryGrade(original,false,out var ge),"original end grade");
            Near(gs,forward?.2:-.2,"oriented original start grade");Near(ge,forward?.2:-.2,"oriented original end grade");
            var invalid=candidate;invalid.IsForward=!forward;
            Check(!CombinedProfileInputs.TrySegment(original,invalid,start,end,out _),"direction mismatch");
            invalid=candidate;invalid.Bezier.b.x=float.NaN;
            Check(!CombinedProfileInputs.TrySegment(original,invalid,start,end,out _),"invalid candidate");
            invalid=original;invalid.Bezier.b=invalid.Bezier.a;invalid.Bezier.c=invalid.Bezier.d;
            Check(!CombinedProfileInputs.TryGrade(invalid,true,out _),"undefined start grade");
            Check(!CombinedProfileInputs.TryGrade(invalid,false,out _),"undefined end grade");
        }
        Console.WriteLine($"PASS {checks} combined-profile input assertions against production code");
    }
}
