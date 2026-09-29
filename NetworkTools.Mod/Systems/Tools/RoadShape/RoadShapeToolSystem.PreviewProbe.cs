#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;
    using Colossal.Mathematics;
    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    // Reads after native playback. Supplies diagnostic evidence and the correlated
    // submission required by Debug automation; not a general native completion fence.
    public partial class NT_RoadShapeToolSystem {
        private sealed class PreviewProbe {
            public int Id;
            public Dictionary<Entity, Bezier4x3> Curves = new();
        }
        private static readonly object s_ProbeLock = new();
        private static PreviewProbe s_Probe;
        private string m_LastProbeMessage;
        private long m_PreviewInputRevision;
        private long m_SubmittedPreviewRevision;

        private static void CapturePreviewProbe(int id, ToolOutputMode mode, bool valid,
            NativeArray<EdgeState> edges) {
            var probe = new PreviewProbe { Id = id };
            if (valid && mode == ToolOutputMode.Preview && edges.Length <= 128) {
                foreach (var edge in edges) probe.Curves.Add(edge.EdgeEntity, edge.Bezier);
            }
            lock (s_ProbeLock) { s_Probe = probe; }
        }

        internal void ObservePreviewAfterRebuild() {
            if (!Enabled || Phase != OperationPhase.Ready || Template.Value != ShapeTransformTemplate.CurveSmooth) {
                m_LastProbeMessage = null;
                return;
            }
            if (!m_LastShapeJob.IsCompleted) { return; }
            m_LastShapeJob.Complete();
            PreviewProbe probe;
            lock (s_ProbeLock) { probe = s_Probe; }
            if (probe == null || probe.Id != m_SmoothTraceId || probe.Curves.Count == 0) { return; }
            var matches = new Dictionary<Entity, int>();
            var mismatches = 0;
            var tracks = 0;
            var missingBuffers = 0;
            using (var query = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Curve>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            })) {
                if (query.CalculateEntityCount() > 4096) { return; }
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities) {
                    var original = EntityManager.GetComponentData<Temp>(entity).m_Original;
                    if (!probe.Curves.TryGetValue(original, out var expected)) { continue; }
                    matches.TryGetValue(original, out var count);
                    matches[original] = count + 1;
                    var actual = EntityManager.GetComponentData<Curve>(entity).m_Bezier;
                    if (!SameControl(expected.a, actual.a) || !SameControl(expected.b, actual.b)
                        || !SameControl(expected.c, actual.c) || !SameControl(expected.d, actual.d)) { mismatches++; }
                    if (!EntityManager.HasBuffer<SubLane>(entity)) { missingBuffers++; continue; }
                    var lanes = EntityManager.GetBuffer<SubLane>(entity, true);
                    foreach (var lane in lanes) {
                        if (EntityManager.Exists(lane.m_SubLane) && EntityManager.HasComponent<TrackLane>(lane.m_SubLane)) { tracks++; }
                    }
                }
            }
            var duplicates = 0;
            foreach (var count in matches.Values) { if (count != 1) { duplicates++; } }
            var revisionMatches = m_SubmittedPreviewRevision == m_PreviewInputRevision && !m_UpdateNeeded && m_PathDataValid;
            var originalInputs = OriginalProbeStatus();
            m_AutomationVerifiedSubmission = revisionMatches && originalInputs == "matches"
                && matches.Count == probe.Curves.Count && duplicates == 0 && mismatches == 0 && missingBuffers == 0 ? probe.Id : 0;
            ObserveJunctionSearch(probe.Id, revisionMatches && originalInputs == "matches"
                && matches.Count == probe.Curves.Count && duplicates == 0 && mismatches == 0 && missingBuffers == 0);
            var message = $"[NetworkTools.PreviewProbe] session={s_SmoothSession} submission={probe.Id} inputRevision={m_PreviewInputRevision} submittedRevision={m_SubmittedPreviewRevision} revisionMatches={revisionMatches} dirty={m_UpdateNeeded} expectedEdges={probe.Curves.Count} matchedEdges={matches.Count} ambiguousEdges={duplicates} curveMismatches={mismatches} missingLaneBuffers={missingBuffers} edgeTrackLanes={tracks} phase=AfterModificationEndBarrier validationReady=false";
            message += $" originalInputs={originalInputs}";
            if (message != m_LastProbeMessage) {
                UnityEngine.Debug.Log(message);
                m_LastProbeMessage = message;
            }
        }

        private static bool SameControl(float3 a, float3 b) => math.all(math.isfinite(a))
            && math.all(math.isfinite(b)) && math.distancesq(a, b) <= 0.000001f;
    }

    public partial class NT_PreviewProbeSystem : GameSystemBase {
        protected override void OnUpdate() {
            World.GetExistingSystemManaged<NT_RoadShapeToolSystem>()?.ObservePreviewAfterRebuild();
        }
    }
}
#endif
