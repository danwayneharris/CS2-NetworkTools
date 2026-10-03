#if IS_DEBUG
namespace NetworkTools.Systems.Tools.Connect {
    using Colossal.Mathematics;
    using NetworkTools.Geometry;
    using Unity.Mathematics;
    using Unity.Collections;
    using Game.Net;
    using Game.Tools;
    using Unity.Entities;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    public partial class NT_ConnectToolSystem {
        private static PlanarCubic ProfilePlanar(Bezier4x3 c) => new(
            new Point(c.a.x, c.a.z), new Point(c.b.x, c.b.z),
            new Point(c.c.x, c.c.z), new Point(c.d.x, c.d.z));

        private static Bezier4x3[] ProfileCurves(ConnectJobConfig c) => c.ComplexProfile
            ? new[] {
                new Bezier4x3 { a=c.ComplexStartPointPosition, b=c.ComplexStartControlPointPosition,
                    c=c.ComplexMidStartControlPointPosition, d=c.ComplexMidPosition },
                new Bezier4x3 { a=c.ComplexMidPosition, b=c.ComplexMidEndControlPointPosition,
                    c=c.ComplexEndControlPointPosition, d=c.ComplexEndPointPosition }
            } : new[] { new Bezier4x3 { a=c.CurveStartPointPosition, b=c.CurveStartControlPointPosition,
                c=c.CurveEndControlPointPosition, d=c.CurveEndPointPosition } };

        private unsafe bool TryPrepareProfile(ref ConnectJobConfig config, out string reason) {
            if (!TryReadProfileEndpoints(out var start, out var end, out reason)) return false;
            // Native intersection offsets require an explicit surface convention. Do
            // not mistake a distant edge endpoint for the selected node's height.
            if (math.abs(start.CurveEndpoint.y - start.NodePosition.y) > .05f
                || math.abs(end.CurveEndpoint.y - end.NodePosition.y) > .05f) {
                reason = "profile_endpoint_height_offset_unsupported"; return false;
            }
            var curves = ProfileCurves(config);
            if (math.distance(curves[0].a, start.NodePosition) > .05f
                || math.distance(curves[curves.Length-1].d, end.NodePosition) > .05f) {
                reason = "profile_endpoint_moved_unsupported"; return false;
            }
            var horizontal = stackalloc PlanarCubic[2];
            var heights = stackalloc VerticalLinearProfile.Heights[2];
            for (var i = 0; i < curves.Length; i++) horizontal[i] = ProfilePlanar(curves[i]);
            var startGrade = start.OutwardGrade; var endGrade = -end.OutwardGrade;
            if (LaneAwareDirection.Value) {
                if (!TryResolveLaneDirectionEndpoints(out var laneStart, out var laneEnd, out reason)) return false;
                startGrade = laneStart.HandleGrade; endGrade = -laneEnd.HandleGrade;
            }
            if (!ConnectVerticalProfile.Fit(horizontal, curves.Length, start.NodePosition.y, end.NodePosition.y,
                startGrade, endGrade, heights, out _, out var failure)) {
                reason = "profile_" + failure; return false;
            }
            if (!config.ComplexProfile) {
                config.CurveStartPointPosition.y = (float)heights[0].A;
                config.CurveStartControlPointPosition.y = (float)heights[0].B;
                config.CurveEndControlPointPosition.y = (float)heights[0].C;
                config.CurveEndPointPosition.y = (float)heights[0].D;
            } else {
                config.ComplexStartPointPosition.y = (float)heights[0].A;
                config.ComplexStartControlPointPosition.y = (float)heights[0].B;
                config.ComplexMidStartControlPointPosition.y = (float)heights[0].C;
                config.ComplexMidPosition.y = (float)heights[0].D;
                config.ComplexMidEndControlPointPosition.y = (float)heights[1].B;
                config.ComplexEndControlPointPosition.y = (float)heights[1].C;
                config.ComplexEndPointPosition.y = (float)heights[1].D;
            }
            // Structural elevation flags deliberately retain the legacy generator's
            // policy. Raw world height is not an elevation/bridge classification.
            reason = null; return true;
        }

        private static ConnectProfileCoverage.Cubic CoverageCurve(Bezier4x3 c) => new() {
            Horizontal = ProfilePlanar(c),
            Vertical = new VerticalLinearProfile.Heights { A=c.a.y, B=c.b.y, C=c.c.y, D=c.d.y }
        };

        private unsafe bool ValidateNativeProfile(ConnectJobConfig config, out string reason) {
            var curves = ProfileCurves(config);
            var authored = stackalloc ConnectProfileCoverage.Cubic[2];
            for (var i = 0; i < curves.Length; i++) authored[i] = CoverageCurve(curves[i]);
            using var query = ControlTempQuery();
            using var entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0 || entities.Length > 256) { reason = "profile_native_count"; return false; }
            var native = stackalloc ConnectProfileCoverage.Cubic[256];
            var count = 0;
            foreach (var entity in entities) {
                if (EntityManager.GetComponentData<Temp>(entity).m_Original != Entity.Null) continue;
                native[count++] = CoverageCurve(EntityManager.GetComponentData<Curve>(entity).m_Bezier);
            }
            var mapping = stackalloc ConnectProfileCoverage.Mapping[256];
            if (!ConnectProfileCoverage.Validate(authored, curves.Length, native, count, mapping,
                out var failure, out var failedNative)) {
                reason = "profile_native_" + failure + "_" + failedNative; return false;
            }
            reason = "accepted"; return true;
        }
    }
}
#endif
