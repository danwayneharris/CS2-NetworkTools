#if IS_DEBUG
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

        // Exact typed component copies, scoped to this tool instance/world.
        // Not a complete fingerprint of every prefab or lane-generation input.
        private List<object> CaptureOriginalProbeInputs() {
            if (!m_PathDataValid || m_NodeStates.Length > 128) { return null; }
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

        private string OriginalProbeStatus() {
            var current = CaptureOriginalProbeInputs();
            return NetworkTools.Geometry.OriginalInputComparison.Compare(m_SubmittedOriginalInputs, current);
        }
    }
}
#endif
