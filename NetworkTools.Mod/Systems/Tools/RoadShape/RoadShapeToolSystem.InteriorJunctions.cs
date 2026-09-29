#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;
    using Game.Common;
    using Game.Net;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using PathNode = Game.Pathfind.PathNode;

    public partial class NT_RoadShapeToolSystem {
        private sealed class InteriorJunction {
            public Entity Node;
            public readonly List<Entity> Edges = new();
            public HashSet<(byte, PathNode, PathNode)> Required;
        }
        private readonly List<InteriorJunction> m_InteriorJunctions = new();
        private long m_InteriorRevision = -1;
        private int m_InteriorSubmission;
        private int m_InteriorStableFrames;
        private int m_InteriorWaitFrames;
        private bool m_InteriorFailed;
        private bool m_InteriorAccepted;

        private void ConfigureInteriorJunctions(ref ShapeJobConfig config) {
            if (config.Template != ShapeTransformTemplate.CurveSmooth) return;
            if (m_InteriorRevision != m_PreviewInputRevision) {
                m_InteriorRevision = m_PreviewInputRevision;
                m_InteriorJunctions.Clear();
                m_InteriorFailed = false;
                m_InteriorAccepted = false;
                m_InteriorSubmission = 0;
                m_InteriorStableFrames = 0;
                m_InteriorWaitFrames = 0;
                for (var i = 1; i < m_NodeStates.Length - 1; i++) {
                    var state = m_NodeStates[i];
                    if (!state.SmoothPinned) continue;
                    if (!EntityManager.HasBuffer<ConnectedEdge>(state.Entity)) { m_InteriorFailed = true; break; }
                    var edges = EntityManager.GetBuffer<ConnectedEdge>(state.Entity, true);
                    if (edges.Length < 3 || edges.Length > 8) { m_InteriorFailed = true; break; }
                    var junction = new InteriorJunction { Node = state.Entity };
                    var owners = new Dictionary<int, Entity>();
                    foreach (var edge in edges) {
                        if (!EntityManager.Exists(edge.m_Edge) || owners.ContainsKey(edge.m_Edge.Index)) {
                            m_InteriorFailed = true; break;
                        }
                        owners.Add(edge.m_Edge.Index, edge.m_Edge);
                        junction.Edges.Add(edge.m_Edge);
                    }
                    if (m_InteriorFailed) break;
                    junction.Required = ReadInteriorConnections(junction.Node, owners);
                    if (junction.Required == null) { m_InteriorFailed = true; break; }
                    m_InteriorJunctions.Add(junction);
                }
            }
            config.AllowInteriorJunctions = !m_InteriorFailed && m_InteriorJunctions.Count > 0;
        }

        private HashSet<(byte, PathNode, PathNode)> ReadInteriorConnections(Entity node, Dictionary<int, Entity> owners) {
            if (!EntityManager.Exists(node) || EntityManager.HasComponent<Deleted>(node)
                || !EntityManager.HasBuffer<SubLane>(node)) return null;
            var lanes = EntityManager.GetBuffer<SubLane>(node, true);
            if (lanes.Length > 512) return null;
            var result = new HashSet<(byte, PathNode, PathNode)>();
            foreach (var sub in lanes) {
                var entity = sub.m_SubLane;
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) return null;
                var track = EntityManager.HasComponent<TrackLane>(entity);
                var car = EntityManager.HasComponent<CarLane>(entity);
                if (!track && !car) continue;
                if (!EntityManager.HasComponent<Lane>(entity)) return null;
                var lane = EntityManager.GetComponentData<Lane>(entity);
                var a = lane.m_StartNode; var b = lane.m_EndNode;
                // Unsupported connector ownership is explicit, never an empty set.
                if (!owners.TryGetValue(a.GetOwnerIndex(), out var source)
                    || !owners.TryGetValue(b.GetOwnerIndex(), out var target)) return null;
                a.SetOwner(source); b.SetOwner(target);
                if (track) result.Add(((byte)1, a.StripCurvePos(), b.StripCurvePos()));
                if (car) result.Add(((byte)2, a.StripCurvePos(), b.StripCurvePos()));
            }
            return result;
        }

        private bool InteriorBaselineMatches() {
            foreach (var junction in m_InteriorJunctions) {
                var owners = new Dictionary<int, Entity>();
                foreach (var edge in junction.Edges) owners.Add(edge.Index, edge);
                var current = ReadInteriorConnections(junction.Node, owners);
                if (current == null || !junction.Required.SetEquals(current)) return false;
            }
            return true;
        }

        private bool InteriorJunctionsAllowApply() => m_InteriorRevision == m_PreviewInputRevision
            && !m_InteriorFailed && (m_InteriorJunctions.Count == 0
                || (m_InteriorAccepted && m_InteriorSubmission == m_SmoothTraceId && InteriorBaselineMatches()));

        private void ObserveInteriorJunctions(int submission, bool fresh) {
            if (m_InteriorFailed || m_InteriorRevision != m_PreviewInputRevision || m_InteriorJunctions.Count == 0) return;
            if (m_InteriorSubmission != submission) {
                m_InteriorSubmission = submission;
                m_InteriorStableFrames = 0;
                m_InteriorWaitFrames = 0;
                m_InteriorAccepted = false;
            }
            if (!fresh) { m_InteriorAccepted = false; m_InteriorStableFrames = 0; return; }
            if (!InteriorBaselineMatches()) { RejectInterior("baseline connections changed"); return; }
            var maps = new Dictionary<Entity, Entity>();
            var needed = new HashSet<Entity>();
            foreach (var junction in m_InteriorJunctions) foreach (var edge in junction.Edges) needed.Add(edge);
            using (var query = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Curve>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            })) {
                if (query.CalculateEntityCount() > 4096) { WaitInterior(); return; }
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities) {
                    var original = EntityManager.GetComponentData<Temp>(entity).m_Original;
                    if (!needed.Contains(original)) continue;
                    if (maps.ContainsKey(original)) { WaitInterior(); return; }
                    maps.Add(original, entity);
                }
            }
            if (maps.Count != needed.Count) { WaitInterior(); return; }
            foreach (var junction in m_InteriorJunctions) {
                HashSet<Entity> shared = null;
                var owners = new Dictionary<int, Entity>();
                foreach (var original in junction.Edges) {
                    var temporary = maps[original];
                    var edge = EntityManager.GetComponentData<Edge>(temporary);
                    var endpoints = new HashSet<Entity> { edge.m_Start, edge.m_End };
                    if (shared == null) shared = endpoints; else shared.IntersectWith(endpoints);
                    owners[temporary.Index] = original; owners[original.Index] = original;
                    var selected = false;
                    foreach (var state in m_EdgeStates) if (state.EdgeEntity == original) { selected = true; break; }
                    if (!selected) {
                        var a = EntityManager.GetComponentData<Curve>(temporary).m_Bezier;
                        var b = EntityManager.GetComponentData<Curve>(original).m_Bezier;
                        if (!SameControl(a.a,b.a) || !SameControl(a.b,b.b) || !SameControl(a.c,b.c) || !SameControl(a.d,b.d)) {
                            RejectInterior("unselected curve changed"); return;
                        }
                    }
                }
                if (shared == null || shared.Count != 1) { WaitInterior(); return; }
                Entity candidate = Entity.Null;
                foreach (var node in shared) candidate = node;
                if (!EntityManager.HasComponent<Temp>(candidate)) { WaitInterior(); return; }
                var actual = ReadInteriorConnections(candidate, owners);
                if (actual == null || !junction.Required.SetEquals(actual)) { WaitInterior(); return; }
            }
            if (++m_InteriorStableFrames >= 3) {
                if (!m_InteriorAccepted) UnityEngine.Debug.Log($"[NetworkTools.InteriorJunction] accepted submission={submission} junctions={m_InteriorJunctions.Count}");
                m_InteriorAccepted = true;
            }
        }

        private void WaitInterior() {
            m_InteriorAccepted = false; m_InteriorStableFrames = 0;
            if (++m_InteriorWaitFrames >= 120) RejectInterior("missing, ambiguous or changed native connections");
        }
        private void RejectInterior(string reason) {
            m_InteriorFailed = true; m_InteriorAccepted = false;
            UnityEngine.Debug.Log("[NetworkTools.InteriorJunction] rejected: " + reason);
        }
    }
}
#endif
