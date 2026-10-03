#if IS_DEBUG
namespace CitiesBridge {
    using Newtonsoft.Json.Linq;
    public static class ProviderV1 {
        private static JObject Obj(JObject props, params string[] required) => new JObject {
            ["type"] = "object", ["properties"] = props, ["required"] = new JArray(required), ["additionalProperties"] = false };
        private static JObject Integer() => new JObject { ["type"] = "integer", ["minimum"] = 0 };
        private static JObject Node() => Obj(new JObject { ["index"] = Integer(), ["version"] = Integer() }, "index", "version");
        public static string DescribeV1() {
            var commands = new JArray();
            foreach (string action in new[] { "state", "activate", "clear", "select", "strength", "split", "combined", "apply" }) {
                var props = new JObject();
                var required = new System.Collections.Generic.List<string>();
                if (action != "state" && action != "activate") {
                    props["session"] = new JObject { ["type"] = "string" };
                    props["revision"] = Integer(); required.Add("session"); required.Add("revision");
                }
                if (action == "select") { props["start"] = Node(); props["end"] = Node(); required.Add("start"); required.Add("end"); }
                if (action == "strength") { props["value"] = new JObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 }; required.Add("value"); }
                if (action == "split") { props["node"] = Node(); props["enabled"] = new JObject { ["type"] = "boolean" }; required.Add("node"); required.Add("enabled"); }
                if (action == "combined") {
                    props["enabled"] = new JObject { ["type"] = "boolean" }; required.Add("enabled");
                    props["smoothStart"] = new JObject { ["type"] = "boolean" };
                    props["smoothEnd"] = new JObject { ["type"] = "boolean" };
                    props["allowJunctionElevation"] = new JObject { ["type"] = "boolean" };
                    props["unlimitedJunctionElevation"] = new JObject { ["type"] = "boolean" };
                    props["junctionElevationLimit"] = new JObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 20 };
                }
                if (action == "apply") { props["submission"] = Integer(); required.Add("submission"); }
                commands.Add(new JObject { ["name"] = action, ["readOnly"] = action == "state",
                    ["description"] = "Smooth Curve " + action + ". Requires a loaded paused city. Changes require local controls. Apply acceptance is not completion; verify permanent geometry independently.",
                    ["inputSchema"] = Obj(props, required.ToArray()), ["outputSchema"] = new JObject { ["type"] = "object" } });
            }
            foreach (var tool in new[] { "slope", "connect" }) {
                foreach (var action in new[] { "state", "activate", "clear", "select", "configure", "apply" }) {
                    var props = new JObject();
                    var required = new System.Collections.Generic.List<string>();
                    void Add(string key, JObject schema) { props[key] = schema; required.Add(key); }
                    JObject Number(double min, double max) => new JObject { ["type"] = "number", ["minimum"] = min, ["maximum"] = max };
                    if (action != "state" && action != "activate") {
                        Add("session", new JObject { ["type"] = "string" }); Add("revision", Integer());
                    }
                    if (action == "select") { Add("start", Node()); Add("end", Node()); }
                    if (action == "apply") Add("submission", Integer());
                    if (action == "configure" && tool == "slope") {
                        Add("mode", new JObject { ["type"] = "string", ["enum"] = new JArray("linear", "ease", "arch") });
                        Add("easeIn", Number(0,.5)); Add("easeOut", Number(0,.5));
                        Add("archHeight", Number(-80,80)); Add("archPosition", Number(.1,.9));
                        Add("smoothStart", new JObject { ["type"] = "boolean" }); Add("smoothEnd", new JObject { ["type"] = "boolean" });
                    }
                    if (action == "configure" && tool == "connect") {
                        JObject Point() => new JObject { ["type"] = "array", ["minItems"] = 3, ["maxItems"] = 3,
                            ["items"] = Number(-100000,100000) };
                        foreach (var key in new[] { "startControl", "endControl", "midPoint", "midStartControl", "midEndControl" }) props[key] = Point();
                        props["mode"] = new JObject { ["type"] = "string", ["enum"] = new JArray("SimpleCurve", "ComplexCurve") };
                        props["smoothElevationProfile"] = new JObject { ["type"] = "boolean" };
                        props["startApproach"] = Node(); props["endApproach"] = Node();
                        props["laneAwareDirection"] = new JObject { ["type"] = "boolean" };
                        JObject LaneGroup() => new JObject { ["type"] = "array", ["items"] = Node(),
                            ["minItems"] = 1, ["maxItems"] = 64, ["uniqueItems"] = true };
                        props["startLanes"] = LaneGroup(); props["endLanes"] = LaneGroup();
                    }
                    commands.Add(new JObject { ["name"] = tool + "_" + action, ["readOnly"] = action == "state",
                        ["description"] = tool + " " + action + ". Paused city and local controls required. Connect supports SimpleCurve and ComplexCurve; profile configuration requires current, unambiguous incident approaches. Configure requires at least one option or control point. Apply requires current observed preview; independently inspect permanent results.",
                        ["inputSchema"] = Obj(props, required.ToArray()), ["outputSchema"] = new JObject { ["type"] = "object" } });
                }
            }
            return new JObject { ["protocol"] = 1, ["id"] = "networktools", ["version"] = "1.2.0",
                ["commands"] = commands }.ToString(Newtonsoft.Json.Formatting.None);
        }
        public static string InvokeV1(string command, string argumentsJson, string contextJson) {
            // The bridge enforces its transport/control policy; NT still validates live tool state.
            return NetworkTools.Automation.BridgeApi.InvokeV1(command, argumentsJson);
        }
    }
}
#endif
