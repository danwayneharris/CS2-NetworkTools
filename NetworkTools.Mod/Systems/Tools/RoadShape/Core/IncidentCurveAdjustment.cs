namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Mathematics;
    using Game.Net;
    using Unity.Mathematics;

    // Endpoint deltas act on disjoint control pairs. Compose both before emitting
    // a whole Curve value; queuing two replacements would lose the first edit.
    public static class IncidentCurveAdjustment {
        public static Bezier4x3 Translate(Bezier4x3 original, float3 startDelta, float3 endDelta) {
            original.a += startDelta;
            original.b += startDelta;
            original.c += endDelta;
            original.d += endDelta;
            return original;
        }

        public static Node MoveNode(Node original, float3 position) {
            original.m_Position = position;
            return original;
        }
    }
}
