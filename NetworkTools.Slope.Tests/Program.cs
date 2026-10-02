using System;
using Colossal.Mathematics;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Collections;
using Unity.Mathematics;

// Deterministic formula-relief fixtures inspired by upstream PR #74's ProvingGround.
// Calls compiled production Slope code. No ECS world, Unity native allocation, terrain
// deformation or renderer: native Preview/Apply remain separate integration tests.
static class Program {
    static int checks;
    static void Near(float actual, float expected, string name, float tolerance = 0.0001f) {
        if (!float.IsFinite(actual) || math.abs(actual-expected)>tolerance)
            throw new Exception($"{name}: expected {expected}, actual {actual}");
        checks++;
    }
    static float Grade(float3 tangent) => tangent.y / math.length(tangent.xz);
    static void Main(string[] args) {
        if (args.Length == 4 && args[0] == "--replay") { CapturedSlopeReplay.Run(args[1],args[2],args[3]); return; }
        PreviewStructureTests.Run();
        IncidentEditTests.Run();
        CombinedProfileInputTests.Run();
        CombinedProfileTransformTests.Run();
        (string name, Func<float,float,float> height)[] terrains = {
            ("flat", (x,z)=>64f),
            ("hillside", (x,z)=>64f+.08f*x+.03f*z),
            ("ridge", (x,z)=>64f+math.max(0f,42f-math.abs(x-280f)*.2f)),
            ("valley", (x,z)=>64f-math.max(0f,20f-math.abs(x-160f)*.1f))
        };
        foreach (var terrain in terrains) foreach (var forward in new[]{true,false}) {
            var curve = new Bezier4x3(new float3(100,terrain.height(100,0),0),
                new float3(120,terrain.height(120,0),0),new float3(160,terrain.height(160,40),40),
                new float3(160,terrain.height(160,60),60));
            var edge = new EdgeState { IsForward=forward, Length=MathUtils.Length(curve),
                Bezier=curve, OriginalBezierA=curve.a, OriginalBezierD=curve.d };
            edge.CalculateControlPointRatios();
            Near(edge.StartControlPointRatio*edge.Length,20,"start handle distance",.001f);
            Near((1-edge.EndControlPointRatio)*edge.Length,20,"end handle distance",.001f);
            var ctx = ShapeTransformContext.Create(new float3(0,terrain.height(0,0),0),
                new float3(500,terrain.height(500,60),60));
            ctx.TotalLength=500;
            edge.StartPointAbsoluteRatio=.2f;
            edge.EndPointAbsoluteRatio=.2f+edge.Length/ctx.TotalLength;
            edge.StartControlPointAbsoluteRatio=.2f+edge.StartControlPointRatio*edge.Length/ctx.TotalLength;
            edge.EndControlPointAbsoluteRatio=.2f+edge.EndControlPointRatio*edge.Length/ctx.TotalLength;
            var config=new ShapeJobConfig { EaseInLength=.1f, EaseOutLength=.1f };
            var transform=new SlopeEaseInOutTransform();
            NativeArray<EdgeState> unused=default;
            transform.PreProcess(ref unused,in ctx,in config);
            transform.Process(ref edge,0,in ctx,in config);
            var start=forward?edge.Bezier.b-edge.Bezier.a:edge.Bezier.c-edge.Bezier.d;
            var end=forward?edge.Bezier.d-edge.Bezier.c:edge.Bezier.a-edge.Bezier.b;
            Near(Grade(start),SlopeUtils.GetSlopeAtPathRatio(transform.ReferenceBezier,edge.StartPointAbsoluteRatio)/ctx.TotalLength,"physical start grade");
            Near(Grade(end),SlopeUtils.GetSlopeAtPathRatio(transform.ReferenceBezier,edge.EndPointAbsoluteRatio)/ctx.TotalLength,"physical end grade");
            var before=edge.Bezier;
            // Native shared-node constraint: preserve the original endpoint/node offset,
            // and preserve the fitted tangent while bringing the endpoint to that height.
            SlopeUtils.AlignEndpointHeight(ref edge,true,curve.a.y-3f,72f);
            SlopeUtils.AlignEndpointHeight(ref edge,false,curve.d.y,68f);
            Near(edge.Bezier.a.y,75,"nonzero original offset");
            Near(edge.Bezier.d.y,68,"shared node height");
            Near(Grade(edge.Bezier.b-edge.Bezier.a),Grade(before.b-before.a),"start grade after alignment");
            Near(Grade(edge.Bezier.d-edge.Bezier.c),Grade(before.d-before.c),"end grade after alignment");
            foreach(var pair in new[]{(edge.Bezier.a,curve.a),(edge.Bezier.b,curve.b),(edge.Bezier.c,curve.c),(edge.Bezier.d,curve.d)})
                Near(math.distance(pair.Item1.xz,pair.Item2.xz),0,"horizontal geometry unchanged");
            Console.WriteLine($"PASS {terrain.name}, forward={forward}");
        }
        Console.WriteLine($"PASS {checks} assertions against production code");
    }
}
