namespace NetworkTools.Systems.Tools.RoadShape {
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    /// <summary>Maps a shared horizontal target onto the existing path topology.</summary>
    public static class CurveSmoothTransform {
        private static Point Horizontal(float3 p) => new Point(p.x, p.z);
        private static float3 WithHorizontal(float3 original, Point p) => new float3((float)p.X, original.y, (float)p.Z);

        public static unsafe bool Execute(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes, float strength) {
            if (nodes.Length < 2 || edges.Length != nodes.Length - 1) return false;
            var input = new NativeArray<Point>(nodes.Length, Allocator.Temp);
            var fitted = new NativeArray<Point>(nodes.Length, Allocator.Temp);
            var curves = new NativeArray<PlanarCubic>(edges.Length, Allocator.Temp);
            var output = new NativeArray<PlanarCubic>(edges.Length, Allocator.Temp);
            var stations = new NativeArray<double>(nodes.Length, Allocator.Temp);
            var valid = true;
            for (var i = 0; i < nodes.Length; i++) {
                var p = nodes[i].OriginalPosition;
                valid &= math.all(math.isfinite(p));
                input[i] = new Point(p.x, p.z, nodes[i].SmoothPinned);
                for (var j = 0; j < i; j++) {
                    if (nodes[j].Entity == nodes[i].Entity) valid = false;
                }
            }
            for (var i = 0; i < edges.Length; i++) {
                var c = edges[i].Bezier;
                valid &= math.all(math.isfinite(c.a)) && math.all(math.isfinite(c.b))
                    && math.all(math.isfinite(c.c)) && math.all(math.isfinite(c.d));
                curves[i] = edges[i].IsForward
                    ? new PlanarCubic(Horizontal(c.a), Horizontal(c.b), Horizontal(c.c), Horizontal(c.d))
                    : new PlanarCubic(Horizontal(c.d), Horizontal(c.c), Horizontal(c.b), Horizontal(c.a));
            }
            valid = valid && PlanarPathTarget.Fit((Point*)input.GetUnsafeReadOnlyPtr(),
                (PlanarCubic*)curves.GetUnsafeReadOnlyPtr(), nodes.Length, strength,
                (Point*)fitted.GetUnsafePtr(), (PlanarCubic*)output.GetUnsafePtr(), (double*)stations.GetUnsafePtr());
            // Validate float conversion before publishing any results.
            for (var i = 0; valid && i < nodes.Length; i++) {
                valid &= math.all(math.isfinite(WithHorizontal(nodes[i].OriginalPosition, fitted[i])));
            }
            for (var i = 0; valid && i < edges.Length; i++) {
                var c = output[i];
                valid &= math.all(math.isfinite(WithHorizontal(float3.zero, c.A)))
                    && math.all(math.isfinite(WithHorizontal(float3.zero, c.B)))
                    && math.all(math.isfinite(WithHorizontal(float3.zero, c.C)))
                    && math.all(math.isfinite(WithHorizontal(float3.zero, c.D)));
            }
            if (valid && strength > 0) {
                for (var i = 0; i < edges.Length; i++) {
                    var edge = edges[i]; var c = edge.Bezier; var target = output[i];
                    c.a = WithHorizontal(c.a, edge.IsForward ? target.A : target.D);
                    c.b = WithHorizontal(c.b, edge.IsForward ? target.B : target.C);
                    c.c = WithHorizontal(c.c, edge.IsForward ? target.C : target.B);
                    c.d = WithHorizontal(c.d, edge.IsForward ? target.D : target.A);
                    edge.Bezier = c;
                    edges[i] = edge;
                }
                for (var i = 0; i < nodes.Length; i++) {
                    var node = nodes[i];
                    node.Position = WithHorizontal(node.OriginalPosition, fitted[i]);
                    nodes[i] = node;
                }
            }
            stations.Dispose(); output.Dispose(); curves.Dispose(); fitted.Dispose(); input.Dispose();
            return valid;
        }
    }
}
