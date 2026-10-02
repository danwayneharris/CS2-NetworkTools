#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;
    using Colossal.Mathematics;
    using Game.Common;
    using Game.Net;
    using Game.Tools;
    using NetworkTools.Geometry;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    // Experimental native fixed-point iteration. Every job starts from the same
    // authored baseline; only generated boundary references are fed back.
    public partial class NT_RoadShapeToolSystem {
        private NativeParallelHashMap<Entity, EdgeGeometry> m_SurfaceReferences;
        private NativeReference<int> m_SurfaceCorrected;
        private long m_SurfaceRevision = -1;
        private int m_SurfaceObserved, m_SurfaceStable;
        private bool m_SurfaceAccepted, m_SurfaceFailed;
        private SurfacePreviewConvergence m_SurfaceConvergence;
        private Dictionary<Entity, Bezier4x3> m_SurfacePrevious;
        private Dictionary<Entity, EdgeGeometry> m_SurfaceObservedGeometry;
        private readonly HashSet<Entity> m_SurfaceRequired = new();
        private readonly System.Diagnostics.Stopwatch m_SurfaceClock = new();

        private ShapeJobConfig m_SurfaceConfig;
        private readonly System.Diagnostics.Stopwatch m_CombinedClock = new();
        private bool UsesSurfaceValidation => Template.Value == ShapeTransformTemplate.SlopeLinear || CombinedMode;

        private void ConfigureSurfacePreview(in ShapeJobConfig config) {
            if (!UsesSurfaceValidation) { return; }
            var newRevision = m_SurfaceRevision != m_PreviewInputRevision;
            var newHorizontal = CombinedMode && (config.JunctionStartRotation != m_SurfaceConfig.JunctionStartRotation
                || config.JunctionEndRotation != m_SurfaceConfig.JunctionEndRotation
                || config.InteriorHandleScale != m_SurfaceConfig.InteriorHandleScale
                || config.InteriorRotation != m_SurfaceConfig.InteriorRotation);
            if (newRevision) m_CombinedClock.Restart();
            m_SurfaceConfig = config;
            if (newRevision || newHorizontal) {
                m_SurfaceReferences.Clear(); m_SurfaceRequired.Clear();
                m_SurfaceRevision = m_PreviewInputRevision;
                m_SurfaceObserved = 0; m_SurfaceStable = 0;
                m_SurfaceAccepted = m_SurfaceFailed = false;
                m_SurfacePrevious = null; m_SurfaceObservedGeometry = null;
                m_SurfaceConvergence = new SurfacePreviewConvergence();
                m_SurfaceClock.Restart();
                foreach (var n in m_NodeStates) {
                    if (!EntityManager.HasBuffer<ConnectedEdge>(n.Entity)) { m_SurfaceFailed = true; continue; }
                    foreach (var e in EntityManager.GetBuffer<ConnectedEdge>(n.Entity, true)) { m_SurfaceRequired.Add(e.m_Edge); }
                }
                if (m_SurfaceRequired.Count > 512) { m_SurfaceFailed = true; }
            }
        }

        private bool SurfacePreviewAllowsApply() => m_SurfaceRevision == m_PreviewInputRevision
            && m_SurfaceAccepted && !m_SurfaceFailed && m_SurfaceObserved == m_SmoothTraceId
            && m_AutomationVerifiedSubmission == m_SmoothTraceId && OriginalProbeStatus() == "matches";

        private void ObserveSurfacePreview(PreviewProbe probe, bool fresh) {
            if (!UsesSurfaceValidation || m_SurfaceRevision != m_PreviewInputRevision
                || m_SurfaceFailed) { return; }
            if (CombinedMode && m_CombinedClock.Elapsed.TotalSeconds > 60 && !m_SurfaceAccepted) {
                m_SurfaceFailed = true; return;
            }
            if (m_SurfaceAccepted) {
                if (m_SurfaceObserved == probe.Id) { return; }
                // A same-input rebuild still needs evidence for its new submission.
                m_SurfaceAccepted = false;
                m_SurfaceClock.Restart();
            }
            if (m_SurfaceClock.Elapsed.TotalSeconds > 20) { m_SurfaceFailed = true; return; }
            if (!fresh) { m_SurfaceStable = 0; return; }
            if (m_SurfaceObserved != probe.Id) {
                m_SurfaceObserved = probe.Id; m_SurfaceStable = 0; m_SurfaceObservedGeometry = null;
            }
            var corrected = m_SurfaceCorrected.Value == 1;
            var priming = CombinedMode && m_SurfaceCorrected.Value == 2;
            var observed = new Dictionary<Entity, EdgeGeometry>();
            if (corrected || priming) {
                using var query = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<EdgeGeometry>(), ComponentType.ReadOnly<Temp>() },
                    None = new[] { ComponentType.ReadOnly<Deleted>() }
                });
                if (query.CalculateEntityCount() > 4096) { m_SurfaceFailed = true; return; }
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var e in entities) {
                    var original = EntityManager.GetComponentData<Temp>(e).m_Original;
                    if (!m_SurfaceRequired.Contains(original)) { continue; }
                    if (observed.ContainsKey(original)) { m_SurfaceStable = 0; return; }
                    observed.Add(original, EntityManager.GetComponentData<EdgeGeometry>(e));
                }
                if (observed.Count != m_SurfaceRequired.Count) { m_SurfaceStable = 0; return; }
            }
            var stable = m_SurfaceObservedGeometry != null && observed.Count == m_SurfaceObservedGeometry.Count;
            foreach (var pair in observed) {
                stable &= m_SurfaceObservedGeometry != null && m_SurfaceObservedGeometry.TryGetValue(pair.Key, out var old) && old.Equals(pair.Value);
            }
            m_SurfaceObservedGeometry = observed;
            m_SurfaceStable = stable ? m_SurfaceStable + 1 : 0;
            if (m_SurfaceStable < 2) { return; }
            if (priming) {
                m_SurfaceReferences.Clear();
                foreach (var pair in observed) m_SurfaceReferences.Add(pair.Key, pair.Value);
                m_UpdateNeeded = true;
                UnityEngine.Debug.Log($"[NetworkTools.SurfacePreview] submission={probe.Id} primedReferences={observed.Count}");
                return;
            }
            var displacement = double.PositiveInfinity;
            if (m_SurfacePrevious != null && m_SurfacePrevious.Count == probe.Curves.Count) {
                displacement = 0;
                foreach (var pair in probe.Curves) {
                    if (!m_SurfacePrevious.TryGetValue(pair.Key, out var old)) { m_SurfaceFailed = true; return; }
                    var c = pair.Value;
                    displacement = System.Math.Max(displacement, math.cmax(new float4(math.distance(c.a, old.a),
                        math.distance(c.b, old.b), math.distance(c.c, old.c), math.distance(c.d, old.d))));
                }
            }
            var result = m_SurfaceConvergence.Observe(corrected, displacement);
            UnityEngine.Debug.Log($"[NetworkTools.SurfacePreview] submission={probe.Id} corrected={corrected} displacement={displacement} result={result}");
            if (result == SurfacePreviewConvergence.Result.Failed) { m_SurfaceFailed = true; return; }
            if (result == SurfacePreviewConvergence.Result.Accepted) { m_SurfaceAccepted = true; return; }
            // Copy plain component values, never retain temporary entity identities.
            m_SurfaceReferences.Clear();
            foreach (var pair in observed) { m_SurfaceReferences.Add(pair.Key, pair.Value); }
            m_SurfacePrevious = new Dictionary<Entity, Bezier4x3>(probe.Curves);
            m_UpdateNeeded = true;
        }
    }
}
#endif
