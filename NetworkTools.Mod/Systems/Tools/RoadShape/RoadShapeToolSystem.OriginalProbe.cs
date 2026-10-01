namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_RoadShapeToolSystem {
        private List<object> m_SubmittedOriginalInputs;
        private List<object> m_CachedOriginalInputs;
        private long m_PreviewInputRevision;
        private long m_SubmittedPreviewRevision;
#if IS_DEBUG
        private long m_LastFailureTestRevision = -1;
        private long m_HeldProbeRevision;
        private List<object> m_HeldProbeInputs;

#endif

        // Exact typed component copies, scoped to this tool instance/world.
        // Not a complete fingerprint of every prefab or lane-generation input.
        private List<object> CaptureOriginalProbeInputs() {
            if (!m_PathDataValid || m_NodeStates.Length > 128 || !CurrentPathCanBeGathered()) { return null; }
            var values = new List<object>();
            var edges = new HashSet<Entity>();
            for (var i = 0; i < m_NodeStates.Length; i++) {
                var entity = m_NodeStates[i].Entity;
                if (!ReadProbeNode(entity, values) || !EntityManager.HasBuffer<ConnectedEdge>(entity)) { return null; }
                var incident = EntityManager.GetBuffer<ConnectedEdge>(entity, true);
                if (incident.Length > 64) { return null; }
                var identities = new List<Entity>();
                foreach (var item in incident) { identities.Add(item.m_Edge); edges.Add(item.m_Edge); }
                identities.Sort(CompareProbeEntities);
                values.Add(identities.Count);
                foreach (var id in identities) { values.Add(id); }
            }
            if (edges.Count > 512) { return null; }
            var ordered = new List<Entity>(edges);
            ordered.Sort(CompareProbeEntities);
            foreach (var entity in ordered) {
                if (!ProbeLive(entity) || !EntityManager.HasComponent<Edge>(entity)
                    || !EntityManager.HasComponent<Curve>(entity)) { return null; }
                var edge = EntityManager.GetComponentData<Edge>(entity);
                var curve = EntityManager.GetComponentData<Curve>(entity);
                var b = curve.m_Bezier;
                if (!math.all(math.isfinite(b.a)) || !math.all(math.isfinite(b.b))
                    || !math.all(math.isfinite(b.c)) || !math.all(math.isfinite(b.d))) { return null; }
                values.Add(entity); values.Add(edge); values.Add(curve);
                AddProbeComponent<PrefabRef>(entity, values);
                AddProbeComponent<Composition>(entity, values);
                AddProbeComponent<PseudoRandomSeed>(entity, values);
                AddProbeComponent<Upgraded>(entity, values);
                AddProbeComponent<Elevation>(entity, values);
                if (!ReadProbeNode(edge.m_Start, values) || !ReadProbeNode(edge.m_End, values)) { return null; }
            }
            return values;
        }

        private bool ProbeLive(Entity entity) => EntityManager.Exists(entity)
            && !EntityManager.HasComponent<Deleted>(entity) && !EntityManager.HasComponent<Temp>(entity);

        private bool ReadProbeNode(Entity entity, List<object> values) {
            if (!ProbeLive(entity) || !EntityManager.HasComponent<Node>(entity)) { return false; }
            var node = EntityManager.GetComponentData<Node>(entity);
            if (!math.all(math.isfinite(node.m_Position))) { return false; }
            values.Add(entity); values.Add(node);
            AddProbeComponent<PrefabRef>(entity, values);
            AddProbeComponent<Elevation>(entity, values);
            return true;
        }

        private void AddProbeComponent<T>(Entity entity, List<object> values) where T : unmanaged, IComponentData {
            var present = EntityManager.HasComponent<T>(entity);
            values.Add(present);
            if (present) { values.Add(EntityManager.GetComponentData<T>(entity)); }
        }

        private static int CompareProbeEntities(Entity a, Entity b) => a.Index != b.Index
            ? a.Index.CompareTo(b.Index) : a.Version.CompareTo(b.Version);

        private string CachedOriginalStatus() => NetworkTools.Geometry.OriginalInputComparison.Compare(
            m_CachedOriginalInputs, CaptureOriginalProbeInputs());

#if IS_DEBUG
        private string OriginalProbeStatus() {
            var current = CaptureOriginalProbeInputs();
            var status = NetworkTools.Geometry.OriginalInputComparison.Observe(
                m_PreviewInputRevision, m_SubmittedPreviewRevision, m_SubmittedOriginalInputs, current);
            if (status == "matches" && current.Count > 0 && m_LastFailureTestRevision != m_PreviewInputRevision) {
                m_LastFailureTestRevision = m_PreviewInputRevision;
                // Deliberately corrupt a COPY, never an ECS component or the real baseline.
                var altered = new List<object>(current);
                var entity = (Entity)altered[0];
                entity.Version = entity.Version == int.MaxValue ? 1 : entity.Version + 1;
                altered[0] = entity;
                var changed = NetworkTools.Geometry.OriginalInputComparison.Observe(
                    m_PreviewInputRevision, m_SubmittedPreviewRevision, m_SubmittedOriginalInputs, altered);
                var missing = NetworkTools.Geometry.OriginalInputComparison.Observe(
                    m_PreviewInputRevision, m_SubmittedPreviewRevision, m_SubmittedOriginalInputs, null);
                UnityEngine.Debug.Log($"[NetworkTools.PreviewFailureTest] revision={m_PreviewInputRevision} copiedEntityVersion={changed} missingCopy={missing} expected=changed,unavailable diagnosticOnly=true");
                if (m_HeldProbeInputs != null && m_HeldProbeRevision != m_PreviewInputRevision) {
                    var stale = NetworkTools.Geometry.OriginalInputComparison.Observe(
                        m_PreviewInputRevision, m_HeldProbeRevision, m_HeldProbeInputs, current);
                    UnityEngine.Debug.Log($"[NetworkTools.PreviewFailureTest] heldRevision={m_HeldProbeRevision} currentRevision={m_PreviewInputRevision} delayedObservation={stale} expected=stale_revision diagnosticOnly=true");
                }
                m_HeldProbeRevision = m_PreviewInputRevision;
                m_HeldProbeInputs = new List<object>(current);
            }
            return status;
        }
#endif
    }
}
