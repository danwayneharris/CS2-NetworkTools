using System;
using Colossal.Mathematics;
using Game.Net;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Mathematics;

static class IncidentEditTests {
    static void Same(float3 a, float3 b, string label) {
        if (!math.all(a == b)) throw new Exception(label);
    }
    public static void Run() {
        var original = new Bezier4x3(new float3(1,2,3), new float3(4,5,6), new float3(7,8,9), new float3(10,11,12));
        var start = new float3(2,3,4);
        var end = new float3(-1,5,2);
        var result = IncidentCurveAdjustment.Translate(original, start, end);
        Same(result.a, original.a + start, "start endpoint lost");
        Same(result.b, original.b + start, "start handle lost");
        Same(result.c, original.c + end, "end handle lost");
        Same(result.d, original.d + end, "end endpoint lost");
        Same(result.b-result.a, original.b-original.a, "start tangent changed");
        Same(result.c-result.d, original.c-original.d, "end tangent changed");
        var reverse = IncidentCurveAdjustment.Translate(new Bezier4x3(original.d,original.c,original.b,original.a),end,start);
        Same(reverse.a,result.d,"reversed start"); Same(reverse.b,result.c,"reversed start handle");
        Same(reverse.c,result.b,"reversed end handle"); Same(reverse.d,result.a,"reversed end");
        var one = IncidentCurveAdjustment.Translate(original,start,float3.zero);
        Same(one.c, original.c,"unchanged end handle"); Same(one.d,original.d,"unchanged end");
        var node = new Node { m_Position = original.a, m_Rotation = quaternion.RotateY(0.7f) };
        foreach (var position in new[]{node.m_Position,new float3(20,30,40)}) {
            var moved = IncidentCurveAdjustment.MoveNode(node,position);
            Same(moved.m_Position,position,"node position");
            if (!math.all(moved.m_Rotation.value == node.m_Rotation.value)) throw new Exception("node rotation discarded");
        }
        Console.WriteLine("Incident edit regressions passed: both ends, reversal, one unchanged end, tangents, rotation.");
    }
}
