namespace NetworkTools.Systems.Tools.Connect {
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        public string LaneDirectionStatus {
            get {
#if IS_DEBUG
                if (!LaneAwareDirection.Value) return "disabled";
                if (m_UpdateNeeded || !m_ControlJob.IsCompleted) return "preview_pending";
                return m_ControlRejection ?? "preview_pending";
#else
                return "unavailable";
#endif
            }
        }
        private float3 EffectiveLaneAxis(bool start) {
#if IS_DEBUG
            if (LaneAwareDirection.Value && TryResolveLaneDirectionEndpoints(out var first, out var last, out _)) {
                var axis = start ? first.HandleDirection : last.HandleDirection;
                var legacy = start ? StartDirection.Value : EndDirection.Value;
                var length = math.length(legacy.xz);
                axis.y = length > 1e-5f ? legacy.y / length : 0;
                return math.normalizesafe(axis);
            }
#endif
            return start ? StartDirection.Value : EndDirection.Value;
        }
        public float3 EffectiveStartDirection => EffectiveLaneAxis(true);
        public float3 EffectiveEndDirection => EffectiveLaneAxis(false);

#if IS_DEBUG
        private bool TryPrepareLaneDirections(ref ConnectJobConfig config, out string reason) {
            if (!TryResolveLaneDirectionEndpoints(out var start, out var end, out reason)) return false;
            bool Move(ref float3 control, float3 endpoint, float3 axis) {
                var length = math.length((control - endpoint).xz);
                if (!math.isfinite(length) || length < .01f) return false;
                control.x = endpoint.x + length * axis.x;
                control.z = endpoint.z + length * axis.z;
                return math.all(math.isfinite(control));
            }
            var valid = config.ComplexProfile
                ? Move(ref config.ComplexStartControlPointPosition, config.ComplexStartPointPosition, start.HandleDirection)
                    && Move(ref config.ComplexEndControlPointPosition, config.ComplexEndPointPosition, end.HandleDirection)
                : Move(ref config.CurveStartControlPointPosition, config.CurveStartPointPosition, start.HandleDirection)
                    && Move(ref config.CurveEndControlPointPosition, config.CurveEndPointPosition, end.HandleDirection);
            reason = valid ? null : "lane_handle_invalid";
            return valid;
        }
#endif
    }
}