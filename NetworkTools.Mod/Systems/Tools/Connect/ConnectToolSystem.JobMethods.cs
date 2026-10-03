namespace NetworkTools.Systems.Tools.Connect {
    using Colossal.Entities;

    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Notifications;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;

    using Unity.Entities;
    using Unity.Jobs;

    public partial class NT_ConnectToolSystem {
#if IS_DEBUG
        private bool m_ControlClearingPreview;
#endif
        /// <summary>
        ///     Builds a Burst-compatible snapshot struct from the current parameter values and contextual state.
        /// </summary>
        internal ConnectJobConfig BuildJobConfig() {
            return new ConnectJobConfig {
#if IS_DEBUG
                SmoothElevationProfile = SmoothElevationProfile.Value,
                ComplexProfile = Mode.Value == ConnectMode.ComplexCurve,
#endif
                StartPosition                  = StartPosition.Value,
                EndPosition                    = EndPosition.Value,
                StartDirection                 = StartDirection.Value,
                EndDirection                   = EndDirection.Value,
                CurveStartPointPosition        = CurveStartPointPosition.Value,
                CurveStartControlPointPosition = CurveStartControlPointPosition.Value,
                CurveEndControlPointPosition   = CurveEndControlPointPosition.Value,
                CurveEndPointPosition          = CurveEndPointPosition.Value,
                ComplexStartPointPosition        = ComplexStartPointPosition.Value,
                ComplexStartControlPointPosition = ComplexStartControlPointPosition.Value,
                ComplexEndControlPointPosition   = ComplexEndControlPointPosition.Value,
                ComplexEndPointPosition          = ComplexEndPointPosition.Value,
                ComplexMidPosition                  = ComplexMidPosition.Value,
                ComplexMidStartControlPointPosition = ComplexMidStartControlPointPosition.Value,
                ComplexMidEndControlPointPosition   = ComplexMidEndControlPointPosition.Value,
                LoopRadiusFactor               = LoopRadiusFactor.Value,
                LoopArcSide                    = LoopArc.Value,
            };
        }

        private JobHandle ScheduleDefinitionsJob(JobHandle inputDeps, ToolOutputMode outputMode, ConnectJobConfig? acceptedConfig = null) {
            m_Log.Debug($"ScheduleDefinitionsJob: Mode={Mode.Value}");

            if (m_SelectedNodes.Length != 2) {
                return inputDeps;
            }

            inputDeps = DestroyDefinitions(m_DefinitionQuery, m_Barrier, inputDeps);

            var config = acceptedConfig ?? BuildJobConfig();
            var netPrefabEntity = acceptedConfig.HasValue ? config.NetPrefabEntity : NetPrefab.NetPrefabEntity;
            var netLanePrefabEntity = acceptedConfig.HasValue ? config.NetLanePrefabEntity : NetPrefab.NetLanePrefabEntity;

            if (!acceptedConfig.HasValue && netPrefabEntity == Entity.Null && netLanePrefabEntity == Entity.Null) {
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(m_SelectedNodes[0]);
                netPrefabEntity = prefabRef.m_Prefab;
            }

            config.NetPrefabEntity = netPrefabEntity;
            config.NetLanePrefabEntity = netLanePrefabEntity;
#if IS_DEBUG
            if (outputMode == ToolOutputMode.Preview) {
                if ((LaneAwareDirection.Value && !TryPrepareLaneDirections(ref config, out m_ControlRejection))
                    || (config.SmoothElevationProfile && !TryPrepareProfile(ref config, out m_ControlRejection))) {
                    m_ControlCandidate = null; m_ControlAcceptedCandidate = null;
                    return inputDeps;
                }
                BeginControlPreview(config);
            }
#endif
            var jobHandle = new CreateDefinitionsJob {
                Mode = Mode.Value,
                Config = config,
                SelectedNodeEntities = m_SelectedNodes,
                NetPrefabEntity = netPrefabEntity,
                NetLanePrefabEntity = netLanePrefabEntity,
                OutputMode = outputMode,

                // Lookups needed for output and intersection adjustments
                NodeLookup = SystemAPI.GetComponentLookup<Node>(true),
                CurveLookup = SystemAPI.GetComponentLookup<Curve>(true),
                EdgeLookup = SystemAPI.GetComponentLookup<Edge>(true),
                UpgradedLookup = SystemAPI.GetComponentLookup<Upgraded>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<PseudoRandomSeed>(true),
                ConnectedEdgeLookup = SystemAPI.GetBufferLookup<ConnectedEdge>(true),
                AggregatedLookup = SystemAPI.GetComponentLookup<Aggregated>(true),
                NetGeometryDataLookup = SystemAPI.GetComponentLookup<NetGeometryData>(true),
                ECB = m_Barrier.CreateCommandBuffer(),
            }.Schedule(inputDeps);
#if IS_DEBUG
            m_ControlJob = jobHandle;
#endif
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

#if IS_DEBUG
            // A rejected request may have lost a selected entity/component. Do not
            // immediately feed that missing context into the preview generator.
            if (ControlCandidateRequired && ControlInputs() == null) {
                m_ControlCandidate = null;
                m_ControlAcceptedCandidate = null;
                m_ControlRejection = "inputs_unavailable";
                return Clear(inputDeps);
            }
#endif
#if IS_DEBUG
            // A native rebuild may reuse temporary IDs. Establish an empty preview
            // boundary before replacing a guarded candidate; never accept old IDs
            // merely because their coordinates happen to match the new request.
            if (ControlCandidateRequired && (m_ControlCandidate != null || m_ControlClearingPreview)) {
                m_ControlCandidate = null; m_ControlAcceptedCandidate = null;
                using var oldPreview = ControlTempQuery();
                if (!m_ControlClearingPreview || oldPreview.CalculateEntityCount() != 0) {
                    m_ControlClearingPreview = true;
                    m_ControlRejection = "preview_clearing";
                    return Clear(inputDeps);
                }
                m_ControlClearingPreview = false;
            }
#endif
            // Recreate temp entities
            applyMode = ApplyMode.Clear;
            inputDeps = ScheduleDefinitionsJob(inputDeps, ToolOutputMode.Preview);

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
            ConnectJobConfig? acceptedConfig = null;
#if IS_DEBUG
            if (m_ControlAcceptedCandidate != null || ControlCandidateRequired) {
                inputDeps.Complete();
                m_ControlJob.Complete();
                if (!ControlCandidateAllowsApply(executing: true)) {
                    m_ControlAcceptedCandidate = null;
                    Phase = OperationPhase.Ready;
                    m_UpdateNeeded = true;
                    return Update(inputDeps);
                }
                acceptedConfig = m_ControlAcceptedCandidate.Config;
            }
#endif
            applyMode = ApplyMode.Apply;
            var jobHandle = ScheduleDefinitionsJob(inputDeps, ToolOutputMode.Apply, acceptedConfig);

            jobHandle.Complete();

            ResetToIdle();

            return jobHandle;
        }
    }
}
