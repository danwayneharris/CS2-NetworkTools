using System;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Entities;
using Unity.Mathematics;

static class PreviewStructureTests {
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run() {
        var curve = new Bezier4x3(new float3(0,10,0),new float3(10,12,0),new float3(20,13,0),new float3(30,14,0));
        var limit = new NetGeometryData { m_ElevationLimit = 4 };
        var cases = new[] {
            (edge:new float2(0),start:new float2(0),end:new float2(0)),
            (edge:new float2(8),start:new float2(0),end:new float2(8)),
            (edge:new float2(8),start:new float2(8),end:new float2(8)),
            (edge:new float2(-12),start:new float2(0),end:new float2(-12)),
            (edge:new float2(-12),start:new float2(-12),end:new float2(-12)),
            (edge:new float2(4,0),start:new float2(0,4),end:new float2(-4,0))
        };
        int checks=0;
        foreach (var c in cases) foreach (var reverse in new[]{false,true}) {
            var start=reverse?c.end:c.start; var end=reverse?c.start:c.end;
            var a=reverse?curve.d:curve.a; var d=reverse?curve.a:curve.d;
            var b=reverse?new Bezier4x3(curve.d,curve.c,curve.b,curve.a):curve;
            var course=RoadShapePreviewCourse.Create(b,30,Entity.Null,new Entity{Index=7,Version=3},a,d,c.edge,start,end);
            Check(math.all(course.m_Elevation==c.edge),"edge metadata changed");
            Check(math.all(course.m_StartPosition.m_Elevation==start),"start metadata changed");
            Check(math.all(course.m_EndPosition.m_Elevation==end),"end metadata changed");
            Check((course.m_StartPosition.m_Flags & CoursePosFlags.ForceElevatedNode)==0,"start force elevation");
            Check((course.m_EndPosition.m_Flags & CoursePosFlags.ForceElevatedNode)==0,"end force elevation");
            Check(math.all(course.m_Curve.b==b.b) && math.all(course.m_StartPosition.m_Position==a),"geometry changed");
            Check((course.m_StartPosition.m_Flags & CoursePosFlags.IsLast)!=0 && (course.m_EndPosition.m_Flags & CoursePosFlags.IsFirst)!=0,"incident references changed");
            checks+=7;
        }
        // Captured ground road next to elevated edge: the shared node stays ground.
        var neighbor=RoadShapePreviewCourse.Create(curve,30,Entity.Null,Entity.Null,curve.a,curve.d,new float2(8),new float2(0),new float2(8));
        var ground=NetCompositionHelpers.GetElevationFlags(new Elevation(neighbor.m_StartPosition.m_Elevation),new Elevation(new float2(0)),new Elevation(new float2(0)),limit);
        var polluted=NetCompositionHelpers.GetElevationFlags(new Elevation(new float2(8)),new Elevation(new float2(0)),new Elevation(new float2(0)),limit);
        Check((ground.m_General & CompositionFlags.General.Elevated)==0,"ground road promoted by neighbor");
        Check((polluted.m_General & CompositionFlags.General.Elevated)!=0,"counterexample no longer discriminates native rule");
        Console.WriteLine($"Preview structural regressions passed: {checks+2} checks, ground/bridge/portal/tunnel/asymmetric/reversed and native composition counterexample.");
    }
}
