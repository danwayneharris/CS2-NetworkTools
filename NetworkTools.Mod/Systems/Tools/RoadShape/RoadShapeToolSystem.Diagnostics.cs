#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using System.Text;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Entities;
    using Game.Net;
    using NetworkTools.Geometry;

    public partial class NT_RoadShapeToolSystem {
        private int m_SmoothTraceId;
        private static readonly string s_SmoothSession = Guid.NewGuid().ToString("N");

        // Called on the actual job arrays, not a second execution of the solver.
        // Debug-only: no managed formatting/logging is present in Release/Burst jobs.
        private static void TraceSmooth(int id, ToolOutputMode mode, float strength, bool valid,
            NativeList<NodeState> originalNodes, NativeList<EdgeState> originalEdges,
            NativeArray<NodeState> nodes, NativeArray<EdgeState> edges,
            SmoothFailure failure, int failureIndex, BufferLookup<ConnectedEdge> connected,
            ComponentLookup<Edge> edgeLookup, NativeList<Entity> selectedNodes) {
            CapturePreviewProbe(id, mode, valid, edges);
            var text = new StringBuilder();
            var strengthJson = math.isfinite(strength) ? strength.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "null";
            text.Append(FormattableString.Invariant($"[NetworkTools.SmoothTrace] {{\"id\":{id},\"mode\":\"{mode}\",\"strength\":{strengthJson},\"valid\":{(valid ? "true" : "false")},\"nodeCount\":{nodes.Length},\"edgeCount\":{edges.Length}"));
            // Avoid flooding logs on city-scale selections. Use a short selection for captures.
            text.Append(FormattableString.Invariant($",\"schema\":2,\"session\":\"{s_SmoothSession}\",\"failure\":\"{failure}\",\"failureIndex\":{failureIndex}"));
            if (nodes.Length > 128) {
                text.Append(",\"detailsOmitted\":true}");
                UnityEngine.Debug.Log(text.ToString());
                return;
            }
            text.Append(",\"selectedNodes\":[");
            for (var i = 0; i < selectedNodes.Length; i++) {
                if (i > 0) text.Append(',');
                var entity = selectedNodes[i];
                text.Append(FormattableString.Invariant($"\"{entity.Index}:{entity.Version}\""));
            }
            text.Append("],\"nodes\":[");
            for (var i = 0; i < nodes.Length; i++) {
                if (i > 0) { text.Append(','); }
                var input = originalNodes[i];
                text.Append(FormattableString.Invariant($"{{\"entity\":\"{input.Entity.Index}:{input.Entity.Version}\",\"pinned\":{(input.SmoothPinned || i == 0 || i == nodes.Length - 1 ? "true" : "false")},\"input\":"));
                AppendPosition(text, input.OriginalPosition);
                text.Append(",\"output\":");
                AppendPosition(text, nodes[i].Position);
                text.Append(",\"incidentEdges\":");
                if (!connected.TryGetBuffer(input.Entity, out var incident)) text.Append("null");
                else {
                    text.Append('[');
                    for (var j = 0; j < incident.Length; j++) {
                        if (j > 0) text.Append(',');
                        var entity = incident[j].m_Edge;
                        text.Append(FormattableString.Invariant($"\"{entity.Index}:{entity.Version}\""));
                    }
                    text.Append(']');
                }
                text.Append('}');
            }
            text.Append("],\"edges\":[");
            for (var i = 0; i < edges.Length; i++) {
                if (i > 0) { text.Append(','); }
                var input = originalEdges[i];
                text.Append(FormattableString.Invariant($"{{\"entity\":\"{input.EdgeEntity.Index}:{input.EdgeEntity.Version}\",\"forward\":{(input.IsForward ? "true" : "false")},\"input\":["));
                AppendPosition(text, input.Bezier.a); text.Append(',');
                AppendPosition(text, input.Bezier.b); text.Append(',');
                AppendPosition(text, input.Bezier.c); text.Append(',');
                AppendPosition(text, input.Bezier.d);
                text.Append("],\"output\":[");
                var output = edges[i].Bezier;
                AppendPosition(text, output.a); text.Append(',');
                AppendPosition(text, output.b); text.Append(',');
                AppendPosition(text, output.c); text.Append(',');
                AppendPosition(text, output.d);
                text.Append(']');
                if (edgeLookup.TryGetComponent(input.EdgeEntity, out var topology)) {
                    text.Append(FormattableString.Invariant($",\"startNode\":\"{topology.m_Start.Index}:{topology.m_Start.Version}\",\"endNode\":\"{topology.m_End.Index}:{topology.m_End.Version}\""));
                }
                text.Append('}');
            }
            text.Append("]}");
            UnityEngine.Debug.Log(text.ToString());
        }

        private static void AppendPosition(StringBuilder text, float3 p) {
            if (!math.all(math.isfinite(p))) { text.Append("null"); return; }
            text.Append(FormattableString.Invariant($"[{p.x:R},{p.y:R},{p.z:R}]"));
        }
    }
}
#endif
