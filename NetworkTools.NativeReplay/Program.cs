using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Unity.Collections;
using Unity.Mathematics;

// Feasibility probe, not an ECS simulation. Calls installed implementation directly.
static class Program {
    static object Attempt(string name, Func<object> action) {
        try { return new { name, status = "executed", result = action() }; }
        catch (Exception error) {
            var cause = error.GetBaseException();
            return new { name, status = "unavailable", error = cause.GetType().FullName,
                message = cause.Message };
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object FitCurve() {
        var curve = Game.Net.NetUtils.FitCurve(new float3(0, 2, 0), new float3(1, 0, 0),
            new float3(1, 0, 0), new float3(100, 2, 0));
        var controls = new[] { curve.a, curve.b, curve.c, curve.d };
        if (controls.Any(p => !math.all(math.isfinite(p))) || curve.a.x != 0 || curve.d.x != 100)
            throw new InvalidOperationException("Native FitCurve returned invalid endpoints/controls");
        return controls.Select(p => new[] { p.x, p.y, p.z }).ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object AllocateNative() {
        var values = new NativeArray<int>(2, Allocator.Temp);
        try {
            values[0] = 17;
            return new { value = values[0], length = values.Length };
        } finally { values.Dispose(); }
    }

    static int Main(string[] args) {
        try { return Run(args); }
        catch (Exception error) {
            var cause = error.GetBaseException();
            Console.Error.WriteLine($"Native replay rejected: {cause.GetType().Name}: {cause.Message}");
            return 2;
        }
    }

    static int Run(string[] args) {
        if (args.Length == 3 && args[0] == "--pipeline") return RawPipelineCapture.Run(args[1], args[2]);
        if (args.Length == 3 && args[0] == "--pipeline-junction") return RawPipelineCapture.Run(args[1], args[2], true);
        if (args.Length == 4 && args[0] == "--raw-edge") return RawEdgeCapture.Run(args[1], args[2], args[3]);
        if (args.Length == 4 && args[0] == "--schedule-transpiler-tests") return ScheduleTranspilerTests.Run(args[1], args[2], args[3]);
        if (args.Length == 3 && args[0] == "--world") return WorldCapture.Run(args[1], args[2]);
        if (args.Length == 2 && args[0] == "--world-tests") return WorldStageTests.Run(args[1]);
        if (args.Length == 3 && args[0] == "--stages") return NativeStages.Run(args[1], args[2]);
        if (args.Length != 1) throw new ArgumentException("Supply a new report path");
        if (File.Exists(args[0])) throw new IOException("Refusing to overwrite evidence");
        var native = typeof(Game.Net.NetUtils).Assembly;
        var rows = new[] { Attempt("Game.Net.NetUtils.FitCurve", FitCurve),
            Attempt("Unity.Collections.NativeArray allocation", AllocateNative),
            Attempt("GeometrySystem private job metadata", () =>
                typeof(Game.Net.GeometrySystem).GetNestedTypes(BindingFlags.NonPublic)
                    .Where(t => t.Name.Contains("GeometryJob"))
                    .Select(t => new { t.FullName, fields = t.GetFields().Select(f =>
                        new { f.Name, type = f.FieldType.FullName }).ToArray() }).ToArray()) };
        var report = new { schemaVersion = 1, scope = "Direct assembly feasibility; no game connection or ECS execution",
            utc = DateTimeOffset.UtcNow, runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            gameAssembly = native.Location,
            gameSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(native.Location))), probes = rows };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(args[0], json);
        Console.WriteLine($"Assembly feasibility report: {args[0]}");
        // A report is a completed experiment, not a claim every execution tier worked.
        return 0;
    }
}
