#if IS_DEBUG
namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using System.Collections.Generic;
    using Colossal.Mathematics;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;

    public partial class NT_ConnectToolSystem {
        private readonly string m_ControlSession = Guid.NewGuid().ToString("N");
        private long m_ControlRevision;
        private long m_ControlSubmission;
        private long m_ControlSubmittedRevision;
        private string m_ControlInputs;
        private string m_ControlSubmittedInputs;
        private string m_ControlPreview;
        private int m_ControlStableFrames;
        private HashSet<Entity> m_ControlPreviousTemps = new();
        private JobHandle m_ControlJob;
        private bool m_ControlActivatePending;

        // Scoped snapshot: selected endpoints, all incident edges and their far nodes,
        // parameters, prefab selection and validation setting. Not a city-wide lock.
        private string ControlInputs() {
            var data = new List<object> { Enabled, Phase, AnarchyEnabled, Mode.Value,
                NetPrefab.NetPrefabEntity, NetPrefab.NetLanePrefabEntity, BuildJobConfig() };
            var incident = new HashSet<Entity>();
            foreach (var node in m_SelectedNodes) {
                if (!ControlLive(node) || !EntityManager.HasComponent<Node>(node)
                    || !EntityManager.HasBuffer<ConnectedEdge>(node)) return null;
                data.Add(node); data.Add(EntityManager.GetComponentData<Node>(node));
                foreach (var e in EntityManager.GetBuffer<ConnectedEdge>(node, true)) incident.Add(e.m_Edge);
            }
            if (incident.Count > 64) return null;
            var ordered = new List<Entity>(incident); ordered.Sort((a,b) => a.Index.CompareTo(b.Index));
            foreach (var entity in ordered) {
                if (!ControlLive(entity) || !EntityManager.HasComponent<Curve>(entity)) return null;
                var edge = EntityManager.GetComponentData<Edge>(entity);
                data.Add(entity); data.Add(edge); data.Add(EntityManager.GetComponentData<Curve>(entity));
                data.Add(EntityManager.GetComponentData<PrefabRef>(entity));
                data.Add(EntityManager.GetComponentData<Node>(edge.m_Start));
                data.Add(EntityManager.GetComponentData<Node>(edge.m_End));
                if (EntityManager.HasComponent<Upgraded>(entity)) data.Add(EntityManager.GetComponentData<Upgraded>(entity));
                if (EntityManager.HasComponent<Elevation>(entity)) data.Add(EntityManager.GetComponentData<Elevation>(entity));
            }
            return JsonConvert.SerializeObject(data, NetworkTools.Automation.VectorJsonConverter.Settings);
        }
        private bool ControlLive(Entity e) => EntityManager.Exists(e)
            && !EntityManager.HasComponent<Temp>(e) && !EntityManager.HasComponent<Deleted>(e);
        private void RefreshControlInputs() {
            var inputs = ControlInputs();
            if (inputs == null || inputs != m_ControlInputs) {
                m_ControlInputs = inputs; ++m_ControlRevision; m_ControlStableFrames = 0; m_ControlPreview = null;
            }
        }
        private EntityQuery ControlTempQuery() => EntityManager.CreateEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<Temp>() },
            None = new[] { ComponentType.ReadOnly<Deleted>() }
        });
        private void BeginControlPreview() {
            RefreshControlInputs(); ++m_ControlSubmission;
            m_ControlSubmittedRevision = m_ControlRevision; m_ControlSubmittedInputs = m_ControlInputs;
            m_ControlStableFrames = 0; m_ControlPreview = null; m_ControlPreviousTemps.Clear();
            using var q = ControlTempQuery();
            using var es = q.ToEntityArray(Allocator.Temp);
            foreach (var e in es) m_ControlPreviousTemps.Add(e);
        }
        private string ReadControlPreview() {
            using var q = ControlTempQuery();
            if (q.CalculateEntityCount() == 0 || q.CalculateEntityCount() > 256) return null;
            using var es = q.ToEntityArray(Allocator.Temp);
            var ids = new List<Entity>(); foreach (var e in es) ids.Add(e);
            ids.Sort((a,b) => a.Index.CompareTo(b.Index));
            var values = new List<object>(); var newEdges = 0;
            foreach (var e in ids) {
                // Require a newly rebuilt set after this submission; native reuse stays
                // explicitly unverified, rather than accepting a stale previous preview.
                if (m_ControlPreviousTemps.Contains(e) || EntityManager.HasComponent<Updated>(e)
                    || EntityManager.HasComponent<Created>(e) || !EntityManager.HasBuffer<Game.Net.SubLane>(e)) return null;
                var temp = EntityManager.GetComponentData<Temp>(e);
                if (temp.m_Original == Entity.Null) newEdges++;
                var c = EntityManager.GetComponentData<Curve>(e).m_Bezier;
                if (!math.all(math.isfinite(c.a)) || !math.all(math.isfinite(c.b))
                    || !math.all(math.isfinite(c.c)) || !math.all(math.isfinite(c.d))) return null;
                values.Add(e); values.Add(temp); values.Add(EntityManager.GetComponentData<Edge>(e)); values.Add(c);
                values.Add(EntityManager.GetComponentData<PrefabRef>(e));
                foreach (var lane in EntityManager.GetBuffer<Game.Net.SubLane>(e, true)) values.Add(lane.m_SubLane);
            }
            return newEdges > 0 ? JsonConvert.SerializeObject(values, NetworkTools.Automation.VectorJsonConverter.Settings) : null;
        }
        internal void ObserveAutomationPreview() {
            if (!Enabled || m_ToolSystem.activeTool != this || Phase != OperationPhase.Ready) return;
            RefreshControlInputs();
            if (m_UpdateNeeded || !m_ControlJob.IsCompleted || m_ControlInputs == null
                || m_ControlSubmittedRevision != m_ControlRevision || m_ControlInputs != m_ControlSubmittedInputs) {
                m_ControlStableFrames = 0; return;
            }
            m_ControlJob.Complete();
            var preview = ReadControlPreview();
            m_ControlStableFrames = preview != null && preview == m_ControlPreview ? m_ControlStableFrames + 1 : 0;
            m_ControlPreview = preview;
        }
        internal JObject AutomationState() {
            RefreshControlInputs();
            var ready = Enabled && m_ToolSystem.activeTool == this && CanApply && Mode.Value == ConnectMode.SimpleCurve
                && !m_UpdateNeeded && m_ControlStableFrames >= 3 && m_ControlSubmittedRevision == m_ControlRevision
                && m_ControlInputs != null && m_ControlInputs == m_ControlSubmittedInputs;
            return new JObject { ["apiVersion"] = 1, ["tool"] = "connect", ["session"] = m_ControlSession,
                ["revision"] = m_ControlRevision, ["submission"] = m_ControlSubmission,
                ["active"] = Enabled && m_ToolSystem.activeTool == this, ["phase"] = Phase.ToString(),
                ["previewReady"] = ready, ["mode"] = Mode.Value.ToString(), ["anarchy"] = AnarchyEnabled,
                ["start"] = JToken.FromObject(StartNode), ["end"] = JToken.FromObject(EndNode),
                ["parameters"] = JToken.Parse(JsonConvert.SerializeObject(BuildJobConfig(), NetworkTools.Automation.VectorJsonConverter.Settings)),
                ["previewObservation"] = m_ControlPreview == null ? JValue.CreateNull() : JToken.Parse(m_ControlPreview),
                ["limits"] = "SimpleCurve, existing dead-end nodes with matching prefab; native preview stability is not collision or lane-connectivity certification." };
        }
        internal JObject AutomationCommand(string action, JObject args) {
            if (action == "state") return AutomationState();
            if (Phase == OperationPhase.Applying) throw new InvalidOperationException("apply_in_progress");
            if (action == "activate") {
                m_ControlActivatePending = !(Enabled && m_ToolSystem.activeTool == this);
                RequestEnable();
                if (!m_ControlActivatePending) { ResetToIdle(); Mode.Value = ConnectMode.SimpleCurve; NetPrefab.ResetToDefault(); }
                return AutomationState();
            }
            var state = AutomationState();
            if (!(bool)state["active"] || Mode.Value != ConnectMode.SimpleCurve) throw new InvalidOperationException("connect_not_active");
            if ((string)args["session"] != m_ControlSession || args["revision"]?.Type != JTokenType.Integer
                || (long)args["revision"] != m_ControlRevision) throw new InvalidOperationException("stale_tool_revision");
            m_ControlJob.Complete(); Dependency.Complete();
            Entity Endpoint(JToken token) {
                if (token?["index"]?.Type != JTokenType.Integer || token?["version"]?.Type != JTokenType.Integer)
                    throw new ArgumentException("entity_identity_required");
                var e = new Entity { Index = (int)token["index"], Version = (int)token["version"] };
                if (!ControlLive(e) || !EntityManager.HasComponent<Node>(e) || !EntityManager.HasBuffer<ConnectedEdge>(e)
                    || EntityManager.GetBuffer<ConnectedEdge>(e, true).Length != 1) throw new ArgumentException("existing_dead_end_required");
                return e;
            }
            switch (action) {
                case "clear": ResetToIdle(); m_UpdateNeeded = true; break;
                case "select":
                    if (Phase != OperationPhase.Idle) throw new InvalidOperationException("clear_selection_first");
                    var start = Endpoint(args["start"]); var end = Endpoint(args["end"]);
                    if (start == end || math.distance(EntityManager.GetComponentData<Node>(start).m_Position,
                        EntityManager.GetComponentData<Node>(end).m_Position) > 2000) throw new ArgumentException("invalid_endpoint_pair");
                    if (EntityManager.GetComponentData<PrefabRef>(start).m_Prefab != EntityManager.GetComponentData<PrefabRef>(end).m_Prefab)
                        throw new ArgumentException("matching_prefabs_required");
                    HandleAddNode(start); HandleAddNode(end); m_UpdateNeeded = true; break;
                case "configure":
                    float3 Point(string key) {
                        var a = args[key] as JArray;
                        if (a == null || a.Count != 3) throw new ArgumentException("three_coordinates_required");
                        var v = new float3((float)a[0], (float)a[1], (float)a[2]);
                        if (!math.all(math.isfinite(v)) || math.distance(v, StartPosition.Value) > 4000)
                            throw new ArgumentException("control_point_out_of_range");
                        return v;
                    }
                    if (Phase != OperationPhase.Ready) throw new InvalidOperationException("select_endpoints_first");
                    var b = Point("startControl"); var c = Point("endControl");
                    CurveStartControlPointPosition.Value = b; CurveEndControlPointPosition.Value = c; m_UpdateNeeded = true; break;
                case "apply":
                    if (!(bool)state["previewReady"] || args["submission"]?.Type != JTokenType.Integer
                        || (long)args["submission"] != m_ControlSubmission || ReadControlPreview() != m_ControlPreview)
                        throw new InvalidOperationException("stale_or_unverified_preview");
                    RequestApply(); break;
                default: throw new ArgumentException("unknown_action");
            }
            return AutomationState();
        }
    }
}
#endif


