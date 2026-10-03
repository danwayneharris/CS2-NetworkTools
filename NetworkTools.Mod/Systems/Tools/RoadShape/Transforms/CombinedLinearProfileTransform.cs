#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>Vertical stage of a combined candidate. Never publishes a partial fit.</summary>
    public static unsafe class CombinedLinearProfileTransform {
        public enum Failure { None, InvalidInput, UndefinedGrade, SplitGradeConflict, FitFailed, InvalidElevationLimit, ElevationLimitExceeded, BoundedCapacityExceeded, BoundedIllConditioned, BoundedIterationLimit, BoundedResidualFailure }

        /// <summary>Final guard after any later native-surface correction stage.</summary>
        public static bool WithinElevationLimits(in NativeArray<NodeState> nodes, in ShapeJobConfig config) {
            if (!config.ConstrainJunctionElevation) return true;
            if (!math.isfinite(config.JunctionElevationLimit) || config.JunctionElevationLimit < 0
                || config.JunctionElevationLimit > 20) return false;
            for (var i = 1; i < nodes.Length - 1; i++) {
                var node = nodes[i];
                if (node.SmoothJunction && (!math.isfinite(node.Position.y)
                    || Math.Abs(node.Position.y - node.OriginalPosition.y) > config.JunctionElevationLimit + 1e-4)) return false;
            }
            return true;
        }
        // This is numerical grade equality, not a positional tolerance or a new transition policy.
        private const double GradeEquality = 1e-6;

        public static bool Execute(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes,
            in NativeList<EdgeState> originals, in ShapeTransformContext context, in ShapeJobConfig config,
            out Failure failure, out int failedIndex) {
            var count = edges.Length;
            failure = Failure.InvalidInput; failedIndex = -1;
            if (count < 1 || nodes.Length != count + 1 || originals.Length != count) return false;
            using (var segments = new NativeArray<VerticalLinearProfile.Segment>(count, Allocator.Temp))
            using (var anchors = new NativeArray<SectionedVerticalProfile.Anchor>(count + 1, Allocator.Temp))
            using (var heights = new NativeArray<double>(count + 1, Allocator.Temp))
            using (var curves = new NativeArray<VerticalLinearProfile.Heights>(count, Allocator.Temp)) {
                return Execute((EdgeState*)edges.GetUnsafePtr(), (NodeState*)nodes.GetUnsafePtr(),
                    (EdgeState*)originals.GetUnsafeReadOnlyPtr(), count, context, config,
                    (VerticalLinearProfile.Segment*)segments.GetUnsafePtr(),
                    (SectionedVerticalProfile.Anchor*)anchors.GetUnsafePtr(),
                    (double*)heights.GetUnsafePtr(), (VerticalLinearProfile.Heights*)curves.GetUnsafePtr(),
                    out failure, out failedIndex);
            }
        }

        /// <summary>Caller-owned, disjoint buffers allow tests to execute production fitting without Unity allocation.</summary>
        public static bool Execute(EdgeState* edges, NodeState* nodes, EdgeState* originals, int count,
            in ShapeTransformContext context, in ShapeJobConfig config,
            VerticalLinearProfile.Segment* segments, SectionedVerticalProfile.Anchor* anchors,
            double* heights, VerticalLinearProfile.Heights* curves, out Failure failure, out int failedIndex) {
            failure = Failure.InvalidInput; failedIndex = -1;
            if (count < 1 || edges == null || nodes == null || originals == null || segments == null
                || anchors == null || heights == null || curves == null) return false;
            if (config.ConstrainJunctionElevation && (!math.isfinite(config.JunctionElevationLimit)
                || config.JunctionElevationLimit < 0 || config.JunctionElevationLimit > 20)) {
                failure = Failure.InvalidElevationLimit; return false;
            }
            for (var i = 0; i < count; i++) {
                if (!CombinedProfileInputs.TrySegment(originals[i], edges[i], nodes[i], nodes[i+1], out segments[i])) {
                    failedIndex = i; return false;
                }
            }
            for (var i = 0; i <= count; i++) {
                var n = nodes[i];
                var fixedNode = i == 0 || i == count || (n.SmoothPinned && !n.SmoothJunction) || n.SmoothSplit;
                anchors[i] = new SectionedVerticalProfile.Anchor { Fixed = fixedNode, Height = n.OriginalPosition.y };
                if (!fixedNode) continue;
                // Horizontal stage must already have honored fixed XYZ anchors.
                if (!math.all(math.isfinite(n.Position)) || !math.all(n.Position == n.OriginalPosition)) {
                    failedIndex = i; return false;
                }
                var a = anchors[i];
                if (i > 0 && (i < count || n.SmoothJunction)) {
                    if (!CombinedProfileInputs.TryGrade(originals[i-1], false, out a.IncomingGrade)) {
                        failure = Failure.UndefinedGrade; failedIndex = i; return false;
                    }
                    a.MatchIncoming = true;
                }
                if (i < count && (i > 0 || n.SmoothJunction)) {
                    if (!CombinedProfileInputs.TryGrade(originals[i], true, out a.OutgoingGrade)) {
                        failure = Failure.UndefinedGrade; failedIndex = i; return false;
                    }
                    a.MatchOutgoing = true;
                }
                if (i > 0 && i < count && n.SmoothSplit && !n.SmoothPinned) {
                    if (Math.Abs(a.IncomingGrade - a.OutgoingGrade) > GradeEquality) {
                        failure = Failure.SplitGradeConflict; failedIndex = i; return false;
                    }
                    // Equivalent original grades: preserve the incoming one, no policy averaging.
                    a.OutgoingGrade = a.IncomingGrade;
                }
                if (i == 0 && !n.SmoothJunction && config.SmoothStart && context.StartSmoothEligible) {
                    a.MatchOutgoing = true; a.OutgoingGrade = context.StartAnchorSlope;
                }
                if (i == count && !n.SmoothJunction && config.SmoothEnd && context.EndSmoothEligible) {
                    a.MatchIncoming = true; a.IncomingGrade = context.EndAnchorSlope;
                }
                anchors[i] = a;
            }
            var fitted = false;
            var hasBoundedJunction = false;
            for (var i = 1; i < count; i++) hasBoundedJunction |= nodes[i].SmoothJunction && !anchors[i].Fixed;
            if (config.ConstrainJunctionElevation && hasBoundedJunction) {
                // Bounds are captured from immutable authored heights. A zero interval fixes
                // height only; it does not introduce an authored tangent/grade constraint.
                var lower = new double[count + 1];
                var upper = new double[count + 1];
                for (var i = 0; i <= count; i++) {
                    var bounded = i > 0 && i < count && nodes[i].SmoothJunction && !anchors[i].Fixed;
                    lower[i] = bounded ? nodes[i].OriginalPosition.y - config.JunctionElevationLimit : double.NegativeInfinity;
                    upper[i] = bounded ? nodes[i].OriginalPosition.y + config.JunctionElevationLimit : double.PositiveInfinity;
                }
                fixed (double* low = lower) fixed (double* high = upper) {
                    fitted = BoundedVerticalProfile.Fit(segments, count, anchors, low, high,
                        heights, curves, out var boundedFailure, out _);
                    if (!fitted) {
                        failure = boundedFailure switch {
                            BoundedVerticalProfile.Failure.CapacityExceeded => Failure.BoundedCapacityExceeded,
                            BoundedVerticalProfile.Failure.IllConditioned => Failure.BoundedIllConditioned,
                            BoundedVerticalProfile.Failure.IterationLimit => Failure.BoundedIterationLimit,
                            BoundedVerticalProfile.Failure.ResidualFailure => Failure.BoundedResidualFailure,
                            _ => Failure.FitFailed,
                        };
                        return false;
                    }
                }
            } else {
                // Exact existing path for the default Unlimited behavior.
                fitted = SectionedVerticalProfile.Fit(segments, count, anchors, heights, curves, out failedIndex);
            }
            if (!fitted) { failure = Failure.FitFailed; return false; }
            for (var i = 0; i <= count; i++) {
                if (!math.isfinite((float)heights[i])) { failedIndex = i; return false; }
                if (config.ConstrainJunctionElevation && i > 0 && i < count && nodes[i].SmoothJunction
                    && Math.Abs((float)heights[i] - nodes[i].OriginalPosition.y) > config.JunctionElevationLimit + 1e-4) {
                    failure = Failure.ElevationLimitExceeded; failedIndex = i; return false;
                }
            }
            for (var i = 0; i < count; i++) {
                var h = curves[i];
                if (!math.all(math.isfinite(new float4((float)h.A, (float)h.B, (float)h.C, (float)h.D)))) {
                    failedIndex = i; return false;
                }
            }
            for (var i = 0; i < count; i++) {
                var h = curves[i];
                edges[i].Bezier = SlopeUtils.ApplyHeightsToBezier(edges[i].Bezier,
                    (float)h.A, (float)h.B, (float)h.C, (float)h.D, edges[i].IsForward);
            }
            for (var i = 1; i < count; i++) {
                if (!anchors[i].Fixed) nodes[i].Position.y = (float)heights[i];
            }
            failure = Failure.None; failedIndex = -1; return true;
        }
    }
}
#endif
