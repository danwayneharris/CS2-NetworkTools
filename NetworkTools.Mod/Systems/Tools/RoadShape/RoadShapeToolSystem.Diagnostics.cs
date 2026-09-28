#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using System.Text;
    using Unity.Collections;
    using Unity.Mathematics;

    public partial class NT_RoadShapeToolSystem {
        private int m_SmoothTraceId;

        // Called on the actual job arrays, not a second execution of the solver.
        // Debug-only: no managed formatting/logging is present in Release/Burst jobs.
        private static void TraceSmooth(int id, ToolOutputMode mode, float strength, bool valid,
            NativeList<NodeState> originalNodes, NativeList<EdgeState> originalEdges,
            NativeArray<NodeState> nodes, NativeArray<EdgeState> edges) {
            var text = new StringBuilder();
            text.Append(FormattableString.Invariant($"[NetworkTools.SmoothTrace] {{\"id\":{id},\"mode\":\"{mode}\",\"strength\":{strength:R},\"valid\":{(valid ? "true" : "false")},\"nodeCount\":{nodes.Length},\"edgeCount\":{edges.Length}"));
            // Avoid flooding logs on city-scale selections. Use a short selection for captures.
            if (nodes.Length > 128) {
                text.Append(",\"detailsOmitted\":true}");
                UnityEngine.Debug.Log(text.ToString());
                return;
            }
            text.Append(",\"nodes\":[");
            for (var i = 0; i < nodes.Length; i++) {
                if (i > 0) { text.Append(','); }
                var input = originalNodes[i];
                text.Append(FormattableString.Invariant($"{{\"entity\":\"{input.Entity.Index}:{input.Entity.Version}\",\"pinned\":{(input.SmoothPinned || i == 0 || i == nodes.Length - 1 ? "true" : "false")},\"input\":"));
                AppendPosition(text, input.OriginalPosition);
                text.Append(",\"output\":");
                AppendPosition(text, nodes[i].Position);
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
                AppendPosition(text, output.d); text.Append("]}");
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
