namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Mathematics;
    using Game.Net;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>Restricted permanent SmoothElevation junction response, fixed horizontal cut locations.</summary>
    public static class SurfaceJunctionHeightModel {
        public struct Incident {
            public Bezier4x3 Curve;
            public float3 Start, End, Left, Right;
            public float EndHeight, Width, MaxSlope;
            public Layer Layers;
        }
        public struct Pair {
            public float3 Left, Right;
            public float2 Changes;
            public float MaxSlope;
            public Layer Layers;
        }
        public static bool Prepare(Bezier4x3 c, float3 start, float3 end, float startHeight, float endHeight,
            out Bezier4x3 a, out Bezier4x3 b) {
            a = b = default;
            MathUtils.Distance(c.xz, start.xz, out var u); MathUtils.Distance(c.xz, end.xz, out var v);
            u = u < .001f ? 0 : u; v = v > .999f ? 1 : v;
            if (v - u < .02f) { return false; }
            if (u != 0 || v != 1) { c = MathUtils.Cut(c, new float2(u, v)); c.a.y = start.y; c.d.y = end.y; }
            var t = NetUtils.FindMiddleTangentPos(c.xz, new float2(0, 1));
            MathUtils.Divide(c, out a, out b, t);
            a.a.y = startHeight; b.d.y = endHeight;
            var la = math.distance(a.c.xz, a.d.xz); var lb = math.distance(b.b.xz, b.a.xz);
            var h = math.lerp(a.c.y, b.b.y, la / math.max(.1f, la + lb)) - a.d.y;
            a.c.y -= h * .4f; a.d.y += h * .6f; b.a.y += h * .6f; b.b.y -= h * .4f;
            return true;
        }
        private static float3 Boundary(Bezier4x3 a, Bezier4x3 b, float offset, float3 target) {
            a = NetUtils.OffsetCurveLeftSmooth(a, new float2(offset));
            b = NetUtils.OffsetCurveLeftSmooth(b, new float2(offset));
            var da = MathUtils.Distance(a.xz, target.xz, out var ta);
            var db = MathUtils.Distance(b.xz, target.xz, out var tb);
            return da < db ? MathUtils.Position(a, ta) : MathUtils.Position(b, tb);
        }
        public static bool Predict(NativeArray<Incident> inputs, Bezier4x3 selected, float3 selectedEnd,
            float selectedEndHeight, out float leftHeight, out float rightHeight) {
            leftHeight = rightHeight = 0;
            var sum = 0f; var weights = 0f;
            for (var i = 0; i < inputs.Length; i++) {
                var c = i == 0 ? selected : inputs[i].Curve;
                var length = math.distance(c.a.xz, c.b.xz);
                if (length < .1f) { return false; }
                sum += c.b.y / length; weights += 1 / length;
            }
            var height = .5f * (inputs[0].Start.y + sum / weights);
            var pairs = new NativeArray<Pair>(inputs.Length, Allocator.Temp);
            var valid = true;
            for (var i = 0; i < inputs.Length && valid; i++) {
                var x = inputs[i];
                valid = Prepare(i == 0 ? selected : x.Curve, x.Start, i == 0 ? selectedEnd : x.End,
                    height, i == 0 ? selectedEndHeight : x.EndHeight, out var a, out var b);
                var left = Boundary(a, b, x.Width * .5f, x.Left);
                var right = Boundary(a, b, -x.Width * .5f, x.Right);
                valid = valid && math.distance(left.xz, x.Left.xz) <= .05f && math.distance(right.xz, x.Right.xz) <= .05f;
                // Native pair orientation is opposite the physical left/right of
                // the outward-oriented authored curve (GeometrySystem EdgeData).
                pairs[i] = new Pair { Left = right, Right = left, MaxSlope = x.MaxSlope, Layers = x.Layers };
            }
            var converged = false;
            for (var iteration = 0; iteration < 100 && valid; iteration++) {
                var active = false;
                for (var i = 1; i < pairs.Length; i++) {
                    for (var j = 0; j < i; j++) {
                        var a = pairs[i]; var b = pairs[j];
                        if ((a.Layers & b.Layers) == 0) { continue; }
                        var separation = new float2(math.distance(a.Left.xz, b.Right.xz), math.distance(a.Right.xz, b.Left.xz));
                        var difference = new float2(b.Right.y - a.Left.y, b.Left.y - a.Right.y);
                        var limit = a.MaxSlope + b.MaxSlope;
                        if (!math.any(difference * difference > separation * separation * limit * limit * 1.0001f)) { continue; }
                        var excess = math.max(0, math.abs(difference) - separation * limit);
                        var sign = difference >= 0;
                        var change = sign.x != sign.y ? math.csum(math.select(-excess, excess, sign)) * .5f
                            : math.cmax(excess) * (sign.x ? 1 : -1);
                        var room = change >= 0
                            ? new float2(math.max(0, height - math.max(a.Left.y, a.Right.y)), math.min(0, height - math.min(b.Left.y, b.Right.y)))
                            : new float2(math.min(0, height - math.min(a.Left.y, a.Right.y)), math.max(0, height - math.max(b.Left.y, b.Right.y)));
                        room *= math.min(1, math.abs(change) / math.max(.001f, math.csum(math.abs(room))));
                        a.Changes = new float2(math.min(a.Changes.x, room.x), math.max(a.Changes.y, room.x));
                        b.Changes = new float2(math.min(b.Changes.x, room.y), math.max(b.Changes.y, room.y));
                        pairs[i] = a; pairs[j] = b; active = true;
                    }
                }
                if (!active) { converged = true; break; }
                var changed = false;
                for (var i = 0; i < pairs.Length; i++) {
                    var a = pairs[i]; var change = math.csum(a.Changes);
                    var before = new float2(a.Left.y, a.Right.y);
                    a.Left.y += change; a.Right.y += change; a.Changes = 0; pairs[i] = a;
                    changed |= math.any(before != new float2(a.Left.y, a.Right.y));
                }
                // A requested sub-ULP correction can leave float heights unchanged
                // while the pair inequality still fails. This is a fixed state,
                // not divergence; native code retains it after its iteration cap.
                if (!changed) { converged = true; break; }
            }
            if (valid && converged) { leftHeight = pairs[0].Right.y; rightHeight = pairs[0].Left.y; }
            pairs.Dispose();
            return valid && converged && math.isfinite(leftHeight) && math.isfinite(rightHeight);
        }
    }
}
