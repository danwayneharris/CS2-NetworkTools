namespace NetworkTools.Systems.Tools.RoadShape {
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    /// <summary>
    /// Fits nodes and reconstructs horizontal cubics as one validated path operation.
    /// </summary>
    public static class CurveSmoothTransform {
        /// <summary>Only publishes geometry when the whole path is suitable.</summary>
        public static unsafe bool Execute(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes, float strength) {
            if (nodes.Length < 2 || edges.Length != nodes.Length - 1) { return false; }
            var input = new NativeArray<Point>(nodes.Length, Allocator.Temp);
            var fitted = new NativeArray<Point>(nodes.Length, Allocator.Temp);
            var scratch = new NativeArray<double>(nodes.Length * 5, Allocator.Temp);
            var candidates = new NativeArray<EdgeState>(edges.Length, Allocator.Temp);
            var valid = true;
            for (var i = 0; i < nodes.Length; i++) {
                var position = nodes[i].OriginalPosition;
                valid &= math.all(math.isfinite(position));
                input[i] = new Point(position.x, position.z, nodes[i].SmoothPinned);
                // A repeated entity cannot be assigned two fitted positions in one Apply.
                for (var j = 0; j < i; j++) {
                    if (nodes[j].Entity == nodes[i].Entity) { valid = false; }
                }
            }
            valid = valid && PlanarFairing.Fit((Point*)input.GetUnsafeReadOnlyPtr(), (Point*)fitted.GetUnsafePtr(),
                nodes.Length, strength, (double*)scratch.GetUnsafePtr());
            for (var i = 0; valid && i < edges.Length; i++) {
                var edge = edges[i];
                var curve = edge.Bezier;
                var a = edge.IsForward ? curve.a : curve.d;
                var b = edge.IsForward ? curve.b : curve.c;
                var c = edge.IsForward ? curve.c : curve.b;
                var d = edge.IsForward ? curve.d : curve.a;
                if (!math.all(math.isfinite(a)) || !math.all(math.isfinite(b))
                    || !math.all(math.isfinite(c)) || !math.all(math.isfinite(d))) { valid = false; break; }
                if (strength > 0) {
                    // Move intersection offsets with their node; retain every control point's Y.
                    var startDelta = new float3((float)(fitted[i].X - input[i].X), 0, (float)(fitted[i].Z - input[i].Z));
                    var endDelta = new float3((float)(fitted[i + 1].X - input[i + 1].X), 0, (float)(fitted[i + 1].Z - input[i + 1].Z));
                    var startDirection = new Point(b.x - a.x, b.z - a.z);
                    var endDirection = new Point(d.x - c.x, d.z - c.z);
                    if (i > 0 && !input[i].Fixed) {
                        valid = PlanarBezier.Tangent(fitted[i - 1], fitted[i], fitted[i + 1], out startDirection);
                    }
                    if (valid && i + 2 < nodes.Length && !input[i + 1].Fixed) {
                        valid = PlanarBezier.Tangent(fitted[i], fitted[i + 1], fitted[i + 2], out endDirection);
                    }
                    a += startDelta;
                    d += endDelta;
                    if (!valid || !PlanarBezier.Handles(new Point(a.x, a.z), new Point(d.x, d.z),
                        startDirection, endDirection, out var first, out var second)) { valid = false; break; }
                    b.x = (float)first.X; b.z = (float)first.Z;
                    c.x = (float)second.X; c.z = (float)second.Z;
                    curve.a = edge.IsForward ? a : d;
                    curve.b = edge.IsForward ? b : c;
                    curve.c = edge.IsForward ? c : b;
                    curve.d = edge.IsForward ? d : a;
                    edge.Bezier = curve;
                }
                candidates[i] = edge;
            }
            if (valid) {
                for (var i = 0; i < edges.Length; i++) { edges[i] = candidates[i]; }
                for (var i = 0; i < nodes.Length; i++) {
                    var node = nodes[i];
                    node.Position = new float3((float)fitted[i].X, node.OriginalPosition.y, (float)fitted[i].Z);
                    nodes[i] = node;
                }
            }
            candidates.Dispose();
            scratch.Dispose();
            fitted.Dispose();
            input.Dispose();
            return valid;
        }
    }
}
