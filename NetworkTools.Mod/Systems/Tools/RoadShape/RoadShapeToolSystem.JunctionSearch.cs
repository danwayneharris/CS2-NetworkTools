#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using System.Collections.Generic;
    using Game.Common;
    using Game.Net;
    using PathNode = Game.Pathfind.PathNode;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;

    // Experimental native-preview search. Never applies a candidate automatically.
    public partial class NT_RoadShapeToolSystem {
        private long m_JunctionRevision = -1;
        private Entity m_Junction;
        private bool m_JunctionAtStart;
        private int m_JunctionAttempt;
        private int m_JunctionObservedId;
        private int m_JunctionStableFrames;
        private int m_JunctionWaitFrames;
        private bool m_JunctionAccepted;
        private bool m_JunctionFailed;
        private readonly List<Entity> m_JunctionEdges = new();
        private HashSet<(PathNode, PathNode)> m_JunctionRequired;
        private List<object> m_JunctionOriginalInputs;
        private HashSet<(PathNode, PathNode)> m_JunctionLastConnections;

        private double JunctionRotation => m_JunctionAttempt == 0 ? 0
            : ((m_JunctionAttempt + 1) / 2) * (m_JunctionAttempt % 2 == 1 ? 1 : -1) * Math.PI / 180;

        private void ConfigureJunctionSearch(ref ShapeJobConfig config) {
            if (config.Template != ShapeTransformTemplate.CurveSmooth) { return; }
            if (m_JunctionRevision != m_PreviewInputRevision) {
                m_JunctionRevision = m_PreviewInputRevision;
                m_Junction = Entity.Null;
                m_JunctionAttempt = 0;
                m_JunctionAccepted = false;
                m_JunctionFailed = false;
                m_JunctionObservedId = 0;
                m_JunctionStableFrames = 0;
                m_JunctionWaitFrames = 0;
                m_JunctionEdges.Clear();
                m_JunctionRequired = null;
                m_JunctionLastConnections = null;
                m_JunctionOriginalInputs = CaptureOriginalProbeInputs();
                for (var i = 0; i < m_NodeStates.Length; i++) {
                    if (i != 0 && i != m_NodeStates.Length - 1) { continue; }
                    var node = m_NodeStates[i].Entity;
                    if (!EntityManager.HasBuffer<ConnectedEdge>(node)) { continue; }
                    var incident = EntityManager.GetBuffer<ConnectedEdge>(node, true);
                    if (incident.Length <= 2) { continue; }
                    // Only rail junctions participate in this first native experiment.
                    var ids = new Dictionary<int, Entity>();
                    foreach (var e in incident) { ids[e.m_Edge.Index] = e.m_Edge; }
                    var required = ReadJunctionConnections(node, ids);
                    if (required == null) { m_JunctionFailed = true; continue; }
                    if (required.Count == 0) { continue; }
                    if (m_Junction != Entity.Null || incident.Length != 3) {
                        m_JunctionFailed = true; continue;
                    }
                    m_Junction = node;
                    m_JunctionAtStart = i == 0;
                    m_JunctionRequired = required;
                    foreach (var e in incident) { m_JunctionEdges.Add(e.m_Edge); }
                }
                if (m_Junction != Entity.Null && m_JunctionOriginalInputs == null) { m_JunctionFailed = true; }
                if (m_Junction != Entity.Null || m_JunctionFailed)
                    UnityEngine.Debug.Log($"[NetworkTools.JunctionSearch] initialized revision={m_JunctionRevision} junction={m_Junction} rejected={m_JunctionFailed} required={m_JunctionRequired?.Count ?? 0}");
            }
            if (m_JunctionAtStart) { config.JunctionStartRotation = JunctionRotation; }
            else { config.JunctionEndRotation = JunctionRotation; }
        }

        private HashSet<(PathNode, PathNode)> ReadJunctionConnections(Entity junction, Dictionary<int, Entity> owners) {
            if (!EntityManager.Exists(junction) || !EntityManager.HasBuffer<SubLane>(junction)) { return null; }
            var result = new HashSet<(PathNode, PathNode)>();
            var lanes = EntityManager.GetBuffer<SubLane>(junction, true);
            if (lanes.Length > 512) { return null; }
            foreach (var sub in lanes) {
                var laneEntity = sub.m_SubLane;
                if (!EntityManager.Exists(laneEntity) || EntityManager.HasComponent<Deleted>(laneEntity)) { return null; }
                if (!EntityManager.HasComponent<TrackLane>(laneEntity)) { continue; }
                if (!EntityManager.HasComponent<Lane>(laneEntity)) { return null; }
                var lane = EntityManager.GetComponentData<Lane>(laneEntity);
                var a = lane.m_StartNode; var b = lane.m_EndNode;
                if (!owners.TryGetValue(a.GetOwnerIndex(), out var source)
                    || !owners.TryGetValue(b.GetOwnerIndex(), out var target)) { return null; }
                a.SetOwner(source); b.SetOwner(target);
                result.Add((a.StripCurvePos(), b.StripCurvePos()));
            }
            return result;
        }

        private bool JunctionSearchAllowsApply() {
            if (!InteriorJunctionsAllowApply()) { return false; }
            if (m_JunctionRevision != m_PreviewInputRevision || m_JunctionFailed) { return false; }
            if (m_Junction == Entity.Null) { return true; }
            return m_JunctionAccepted && m_JunctionObservedId == m_SmoothTraceId
                && BaselineConnectionsMatch()
                && NetworkTools.Geometry.OriginalInputComparison.Compare(m_JunctionOriginalInputs, CaptureOriginalProbeInputs()) == "matches";
        }

        private bool BaselineConnectionsMatch() {
            var owners = new Dictionary<int, Entity>();
            foreach (var e in m_JunctionEdges) { owners.Add(e.Index, e); }
            var actual = ReadJunctionConnections(m_Junction, owners);
            return actual != null && m_JunctionRequired.SetEquals(actual);
        }

        private void ObserveJunctionSearch(int id, bool fresh) {
            if (m_Junction == Entity.Null || m_JunctionFailed || m_JunctionRevision != m_PreviewInputRevision) { return; }
            if (m_JunctionObservedId != id) {
                m_JunctionObservedId = id;
                m_JunctionStableFrames = 0;
                m_JunctionWaitFrames = 0;
                m_JunctionAccepted = false;
                m_JunctionLastConnections = null;
            }
            if (!BaselineConnectionsMatch() || NetworkTools.Geometry.OriginalInputComparison.Compare(m_JunctionOriginalInputs, CaptureOriginalProbeInputs()) != "matches") {
                m_JunctionFailed = true; m_JunctionAccepted = false;
                UnityEngine.Debug.Log("[NetworkTools.JunctionSearch] rejected: original inputs changed");
                return;
            }
            if (!fresh) { m_JunctionAccepted = false; m_JunctionStableFrames = 0; return; }
            var maps = new Dictionary<Entity, Entity>();
            using (var query = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Temp>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            })) {
                if (query.CalculateEntityCount() > 4096) { return; }
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var e in entities) {
                    var original = EntityManager.GetComponentData<Temp>(e).m_Original;
                    if (!m_JunctionEdges.Contains(original)) { continue; }
                    if (maps.ContainsKey(original)) { WaitForJunction(); return; }
                    maps.Add(original, e);
                }
            }
            if (maps.Count != m_JunctionEdges.Count) { WaitForJunction(); return; }
            HashSet<Entity> shared = null;
            var owners = new Dictionary<int, Entity>();
            foreach (var pair in maps) {
                var edge = EntityManager.GetComponentData<Edge>(pair.Value);
                var selected = false;
                foreach (var state in m_EdgeStates) { if (state.EdgeEntity == pair.Key) { selected = true; break; } }
                if (!selected) {
                    if (!EntityManager.HasComponent<Curve>(pair.Value) || !EntityManager.HasComponent<Curve>(pair.Key)) { WaitForJunction(); return; }
                    var a = EntityManager.GetComponentData<Curve>(pair.Value).m_Bezier;
                    var b = EntityManager.GetComponentData<Curve>(pair.Key).m_Bezier;
                    if (!SameControl(a.a,b.a) || !SameControl(a.b,b.b) || !SameControl(a.c,b.c) || !SameControl(a.d,b.d)) {
                        m_JunctionFailed = true; m_JunctionAccepted = false;
                        UnityEngine.Debug.Log("[NetworkTools.JunctionSearch] rejected: unselected curve changed");
                        return;
                    }
                }
                var endpoints = new HashSet<Entity> { edge.m_Start, edge.m_End };
                if (shared == null) { shared = endpoints; } else { shared.IntersectWith(endpoints); }
                owners.Add(pair.Value.Index, pair.Key);
                owners[pair.Key.Index] = pair.Key;
            }
            if (shared == null || shared.Count != 1) { WaitForJunction(); return; }
            Entity temporary = Entity.Null;
            foreach (var e in shared) { temporary = e; }
            if (!EntityManager.HasComponent<Temp>(temporary)) { WaitForJunction(); return; }
            var connections = ReadJunctionConnections(temporary, owners);
            if (connections == null) { WaitForJunction(); return; }
            // Require repeated observations after curve/revision freshness checks.
            if (m_JunctionLastConnections == null || !m_JunctionLastConnections.SetEquals(connections)) {
                m_JunctionAccepted = false;
                m_JunctionStableFrames = 0;
                m_JunctionLastConnections = connections;
            }
            if (++m_JunctionStableFrames < 3) { return; }
            if (m_JunctionRequired.IsSubsetOf(connections)) {
                if (!m_JunctionAccepted) UnityEngine.Debug.Log($"[NetworkTools.JunctionSearch] accepted submission={id} attempt={m_JunctionAttempt} rotationDegrees={JunctionRotation * 180 / Math.PI} required={m_JunctionRequired.Count} observed={connections.Count}");
                m_JunctionAccepted = true;
                return;
            }
            m_JunctionAccepted = false;
            if (m_JunctionAttempt >= 30 || SmoothingFactor.Value == 0) {
                m_JunctionFailed = true;
                UnityEngine.Debug.Log("[NetworkTools.JunctionSearch] rejected: no verified candidate within +/-15 degrees");
                return;
            }
            ++m_JunctionAttempt;
            m_UpdateNeeded = true; // Same user revision; next job has a new submission ID.
            UnityEngine.Debug.Log($"[NetworkTools.JunctionSearch] retry attempt={m_JunctionAttempt}");
        }

        private void WaitForJunction() {
            m_JunctionAccepted = false;
            m_JunctionStableFrames = 0;
            if (++m_JunctionWaitFrames >= 120) {
                m_JunctionFailed = true;
                UnityEngine.Debug.Log("[NetworkTools.JunctionSearch] rejected: missing or ambiguous preview junction");
            }
        }
    }
}
#endif
