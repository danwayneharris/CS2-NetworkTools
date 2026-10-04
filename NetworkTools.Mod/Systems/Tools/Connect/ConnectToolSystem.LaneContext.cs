// NT-003 live endpoint lane adapter; direction/group policy is independently tested.
namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using System.Collections.Generic;
    using Colossal.Entities;
    using Game.Common;
    using Game.Net;
    using Game.Pathfind;
    using Edge = Game.Net.Edge;
    using SubLane = Game.Net.SubLane;
    using CarLane = Game.Net.CarLane;
    using Game.Prefabs;
    using NetworkTools.Systems.Tools.Parameters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        public BoolParameter LaneAwareDirection = new("connect.laneAwareDirection", false,
            label: "NetworkTools.UI.Connect.LaneAwareDirection", persist: false) {
#if !IS_DEBUG
            ValidateValue = value => !value,
#endif
        };

        private sealed class LaneDirectionSelection {
            public Entity Node, Approach;
            public Entity[] Lanes = Array.Empty<Entity>();
            public string SourceIdentity;
        }
        private LaneDirectionSelection m_StartLaneDirection, m_EndLaneDirection;
        private string m_StartLaneChoiceError, m_EndLaneChoiceError;

        public readonly struct LaneDirectionEndpoint {
            public readonly Entity Node, Approach, EdgeComposition, NodeComposition;
            public readonly float3 HandleDirection;
            public readonly double HandleGrade;
            public readonly string SourceIdentity;
            // A copy is returned to prevent callers mutating the stored user selection.
            public readonly Entity[] Lanes;
            public LaneDirectionEndpoint(Entity node, Entity approach, Entity edgeComposition, Entity nodeComposition,
                float3 direction, double grade, string identity, Entity[] lanes) {
                Node = node; Approach = approach; EdgeComposition = edgeComposition; NodeComposition = nodeComposition;
                HandleDirection = direction; HandleGrade = grade; SourceIdentity = identity; Lanes = (Entity[])lanes.Clone();
            }
        }

        private sealed class LaneDirectionChoice {
            public Entity Lane;
            public byte CompositionIndex, Carriageway, Group;
            public float DiagramPosition;
            public float3 HandleDirection;
            public double HandleGrade;
            public bool Incoming;
            public string Reason;
            public JObject Evidence;
        }
        private sealed class LaneDirectionContext {
            public Entity Node, Approach, EdgeComposition, NodeComposition;
            public readonly List<LaneDirectionChoice> Choices = new();
            public readonly List<float> PhysicalRoadPositions = new();
            public string Identity;
        }

        public void ResetLaneDirectionChoices() {
            m_StartLaneDirection = m_EndLaneDirection = null;
            m_StartLaneChoiceError = m_EndLaneChoiceError = null;
        }
        private static JObject LaneEntityJson(Entity entity) => new() { ["index"] = entity.Index, ["version"] = entity.Version };
        private static JArray LaneVector(float3 p) => new(p.x, p.y, p.z);
        private static LaneDirectionPolicy.Point LanePoint(float3 p) => new(p.x, p.y, p.z);
        private static LaneDirectionPolicy.Key LaneKey(Entity entity) => new(entity.Index, entity.Version);
        private static JObject LanePathJson(PathNode path) => new() {
            ["ownerIndex"] = path.GetOwnerIndex(), ["laneAndSegment"] = path.GetLaneIndex(),
            ["curvePosition"] = path.GetCurvePos(), ["secondary"] = path.IsSecondary()
        };
        private bool LaneSourceLive(Entity entity) => entity != Entity.Null && EntityManager.Exists(entity)
            && !EntityManager.HasComponent<Deleted>(entity) && !EntityManager.HasComponent<Game.Tools.Temp>(entity)
            && !EntityManager.HasComponent<Updated>(entity);

        private bool TryLaneDirectionContext(Entity node, Entity approachChoice, bool start,
            out LaneDirectionContext context, out string reason) {
            context = null;
            if (!TryResolveProfileEndpoint(node, approachChoice, out var endpoint, out reason)) return false;
            var approach = endpoint.ApproachEdge;
            if (!LaneSourceLive(node) || !LaneSourceLive(approach)
                || !EntityManager.TryGetComponent<Composition>(approach, out var composition)
                || !EntityManager.TryGetComponent<Edge>(approach, out var edge)
                || !EntityManager.HasBuffer<SubLane>(approach)
                || !EntityManager.Exists(composition.m_Edge)
                || !EntityManager.HasBuffer<NetCompositionLane>(composition.m_Edge)) {
                reason = "lane_context_pending"; return false;
            }
            var atEnd = edge.m_End == node;
            var nodeComposition = atEnd ? composition.m_EndNode : composition.m_StartNode;
            if (!EntityManager.Exists(nodeComposition)) { reason = "lane_composition_missing"; return false; }
            context = new LaneDirectionContext { Node = node, Approach = approach,
                EdgeComposition = composition.m_Edge, NodeComposition = nodeComposition };
            var compositionLanes = EntityManager.GetBuffer<NetCompositionLane>(composition.m_Edge);
            var compositionEvidence = new JArray();
            for (var i = 0; i < compositionLanes.Length; i++) {
                var item = compositionLanes[i];
                if ((item.m_Flags & (LaneFlags.Road | LaneFlags.Master)) == LaneFlags.Road) {
                    if (!math.all(math.isfinite(item.m_Position))) { reason = "lane_source_nonfinite"; return false; }
                    context.PhysicalRoadPositions.Add(atEnd ? item.m_Position.x : -item.m_Position.x);
                }
                compositionEvidence.Add(new JObject { ["prefab"] = LaneEntityJson(item.m_Lane),
                    ["index"] = item.m_Index, ["position"] = LaneVector(item.m_Position), ["flags"] = (int)item.m_Flags,
                    ["carriageway"] = item.m_Carriageway, ["group"] = item.m_Group });
            }
            var sublanes = EntityManager.GetBuffer<SubLane>(approach);
            var seen = new HashSet<Entity>();
            var mappedIndices = new HashSet<byte>();
            var station = atEnd ? 1f : 0f;
            for (var i = 0; i < sublanes.Length; i++) {
                var entity = sublanes[i].m_SubLane;
                if (!seen.Add(entity)) { reason = "lane_membership_ambiguous"; return false; }
                if (EntityManager.HasComponent<Game.Net.MasterLane>(entity)) continue;
                if (!EntityManager.HasComponent<CarLane>(entity) || !EntityManager.HasComponent<EdgeLane>(entity)) continue;
                if (!LaneSourceLive(entity)
                    || !EntityManager.TryGetComponent<Owner>(entity, out var owner) || owner.m_Owner != approach
                    || !EntityManager.TryGetComponent<Curve>(entity, out var curve)
                    || !EntityManager.TryGetComponent<Lane>(entity, out var lane)
                    || !EntityManager.TryGetComponent<PrefabRef>(entity, out var prefab)
                    || !EntityManager.TryGetComponent<NetLaneData>(prefab.m_Prefab, out var lanePrefab)
                    || !EntityManager.TryGetComponent<CarLaneData>(prefab.m_Prefab, out var carPrefab)) {
                    reason = "lane_source_unavailable"; return false;
                }
                var edgeLane = EntityManager.GetComponentData<EdgeLane>(entity);
                if (!math.all(math.isfinite(edgeLane.m_EdgeDelta))) { reason = "lane_source_nonfinite"; return false; }
                // Native LaneSystem compares to exact station 0/1; do not guess a near-end segment.
                var atLaneStart = edgeLane.m_EdgeDelta.x == station;
                var atLaneEnd = edgeLane.m_EdgeDelta.y == station;
                if (!atLaneStart && !atLaneEnd) continue;
                if (atLaneStart == atLaneEnd || !LanePathOwnersValid(edge, approach, lane, edgeLane)) {
                    reason = "lane_endpoint_ambiguous"; return false;
                }
                var index = lane.m_MiddleNode.GetLaneIndex() & 255;
                if ((lane.m_StartNode.GetOwnerIndex() == approach.Index && (lane.m_StartNode.GetLaneIndex() & 255) != index)
                    || (lane.m_EndNode.GetOwnerIndex() == approach.Index && (lane.m_EndNode.GetLaneIndex() & 255) != index)) {
                    reason = "lane_path_mapping_unsupported"; return false;
                }
                var matches = 0; NetCompositionLane matched = default;
                for (var j = 0; j < compositionLanes.Length; j++) {
                    var item = compositionLanes[j];
                    if (item.m_Index != index || item.m_Lane != prefab.m_Prefab) continue;
                    matched = item; ++matches;
                }
                // Variant prefabs/geometric rematching need explicit support; never first-match them.
                if (matches != 1 || !mappedIndices.Add(matched.m_Index)) { reason = "lane_composition_mapping_unsupported"; return false; }
                if (!math.all(math.isfinite(matched.m_Position))) { reason = "lane_source_nonfinite"; return false; }
                var directionValid = LaneDirectionPolicy.TryDirection(start, atEnd, edgeLane.m_EdgeDelta.x, edgeLane.m_EdgeDelta.y,
                    (matched.m_Flags & LaneFlags.Invert) != 0, LanePoint(curve.m_Bezier.a), LanePoint(curve.m_Bezier.b),
                    LanePoint(curve.m_Bezier.c), LanePoint(curve.m_Bezier.d), out var direction, out var directionReason);
                if (directionReason == "lane_composition_direction_mismatch") { reason = directionReason; return false; }
                var flags = matched.m_Flags | lanePrefab.m_Flags;
                var car = EntityManager.GetComponentData<CarLane>(entity);
                var choice = new LaneDirectionChoice { Lane = entity, CompositionIndex = matched.m_Index,
                    Carriageway = matched.m_Carriageway, Group = matched.m_Group,
                    DiagramPosition = atEnd ? matched.m_Position.x : -matched.m_Position.x, Incoming = atLaneEnd };
                const LaneFlags unsupported = LaneFlags.Track | LaneFlags.Pedestrian | LaneFlags.Parking
                    | LaneFlags.Utility | LaneFlags.Virtual | LaneFlags.CrossRoad | LaneFlags.Secondary
                    | LaneFlags.Master | LaneFlags.Twoway | LaneFlags.PublicOnly | LaneFlags.BicyclesOnly;
                const CarLaneFlags unsupportedCar = CarLaneFlags.Twoway | CarLaneFlags.PublicOnly
                    | CarLaneFlags.SideConnection | CarLaneFlags.SecondaryStart | CarLaneFlags.SecondaryEnd
                    | CarLaneFlags.Forbidden | CarLaneFlags.Runway;
                if ((flags & LaneFlags.Road) == 0 || (flags & unsupported) != 0
                    || (car.m_Flags & unsupportedCar) != 0 || (carPrefab.m_RoadTypes & RoadTypes.Car) == 0)
                    choice.Reason = "lane_type_unsupported";
                else if ((flags & (atLaneEnd ? LaneFlags.DisconnectedEnd : LaneFlags.DisconnectedStart)) != 0)
                    choice.Reason = "lane_disconnected";
                if (!directionValid && (choice.Reason == null || directionReason == "lane_tangent_invalid")) choice.Reason = directionReason;
                choice.HandleDirection = new float3((float)direction.X, 0f, (float)direction.Z);
                choice.HandleGrade = direction.Grade;
                choice.Evidence = new JObject { ["lane"] = LaneEntityJson(entity), ["prefab"] = LaneEntityJson(prefab.m_Prefab),
                    ["index"] = matched.m_Index, ["position"] = choice.DiagramPosition,
                    ["carriageway"] = matched.m_Carriageway, ["group"] = matched.m_Group,
                    ["flags"] = (int)flags, ["carFlags"] = (uint)car.m_Flags, ["roadTypes"] = (int)carPrefab.m_RoadTypes,
                    ["edgeDelta"] = new JArray(edgeLane.m_EdgeDelta.x, edgeLane.m_EdgeDelta.y),
                    ["pathStart"] = LanePathJson(lane.m_StartNode), ["pathMiddle"] = LanePathJson(lane.m_MiddleNode),
                    ["pathEnd"] = LanePathJson(lane.m_EndNode),
                    ["curve"] = new JArray(LaneVector(curve.m_Bezier.a), LaneVector(curve.m_Bezier.b), LaneVector(curve.m_Bezier.c), LaneVector(curve.m_Bezier.d)),
                    ["incoming"] = atLaneEnd, ["axis"] = LaneVector(choice.HandleDirection), ["handleGrade"] = choice.HandleGrade, ["reason"] = choice.Reason };
                context.Choices.Add(choice);
            }
            context.Choices.Sort((a, b) => {
                var compare = a.DiagramPosition.CompareTo(b.DiagramPosition);
                return compare != 0 ? compare : a.CompositionIndex.CompareTo(b.CompositionIndex);
            });
            for (var i = 1; i < context.Choices.Count; i++)
                if (math.abs(context.Choices[i].DiagramPosition - context.Choices[i - 1].DiagramPosition) <= 1e-4f) {
                    reason = "lane_lateral_order_ambiguous"; return false;
                }
            var evidence = new JArray();
            foreach (var choice in context.Choices) evidence.Add(choice.Evidence.DeepClone());
            context.Identity = new JObject { ["node"] = LaneEntityJson(node), ["edge"] = LaneEntityJson(approach),
                ["edgeComposition"] = LaneEntityJson(composition.m_Edge), ["nodeComposition"] = LaneEntityJson(nodeComposition),
                ["nodePosition"] = LaneVector(endpoint.NodePosition), ["approachDirection"] = LaneVector(endpoint.OutwardDirection),
                ["composition"] = compositionEvidence, ["lanes"] = evidence }.ToString(Formatting.None);
            reason = context.Choices.Count == 0 ? "lane_choices_unavailable" : null;
            return reason == null;
        }

        // LaneReferencesSystem:237-307 can replace an edge endpoint with a shared
        // node-owned port after skipping a degree-two junction lane. The middle
        // remains edge-owned and carries the composition lane/segment identity.
        private static bool LanePathOwnersValid(Edge edge, Entity owner, Lane lane, EdgeLane delta) {
            if (lane.m_MiddleNode.GetOwnerIndex() != owner.Index) return false;
            bool Endpoint(PathNode port, float station) {
                if (port.GetOwnerIndex() == owner.Index) return true;
                return station == 0f ? port.GetOwnerIndex() == edge.m_Start.Index
                    : station == 1f && port.GetOwnerIndex() == edge.m_End.Index;
            }
            return Endpoint(lane.m_StartNode, delta.m_EdgeDelta.x) && Endpoint(lane.m_EndNode, delta.m_EdgeDelta.y);
        }

        private bool TryLaneDirectionGroup(LaneDirectionContext context, Entity[] selected,
            out LaneDirectionEndpoint endpoint, out string reason) {
            endpoint = default;
            var choices = new List<LaneDirectionPolicy.Choice>();
            foreach (var choice in context.Choices) choices.Add(new LaneDirectionPolicy.Choice(LaneKey(choice.Lane),
                choice.DiagramPosition, choice.Carriageway, choice.Group,
                new LaneDirectionPolicy.Direction(choice.HandleDirection.x, choice.HandleDirection.z, choice.HandleGrade, choice.Incoming), choice.Reason));
            var positions = new List<double>();
            foreach (var position in context.PhysicalRoadPositions) positions.Add(position);
            var keys = new List<LaneDirectionPolicy.Key>();
            if (selected != null) foreach (var entity in selected) keys.Add(LaneKey(entity));
            if (!LaneDirectionPolicy.TryGroup(choices, positions, keys, out var representative, out reason)) return false;
            endpoint = new LaneDirectionEndpoint(context.Node, context.Approach, context.EdgeComposition,
                context.NodeComposition, new float3((float)representative.Direction.X, 0f, (float)representative.Direction.Z),
                representative.Direction.Grade, context.Identity, selected);
            return true;
        }

        private bool TryCurrentLaneDirection(bool start, out LaneDirectionEndpoint endpoint, out string reason) {
            endpoint = default;
            var node = start ? StartNode : EndNode;
            if (!TryLaneDirectionContext(node, ProfileChoice(node, start), start, out var context, out reason)) return false;
            var selection = start ? m_StartLaneDirection : m_EndLaneDirection;
            if (selection == null) { reason = "lane_choice_required"; return false; }
            if (selection.Node != node || selection.Approach != context.Approach || selection.SourceIdentity != context.Identity) {
                reason = "lane_choice_stale"; return false;
            }
            return TryLaneDirectionGroup(context, selection.Lanes, out endpoint, out reason);
        }

        public bool TryResolveLaneDirectionEndpoints(out LaneDirectionEndpoint start, out LaneDirectionEndpoint end, out string reason) {
            start = end = default;
            if (Mode.Value != ConnectMode.SimpleCurve && Mode.Value != ConnectMode.ComplexCurve) {
                reason = "lane_mode_unsupported"; return false;
            }
            return TryCurrentLaneDirection(true, out start, out reason) && TryCurrentLaneDirection(false, out end, out reason);
        }

        private static bool TryLaneEntities(JToken token, out Entity[] entities) {
            entities = null;
            if (token is not JArray array || array.Count == 0 || array.Count > 64) return false;
            entities = new Entity[array.Count];
            for (var i = 0; i < array.Count; i++) if (!TryProfileEntity(array[i], out entities[i])) return false;
            return true;
        }

        public bool TryPrepareLaneDirectionOptions(JObject args, ConnectMode requestedMode, out Action apply, out string reason) {
            apply = null; reason = null;
            var enabled = LaneAwareDirection.Value;
            if (args.TryGetValue("laneAwareDirection", out var token)) {
                if (token.Type != JTokenType.Boolean) { reason = "invalid_lane_aware_direction"; return false; }
                enabled = token.Value<bool>();
            }
#if !IS_DEBUG
            if (enabled) { reason = "lane_direction_unavailable"; return false; }
#endif
            if (enabled && requestedMode != ConnectMode.SimpleCurve && requestedMode != ConnectMode.ComplexCurve) {
                reason = "lane_mode_unsupported"; return false;
            }
            var prepared = new LaneDirectionSelection[2];
            for (var i = 0; i < 2; i++) {
                var start = i == 0; var node = start ? StartNode : EndNode;
                var approach = ProfileChoice(node, start);
                var approachToken = args[start ? "startApproach" : "endApproach"];
                if (approachToken != null && !TryProfileEntity(approachToken, out approach)) { reason = "invalid_lane_approach"; return false; }
                var selectedToken = args[start ? "startLanes" : "endLanes"];
                var previous = start ? m_StartLaneDirection : m_EndLaneDirection;
                Entity[] selected = previous?.Lanes;
                if (selectedToken != null && !TryLaneEntities(selectedToken, out selected)) { reason = "invalid_lane_choices"; return false; }
                if (!enabled && selectedToken == null) { prepared[i] = approachToken == null ? previous : null; continue; }
                if (!TryLaneDirectionContext(node, approach, start, out var context, out reason)) return false;
                if (selectedToken == null && previous != null && (previous.Node != node || previous.Approach != context.Approach
                    || previous.SourceIdentity != context.Identity)) { reason = "lane_choice_stale"; return false; }
                if (!TryLaneDirectionGroup(context, selected, out _, out reason)) return false;
                prepared[i] = new LaneDirectionSelection { Node = node, Approach = context.Approach,
                    Lanes = (Entity[])selected.Clone(), SourceIdentity = context.Identity };
            }
            apply = () => {
                m_StartLaneDirection = prepared[0]; m_EndLaneDirection = prepared[1];
                m_StartLaneChoiceError = m_EndLaneChoiceError = null;
                LaneAwareDirection.Value = enabled; m_UpdateNeeded = true;
            };
            return true;
        }

        // UI can stage one endpoint while the other is unresolved. It cannot manufacture valid context.
        public bool SetLaneDirectionChoiceJson(string json) {
#if IS_DEBUG
            if (m_ToolSystem.activeTool != this || Phase != OperationPhase.Ready || string.IsNullOrWhiteSpace(json)
                || (Mode.Value != ConnectMode.SimpleCurve && Mode.Value != ConnectMode.ComplexCurve)) return false;
            JObject args;
            try { args = JObject.Parse(json); } catch (JsonException) { return false; }
            if (args["endpoint"]?.Type != JTokenType.Integer || !int.TryParse(args["endpoint"].ToString(), out var ordinal)
                || (ordinal != 0 && ordinal != 1) || !TryProfileEntity(args["node"], out var node)
                || !TryProfileEntity(args["edge"], out var edge)) return false;
            var clear = args["lanes"] is JArray empty && empty.Count == 0;
            Entity[] selected = Array.Empty<Entity>();
            if (!clear && !TryLaneEntities(args["lanes"], out selected)) return false;
            var start = ordinal == 0;
            if (node != (start ? StartNode : EndNode)
                || !TryLaneDirectionContext(node, ProfileChoice(node, start), start, out var context, out _)
                || edge != context.Approach) return false;
            if (!clear && !TryLaneDirectionGroup(context, selected, out _, out var groupReason)) {
                if (start) m_StartLaneChoiceError = groupReason; else m_EndLaneChoiceError = groupReason;
                return false;
            }
            var choice = clear ? null : new LaneDirectionSelection { Node = node, Approach = edge,
                Lanes = selected, SourceIdentity = context.Identity };
            if (start) { m_StartLaneDirection = choice; m_StartLaneChoiceError = null; }
            else { m_EndLaneDirection = choice; m_EndLaneChoiceError = null; }
            m_UpdateNeeded = true; return true;
#else
            return false;
#endif
        }

        public string LaneDirectionChoicesJson() {
            var endpoints = new JArray();
#if IS_DEBUG
            for (var i = 0; i < 2; i++) {
                var start = i == 0; var node = start ? StartNode : EndNode;
                var valid = TryLaneDirectionContext(node, ProfileChoice(node, start), start, out var context, out var reason);
                var selected = start ? m_StartLaneDirection : m_EndLaneDirection;
                var fresh = valid && selected != null && selected.Node == node && selected.Approach == context.Approach
                    && selected.SourceIdentity == context.Identity;
                var choices = new JArray();
                if (valid) foreach (var choice in context.Choices) {
                    var entry = (JObject)choice.Evidence.DeepClone();
                    entry["eligible"] = choice.Reason == null;
                    entry["selected"] = fresh && Array.IndexOf(selected.Lanes, choice.Lane) >= 0;
                    choices.Add(entry);
                }
                endpoints.Add(new JObject { ["endpoint"] = i, ["node"] = LaneEntityJson(node),
                    ["approach"] = LaneEntityJson(context?.Approach ?? Entity.Null),
                    ["role"] = start ? "incoming_to_departure" : "arrival_to_outgoing",
                    ["frame"] = "looking_from_junction_out_along_approach", ["choices"] = choices,
                    ["choiceError"] = start ? m_StartLaneChoiceError : m_EndLaneChoiceError,
                    ["reason"] = valid ? (fresh ? null : selected == null ? "lane_choice_required" : "lane_choice_stale") : reason });
            }
#endif
            return endpoints.ToString(Formatting.None);
        }

        public string LaneDirectionContextIdentity {
            get {
                if (!LaneAwareDirection.Value) return "lane_direction_disabled";
                var snapshots = new JArray();
                for (var i = 0; i < 2; i++) {
                    var start = i == 0; var node = start ? StartNode : EndNode;
                    var valid = TryLaneDirectionContext(node, ProfileChoice(node, start), start, out var context, out var reason);
                    var selected = start ? m_StartLaneDirection : m_EndLaneDirection;
                    var lanes = new JArray();
                    if (selected != null) foreach (var lane in selected.Lanes) lanes.Add(LaneEntityJson(lane));
                    snapshots.Add(new JObject { ["context"] = valid ? context.Identity : reason,
                        ["node"] = LaneEntityJson(node), ["approach"] = LaneEntityJson(ProfileChoice(node, start)),
                        ["selected"] = lanes, ["selectedContext"] = selected?.SourceIdentity });
                }
                return new JObject { ["enabled"] = LaneAwareDirection.Value, ["endpoints"] = snapshots }.ToString(Formatting.None);
            }
        }
    }
}
