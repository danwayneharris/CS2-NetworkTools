namespace NetworkTools.Systems.Tools.RoadShape {
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    /// <summary>Fits endpoint-compatible vertical geometry before native reconstruction.</summary>
    public static class SlopeLinearProfileTransform {
        private static Point XZ(float3 p) => new Point(p.x, p.z);

        public static unsafe bool Execute(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes,
            in ShapeTransformContext ctx, in ShapeJobConfig config) {
            if (edges.Length < 1 || nodes.Length != edges.Length + 1) { return false; }
            var input = new NativeArray<VerticalLinearProfile.Segment>(edges.Length, Allocator.Temp);
            var output = new NativeArray<VerticalLinearProfile.Heights>(edges.Length, Allocator.Temp);
            var heights = new NativeArray<double>(nodes.Length, Allocator.Temp);
            var valid = true;
            for (var i = 0; i < edges.Length; i++) {
                var e = edges[i]; var c = e.Bezier;
                var a = e.IsForward ? c.a : c.d;
                var b = e.IsForward ? c.b : c.c;
                var cp = e.IsForward ? c.c : c.b;
                var d = e.IsForward ? c.d : c.a;
                if (!math.all(math.isfinite(a)) || !math.all(math.isfinite(b))
                    || !math.all(math.isfinite(cp)) || !math.all(math.isfinite(d))
                    || !VerticalLinearProfile.TryHorizontalLength(new PlanarCubic(XZ(a), XZ(b), XZ(cp), XZ(d)), out var length)) {
                    valid = false; break;
                }
                input[i] = new VerticalLinearProfile.Segment {
                    Length = length,
                    StartHandle = math.distance(a.xz, b.xz), EndHandle = math.distance(cp.xz, d.xz),
                    StartOffset = (double)a.y - nodes[i].OriginalPosition.y,
                    EndOffset = (double)d.y - nodes[i + 1].OriginalPosition.y
                };
            }
            if (valid) {
                valid = VerticalLinearProfile.Fit((VerticalLinearProfile.Segment*)input.GetUnsafeReadOnlyPtr(), edges.Length,
                    nodes[0].OriginalPosition.y, nodes[nodes.Length - 1].OriginalPosition.y,
                    config.SmoothStart && ctx.StartSmoothEligible, ctx.StartAnchorSlope,
                    config.SmoothEnd && ctx.EndSmoothEligible, ctx.EndAnchorSlope,
                    (double*)heights.GetUnsafePtr(), (VerticalLinearProfile.Heights*)output.GetUnsafePtr(), out _);
            }
            for (var i = 0; valid && i < nodes.Length; i++) {
                valid = math.isfinite((float)heights[i]);
            }
            for (var i = 0; valid && i < edges.Length; i++) {
                var h = output[i];
                valid = math.all(math.isfinite(new float4((float)h.A, (float)h.B, (float)h.C, (float)h.D)));
            }
            if (valid) {
                for (var i = 0; i < edges.Length; i++) {
                    var e = edges[i]; var h = output[i];
                    e.Bezier = SlopeUtils.ApplyHeightsToBezier(e.Bezier, (float)h.A, (float)h.B, (float)h.C, (float)h.D, e.IsForward);
                    edges[i] = e;
                }
                for (var i = 1; i < nodes.Length - 1; i++) {
                    var n = nodes[i]; n.Position.y = (float)heights[i]; nodes[i] = n;
                }
            }
            heights.Dispose(); output.Dispose(); input.Dispose();
            return valid;
        }
    }
}