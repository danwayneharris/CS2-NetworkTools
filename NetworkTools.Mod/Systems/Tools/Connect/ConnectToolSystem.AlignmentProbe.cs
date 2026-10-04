#if IS_DEBUG
namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using Colossal.Entities;
    using Game.Prefabs;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        // Developer preview-only experiment, not the user-facing NT-022 feature.
        // Fixed offsets deliberately exercise CoursePos/node vs curve endpoints.
        private static readonly bool AlignmentProbeEnabled = Array.IndexOf(
            Environment.GetCommandLineArgs(), "--nt-connect-fixed-anchor-probe") >= 0;
        private bool PrepareAlignmentProbe(ref ConnectJobConfig config, out string reason) {
            reason = null;
            if (!AlignmentProbeEnabled) return true;
            if (Mode.Value != ConnectMode.SimpleCurve || config.SmoothElevationProfile || LaneAwareDirection.Value) {
                reason = "alignment_probe_requires_legacy_simple"; return false;
            }
            if (config.NetPrefabEntity == Entity.Null
                || !EntityManager.TryGetComponent<NetGeometryData>(config.NetPrefabEntity, out var geometry)
                || (geometry.m_Flags & (Game.Net.GeometryFlags.StrictNodes | Game.Net.GeometryFlags.StraightEdges)) != 0) {
                reason = "alignment_probe_prefab_unsupported"; return false;
            }
            var first = config.CurveStartControlPointPosition.xz - config.CurveStartPointPosition.xz;
            var last = config.CurveEndPointPosition.xz - config.CurveEndControlPointPosition.xz;
            if (math.length(first) < .01f || math.length(last) < .01f) { reason = "alignment_probe_degenerate"; return false; }
            first = math.normalize(first); last = math.normalize(last);
            var ds = new float3(-first.y, 0, first.x) * 1.5f;
            var de = new float3(-last.y, 0, last.x) * 1.5f;
            config.CurveStartPointPosition += ds; config.CurveStartControlPointPosition += ds;
            config.CurveEndControlPointPosition += de; config.CurveEndPointPosition += de;
            config.FixedNodeAlignmentProbe = true;
            return true;
        }
    }
}
#endif
