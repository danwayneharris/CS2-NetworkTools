using System.Reflection;
using System.Text.Json;
using Colossal.Mathematics;
using Unity.Mathematics;

// Actual installed private game methods. No translated geometry formulas or ECS fakes.
static class NativeStages {
    static float Number(JsonElement value) {
        float result = value.GetSingle();
        if (!float.IsFinite(result)) throw new ArgumentException("Nonfinite capture value");
        return result;
    }
    static Bezier4x3 Curve(JsonElement value) {
        if (value.GetArrayLength() != 4) throw new ArgumentException("Curve requires four controls");
        var points = value.EnumerateArray().Select(p => {
            if (p.GetArrayLength() != 3) throw new ArgumentException("Control requires XYZ");
            return new float3(Number(p[0]), Number(p[1]), Number(p[2]));
        }).ToArray();
        return new Bezier4x3(points[0], points[1], points[2], points[3]);
    }
    static float[][] Points(Bezier4x3 c) => new[] { c.a, c.b, c.c, c.d }
        .Select(p => new[] { p.x, p.y, p.z }).ToArray();

    internal static object Execute(JsonElement input) {
        string stage = input.GetProperty("stage").GetString()!;
        var start = Curve(input.GetProperty("start"));
        var end = Curve(input.GetProperty("end"));
        string jobName;
        object[] parameters;
        switch (stage) {
            case "StraightenMiddleHeights":
                jobName = "FinishEdgeGeometryJob";
                parameters = new object[] { start, end };
                break;
            case "LimitMiddleHeights":
                jobName = "FinishEdgeGeometryJob";
                parameters = new object[] { start, end, Number(input.GetProperty("maxSlope")),
                    Number(input.GetProperty("width")) };
                break;
            case "CalculateCutOffset":
            case "Cut":
                jobName = "CalculateEdgeGeometryJob";
                parameters = new object[] { start, end, Number(input.GetProperty("startOffset")),
                    Number(input.GetProperty("endOffset")), Number(input.GetProperty(
                        stage == "Cut" ? "cutOffset" : "width")) };
                break;
            default: throw new ArgumentException("Unsupported native stage: " + stage);
        }
        var type = typeof(Game.Net.GeometrySystem).GetNestedType(jobName, BindingFlags.NonPublic)
            ?? throw new MissingMemberException(jobName);
        var method = type.GetMethod(stage, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(jobName, stage);
        // These selected methods use their explicit value arguments. No live system created.
        var result = method.Invoke(Activator.CreateInstance(type), parameters);
        if (stage == "CalculateCutOffset") return new { stage, value = result };
        if (stage == "Cut") return new { stage, curve = Points((Bezier4x3)result!) };
        return new { stage, start = Points((Bezier4x3)parameters[0]), end = Points((Bezier4x3)parameters[1]) };
    }

    internal static int Run(string fixturePath, string outputPath) {
        if (File.Exists(outputPath)) throw new IOException("Refusing to overwrite evidence");
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1) throw new ArgumentException("Unsupported schema");
        var assembly = typeof(Game.Net.GeometrySystem).Assembly;
        string actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assembly.Location)));
        if (!string.Equals(actualHash, root.GetProperty("gameSha256").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Game assembly fingerprint changed; revalidate capture contract");
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        if (cases.Length == 0) throw new ArgumentException("No cases");
        var results = cases.Select(c => new { id = c.GetProperty("id").GetString(), result = Execute(c) }).ToArray();
        var report = new { schemaVersion = 1, gameSha256 = actualHash,
            scope = "Original binary value-only geometry stages; excludes ECS participant selection, retention, scheduling and terrain",
            cases = results };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Executed {results.Length} original binary stage cases; report: {outputPath}");
        return 0;
    }
}
