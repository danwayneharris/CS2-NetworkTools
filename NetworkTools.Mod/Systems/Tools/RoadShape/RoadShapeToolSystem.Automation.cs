#if IS_DEBUG
namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using Game.Common;
    using Game.Net;
    using Game.Tools;
    using Newtonsoft.Json.Linq;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public partial class NT_RoadShapeToolSystem {
        private string m_AutomationSession;
        private int m_AutomationVerifiedSubmission;
        private bool m_AutomationActivatePending;
        private ShapeTransformTemplate m_AutomationRequestedTemplate = ShapeTransformTemplate.CurveSmooth;
        private bool AutomationSlope => Template.Value == ShapeTransformTemplate.SlopeLinear
            || Template.Value == ShapeTransformTemplate.SlopeEaseInOut || Template.Value == ShapeTransformTemplate.SlopeArch;

        internal JObject AutomationState() {
            m_AutomationSession ??= Guid.NewGuid().ToString("N");
            var active = m_ToolSystem.activeTool == this && Enabled;
            var ready = active && (Template.Value == ShapeTransformTemplate.CurveSmooth || AutomationSlope) && CanApply
                && !m_UpdateNeeded && m_SubmittedPreviewRevision == m_PreviewInputRevision
                && m_AutomationVerifiedSubmission == m_SmoothTraceId && m_SmoothTraceId > 0;
            return new JObject {
                ["apiVersion"] = 1, ["session"] = m_AutomationSession,
                ["active"] = active, ["smoothMode"] = Template.Value == ShapeTransformTemplate.CurveSmooth, ["phase"] = Phase.ToString(),
                ["revision"] = m_PreviewInputRevision, ["submission"] = m_SmoothTraceId,
                ["mode"] = Template.Value.ToString(),
                ["slopeParameters"] = new JObject { ["easeIn"] = EaseInLength.Value, ["easeOut"] = EaseOutLength.Value,
                    ["archHeight"] = ArchHeight.Value, ["archPosition"] = ArchPosition.Value,
                    ["smoothStart"] = SmoothStart.Value, ["smoothEnd"] = SmoothEnd.Value },
                ["strength"] = SmoothingFactor.Value, ["previewReady"] = ready,
                ["splitChoices"] = JArray.Parse(SplitChoicesJson()),
                ["start"] = new JObject { ["index"] = StartNode.Index, ["version"] = StartNode.Version },
                ["end"] = new JObject { ["index"] = EndNode.Index, ["version"] = EndNode.Version },
                ["pathNodes"] = PathNodeCount, ["pathEdges"] = PathEdgeCount
            };
        }

        internal JObject AutomationCommand(string action, JObject args) {
            var slope = action.StartsWith("slope_", StringComparison.Ordinal);
            if (slope) action = action.Substring(6);
            if (action == "state") return AutomationState();
            if (action == "activate") {
                if (Phase == OperationPhase.Applying) throw new InvalidOperationException("apply_in_progress");
                // OnStartRunning restores persisted parameters. Apply the requested
                // mode afterwards, or immediately if the tool is already running.
                m_AutomationRequestedTemplate = slope ? ShapeTransformTemplate.SlopeLinear : ShapeTransformTemplate.CurveSmooth;
                m_AutomationActivatePending = !(m_ToolSystem.activeTool == this && Enabled);
                RequestEnable();
                if (!m_AutomationActivatePending) { ResetToIdle(); Template.Value = m_AutomationRequestedTemplate; MarkDirty(); }
                return new JObject { ["accepted"] = true, ["state"] = AutomationState() };
            }
            var state = AutomationState();
            if (!(bool)state["active"] || (slope ? !AutomationSlope : Template.Value != ShapeTransformTemplate.CurveSmooth))
                throw new InvalidOperationException("smooth_tool_not_active");
            if ((string)args["session"] != m_AutomationSession
                || args["revision"]?.Type != JTokenType.Integer
                || (long)args["revision"] != m_PreviewInputRevision)
                throw new InvalidOperationException("stale_tool_revision");
            if (Phase == OperationPhase.Applying) throw new InvalidOperationException("apply_in_progress");
            m_LastShapeJob.Complete();
            Dependency.Complete();
            switch (action) {
                case "configure":
                    if (!slope) throw new ArgumentException("slope_command_required");
                    ConfigureAutomationSlope(args);
                    break;
                case "split":
                    if (args["enabled"]?.Type != JTokenType.Boolean
                        || !SetSplitNode(AutomationNode(args["node"]), (bool)args["enabled"]))
                        throw new ArgumentException("ineligible_split_node");
                    break;
                case "clear": ResetToIdle(); break;
                case "strength":
                    if (args["value"] == null || (args["value"].Type != JTokenType.Float && args["value"].Type != JTokenType.Integer))
                        throw new ArgumentException("numeric_strength_required");
                    var value = (double)args["value"];
                    if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
                        throw new ArgumentException("strength_out_of_range");
                    SmoothingFactor.Value = (float)value;
                    break;
                case "select":
                    if (Phase != OperationPhase.Idle) throw new InvalidOperationException("clear_selection_first");
                    var start = AutomationNode(args["start"]);
                    var end = AutomationNode(args["end"]);
                    if (start == end) throw new ArgumentException("distinct_endpoints_required");
                    var nodes = new NativeList<Entity>(Allocator.Temp);
                    var edges = new NativeList<Entity>(Allocator.Temp);
                    try {
                        if (!FindPathBetween(start,end,ref nodes,ref edges) || nodes.Length > 128
                            || nodes.Length < 2 || edges.Length != nodes.Length-1)
                            throw new InvalidOperationException("unsupported_or_missing_path");
                        foreach (var n in nodes) if (!AutomationLive(n)) throw new InvalidOperationException("path_changed");
                        foreach (var e in edges) if (!AutomationLive(e)) throw new InvalidOperationException("path_changed");
                        m_NextPathNodes.Clear(); m_NextPathEdges.Clear();
                        HandleAddNode(start);
                        m_NextPathNodes.AddRange(nodes.AsArray());
                        m_NextPathEdges.AddRange(edges.AsArray());
                        HandleAddNode(end);
                        MarkDirty();
                    } finally { nodes.Dispose(); edges.Dispose(); }
                    break;
                case "apply":
                    if (args["submission"]?.Type != JTokenType.Integer
                        || (int)args["submission"] != m_SmoothTraceId || !(bool)state["previewReady"]
                        || OriginalProbeStatus() != "matches")
                        throw new InvalidOperationException("stale_or_unverified_preview");
                    RequestApply();
                    return new JObject { ["accepted"] = true, ["status"] = "apply_requested",
                        ["submission"] = m_SmoothTraceId, ["note"] = "Poll state and independently verify permanent geometry; request acceptance is not completion." };
                default: throw new ArgumentException("unknown_action");
            }
            return AutomationState();
        }

        private void ConfigureAutomationSlope(JObject args) {
            // Validate the entire request before changing any parameter.
            var mode = (string)args["mode"];
            var template = mode switch {
                "linear" => ShapeTransformTemplate.SlopeLinear,
                "ease" => ShapeTransformTemplate.SlopeEaseInOut,
                "arch" => ShapeTransformTemplate.SlopeArch,
                _ => throw new ArgumentException("unsupported_slope_mode")
            };
            float Number(string key, float min, float max) {
                var token = args[key];
                if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
                    throw new ArgumentException("numeric_parameter_required: " + key);
                var v = (double)token;
                if (double.IsNaN(v) || double.IsInfinity(v) || v < min || v > max)
                    throw new ArgumentException("parameter_out_of_range: " + key);
                return (float)v;
            }
            var easeIn = Number("easeIn", 0, .5f); var easeOut = Number("easeOut", 0, .5f);
            var height = Number("archHeight", -80, 80); var location = Number("archPosition", .1f, .9f);
            if (args["smoothStart"]?.Type != JTokenType.Boolean || args["smoothEnd"]?.Type != JTokenType.Boolean)
                throw new ArgumentException("boolean_parameters_required");
            Template.Value = template;
            EaseInLength.Value = easeIn; EaseOutLength.Value = easeOut;
            ArchHeight.Value = height; ArchPosition.Value = location;
            SmoothStart.Value = (bool)args["smoothStart"]; SmoothEnd.Value = (bool)args["smoothEnd"];
            MarkDirty();
        }

        private bool AutomationLive(Entity e) => EntityManager.Exists(e)
            && !EntityManager.HasComponent<Deleted>(e) && !EntityManager.HasComponent<Temp>(e);
        private Entity AutomationNode(JToken token) {
            if (token?["index"]?.Type != JTokenType.Integer || token?["version"]?.Type != JTokenType.Integer)
                throw new ArgumentException("entity_identity_required");
            var e = new Entity { Index = (int)token["index"], Version = (int)token["version"] };
            if (!AutomationLive(e) || !EntityManager.HasComponent<Node>(e)) throw new ArgumentException("node_not_live");
            return e;
        }
    }
}

namespace NetworkTools.Automation {
    using System;
    using Game;
    using Game.SceneFlow;
    using Game.Simulation;
    using Newtonsoft.Json.Linq;
    using Unity.Entities;
    using NetworkTools.Systems.Tools.RoadShape;

    // Fixed, versioned public endpoint for the bridge's main-thread dispatcher.
    public static class BridgeApi {
        public static string InvokeV1(string action, string json) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || GameManager.instance.isGameLoading
                || GameManager.instance.gameMode != GameMode.Game) throw new InvalidOperationException("no_loaded_city");
            if (world.GetExistingSystemManaged<SimulationSystem>().selectedSpeed != 0)
                throw new InvalidOperationException("pause_before_networktools_control");
            if (action.StartsWith("connect_", StringComparison.Ordinal)) {
                var connect = world.GetExistingSystemManaged<NetworkTools.Systems.Tools.Connect.NT_ConnectToolSystem>();
                if (connect == null) throw new InvalidOperationException("connect_unavailable");
                return connect.AutomationCommand(action.Substring(8), JObject.Parse(json)).ToString(Newtonsoft.Json.Formatting.None);
            }
            var tool = world.GetExistingSystemManaged<NT_RoadShapeToolSystem>();
            if (tool == null) throw new InvalidOperationException("networktools_unavailable");
            return tool.AutomationCommand(action, JObject.Parse(json)).ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
#endif
