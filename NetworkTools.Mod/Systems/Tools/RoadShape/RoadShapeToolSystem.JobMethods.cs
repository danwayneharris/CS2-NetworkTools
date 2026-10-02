namespace NetworkTools.Systems.Tools.RoadShape {
    using Colossal.Entities;

    using Game.Common;
    using Game.Net;
    using Game.Notifications;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;

    using NetworkTools.Components;
    using NetworkTools.Settings;

    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;

    public partial class NT_RoadShapeToolSystem {
        private JobHandle SchedulePathTransformJob(JobHandle inputDeps, ToolOutputMode outputMode) {
            // Ensure path data is valid before scheduling
            if (!m_PathDataValid || m_EdgeStates.Length == 0) {
                m_Log.Debug("SchedulePathTransformJob: No valid path data, skipping");
                return inputDeps;
            }

            // A parameter edit may follow a network edit by another tool/system.
            // Refresh both transform inputs and their comparison baseline before fit.
            m_LastShapeJob.Complete();
            inputDeps.Complete();
            if (CachedOriginalStatus() != "matches") {
                if (outputMode == ToolOutputMode.Apply) return inputDeps;
                RefreshPathData();
                if (!m_PathDataValid) return inputDeps;
            }
            var config = BuildJobConfig();
            // Complete previous readers before collecting candidate-search inputs.
            m_LastShapeJob.Complete();
            SnapshotSplitNodes();
#if IS_DEBUG
            ConfigureJunctionSearch(ref config);
            ConfigureInteriorJunctions(ref config);
            ConfigureSurfacePreview();
#endif
            // The result is consumed by the UI only after this job completes.
            m_LastShapeJob.Complete();
            if (outputMode == ToolOutputMode.Preview) {
                m_SubmittedPreviewRevision = m_PreviewInputRevision;
                m_SubmittedOriginalInputs = new System.Collections.Generic.List<object>(m_CachedOriginalInputs);
            }
            m_SmoothResult.Value = 0;
            m_Log.Debug($"SchedulePathTransformJob: Template={config.Template}, EaseIn={config.EaseInLength:F3}, EaseOut={config.EaseOutLength:F3}");
            m_Log.Debug($"  Path: Start={m_ShapeTransformContext.StartPosition}, End={m_ShapeTransformContext.EndPosition}, DeltaHeight={m_ShapeTransformContext.DeltaHeight:F2}");

            var jobHandle = new ShapeTransformJob {
                // Pre-computed path data
                EdgeStates = m_EdgeStates,
                NodeStates = m_NodeStates,
                Context = m_ShapeTransformContext,
                Config = config,

                // Lookups needed for output and intersection adjustments
                CurrentPathNodes = m_CurrentPathNodes,
                SurfaceGeometryLookup = SystemAPI.GetComponentLookup<EdgeGeometry>(true),
                SurfaceNodeGeometryLookup = SystemAPI.GetComponentLookup<NodeGeometry>(true),
                SurfacePrefabGeometryLookup = SystemAPI.GetComponentLookup<NetGeometryData>(true),
                SurfaceCompositionLookup = SystemAPI.GetComponentLookup<Composition>(true),
                SurfaceCompositionDataLookup = SystemAPI.GetComponentLookup<NetCompositionData>(true),
                NodeLookup = SystemAPI.GetComponentLookup<Node>(true),
                CurveLookup = SystemAPI.GetComponentLookup<Curve>(true),
                EdgeLookup = SystemAPI.GetComponentLookup<Edge>(true),
                UpgradedLookup = SystemAPI.GetComponentLookup<Upgraded>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<PseudoRandomSeed>(true),
                ConnectedEdgeLookup = SystemAPI.GetBufferLookup<ConnectedEdge>(true),
                AggregatedLookup = SystemAPI.GetComponentLookup<Aggregated>(true),
                ElevationLookup = SystemAPI.GetComponentLookup<Elevation>(true),
                OutputMode = outputMode,
                ECB = m_Barrier.CreateCommandBuffer(),
                SmoothResult = m_SmoothResult,
#if IS_DEBUG
                SmoothTraceId = ++m_SmoothTraceId,
                SurfaceReferences = m_SurfaceReferences,
                SurfaceCorrected = m_SurfaceCorrected,
                SmoothSelectedNodes = m_SelectedNodes,
#endif
            }.Schedule(inputDeps);
            m_LastShapeJob = jobHandle;
            m_Barrier.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }

        private JobHandle Update(JobHandle inputDeps) {
            // Check if we can reuse existing temp entities
            // This will be true if the selected nodes and operation config didn't change
            if (!m_UpdateNeeded)
            {
                applyMode = ApplyMode.None;
                return inputDeps;
            }

            // Recreate temp entities
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionQuery, m_Barrier, inputDeps);
            inputDeps = SchedulePathTransformJob(inputDeps, ToolOutputMode.Preview);

            // Reset the flag after processing
            m_UpdateNeeded = false;

            return inputDeps;
        }

        private JobHandle Clear(JobHandle inputDeps) {
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionQuery, m_Barrier, inputDeps);
            return inputDeps;
        }

        private JobHandle Apply(JobHandle inputDeps) {
            // RequestApply and execution can occur in different updates. Recheck
            // original inputs and candidate identity immediately before scheduling.
            inputDeps.Complete();
            if (!CandidateAllowsApply()) {
                Phase = OperationPhase.Ready;
                MarkDirty();
                return Update(inputDeps);
            }
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionQuery, m_Barrier, inputDeps);
            var jobHandle = SchedulePathTransformJob(inputDeps, ToolOutputMode.Apply);

            // Reset clears native selection lists read by the scheduled job.
            jobHandle.Complete();
            ResetToIdle();

            return jobHandle;
        }
    }
}
