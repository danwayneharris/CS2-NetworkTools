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
            foreach (string action in new[] { "state", "activate", "clear", "select", "strength", "split", "apply" }) {
                var props = new JObject();
                var required = new System.Collections.Generic.List<string>();
                if (action != "state" && action != "activate") {
                    props["session"] = new JObject { ["type"] = "string" };
                    props["revision"] = Integer(); required.Add("session"); required.Add("revision");
                }
                if (action == "select") { props["start"] = Node(); props["end"] = Node(); required.Add("start"); required.Add("end"); }
                if (action == "strength") { props["value"] = new JObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 }; required.Add("value"); }
                if (action == "split") { props["node"] = Node(); props["enabled"] = new JObject { ["type"] = "boolean" }; required.Add("node"); required.Add("enabled"); }
                if (action == "apply") { props["submission"] = Integer(); required.Add("submission"); }
                commands.Add(new JObject { ["name"] = action, ["readOnly"] = action == "state",
                    ["description"] = "Smooth Curve " + action + ". Requires a loaded paused city. Changes require local controls. Apply acceptance is not completion; verify permanent geometry independently.",
                    ["inputSchema"] = Obj(props, required.ToArray()), ["outputSchema"] = new JObject { ["type"] = "object" } });
            }
            return new JObject { ["protocol"] = 1, ["id"] = "networktools", ["version"] = "1.0.0",
                ["commands"] = commands }.ToString(Newtonsoft.Json.Formatting.None);
        }
        public static string InvokeV1(string command, string argumentsJson, string contextJson) {
            // The bridge enforces its transport/control policy; NT still validates live tool state.
            return NetworkTools.Automation.BridgeApi.InvokeV1(command, argumentsJson);
        }
    }
}
#endif
