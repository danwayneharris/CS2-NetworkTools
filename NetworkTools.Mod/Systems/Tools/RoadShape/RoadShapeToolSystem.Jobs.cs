namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Mathematics;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using NetworkTools.Components;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;

    public partial class NT_RoadShapeToolSystem {
#if USE_BURST
        [BurstCompile]
#endif
        internal struct ShapeTransformJob : IJob {
            [ReadOnly] public required NativeList<EdgeState>             EdgeStates;
            [ReadOnly] public required NativeList<NodeState>             NodeStates;
            [ReadOnly] public required ShapeTransformContext             Context;
            [ReadOnly] public required ShapeJobConfig                     Config;
            [ReadOnly] public required NativeList<Entity>                CurrentPathNodes;
            [ReadOnly] public required ComponentLookup<Node>             NodeLookup;
            [ReadOnly] public required ComponentLookup<PrefabRef>        PrefabRefLookup;
            [ReadOnly] public required ComponentLookup<PseudoRandomSeed> PseudoRandomSeedLookup;
            [ReadOnly] public required BufferLookup<ConnectedEdge>       ConnectedEdgeLookup;
            [ReadOnly] public required ComponentLookup<Edge>             EdgeLookup;
            [ReadOnly] public required ComponentLookup<Curve>            CurveLookup;
            [ReadOnly] public required ComponentLookup<Upgraded>         UpgradedLookup;
            [ReadOnly] public required ComponentLookup<Aggregated>       AggregatedLookup;
            [ReadOnly] public required ComponentLookup<Elevation>        ElevationLookup;
            [ReadOnly] public ComponentLookup<EdgeGeometry> SurfaceGeometryLookup;
            [ReadOnly] public ComponentLookup<NodeGeometry> SurfaceNodeGeometryLookup;
            [ReadOnly] public ComponentLookup<NetGeometryData> SurfacePrefabGeometryLookup;
            [ReadOnly] public ComponentLookup<Composition> SurfaceCompositionLookup;
            [ReadOnly] public ComponentLookup<NetCompositionData> SurfaceCompositionDataLookup;
            public required            ToolOutputMode                    OutputMode;
            public required            EntityCommandBuffer               ECB;
            public NativeReference<int> SmoothResult;
#if IS_DEBUG
            public int SmoothTraceId;
            [ReadOnly] public NativeParallelHashMap<Entity, EdgeGeometry> SurfaceReferences;
            public NativeReference<int> SurfaceCorrected;
            [ReadOnly] public NativeList<Entity> SmoothSelectedNodes;
#endif


#if IS_DEBUG
            [BurstDiscard]
            private static void TraceSurfaceFit(bool applied) {
                UnityEngine.Debug.Log("[NetworkTools SurfaceProfile] Correction applied=" + applied);
            }
#endif

#if IS_DEBUG
            private void ApplySurfaceCorrection(ref NativeArray<EdgeState> edges, ref NativeArray<NodeState> nodes) {
                if (Config.CombinedSlope && SurfaceReferences.Count() == 0) {
                    // Native cuts must first be observed for this horizontal candidate.
                    SurfaceCorrected.Value = 2;
                    return;
                }
                var surfaceFit = SlopeSurfaceProfileTransform.TryExecute(ref edges, ref nodes, in Config, in ConnectedEdgeLookup,
                    in SurfaceGeometryLookup, in SurfaceNodeGeometryLookup, in PrefabRefLookup,
                    in SurfacePrefabGeometryLookup, in SurfaceCompositionLookup, in SurfaceCompositionDataLookup,
                    in CurveLookup, in EdgeLookup, in NodeLookup, in SurfaceReferences);
                SurfaceCorrected.Value = surfaceFit ? 1 : 0;
                TraceSurfaceFit(surfaceFit);
            }
#endif

            /// <summary>
            ///     Minimum XZ delta squared (in meters²) to consider for intersection adjustments.
            /// </summary>


            public void Execute() {
                if (EdgeStates.Length == 0) {
                    return;
                }

#if !IS_DEBUG
                // Guard the candidate path too: unsupported Release selections must
                // not display an apparently valid unchecked smoothing proposal.
                if (Config.Template == ShapeTransformTemplate.CurveSmooth) {
                    foreach (var state in NodeStates) {
                        if (!ConnectedEdgeLookup.TryGetBuffer(state.Entity, out var incident)
                            || incident.Length > 2) {
                            SmoothResult.Value = -1;
                            return;
                        }
                    }
                }
#endif

                // 1. Copy cached data to mutable arrays for transform pipeline
                var edges = new NativeArray<EdgeState>(EdgeStates.Length, Allocator.Temp);
                for (var i = 0; i < EdgeStates.Length; i++) {
                    edges[i] = EdgeStates[i];
                }

                var nodes = new NativeArray<NodeState>(NodeStates.Length, Allocator.Temp);
                for (var i = 0; i < NodeStates.Length; i++) {
                    nodes[i] = NodeStates[i];
                }

                // 2. Execute transformation (context = path geometry, config = user settings)
                switch (Config.Template) {
                    case ShapeTransformTemplate.SlopeLinear:
                        var linearValid = SlopeLinearProfileTransform.Execute(ref edges, ref nodes, in Context, in Config);
#if IS_DEBUG
                        if (linearValid) {
                            ApplySurfaceCorrection(ref edges, ref nodes);
                        }
#endif
                        SmoothResult.Value = linearValid ? 1 : -1;
                        if (!linearValid) {
#if IS_DEBUG
                            CapturePreviewProbe(SmoothTraceId, OutputMode, false, edges);
#endif
                            edges.Dispose(); nodes.Dispose();
                            return;
                        }
                        break;
                    case ShapeTransformTemplate.SlopeEaseInOut:
                        var easeInOutTransform = new SlopeEaseInOutTransform();
                        TransformPipeline.Execute(ref easeInOutTransform, ref edges, ref nodes, in Context, in Config);
                        break;
                    case ShapeTransformTemplate.SlopeArch:
                        var archTransform = new SlopeArchTransform();
                        TransformPipeline.Execute(ref archTransform, ref edges, ref nodes, in Context, in Config);
                        break;
                    case ShapeTransformTemplate.CurveStraighten:
                        var straightenTransform = new CurveStraightenTransform();
                        TransformPipeline.Execute(ref straightenTransform, ref edges, ref nodes, in Context, in Config);
                        break;
                    case ShapeTransformTemplate.CurveSmooth:
                        var valid = CurveSmoothTransform.Execute(ref edges, ref nodes, Config.SmoothingFactor, out var failure, out var failureIndex,
                            Config.JunctionStartRotation, Config.JunctionEndRotation, Config.AllowInteriorJunctions, Config.InteriorHandleScale, Config.InteriorRotation);
#if IS_DEBUG
                        if (valid && Config.CombinedSlope) {
                            valid = CombinedLinearProfileTransform.Execute(ref edges, ref nodes, in EdgeStates,
                                in Context, in Config, out var verticalFailure, out var verticalIndex);
                            UnityEngine.Debug.Log($"[NetworkTools.CombinedProfile] submission={SmoothTraceId} valid={valid} failure={verticalFailure} index={verticalIndex}");
                            if (valid) ApplySurfaceCorrection(ref edges, ref nodes);
                        }
#endif
                        SmoothResult.Value = valid ? 1 : -1;
#if IS_DEBUG
                        TraceSmooth(SmoothTraceId, OutputMode, Config.SmoothingFactor, valid,
                            NodeStates, EdgeStates, nodes, edges, failure, failureIndex, ConnectedEdgeLookup, EdgeLookup, SmoothSelectedNodes);
#endif
                        if (!valid) {
                            edges.Dispose();
                            nodes.Dispose();
                            return;
                        }
                        break;
                }

                // Slope must submit the same endpoint/node relationship to Preview and Apply.
                // The pipeline averages interior node displacements; fit each incident curve
                // to that common height without changing its fitted endpoint grade.
                if (Config.Template == ShapeTransformTemplate.SlopeEaseInOut
                    || Config.Template == ShapeTransformTemplate.SlopeArch) {
                    for (var i = 0; i < edges.Length; i++) {
                        var edge = edges[i];
                        SlopeUtils.AlignEndpointHeight(ref edge, edge.IsForward,
                            nodes[i].OriginalPosition.y, nodes[i].Position.y);
                        SlopeUtils.AlignEndpointHeight(ref edge, !edge.IsForward,
                            nodes[i + 1].OriginalPosition.y, nodes[i + 1].Position.y);
                        edges[i] = edge;
                    }
                }
#if IS_DEBUG
                if (Config.Template != ShapeTransformTemplate.CurveSmooth)
                    CapturePreviewProbe(SmoothTraceId, OutputMode, true, edges);
#endif
                // 3. Write slope metadata to edge entities (preview only — Apply resets the tool
                //    immediately, so ECB additions would outlive the tool session).
                if (OutputMode == ToolOutputMode.Preview) {
                    OutputMetadata(edges);
                }

                // 4. Output
                if (OutputMode == ToolOutputMode.Preview)
                {
                    OutputPreview(edges, nodes);
                } else
                {
                    OutputApply(edges, nodes);
                }

                // Cleanup
                edges.Dispose();
                nodes.Dispose();
            }

            /// <summary>
            ///     Gets the network composition from an entity's Upgraded component.
            /// </summary>
            private NetworkComposition GetNetworkComposition(Entity entity) {
                if (!UpgradedLookup.TryGetComponent(entity, out var upgraded)) {
                    return NetworkComposition.None;
                }

                if ((upgraded.m_Flags.m_General & CompositionFlags.General.Elevated) != 0) {
                    return NetworkComposition.Elevated;
                }

                if ((upgraded.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0) {
                    return NetworkComposition.Tunnel;
                }

                return NetworkComposition.Ground;
            }

            /// <summary>
            ///     Writes NT_Metadata (existing and new slope) to each selected edge entity.
            /// </summary>
            private void OutputMetadata(NativeArray<EdgeState> edges) {
                for (var i = 0; i < edges.Length; i++) {
                    var edge = edges[i];
                    var existingDeltaY = math.abs(edge.OriginalBezierD.y - edge.OriginalBezierA.y);
                    var existingSlope = edge.Length > 0.01f
                        ? existingDeltaY / edge.Length * 100f
                        : 0f;

                    var newDeltaY = math.abs(edge.Bezier.d.y - edge.Bezier.a.y);
                    var newLength = MathUtils.Length(edge.Bezier);
                    var newSlope = newLength > 0.01f
                        ? newDeltaY / newLength * 100f
                        : 0f;

                    ECB.AddComponent(edge.EdgeEntity, new NT_Metadata {
                        ExistingSlope = existingSlope,
                        NewSlope = newSlope,
                    });
                }
            }

            /// <summary>
            ///     Creates CreationDefinition + NetCourse entities for preview.
            /// </summary>
            private void OutputPreview(NativeArray<EdgeState> edges, NativeArray<NodeState> nodes) {
                var nodePositionMap = new NativeHashMap<Entity, float3>(nodes.Length, Allocator.Temp);
                for (var i = 0; i < nodes.Length; i++) {
                    nodePositionMap.TryAdd(nodes[i].Entity, nodes[i].Position);
                }

                // Output selected edges
                for (var i = 0; i < edges.Length; i++) {
                    var state = edges[i];
                    var startNodePos = nodePositionMap.TryGetValue(state.StartNode, out var snp) ? snp : state.Bezier.a;
                    var endNodePos   = nodePositionMap.TryGetValue(state.EndNode, out var enp)   ? enp : state.Bezier.d;
                    OutputPreviewEdge(state.EdgeEntity,
                                      state.Bezier,
                                      MathUtils.Length(state.Bezier),
                                      state.NetworkComposition,
                                      Entity.Null,
                                      Entity.Null,
                                      startNodePos,
                                      endNodePos);
                }

                // A side edge may touch two selected nodes. Emit it once, with both
                // endpoint adjustments derived from the same original curve.
                var incident = GatherIncidentEdits(edges, nodes);
                for (var i = 0; i < incident.Length; i++) {
                    var edit = incident[i];
                    OutputPreviewEdge(edit.Entity, edit.Curve.m_Bezier, edit.Curve.m_Length,
                        GetNetworkComposition(edit.Entity), edit.StartReference, edit.EndReference,
                        edit.StartPosition, edit.EndPosition, false);
                }
                incident.Dispose();
                nodePositionMap.Dispose();
            }

            private struct IncidentEdit {
                public Entity Entity;
                public Curve Curve;
                public Entity StartReference, EndReference;
                public float3 StartPosition, EndPosition;
                public bool Changed;
            }

            private NativeList<IncidentEdit> GatherIncidentEdits(NativeArray<EdgeState> selected, NativeArray<NodeState> nodes) {
                var edits = new NativeList<IncidentEdit>(Allocator.Temp);
                var positions = new NativeHashMap<Entity, float3>(nodes.Length, Allocator.Temp);
                for (var i = 0; i < nodes.Length; i++) positions.TryAdd(nodes[i].Entity, nodes[i].Position);
                for (var i = 0; i < nodes.Length; i++) {
                    if (!ConnectedEdgeLookup.TryGetBuffer(nodes[i].Entity, out var incident)) continue;
                    for (var j = 0; j < incident.Length; j++) {
                        var entity = incident[j].m_Edge;
                        if (IsEdgeInSelection(entity, selected)) continue;
                        var seen = false;
                        for (var k = 0; k < edits.Length; k++) if (edits[k].Entity == entity) { seen = true; break; }
                        if (seen || !EdgeLookup.TryGetComponent(entity, out var edge)
                            || !CurveLookup.TryGetComponent(entity, out var curve)
                            || !NodeLookup.TryGetComponent(edge.m_Start, out var start)
                            || !NodeLookup.TryGetComponent(edge.m_End, out var end)) continue;
                        var hasStart = positions.TryGetValue(edge.m_Start, out var startPosition);
                        var hasEnd = positions.TryGetValue(edge.m_End, out var endPosition);
                        if (!hasStart) startPosition = start.m_Position;
                        if (!hasEnd) endPosition = end.m_Position;
                        var startDelta = startPosition - start.m_Position;
                        var endDelta = endPosition - end.m_Position;
                        curve.m_Bezier = IncidentCurveAdjustment.Translate(curve.m_Bezier, startDelta, endDelta);
                        curve.m_Length = MathUtils.Length(curve.m_Bezier);
                        edits.Add(new IncidentEdit {
                            Entity = entity, Curve = curve,
                            StartReference = hasStart ? Entity.Null : edge.m_Start,
                            EndReference = hasEnd ? Entity.Null : edge.m_End,
                            StartPosition = startPosition, EndPosition = endPosition,
                            Changed = math.any(startDelta != float3.zero) || math.any(endDelta != float3.zero)
                        });
                    }
                }
                positions.Dispose();
                return edits;
            }

            /// <summary>
            ///     Creates a preview entity for an edge with configurable node references.
            /// </summary>
            private void OutputPreviewEdge(
                Entity             edgeEntity,
                Bezier4x3          bezier,
                float              length,
                NetworkComposition composition,
                Entity             startNodeEntity,
                Entity             endNodeEntity,
                float3             startNodePosition,
                float3             endNodePosition,
                bool showAsParent = true) {
                var definitionEntity = ECB.CreateEntity();

                var creationDefinition = new CreationDefinition {
                    m_Original = edgeEntity,
                    m_Flags    = CreationFlags.Recreate
                };

                if (showAsParent) {
                    creationDefinition.m_Flags |= CreationFlags.Parent;
                }

                if (PrefabRefLookup.TryGetComponent(edgeEntity, out var prefabRef)) {
                    creationDefinition.m_Prefab = prefabRef;
                }

                if (PseudoRandomSeedLookup.TryGetComponent(edgeEntity, out var seed)) {
                    creationDefinition.m_RandomSeed = seed.m_Seed;
                }

                ECB.AddComponent(definitionEntity, creationDefinition);
                ECB.AddComponent<Updated>(definitionEntity);

                var startNodeFlags = GetFlagsFromComposition(composition);
                var endNodeFlags   = GetFlagsFromComposition(composition);

                // FreeHeight tells the game to respect our custom heights
                startNodeFlags |= CoursePosFlags.FreeHeight | CoursePosFlags.IsGrid | CoursePosFlags.IsRight;
                endNodeFlags   |= CoursePosFlags.FreeHeight | CoursePosFlags.IsGrid | CoursePosFlags.IsRight;

                // Add flags to force connections
                if (startNodeEntity != Entity.Null && endNodeEntity == Entity.Null) {
                    startNodeFlags |= CoursePosFlags.IsFirst | CoursePosFlags.IsGrid;
                    endNodeFlags |= CoursePosFlags.IsLast | CoursePosFlags.IsGrid;
                } else if (endNodeEntity != Entity.Null && startNodeEntity == Entity.Null) {
                    endNodeFlags |= CoursePosFlags.IsFirst | CoursePosFlags.IsGrid;
                    startNodeFlags |= CoursePosFlags.IsLast | CoursePosFlags.IsGrid;
                }

                // Initialize elevations from what the edge and its nodes store: Apply keeps those,
                // so the preview gets the same ground/elevated/tunnel pieces as the result
                var startElevation = float2.zero;
                var endElevation = float2.zero;
                var courseElevation = float2.zero;

                if (EdgeLookup.TryGetComponent(edgeEntity, out var originalEdge)) {
                    if (ElevationLookup.TryGetComponent(originalEdge.m_Start, out var atStart)) {
                        startElevation = atStart.m_Elevation;
                    }

                    if (ElevationLookup.TryGetComponent(originalEdge.m_End, out var atEnd)) {
                        endElevation = atEnd.m_Elevation;
                    }
                }

                if (ElevationLookup.TryGetComponent(edgeEntity, out var atEdge)) {
                    courseElevation = atEdge.m_Elevation;
                }

                var netCourse = new NetCourse {
                    m_Curve      = bezier,
                    m_Length     = length,
                    m_FixedIndex = -1,
                    m_Elevation  = courseElevation,
                    m_StartPosition = new CoursePos {
                        m_Entity        = startNodeEntity,
                        m_Position      = startNodePosition,
                        m_Rotation      = NetUtils.GetNodeRotation(MathUtils.StartTangent(bezier)),
                        m_CourseDelta   = 0,
                        m_Elevation     = startElevation,
                        m_Flags         = startNodeFlags,
                        m_ParentMesh    = -1,
                        m_SplitPosition = 0
                    },
                    m_EndPosition = new CoursePos {
                        m_Entity        = endNodeEntity,
                        m_Position      = endNodePosition,
                        m_Rotation      = NetUtils.GetNodeRotation(MathUtils.EndTangent(bezier)),
                        m_CourseDelta   = 1,
                        m_Elevation     = endElevation,
                        m_Flags         = endNodeFlags,
                        m_ParentMesh    = -1,
                        m_SplitPosition = 0
                    }
                };

                // Apply composition constraints (ground/tunnel/elevated)
                ApplyCompositionToNetCourse(ref netCourse, composition);

                ECB.AddComponent(definitionEntity, netCourse);
            }

            /// <summary>
            ///     Applies network composition constraints to a NetCourse.
            ///     Ground: forces elevation to 0.
            ///     Tunnel: ensures elevation is at most the tunnel threshold.
            ///     Elevated: ensures elevation is at least the elevated threshold.
            /// </summary>
            private static void ApplyCompositionToNetCourse(ref NetCourse netCourse, NetworkComposition composition) {
                switch (composition) {
                    case NetworkComposition.Ground:
                        netCourse.m_Elevation = SlopeUtils.ForceGroundElevation;
                        netCourse.m_StartPosition.m_Elevation = SlopeUtils.ForceGroundElevation;
                        netCourse.m_EndPosition.m_Elevation = SlopeUtils.ForceGroundElevation;
                        break;

                    case NetworkComposition.Tunnel:
                        netCourse.m_Elevation.x = math.min(netCourse.m_Elevation.x, SlopeUtils.TunnelThreshold.x);
                        netCourse.m_Elevation.y = math.min(netCourse.m_Elevation.y, SlopeUtils.TunnelThreshold.y);
                        netCourse.m_StartPosition.m_Elevation.x = math.min(netCourse.m_StartPosition.m_Elevation.x, SlopeUtils.TunnelThreshold.x);
                        netCourse.m_StartPosition.m_Elevation.y = math.min(netCourse.m_StartPosition.m_Elevation.y, SlopeUtils.TunnelThreshold.y);
                        netCourse.m_EndPosition.m_Elevation.x = math.min(netCourse.m_EndPosition.m_Elevation.x, SlopeUtils.TunnelThreshold.x);
                        netCourse.m_EndPosition.m_Elevation.y = math.min(netCourse.m_EndPosition.m_Elevation.y, SlopeUtils.TunnelThreshold.y);
                        break;

                    case NetworkComposition.Elevated:
                        netCourse.m_Elevation.x = math.max(netCourse.m_Elevation.x, SlopeUtils.ElevatedThreshold.x);
                        netCourse.m_Elevation.y = math.max(netCourse.m_Elevation.y, SlopeUtils.ElevatedThreshold.y);
                        netCourse.m_StartPosition.m_Elevation.x = math.max(netCourse.m_StartPosition.m_Elevation.x, SlopeUtils.ElevatedThreshold.x);
                        netCourse.m_StartPosition.m_Elevation.y = math.max(netCourse.m_StartPosition.m_Elevation.y, SlopeUtils.ElevatedThreshold.y);
                        netCourse.m_EndPosition.m_Elevation.x = math.max(netCourse.m_EndPosition.m_Elevation.x, SlopeUtils.ElevatedThreshold.x);
                        netCourse.m_EndPosition.m_Elevation.y = math.max(netCourse.m_EndPosition.m_Elevation.y, SlopeUtils.ElevatedThreshold.y);
                        break;
                }
            }

            /// <summary>
            ///     Gets the flags for a network composition.
            /// </summary>
            private static CoursePosFlags GetFlagsFromComposition(NetworkComposition composition) {
                return composition switch {
                    NetworkComposition.Elevated => CoursePosFlags.ForceElevatedEdge | CoursePosFlags.ForceElevatedNode,
                    NetworkComposition.Tunnel   => 0,
                    NetworkComposition.Ground   => 0,
                    _                           => 0
                };
            }

            /// <summary>
            ///     Applies transformation changes to existing Curve components, node positions, and intersection adjustments.
            /// </summary>
            private void OutputApply(NativeArray<EdgeState> edges, NativeArray<NodeState> nodes) {
                var processedNodes = new NativeHashSet<Entity>(nodes.Length, Allocator.Temp);

                // Apply curve changes to selected edges
                for (var i = 0; i < edges.Length; i++) {
                    var state = edges[i];
                    ECB.SetComponent(state.EdgeEntity,
                                     new Curve {
                                         m_Bezier = state.Bezier,
                                         m_Length = MathUtils.Length(state.Bezier)
                                     });
                }

                // Preserve fields outside the position contract, including rotation.
                for (var i = 0; i < nodes.Length; i++) {
                    var state = nodes[i];
                    if (!processedNodes.Add(state.Entity)) continue;
                    if (NodeLookup.TryGetComponent(state.Entity, out var original)) {
                        ECB.SetComponent(state.Entity, IncidentCurveAdjustment.MoveNode(original, state.Position));
                        MarkNodeUpdated(state.Entity);
                    }
                }
                var incident = GatherIncidentEdits(edges, nodes);
                for (var i = 0; i < incident.Length; i++) {
                    var edit = incident[i];
                    if (!edit.Changed) continue;
                    ECB.SetComponent(edit.Entity, edit.Curve);
                    MarkUpdated(edit.Entity);
                }
                incident.Dispose();
                processedNodes.Dispose();
            }

            /// <summary>
            ///     Checks if an edge entity is in the selection.
            /// </summary>
            private static bool IsEdgeInSelection(Entity edgeEntity, NativeArray<EdgeState> selectedEdges) {
                for (var i = 0; i < selectedEdges.Length; i++) {
                    if (selectedEdges[i].EdgeEntity == edgeEntity) {
                        return true;
                    }
                }

                return false;
            }


            /// <summary>
            ///     Marks an entity as updated with Updated and BatchesUpdated components.
            /// </summary>
            private void MarkUpdated(Entity entity) {
                ECB.AddComponent<Updated>(entity);
                ECB.AddComponent<BatchesUpdated>(entity);
            }

            /// <summary>
            ///     Marks a node and all its connected edges as updated.
            /// </summary>
            private void MarkNodeUpdated(Entity nodeEntity) {
                MarkUpdated(nodeEntity);

                if (!ConnectedEdgeLookup.TryGetBuffer(nodeEntity, out var connectedEdges)) {
                    return;
                }

                for (var i = 0; i < connectedEdges.Length; i++) {
                    var edgeEntity = connectedEdges[i].m_Edge;

                    if (!EdgeLookup.TryGetComponent(edgeEntity, out var edge)) {
                        continue;
                    }

                    if (edge.m_Start != nodeEntity && edge.m_End != nodeEntity) {
                        continue;
                    }

                    MarkUpdated(edgeEntity);

                    if (edge.m_Start != nodeEntity) {
                        MarkUpdated(edge.m_Start);
                    } else if (edge.m_End != nodeEntity) {
                        MarkUpdated(edge.m_End);
                    }

                    if (AggregatedLookup.TryGetComponent(edgeEntity, out var aggregated)) {
                        MarkUpdated(aggregated.m_Aggregate);
                    }
                }
            }
        }
    }
}
