namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using System.Collections.Generic;
    using Colossal.Entities;
    using Game.Net;
    using Game.Prefabs;
    using Newtonsoft.Json.Linq;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        private Entity m_ProfileStartApproach, m_ProfileEndApproach;
        private Entity m_ProfileStartNode, m_ProfileEndNode;

        public readonly struct ProfileEndpoint {
            public readonly Entity Node, ApproachEdge, Prefab;
            public readonly float3 NodePosition, CurveEndpoint, OutwardDirection;
            public readonly double OutwardGrade;
            public readonly float2 StructuralElevation;
            public readonly bool HasStructuralElevation;
            public ProfileEndpoint(Entity node, Entity edge, Entity prefab, float3 position, float3 endpoint,
                float3 direction, double grade, float2 elevation, bool hasElevation) {
                Node = node; ApproachEdge = edge; Prefab = prefab; NodePosition = position;
                CurveEndpoint = endpoint; OutwardDirection = direction; OutwardGrade = grade;
                StructuralElevation = elevation; HasStructuralElevation = hasElevation;
            }
        }

        public void ResetProfileApproaches() {
            m_ProfileStartNode = m_ProfileEndNode = m_ProfileStartApproach = m_ProfileEndApproach = Entity.Null;
        }

        private Entity ProfileChoice(Entity node, bool start) => start
            ? (m_ProfileStartNode == node ? m_ProfileStartApproach : Entity.Null)
            : (m_ProfileEndNode == node ? m_ProfileEndApproach : Entity.Null);

        public string ProfileContextIdentity => $"{SmoothElevationProfile.Value}:{StartNode}:{ProfileChoice(StartNode, true)}:{EndNode}:{ProfileChoice(EndNode, false)}";

        private bool TryReadProfileEndpoint(Entity nodeEntity, Entity edgeEntity, out ProfileEndpoint endpoint) {
            endpoint = default;
            if (!EntityManager.Exists(nodeEntity) || !EntityManager.Exists(edgeEntity)
                || EntityManager.HasComponent<Game.Common.Deleted>(nodeEntity)
                || EntityManager.HasComponent<Game.Common.Deleted>(edgeEntity)
                || !EntityManager.TryGetComponent<Node>(nodeEntity, out var node)
                || !EntityManager.TryGetComponent<Edge>(edgeEntity, out var edge)
                || !EntityManager.TryGetComponent<Curve>(edgeEntity, out var curve)
                || !EntityManager.TryGetComponent<PrefabRef>(edgeEntity, out var prefab)
                || (edge.m_Start != nodeEntity && edge.m_End != nodeEntity)
                || edge.m_Start == edge.m_End) return false;
            var atStart = edge.m_Start == nodeEntity;
            var point = atStart ? curve.m_Bezier.a : curve.m_Bezier.d;
            // Outward means continuing the incident road beyond this endpoint, away from its interior.
            // The end of the NEW connection must negate this grade for start-to-end station orientation.
            var tangent = atStart ? curve.m_Bezier.a - curve.m_Bezier.b : curve.m_Bezier.d - curve.m_Bezier.c;
            var horizontal = math.length(tangent.xz);
            var hasElevation = EntityManager.TryGetComponent<Elevation>(nodeEntity, out var elevation);
            if (!math.all(math.isfinite(node.m_Position)) || !math.all(math.isfinite(point))
                || !math.all(math.isfinite(tangent)) || horizontal <= 1e-5f
                || (hasElevation && !math.all(math.isfinite(elevation.m_Elevation)))) return false;
            endpoint = new ProfileEndpoint(nodeEntity, edgeEntity, prefab.m_Prefab, node.m_Position,
                point, math.normalize(tangent), tangent.y / (double)horizontal,
                hasElevation ? elevation.m_Elevation : default, hasElevation);
            return true;
        }

        private bool TryProfileChoices(Entity node, out List<ProfileEndpoint> choices, out string reason) {
            choices = new List<ProfileEndpoint>(); reason = "profile_endpoint_unavailable";
            if (node == Entity.Null || !EntityManager.Exists(node) || !EntityManager.HasBuffer<ConnectedEdge>(node)) return false;
            var incident = EntityManager.GetBuffer<ConnectedEdge>(node);
            var seen = new HashSet<Entity>();
            for (var i = 0; i < incident.Length; i++) {
                var edge = incident[i].m_Edge;
                if (!seen.Add(edge)) continue;
                if (!TryReadProfileEndpoint(node, edge, out var endpoint)) { reason = "profile_approach_invalid"; return false; }
                choices.Add(endpoint);
            }
            reason = choices.Count == 0 ? "profile_approach_missing" : null;
            return choices.Count > 0;
        }

        private bool TryResolveProfileEndpoint(Entity node, Entity choice, out ProfileEndpoint endpoint, out string reason) {
            endpoint = default;
            if (!TryProfileChoices(node, out var choices, out reason)) return false;
            if (choice == Entity.Null) {
                if (choices.Count != 1) { reason = "profile_approach_required"; return false; }
                endpoint = choices[0]; return true;
            }
            foreach (var candidate in choices) if (candidate.ApproachEdge == choice) { endpoint = candidate; return true; }
            reason = "profile_approach_stale"; return false;
        }

        public bool TryReadProfileEndpoints(out ProfileEndpoint start, out ProfileEndpoint end, out string reason) {
            start = end = default;
            if (Mode.Value != ConnectMode.SimpleCurve && Mode.Value != ConnectMode.ComplexCurve) {
                reason = "profile_mode_unsupported"; return false;
            }
            return TryResolveProfileEndpoint(StartNode, ProfileChoice(StartNode, true), out start, out reason)
                && TryResolveProfileEndpoint(EndNode, ProfileChoice(EndNode, false), out end, out reason);
        }

        private static bool TryProfileEntity(JToken token, out Entity entity) {
            entity = Entity.Null;
            if (token is not JObject obj || obj.Count != 2
                || obj["index"]?.Type != JTokenType.Integer || obj["version"]?.Type != JTokenType.Integer) return false;
            if (!int.TryParse(obj["index"].ToString(), out var index)
                || !int.TryParse(obj["version"].ToString(), out var version)
                || index <= 0 || version < 0) return false;
            entity = new Entity { Index = index, Version = version }; return true;
        }

        // Prepare only: caller validates every other configure field before invoking the returned action.
        public bool TryPrepareProfileOptions(JObject args, ConnectMode requestedMode, out Action apply, out string reason) {
            apply = null; reason = null;
            var enabled = SmoothElevationProfile.Value;
            if (args.TryGetValue("smoothElevationProfile", out var value)) {
                if (value.Type != JTokenType.Boolean) { reason = "invalid_profile_enabled"; return false; }
                enabled = value.Value<bool>();
            }
#if !IS_DEBUG
            if (enabled) { reason = "profile_unavailable"; return false; }
#endif
            if (enabled && requestedMode != ConnectMode.SimpleCurve && requestedMode != ConnectMode.ComplexCurve) {
                reason = "profile_mode_unsupported"; return false;
            }
            var startNode = StartNode; var endNode = EndNode;
            var startChoice = ProfileChoice(startNode, true); var endChoice = ProfileChoice(endNode, false);
            if (args.TryGetValue("startApproach", out var startToken)
                && !TryProfileEntity(startToken, out startChoice)) { reason = "invalid_start_approach"; return false; }
            if (args.TryGetValue("endApproach", out var endToken)
                && !TryProfileEntity(endToken, out endChoice)) { reason = "invalid_end_approach"; return false; }
            if ((enabled || startToken != null) && !TryResolveProfileEndpoint(startNode, startChoice, out _, out reason)) return false;
            if ((enabled || endToken != null) && !TryResolveProfileEndpoint(endNode, endChoice, out _, out reason)) return false;
            apply = () => {
                m_ProfileStartNode = startNode; m_ProfileEndNode = endNode;
                m_ProfileStartApproach = startChoice; m_ProfileEndApproach = endChoice;
                SmoothElevationProfile.Value = enabled; m_UpdateNeeded = true;
            };
            return true;
        }

        public bool SetProfileApproachJson(string json) {
#if IS_DEBUG
            if (m_ToolSystem.activeTool != this || Phase != OperationPhase.Ready
                || (Mode.Value != ConnectMode.SimpleCurve && Mode.Value != ConnectMode.ComplexCurve)
                || string.IsNullOrWhiteSpace(json)) return false;
            JObject args;
            try { args = JObject.Parse(json); } catch (Newtonsoft.Json.JsonException) { return false; }
            if (args["endpoint"]?.Type != JTokenType.Integer || !TryProfileEntity(args["node"], out var node)
                || !TryProfileEntity(args["edge"], out var edge)) return false;
            if (!int.TryParse(args["endpoint"].ToString(), out var ordinal)
                || (ordinal != 0 && ordinal != 1) || node != (ordinal == 0 ? StartNode : EndNode)) return false;
            if (!TryResolveProfileEndpoint(node, edge, out _, out _)) return false;
            if (ordinal == 0) { m_ProfileStartNode = node; m_ProfileStartApproach = edge; }
            else { m_ProfileEndNode = node; m_ProfileEndApproach = edge; }
            m_UpdateNeeded = true; return true;
#else
            return false;
#endif
        }

        // Presentation of the runtime gate, never acceptance inferred from endpoint validity.
        public string ProfileStatusJson {
            get {
                var reason = "profile_unavailable";
#if IS_DEBUG
                if (!SmoothElevationProfile.Value) reason = "profile_disabled";
                else if (Mode.Value != ConnectMode.SimpleCurve && Mode.Value != ConnectMode.ComplexCurve)
                    reason = "profile_mode_unsupported";
                else {
                    reason = m_ControlRejection ?? "preview_pending";
                    if (reason == "accepted" && (m_ControlCandidate == null || m_UpdateNeeded
                        || !m_ControlJob.IsCompleted || m_ControlStableFrames < 3
                        || m_ControlCandidate.Revision != m_ControlRevision)) reason = "preview_pending";
                }
#endif
                return new JObject { ["reason"] = reason, ["accepted"] = reason == "accepted" }
                    .ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        public string ProfileContextJson() {
            var endpoints = new JArray();
#if IS_DEBUG
            for (var i = 0; i < 2; i++) {
                var node = i == 0 ? StartNode : EndNode;
                var selected = ProfileChoice(node, i == 0);
                var valid = TryProfileChoices(node, out var choices, out var reason);
                var resolved = TryResolveProfileEndpoint(node, selected, out var current, out var resolution);
                var entries = new JArray();
                foreach (var choice in choices) {
                    var name = m_PrefabSystem.TryGetPrefab<PrefabBase>(choice.Prefab, out var prefab) ? prefab.name : "Network";
                    var edge = EntityManager.GetComponentData<Edge>(choice.ApproachEdge);
                    entries.Add(new JObject { ["index"] = choice.ApproachEdge.Index, ["version"] = choice.ApproachEdge.Version,
                        ["label"] = $"{name} · {edge.m_Start.Index} → {edge.m_End.Index}",
                        ["selected"] = resolved && current.ApproachEdge == choice.ApproachEdge });
                }
                endpoints.Add(new JObject { ["endpoint"] = i, ["node"] = new JObject { ["index"] = node.Index, ["version"] = node.Version },
                    ["choices"] = entries, ["reason"] = valid ? resolution : reason });
            }
#endif
            return endpoints.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
