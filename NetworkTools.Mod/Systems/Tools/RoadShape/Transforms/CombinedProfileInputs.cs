#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using NetworkTools.Geometry;
    using Unity.Mathematics;
    using Point = NetworkTools.Geometry.PlanarFairing.Point;

    /// <summary>Builds vertical fitting inputs from candidate XZ and immutable authored Y.</summary>
    public static class CombinedProfileInputs {
        private static Point XZ(float3 p) => new Point(p.x, p.z);

        public static bool TrySegment(in EdgeState original, in EdgeState candidate,
            in NodeState start, in NodeState end, out VerticalLinearProfile.Segment segment) {
            segment = default;
            if (original.EdgeEntity != candidate.EdgeEntity || original.IsForward != candidate.IsForward) return false;
            var c = candidate.Bezier; var o = original.Bezier;
            var a = candidate.IsForward ? c.a : c.d;
            var b = candidate.IsForward ? c.b : c.c;
            var cp = candidate.IsForward ? c.c : c.b;
            var d = candidate.IsForward ? c.d : c.a;
            var oa = original.IsForward ? o.a : o.d;
            var od = original.IsForward ? o.d : o.a;
            if (!math.all(math.isfinite(a)) || !math.all(math.isfinite(b))
                || !math.all(math.isfinite(cp)) || !math.all(math.isfinite(d))
                || !math.all(math.isfinite(oa)) || !math.all(math.isfinite(od))
                || !math.all(math.isfinite(start.OriginalPosition)) || !math.all(math.isfinite(end.OriginalPosition))
                || !VerticalLinearProfile.TryHorizontalLength(new PlanarCubic(XZ(a), XZ(b), XZ(cp), XZ(d)), out var length)) return false;
            segment = new VerticalLinearProfile.Segment {
                Length = length, StartHandle = math.distance(a.xz, b.xz), EndHandle = math.distance(cp.xz, d.xz),
                StartOffset = (double)oa.y - start.OriginalPosition.y,
                EndOffset = (double)od.y - end.OriginalPosition.y
            };
            return true;
        }

        /// <summary>Original grade oriented along the selected path. A zero horizontal tangent has no defined grade.</summary>
        public static bool TryGrade(in EdgeState original, bool atPathStart, out double grade) {
            var c = original.Bezier;
            var tangent = original.IsForward
                ? (atPathStart ? c.b - c.a : c.d - c.c)
                : (atPathStart ? c.c - c.d : c.a - c.b);
            grade = 0;
            var length = math.length(tangent.xz);
            if (!math.all(math.isfinite(tangent)) || !math.isfinite(length) || length <= 1e-6f) return false;
            grade = (double)tangent.y / length;
            return true;
        }
    }
}
#endif
