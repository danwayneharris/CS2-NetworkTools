namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Mathematics;
    using Game.Net;
    using Game.Prefabs;
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    /// <summary>
    /// Experimental fixed-surface-anchor fit for a ground ramp whose junction consumes
    /// the first native half-curve. Unsupported/poorly fitted cases retain the regular fit.
    /// No terrain feedback, topology changes or edits to unselected incident edges.
    /// </summary>
    public static class SlopeSurfaceProfileTransform {
        private static Point XZ(float3 p) => new Point(p.x, p.z);
        private static float Length(Bezier4x3 c) {
            return VerticalLinearProfile.TryHorizontalLength(new PlanarCubic(XZ(c.a), XZ(c.b), XZ(c.c), XZ(c.d)), out var length)
                ? (float)length : float.NaN;
        }
        private static Bezier4x3 Average(Bezier4x3 a, Bezier4x3 b) => new Bezier4x3 {
            a = (a.a + b.a) * .5f, b = (a.b + b.b) * .5f, c = (a.c + b.c) * .5f, d = (a.d + b.d) * .5f
        };
        private static float Y(Bezier4x3 c, int i) => i == 0 ? c.a.y : i == 1 ? c.b.y : i == 2 ? c.c.y : c.d.y;

        public static unsafe bool TryExecute(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes,
            in ShapeJobConfig config, in BufferLookup<ConnectedEdge> connections,
            in ComponentLookup<EdgeGeometry> geometry, in ComponentLookup<NodeGeometry> nodeGeometry,
            in ComponentLookup<PrefabRef> prefabs, in ComponentLookup<NetGeometryData> netGeometry,
            in ComponentLookup<Composition> compositions, in ComponentLookup<NetCompositionData> compositionData) {
            var count = edges.Length;
            if (count < 2 || count > 64 || config.SmoothStart || config.SmoothEnd) { return false; }
            var reverse = connections.HasBuffer(nodes[count].Entity) && connections[nodes[count].Entity].Length >= 3;
            var startIndex = reverse ? count : 0;
            var endIndex = reverse ? 0 : count;
            if (!connections.HasBuffer(nodes[startIndex].Entity) || connections[nodes[startIndex].Entity].Length < 3
                || !connections.HasBuffer(nodes[endIndex].Entity) || connections[nodes[endIndex].Entity].Length != 1) { return false; }
            for (var i = 0; i <= count; i++) {
                if (!nodeGeometry.TryGetComponent(nodes[i].Entity, out var ng) || math.abs(ng.m_Offset) > .001f || ng.m_Flatness != 0
                    || (i > 0 && i < count && (!connections.HasBuffer(nodes[i].Entity) || connections[nodes[i].Entity].Length != 2))) { return false; }
            }
            for (var i = 0; i < count; i++) {
                var entity = edges[i].EdgeEntity;
                if (!prefabs.TryGetComponent(entity, out var prefab) || !netGeometry.TryGetComponent(prefab.m_Prefab, out var data)
                    || (data.m_Flags & GeometryFlags.SmoothElevation) == 0 || (data.m_Flags & GeometryFlags.SmoothSlopes) != 0
                    || !compositions.TryGetComponent(entity, out var comp) || !compositionData.TryGetComponent(comp.m_Edge, out var ed)
                    || !compositionData.TryGetComponent(comp.m_StartNode, out var sn) || !compositionData.TryGetComponent(comp.m_EndNode, out var en)
                    || (ed.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0
                    || ((sn.m_Flags.m_General | en.m_Flags.m_General) & (CompositionFlags.General.Roundabout | CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) != 0
                    || math.abs(ed.m_MiddleOffset) > .001f) { return false; }
            }
            var firstEdge = edges[reverse ? count - 1 : 0];
            if (!geometry.TryGetComponent(firstEdge.EdgeEntity, out var original)) { return false; }
            var forward = firstEdge.IsForward != reverse;
            var firstLeft = forward ? original.m_Start.m_Left : MathUtils.Invert(original.m_End.m_Right);
            var firstRight = forward ? original.m_Start.m_Right : MathUtils.Invert(original.m_End.m_Left);
            var compFirst = compositions[firstEdge.EdgeEntity];
            var width = compositionData[compFirst.m_Edge].m_Width;
            var slopeLimit = netGeometry[prefabs[firstEdge.EdgeEntity].m_Prefab].m_MaxSlopeSteepness;
            var curves = new NativeArray<Bezier4x3>(count, Allocator.Temp);
            var weights = new NativeArray<float>(count + 1, Allocator.Temp);
            var lengths = new NativeArray<float>(count, Allocator.Temp);
            var baseOutput = new NativeArray<Bezier4x3>(4, Allocator.Temp);
            var probeOutput = new NativeArray<Bezier4x3>(4, Allocator.Temp);
            var valid = true;
            var total = 0f;
            for (var i = 0; i < count; i++) {
                var e = edges[reverse ? count - 1 - i : i];
                curves[i] = e.IsForward != reverse ? e.Bezier : MathUtils.Invert(e.Bezier);
                lengths[i] = Length(curves[i]); total += lengths[i];
            }
            valid = math.isfinite(total) && total > .01f;
            var accumulated = 0f;
            for (var i = 0; i <= count; i++) {
                weights[i] = 1 - accumulated / total;
                if (i < count) { accumulated += lengths[i]; }
            }
            var startPosition = nodes[startIndex].Position;
            var nextPosition = nodes[reverse ? count - 1 : 1].Position;
            var response = stackalloc double[48];
            var residual = stackalloc double[16];
            var solution = stackalloc double[3];
            if (valid) { valid = Model(curves, weights, total, startPosition, nextPosition,
                nodeGeometry[nodes[startIndex].Entity].m_Position, width, firstLeft.a, firstRight.a, float3.zero, baseOutput); }
            if (valid) {
                var endLeft = forward ? original.m_End.m_Left : MathUtils.Invert(original.m_Start.m_Right);
                var endRight = forward ? original.m_End.m_Right : MathUtils.Invert(original.m_Start.m_Left);
                valid = SameXZ(baseOutput[0], firstLeft) && SameXZ(baseOutput[1], endLeft)
                    && SameXZ(baseOutput[2], firstRight) && SameXZ(baseOutput[3], endRight);
            }
            var target = new NativeArray<Bezier4x3>(4, Allocator.Temp);
            if (valid) {
                var centerStart = Average(baseOutput[0], baseOutput[2]);
                var centerEnd = Average(baseOutput[1], baseOutput[3]);
                var l0 = Length(centerStart); var l1 = Length(centerEnd);
                var anchor = .5f * (firstLeft.a.y + firstRight.a.y);
                var grade = (curves[count - 1].d.y - anchor) / (l0 + l1 + total - lengths[0]);
                valid = math.isfinite(grade) && math.abs(grade) < slopeLimit;
                for (var side = 0; side < 2; side++) {
                    var sideY = side == 0 ? firstLeft.a.y : firstRight.a.y;
                    for (var half = 0; half < 2; half++) {
                        var c = half == 0 ? centerStart : centerEnd;
                        var station = half == 0 ? 0 : l0;
                        var length = half == 0 ? l0 : l1;
                        var positions = new float4(station, station + math.distance(c.a.xz, c.b.xz),
                            station + length - math.distance(c.c.xz, c.d.xz), station + length);
                        var y = anchor + grade * positions + (sideY - anchor) * (1 - positions / (l0 + l1));
                        c.a.y = y.x; c.b.y = y.y; c.c.y = y.z; c.d.y = y.w;
                        target[side * 2 + half] = c;
                    }
                }
                for (var row = 0; row < 16; row++) { residual[row] = Y(target[row / 4], row % 4) - Y(baseOutput[row / 4], row % 4); }
                for (var p = 0; p < 3 && valid; p++) {
                    var delta = new float3(p == 0 ? 1 : 0, p == 1 ? 1 : 0, p == 2 ? 1 : 0);
                    valid = Model(curves, weights, total, startPosition, nextPosition,
                        nodeGeometry[nodes[startIndex].Entity].m_Position, width, firstLeft.a, firstRight.a, delta, probeOutput);
                    for (var row = 0; row < 16; row++) { response[row * 3 + p] = Y(probeOutput[row / 4], row % 4) - Y(baseOutput[row / 4], row % 4); }
                }
                if (valid) { valid = SurfaceProfileResponseFit.Fit(response, residual, 16, solution, .05, 20); }
            }
            if (valid) {
                var delta = new float3((float)solution[0], (float)solution[1], (float)solution[2]);
                valid = Model(curves, weights, total, startPosition, nextPosition,
                    nodeGeometry[nodes[startIndex].Entity].m_Position, width, firstLeft.a, firstRight.a, delta, probeOutput);
                for (var row = 0; row < 16 && valid; row++) {
                    valid = math.abs(Y(probeOutput[row / 4], row % 4) - Y(target[row / 4], row % 4)) <= .05f;
                }
                for (var side = 0; side < 2 && valid; side++) {
                    var a = probeOutput[side * 2]; var b = probeOutput[side * 2 + 1];
                    var la = Length(a); var lb = Length(b);
                    var low = math.max(a.a.y - slopeLimit * la, b.d.y - slopeLimit * lb);
                    var high = math.min(a.a.y + slopeLimit * la, b.d.y + slopeLimit * lb);
                    var middle = (low + high) * .5f; var weight = 1 / (.5f * (la + lb) / math.max(.01f, width) + 1);
                    valid = high >= low && a.d.y >= math.lerp(low, middle, weight) - .05f && a.d.y <= math.lerp(high, middle, weight) + .05f;
                }
                if (valid) {
                    for (var i = 0; i < count; i++) {
                        var index = reverse ? count - 1 - i : i; var e = edges[index];
                        var c = Adjust(curves[i], i, weights, total, delta);
                        e.Bezier = e.IsForward != reverse ? c : MathUtils.Invert(c); edges[index] = e;
                    }
                    for (var i = 1; i < count; i++) {
                        var index = reverse ? count - i : i; var n = nodes[index];
                        n.Position.y += delta.x * weights[i]; nodes[index] = n;
                    }
                }
            }
            target.Dispose(); probeOutput.Dispose(); baseOutput.Dispose(); lengths.Dispose(); weights.Dispose(); curves.Dispose();
            return valid;
        }

        private static bool SameXZ(Bezier4x3 a, Bezier4x3 b) =>
            math.distance(a.a.xz, b.a.xz) <= .05f && math.distance(a.b.xz, b.b.xz) <= .05f
            && math.distance(a.c.xz, b.c.xz) <= .05f && math.distance(a.d.xz, b.d.xz) <= .05f;

        private static Bezier4x3 Adjust(Bezier4x3 c, int i, NativeArray<float> weights, float total, float3 delta) {
            c.a.y += i == 0 ? 0 : delta.x * weights[i];
            c.b.y += delta.x * (weights[i] - math.distance(c.a.xz, c.b.xz) / total) + (i == 0 ? delta.y : 0);
            c.c.y += delta.x * (weights[i + 1] + math.distance(c.c.xz, c.d.xz) / total) + (i == 0 ? delta.z : 0);
            c.d.y += delta.x * weights[i + 1];
            return c;
        }

        private static bool Model(NativeArray<Bezier4x3> curves, NativeArray<float> weights, float total,
            float3 nodeStart, float3 nodeEnd, float startHeight, float width, float3 leftAnchor, float3 rightAnchor,
            float3 delta, NativeArray<Bezier4x3> output) {
            var c = Adjust(curves[0], 0, weights, total, delta);
            var next = Adjust(curves[1], 1, weights, total, delta);
            nodeEnd.y += delta.x * weights[1];
            var w0 = 1 / math.distance(c.c.xz, c.d.xz); var w1 = 1 / math.distance(next.a.xz, next.b.xz);
            var endHeight = .5f * (nodeEnd.y + (w0 * c.c.y + w1 * next.b.y) / (w0 + w1));
            MathUtils.Distance(c.xz, nodeStart.xz, out var u); MathUtils.Distance(c.xz, nodeEnd.xz, out var v);
            u = u < .001f ? 0 : u; v = v > .999f ? 1 : v;
            if (v - u < .02f) { return false; }
            if (u != 0 || v != 1) { c = MathUtils.Cut(c, new float2(u, v)); c.a.y = nodeStart.y; c.d.y = nodeEnd.y; }
            var t = NetUtils.FindMiddleTangentPos(c.xz, new float2(0, 1));
            MathUtils.Divide(c, out var a, out var b, t);
            a.a.y = startHeight; b.d.y = endHeight;
            var la = math.distance(a.c.xz, a.d.xz); var lb = math.distance(b.b.xz, b.a.xz);
            var h = math.lerp(a.c.y, b.b.y, la / math.max(.1f, la + lb)) - a.d.y;
            a.c.y -= h * .4f; a.d.y += h * .6f; b.a.y += h * .6f; b.b.y -= h * .4f;
            for (var side = 0; side < 2; side++) {
                var offset = (side == 0 ? .5f : -.5f) * width;
                var end = NetUtils.OffsetCurveLeftSmooth(b, new float2(offset));
                var anchor = side == 0 ? leftAnchor : rightAnchor;
                var distance = MathUtils.Distance(end.xz, anchor.xz, out var s);
                if (distance > .05f || s <= .001f || s >= .99f) { return false; }
                // Restricted case: native junction cut starts beyond the first half,
                // and the other endpoint is a simple continuation with no cutback.
                var so = 1 + s;
                var remaining = MathUtils.Length(end.xz, new Bounds1(s, 1));
                var cut = 1 - (2 - so) * .5f / (remaining / math.max(.01f, width) + 1);
                var first = MathUtils.Cut(end, new float2(s, so - cut));
                var last = MathUtils.Cut(end, new float2(so - cut, 1));
                first.b.y += anchor.y - first.a.y; first.a.y = anchor.y;
                output[side * 2] = first; output[side * 2 + 1] = last;
                if (!math.all(math.isfinite(new float4(first.a.y, first.b.y, first.c.y, first.d.y)))
                    || !math.all(math.isfinite(new float4(last.a.y, last.b.y, last.c.y, last.d.y)))) { return false; }
            }
            return true;
        }
    }
}
