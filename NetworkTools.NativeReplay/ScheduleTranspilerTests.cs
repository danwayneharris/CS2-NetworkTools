using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

// Executes the research rewriter against actual installed OnUpdate call operands.
// Does not patch or execute Unity, allocate native storage, or connect to the game.
static class ScheduleTranspilerTests {
    public static int Run(string bridgePath, string managedPath, string reportPath) {
        if (File.Exists(reportPath)) throw new IOException("Refusing to overwrite evidence");
        bridgePath = Path.GetFullPath(bridgePath);
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) => {
            var name = new AssemblyName(args.Name).Name + ".dll";
            foreach (var dir in new[] { Path.GetDirectoryName(bridgePath)!, managedPath }) {
                var path = Path.Combine(dir, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        var bridge = Assembly.LoadFrom(bridgePath);
        var mod = bridge.GetType("CitiesIIAgentBridge.Mod", true)!;
        var rewrite = mod.GetMethod("GeometryTraceSchedules", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Research trace was not compiled");
        var instruction = rewrite.GetParameters()[0].ParameterType.GetGenericArguments()[0];
        var operand = instruction.GetField("operand")!;
        var constructor = instruction.GetConstructor(new[] { typeof(OpCode), typeof(object) })!;
        var native = typeof(Game.Net.GeometrySystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var calls = ReadCalls(native).ToArray();
        MethodInfo[] Rewrite(MethodInfo[] methods) {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instruction))!;
            foreach (var method in methods) list.Add(constructor.Invoke(new object[] { OpCodes.Call, method }));
            var result = (IEnumerable)rewrite.Invoke(null, new object[] { list })!;
            return result.Cast<object>().Select(i => (MethodInfo)operand.GetValue(i)!).ToArray();
        }
        var changed = Rewrite(calls);
        var indices = calls.Select((m, i) => i).Where(i => calls[i] != changed[i]).ToArray();
        if (indices.Length != 8) throw new Exception($"Expected 8 replacements, got {indices.Length}");
        foreach (int i in indices) {
            var before = calls[i]; var after = changed[i];
            if (after.DeclaringType != mod || before.ReturnType != after.ReturnType
                || !before.GetParameters().Select(p => p.ParameterType).SequenceEqual(after.GetParameters().Select(p => p.ParameterType))
                || !before.GetGenericArguments().SequenceEqual(after.GetGenericArguments()))
                throw new Exception("Scheduling signature changed: " + before);
        }
        void Reject(MethodInfo[] methods, string expected) {
            try { Rewrite(methods); }
            catch (Exception e) when (e.GetBaseException().Message == expected) { return; }
            throw new Exception("Missing rejection: " + expected);
        }
        Reject(calls.Where((_, i) => i != indices[0]).ToArray(), "geometry_schedule_call_sites_changed:7");
        Reject(calls.Append(calls[indices[0]]).ToArray(), "geometry_schedule_call_sites_changed:9");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new {
            scope = "Actual research transpiler; installed native call operands; no Unity execution",
            gameMvid = native.Module.ModuleVersionId, bridgePath, callCount = calls.Length,
            replacements = indices.Select(i => new { native = calls[i].ToString(), replacement = changed[i].ToString() }),
            negativeTests = new[] { "missing call site rejected", "extra call site rejected" }
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: 8 native scheduling signatures preserved; missing/extra sites rejected");
        return 0;
    }

    static IEnumerable<MethodInfo> ReadCalls(MethodInfo method) {
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => unchecked((ushort)o.Value));
        var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
        for (int p = 0; p < bytes.Length;) {
            ushort code = bytes[p++];
            if (code == 0xfe) code = (ushort)(0xfe00 | bytes[p++]);
            var op = opcodes[code];
            if ((op == OpCodes.Call || op == OpCodes.Callvirt) && method.Module.ResolveMethod(BitConverter.ToInt32(bytes, p)) is MethodInfo call)
                yield return call;
            p += op.OperandType switch {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, p),
                _ => 4
            };
        }
    }
}
