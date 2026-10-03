#if IS_DEBUG
namespace NetworkTools.Systems.Tools.Connect {
    using System.Collections.Generic;
    using Colossal.Entities;
    using Game.Common;
    using Game.Net;
    using Game.Pathfind;
    using Edge = Game.Net.Edge;
    using SubLane = Game.Net.SubLane;
    using CarLane = Game.Net.CarLane;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        // Conservative direct-junction proof, not exclusivity or end-to-end traffic proof.
        // Installed Game 1.6.2f1 LaneSystem:4367-4368/4854-4872 maps original edge lanes
        // by owner-normalized LaneKey. :7280-7282 emits source -> target NodeLane paths.
        // PathNode.Equals compares the entire key (owner, lane/segment, station, secondary).
        // Complex internal-node / roundabout multi-hop routing is deliberately unsupported.
        private static LaneConnectionProof.Port NativeProofPort(PathNode port) => new(
            port.GetOwnerIndex(), port.GetLaneIndex(), port.GetCurvePos(), port.IsSecondary());

        private bool ValidateNativeLaneDirection(out string reason) {
            if (!TryResolveLaneDirectionEndpoints(out var start, out var end, out reason)) return false;
            using var edgeQuery = ControlTempQuery();
            using var edges = edgeQuery.ToEntityArray(Allocator.Temp);
            using var nodeQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Temp>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            });
            using var nodes = nodeQuery.ToEntityArray(Allocator.Temp);
            if (edges.Length == 0 || edges.Length > 256 || nodes.Length > 512) {
                reason = "lane_native_capacity_or_pending"; return false;
            }
            if (!ValidateNativeLaneEndpoint(start, true, edges, nodes, out reason)) {
                reason = "lane_native_start_" + reason; return false;
            }
            if (!ValidateNativeLaneEndpoint(end, false, edges, nodes, out reason)) {
                reason = "lane_native_end_" + reason; return false;
            }
            reason = "accepted"; return true;
        }

        private bool NativeLaneEntityReady(Entity entity) {
            return EntityManager.Exists(entity) && !EntityManager.HasComponent<Deleted>(entity)
                && !EntityManager.HasComponent<Created>(entity) && !EntityManager.HasComponent<Updated>(entity)
                && EntityManager.TryGetComponent<Temp>(entity, out var temp)
                && (temp.m_Flags & (TempFlags.Delete | TempFlags.Cancel | TempFlags.Hidden)) == 0;
        }

        private bool NativeLaneCounterpart(NativeArray<Entity> entities, Entity original, out Entity counterpart) {
            counterpart = Entity.Null;
            foreach (var entity in entities) {
                if (EntityManager.GetComponentData<Temp>(entity).m_Original != original) continue;
                if (counterpart != Entity.Null || !NativeLaneEntityReady(entity)) return false;
                counterpart = entity;
            }
            return counterpart != Entity.Null;
        }

        private bool NativeOrdinaryCarLane(Entity entity, Entity owner, out Lane lane, out PrefabRef prefab) {
            lane = default; prefab = default;
            if (EntityManager.HasComponent<Game.Net.MasterLane>(entity) || !NativeLaneEntityReady(entity)
                || !EntityManager.TryGetComponent<Owner>(entity, out var actualOwner) || actualOwner.m_Owner != owner
                || !EntityManager.TryGetComponent<Lane>(entity, out lane)
                || !EntityManager.TryGetComponent<CarLane>(entity, out var car)
                || !EntityManager.TryGetComponent<PrefabRef>(entity, out prefab)
                || !EntityManager.TryGetComponent<NetLaneData>(prefab.m_Prefab, out var data)
                || !EntityManager.TryGetComponent<CarLaneData>(prefab.m_Prefab, out var carData)) return false;
            const CarLaneFlags unsupportedCar = CarLaneFlags.Twoway | CarLaneFlags.PublicOnly | CarLaneFlags.SideConnection
                | CarLaneFlags.SecondaryStart | CarLaneFlags.SecondaryEnd | CarLaneFlags.Forbidden | CarLaneFlags.Runway;
            const LaneFlags unsupported = LaneFlags.Track | LaneFlags.Pedestrian | LaneFlags.Parking | LaneFlags.Utility
                | LaneFlags.Virtual | LaneFlags.CrossRoad | LaneFlags.Secondary | LaneFlags.Master
                | LaneFlags.Twoway | LaneFlags.PublicOnly | LaneFlags.BicyclesOnly;
            return (car.m_Flags & unsupportedCar) == 0 && (carData.m_RoadTypes & RoadTypes.Car) != 0
                && (data.m_Flags & LaneFlags.Road) != 0 && (data.m_Flags & unsupported) == 0
                && !lane.m_StartNode.IsSecondary() && !lane.m_MiddleNode.IsSecondary() && !lane.m_EndNode.IsSecondary();
        }

        private bool NativeLaneComposition(Entity edge, Lane lane, PrefabRef prefab, out NetCompositionLane matched) {
            matched = default;
            if (!EntityManager.TryGetComponent<Composition>(edge, out var composition)
                || !EntityManager.Exists(composition.m_Edge)
                || !EntityManager.HasBuffer<NetCompositionLane>(composition.m_Edge)) return false;
            int index = lane.m_MiddleNode.GetLaneIndex() & 255;
            if (lane.m_StartNode.GetOwnerIndex() != edge.Index || lane.m_MiddleNode.GetOwnerIndex() != edge.Index
                || lane.m_EndNode.GetOwnerIndex() != edge.Index
                || (lane.m_StartNode.GetLaneIndex() & 255) != index || (lane.m_EndNode.GetLaneIndex() & 255) != index) return false;
            var lanes = EntityManager.GetBuffer<NetCompositionLane>(composition.m_Edge, true);
            int count = 0;
            foreach (var item in lanes) if (item.m_Index == index) {
                if (item.m_Lane != prefab.m_Prefab) return false;
                matched = item; count++;
            }
            const LaneFlags unsupported = LaneFlags.Track | LaneFlags.Pedestrian | LaneFlags.Parking | LaneFlags.Utility
                | LaneFlags.Virtual | LaneFlags.CrossRoad | LaneFlags.Secondary | LaneFlags.Master
                | LaneFlags.Twoway | LaneFlags.PublicOnly | LaneFlags.BicyclesOnly;
            return count == 1 && math.all(math.isfinite(matched.m_Position))
                && (matched.m_Flags & LaneFlags.Road) != 0 && (matched.m_Flags & unsupported) == 0;
        }

        private bool NativeLanePort(Entity laneEntity, Entity edgeEntity, Entity nodeEntity, bool incoming,
            Lane lane, NetCompositionLane composition, out PathNode port) {
            port = default;
            if (!EntityManager.TryGetComponent<Edge>(edgeEntity, out var edge)
                || !EntityManager.TryGetComponent<EdgeLane>(laneEntity, out var edgeLane)) return false;
            bool atStart = edge.m_Start == nodeEntity, atEnd = edge.m_End == nodeEntity;
            if (atStart == atEnd || !math.all(math.isfinite(edgeLane.m_EdgeDelta))) return false;
            float station = atEnd ? 1f : 0f;
            bool laneStart = edgeLane.m_EdgeDelta.x == station, laneEnd = edgeLane.m_EdgeDelta.y == station;
            if (laneStart == laneEnd || incoming != laneEnd
                || laneEnd != (atEnd == ((composition.m_Flags & LaneFlags.Invert) == 0))
                || (composition.m_Flags & (incoming ? LaneFlags.DisconnectedEnd : LaneFlags.DisconnectedStart)) != 0) return false;
            port = incoming ? lane.m_EndNode : lane.m_StartNode;
            return true;
        }

        private bool ValidateNativeLaneEndpoint(LaneDirectionEndpoint endpoint, bool departure,
            NativeArray<Entity> edges, NativeArray<Entity> nodes, out string reason) {
            reason = "counterpart_missing_or_ambiguous";
            if (endpoint.Lanes == null || endpoint.Lanes.Length == 0 || endpoint.Lanes.Length > 64
                || !NativeLaneCounterpart(nodes, endpoint.Node, out var node)
                || !NativeLaneCounterpart(edges, endpoint.Approach, out var approach)) return false;
            if (!EntityManager.HasBuffer<SubLane>(node) || !EntityManager.HasBuffer<SubLane>(approach)) {
                reason = "sublanes_pending"; return false;
            }
            Entity newEdge = Entity.Null;
            foreach (var edgeEntity in edges) {
                if (EntityManager.GetComponentData<Temp>(edgeEntity).m_Original != Entity.Null) continue;
                var edge = EntityManager.GetComponentData<Edge>(edgeEntity);
                if (edge.m_Start != node && edge.m_End != node) continue;
                if (newEdge != Entity.Null || !NativeLaneEntityReady(edgeEntity) || m_ControlPreviousTemps.Contains(edgeEntity)) {
                    reason = "new_edge_ambiguous_or_stale"; return false;
                }
                newEdge = edgeEntity;
            }
            if (newEdge == Entity.Null || !EntityManager.HasBuffer<SubLane>(newEdge)) {
                reason = "new_edge_missing"; return false;
            }
            var newPorts = new List<LaneConnectionProof.Port>();
            var seen = new HashSet<Entity>();
            var newLanes = EntityManager.GetBuffer<SubLane>(newEdge, true);
            if (newLanes.Length > 1024) { reason = "lane_capacity"; return false; }
            foreach (var sub in newLanes) {
                if (!seen.Add(sub.m_SubLane)) { reason = "duplicate_membership"; return false; }
                if (EntityManager.HasComponent<Game.Net.MasterLane>(sub.m_SubLane)) continue;
                if (!EntityManager.HasComponent<CarLane>(sub.m_SubLane) || !EntityManager.HasComponent<EdgeLane>(sub.m_SubLane)) continue;
                if (!NativeOrdinaryCarLane(sub.m_SubLane, newEdge, out var lane, out var prefab)
                    || !NativeLaneComposition(newEdge, lane, prefab, out var composition)) {
                    reason = "new_lane_mapping_unsupported"; return false;
                }
                // Opposite-travel lanes are legitimate, but cannot witness this direction.
                if (!NativeLanePort(sub.m_SubLane, newEdge, node, !departure, lane, composition, out var port)) continue;
                newPorts.Add(NativeProofPort(port));
            }
            if (newPorts.Count == 0) { reason = "new_direction_missing"; return false; }

            var approachLanes = EntityManager.GetBuffer<SubLane>(approach, true);
            var nodeLanes = EntityManager.GetBuffer<SubLane>(node, true);
            if (approachLanes.Length > 1024 || nodeLanes.Length > 4096) { reason = "lane_capacity"; return false; }
            seen.Clear();
            foreach (var sub in approachLanes) if (!seen.Add(sub.m_SubLane)) { reason = "duplicate_membership"; return false; }
            var junctions = new List<LaneConnectionProof.Connection>();
            seen.Clear();

            foreach (var sub in nodeLanes) {
                if (!seen.Add(sub.m_SubLane)) { reason = "duplicate_membership"; return false; }
                if (EntityManager.HasComponent<Game.Net.MasterLane>(sub.m_SubLane)) continue;
                if (!EntityManager.HasComponent<NodeLane>(sub.m_SubLane) || !EntityManager.HasComponent<CarLane>(sub.m_SubLane)) continue;
                if (!NativeOrdinaryCarLane(sub.m_SubLane, node, out var lane, out _)) continue;
                var nodeLane = EntityManager.GetComponentData<NodeLane>(sub.m_SubLane);
                if ((nodeLane.m_Flags & (NodeLaneFlags.StartBicycleOnly | NodeLaneFlags.EndBicycleOnly)) != 0) continue;
                junctions.Add(new LaneConnectionProof.Connection(NativeProofPort(lane.m_StartNode),
                    NativeProofPort(lane.m_MiddleNode), NativeProofPort(lane.m_EndNode)));
            }
            var selectedPorts = new List<LaneConnectionProof.Port>();
            foreach (var originalEntity in endpoint.Lanes) {
                Entity mapped = Entity.Null;
                foreach (var sub in approachLanes) {
                    if (!EntityManager.TryGetComponent<Temp>(sub.m_SubLane, out var temp) || temp.m_Original != originalEntity) continue;
                    if (mapped != Entity.Null) { reason = "selected_lane_mapping_ambiguous"; return false; }
                    mapped = sub.m_SubLane;
                }
                if (mapped == Entity.Null || !NativeOrdinaryCarLane(mapped, approach, out var lane, out var prefab)
                    || !NativeLaneComposition(approach, lane, prefab, out var composition)
                    || !EntityManager.TryGetComponent<Lane>(originalEntity, out var originalLane)
                    || !EntityManager.TryGetComponent<PrefabRef>(originalEntity, out var originalPrefab)
                    || originalPrefab.m_Prefab != prefab.m_Prefab
                    || !NativeLaneComposition(endpoint.Approach, originalLane, originalPrefab, out var originalComposition)) {
                    reason = "selected_lane_mapping_missing"; return false;
                }
                var normalized = lane;
                normalized.m_StartNode.ReplaceOwner(approach, endpoint.Approach);
                normalized.m_MiddleNode.ReplaceOwner(approach, endpoint.Approach);
                normalized.m_EndNode.ReplaceOwner(approach, endpoint.Approach);
                if (!normalized.Equals(originalLane) || composition.m_Index != originalComposition.m_Index
                    || composition.m_Flags != originalComposition.m_Flags || composition.m_Group != originalComposition.m_Group
                    || composition.m_Carriageway != originalComposition.m_Carriageway
                    || !math.all(composition.m_Position == originalComposition.m_Position)) {
                    reason = "selected_lane_identity_changed"; return false;
                }
                if (!NativeLanePort(mapped, approach, node, departure, lane, composition, out var selectedPort)) {
                    reason = "selected_lane_direction_changed"; return false;
                }
                selectedPorts.Add(NativeProofPort(selectedPort));
            }
            if (!LaneConnectionProof.Validate(departure, node.Index, approach.Index, newEdge.Index,
                selectedPorts, newPorts, junctions, out var proofFailure, out var failedSelected)) {
                reason = "direct_proof_" + proofFailure + "_" + failedSelected; return false;
            }
            reason = null; return true;
        }
    }
}
#endif


