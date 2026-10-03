namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Mathematics;
    using Game.Net;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>Preview course for an existing edge, whose Apply preserves structural metadata.</summary>
    public static class RoadShapePreviewCourse {
        public static NetCourse Create(Bezier4x3 curve, float length,
            Entity startReference, Entity endReference, float3 startPosition, float3 endPosition,
            float2 edgeElevation, float2 startElevation, float2 endElevation) {
            // An elevated edge can end at a ground node (and a tunnel at a portal).
            // Preserve each component independently, including asymmetric side values.
            // Edge-wide ForceElevatedNode/clamping would leak into every incident edge
            // through the temporary shared node, unlike permanent Apply.
            var startFlags = CoursePosFlags.FreeHeight | CoursePosFlags.IsGrid | CoursePosFlags.IsRight;
            var endFlags = startFlags;
            if (startReference != Entity.Null && endReference == Entity.Null) {
                startFlags |= CoursePosFlags.IsFirst;
                endFlags |= CoursePosFlags.IsLast;
            } else if (endReference != Entity.Null && startReference == Entity.Null) {
                endFlags |= CoursePosFlags.IsFirst;
                startFlags |= CoursePosFlags.IsLast;
            }
            return new NetCourse {
                m_Curve = curve, m_Length = length, m_FixedIndex = -1,
                m_Elevation = edgeElevation,
                m_StartPosition = new CoursePos {
                    m_Entity = startReference, m_Position = startPosition,
                    m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(curve)),
                    m_CourseDelta = 0, m_Elevation = startElevation,
                    m_Flags = startFlags, m_ParentMesh = -1, m_SplitPosition = 0
                },
                m_EndPosition = new CoursePos {
                    m_Entity = endReference, m_Position = endPosition,
                    m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(curve)),
                    m_CourseDelta = 1, m_Elevation = endElevation,
                    m_Flags = endFlags, m_ParentMesh = -1, m_SplitPosition = 0
                }
            };
        }
    }
}
