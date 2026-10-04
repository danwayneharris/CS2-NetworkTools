#if IS_DEBUG
namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using Colossal.Mathematics;
    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Simulation;
    using Game.Tools;
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using C = NetworkTools.Geometry.ConnectProfileCoverage.Cubic;
    using P = NetworkTools.Geometry.PlanarFairing.Point;

    internal struct NT_ConnectProfileDefinition : IComponentData {
        public Bezier4x3 Authored;
        public int Index;
    }

    // Snapshot explicit ownership before vanilla splitting; custom tags are NOT
    // copied to its newly allocated definitions. Refuse mixed producer batches.
    public partial class NT_ConnectProfileCaptureSystem : GameSystemBase {
        internal Bezier4x3[] Pending;
        internal Entity Prefab;
        private EntityQuery m_Query;
        protected override void OnCreate() {
            base.OnCreate();
            m_Query = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(),
                ComponentType.ReadOnly<NetCourse>(), ComponentType.ReadOnly<Updated>());
        }
        protected override void OnUpdate() {
            Pending = null;
            using var entities = m_Query.ToEntityArray(Allocator.Temp);
            if (entities.Length < 1 || entities.Length > 2) return;
            var curves = new Bezier4x3[entities.Length];
            var seen = new bool[entities.Length];
            var prefab = Entity.Null;
            foreach (var entity in entities) {
                if (!EntityManager.HasComponent<NT_ConnectProfileDefinition>(entity)) return;
                var marker = EntityManager.GetComponentData<NT_ConnectProfileDefinition>(entity);
                var definition = EntityManager.GetComponentData<CreationDefinition>(entity);
                if (marker.Index < 0 || marker.Index >= curves.Length || seen[marker.Index]
                    || definition.m_Original != Entity.Null || definition.m_Owner != Entity.Null
                    || definition.m_SubPrefab != Entity.Null || EntityManager.HasComponent<OwnerDefinition>(entity)) return;
                if (prefab != Entity.Null && prefab != definition.m_Prefab) return;
                prefab = definition.m_Prefab;
                curves[marker.Index] = marker.Authored; seen[marker.Index] = true;
            }
            Prefab = prefab; Pending = curves;
        }
    }

    // Runs after ToolReadyBarrier playback, before GenerateNodes/GenerateEdges.
    // Bounded unowned-course adapter, not a general replacement for CourseSplit.
    public partial class NT_ConnectProfileRestoreSystem : GameSystemBase {
        private EntityQuery m_Query;
        public string Status { get; private set; } = "not_run";
        protected override void OnCreate() {
            base.OnCreate();
            m_Query = GetEntityQuery(ComponentType.ReadWrite<NetCourse>(),
                ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<Updated>());
        }
        internal static C Geometry(Bezier4x3 c) => new C {
            Horizontal = new PlanarCubic(new P(c.a.x,c.a.z),new P(c.b.x,c.b.z),new P(c.c.x,c.c.z),new P(c.d.x,c.d.z)),
            Vertical = new VerticalLinearProfile.Heights { A=c.a.y,B=c.b.y,C=c.c.y,D=c.d.y }
        };
        private bool FixedHeightMatches(CoursePos position, float height) => position.m_Entity == Entity.Null
            || (EntityManager.HasComponent<Node>(position.m_Entity)
                && math.abs(EntityManager.GetComponentData<Node>(position.m_Entity).m_Position.y-height) <= .05f);

        // Native CalculateElevation samples both road edges at A, midpoint, D.
        // Recompute from restored Y before node generation, including placement
        // clamps. Optional interior terrain-transition flags belong to the old
        // sampled profile; existing endpoint transition policy is retained.
        private static bool Reclassify(ref NetCourse course, NetGeometryData geometry, PlaceableNetData placeable, ref TerrainHeightData terrain) {
            var c=course.m_Curve;
            const CoursePosFlags transition=CoursePosFlags.LeftTransition|CoursePosFlags.RightTransition;
            if (course.m_StartPosition.m_Entity==Entity.Null) course.m_StartPosition.m_Flags &= ~transition;
            if (course.m_EndPosition.m_Entity==Entity.Null) course.m_EndPosition.m_Flags &= ~transition;
            for (var i = 0; i < 3; i++) {
                var t = i * .5f;
                var point = MathUtils.Position(c,t);
                var right = math.normalizesafe(MathUtils.Right(MathUtils.Tangent(c,t).xz)) * geometry.m_DefaultWidth * .5f;
                var offset = new float3(right.x,0,right.y);
                var leftHeight = point.y-TerrainUtils.SampleHeight(ref terrain,point-offset);
                var rightHeight = point.y-TerrainUtils.SampleHeight(ref terrain,point+offset);
                if (!math.isfinite(leftHeight) || !math.isfinite(rightHeight)) return false;
                var flags=i==0?course.m_StartPosition.m_Flags:i==2?course.m_EndPosition.m_Flags:0;
                var elevation=new float2(
                    ConnectCourseElevation.Classify(leftHeight,geometry.m_ElevationLimit,placeable.m_ElevationRange.min,placeable.m_ElevationRange.max,(flags&CoursePosFlags.LeftTransition)!=0),
                    ConnectCourseElevation.Classify(rightHeight,geometry.m_ElevationLimit,placeable.m_ElevationRange.min,placeable.m_ElevationRange.max,(flags&CoursePosFlags.RightTransition)!=0));
                if(i==0) course.m_StartPosition.m_Elevation=elevation;
                else if(i==1) course.m_Elevation=elevation;
                else course.m_EndPosition.m_Elevation=elevation;
            }
            return true;
        }
        protected override unsafe void OnUpdate() {
            var capture = World.GetOrCreateSystemManaged<NT_ConnectProfileCaptureSystem>();
            var pending = capture.Pending;
            capture.Pending = null; // One native batch, never reuse after load/reselection.
            if (pending == null) return;
            Status = "unsupported_course_batch";
            var prefab = capture.Prefab;
            if (!EntityManager.HasComponent<NetGeometryData>(prefab)
                || EntityManager.HasBuffer<AuxiliaryNet>(prefab) || EntityManager.HasBuffer<FixedNetElement>(prefab)
                || EntityManager.HasComponent<ServiceUpgradeData>(prefab)) return;
            var geometry = EntityManager.GetComponentData<NetGeometryData>(prefab);
            if ((geometry.m_Flags & (GeometryFlags.OnWater | GeometryFlags.SubOwner | GeometryFlags.StraightEdges | GeometryFlags.RequireElevated)) != 0
                || !math.isfinite(geometry.m_ElevationLimit) || geometry.m_ElevationLimit <= 0) return;
            var placeable=EntityManager.HasComponent<PlaceableNetData>(prefab)?EntityManager.GetComponentData<PlaceableNetData>(prefab):default;
            if ((placeable.m_PlacementFlags & PlacementFlags.ShoreLine) != 0) return;
            using var entities = m_Query.ToEntityArray(Allocator.Temp);
            if (entities.Length < 1 || entities.Length > ConnectProfileCoverage.MaximumNativeCurves) return;
            var courses = new NetCourse[entities.Length];
            var authored = stackalloc C[2];
            var native = stackalloc C[ConnectProfileCoverage.MaximumNativeCurves];
            var restored = stackalloc C[ConnectProfileCoverage.MaximumNativeCurves];
            for (var i=0;i<pending.Length;i++) authored[i]=Geometry(pending[i]);
            for (var i=0;i<entities.Length;i++) {
                var entity=entities[i];
                var definition=EntityManager.GetComponentData<CreationDefinition>(entity);
                if (definition.m_Prefab != prefab || definition.m_Original != Entity.Null || definition.m_Owner != Entity.Null
                    || definition.m_SubPrefab != Entity.Null || EntityManager.HasComponent<OwnerDefinition>(entity)
                    || EntityManager.HasComponent<Upgraded>(entity)) return;
                var course=EntityManager.GetComponentData<NetCourse>(entity);
                if (course.m_FixedIndex != -1
                    || ((course.m_StartPosition.m_Flags | course.m_EndPosition.m_Flags) &
                        (CoursePosFlags.ForceElevatedEdge | CoursePosFlags.ForceElevatedNode)) != 0) return;
                courses[i]=course; native[i]=Geometry(course.m_Curve);
            }
            if (!ConnectProfileCoverage.RestoreHeights(authored,pending.Length,native,entities.Length,restored,out var failure,out var index)) {
                Status="mapping_"+failure+"_"+index; return;
            }
            var terrain=World.GetOrCreateSystemManaged<TerrainSystem>().GetHeightData();
            for (var i=0;i<courses.Length;i++) {
                var course=courses[i]; var h=restored[i].Vertical;
                course.m_Curve.a.y=(float)h.A; course.m_Curve.b.y=(float)h.B;
                course.m_Curve.c.y=(float)h.C; course.m_Curve.d.y=(float)h.D;
                if (!FixedHeightMatches(course.m_StartPosition,course.m_Curve.a.y)
                    || !FixedHeightMatches(course.m_EndPosition,course.m_Curve.d.y)) { Status="existing_connection_height"; return; }
                if (!Reclassify(ref course,geometry,placeable,ref terrain)) { Status="invalid_terrain_sample"; return; }
                course.m_StartPosition.m_Position.y=course.m_Curve.a.y;
                course.m_EndPosition.m_Position.y=course.m_Curve.d.y;
                course.m_Length=MathUtils.Length(course.m_Curve);
                courses[i]=course;
            }
            // No ECS writes until the entire batch and classification are valid.
            for (var i=0;i<courses.Length;i++) EntityManager.SetComponentData(entities[i],courses[i]);
            Status="restored_"+courses.Length;
        }
    }
}
#endif
