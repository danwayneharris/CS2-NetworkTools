using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Game.Net;
using Game.Prefabs;
using NativeReplay;
using Unity.Entities;

// Full captured value fields for a single, explicitly bounded CalculateEdge job.
// The two native archetype handles are outside the hash-pinned stage read set.
static class RawEdgeCapture {
    const string GameHash = "AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A";
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly List<string> exclusions = new();
    static Entity Id(JsonElement e) {
        if (e.GetArrayLength() != 2) throw new ArgumentException("Invalid entity identity");
        return new() { Index = e[0].GetInt32(), Version = e[1].GetInt32() };
    }
    static object Decode(Type type, JsonElement e, string path) {
        if (type == typeof(Entity)) return Id(e);
        if (type.IsEnum) return Enum.ToObject(type, Decode(Enum.GetUnderlyingType(type), e, path));
        if (type.IsPrimitive) {
            object value = JsonSerializer.Deserialize(e.GetRawText(), type)!;
            if (value is float f && !float.IsFinite(f) || value is double d && !double.IsFinite(d))
                throw new ArgumentException("Nonfinite captured number: " + path);
            return value;
        }
        if (type.IsArray) {
            var element = type.GetElementType()!;
            var array = Array.CreateInstance(element, e.GetArrayLength());
            for (int i = 0; i < array.Length; i++) array.SetValue(Decode(element, e[i], path + "/" + i), i);
            return array;
        }
        if (!type.IsValueType || type.IsPointer || type == typeof(IntPtr) || type == typeof(EntityArchetype))
            throw new ArgumentException("Unsupported captured type: " + type.FullName);
        object result = Activator.CreateInstance(type)!;
        var fields = type.GetFields(Fields);
        if (e.ValueKind != JsonValueKind.Object || e.EnumerateObject().Count() != fields.Length)
            throw new ArgumentException("Captured field count changed: " + path);
        foreach (var field in fields) {
            var value = e.GetProperty(field.Name); // Missing fields never become defaults.
            if (type == typeof(NetGeometryData) && field.FieldType == typeof(EntityArchetype)
                && field.Name is "m_EdgeCompositionArchetype" or "m_NodeCompositionArchetype") {
                if (value.GetProperty("representation").GetString() != "archetype-component-types")
                    throw new ArgumentException("Archetype representation changed");
                exclusions.Add(path + "/" + field.Name + ": outside generated stage read set; native handle not reconstructed");
                continue;
            }
            field.SetValue(result, Decode(field.FieldType, value, path + "/" + field.Name));
        }
        return result;
    }
    static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    static Dictionary<Entity, JsonElement> Rows(JsonElement capture) => capture.GetProperty("entities")
        .EnumerateArray().ToDictionary(r => Id(r.GetProperty("id")), r => r.GetProperty("components"));

    static void Compare(object actual, JsonElement expected, string path, List<object> differences, ref int count) {
        Type type = actual.GetType();
        if (type.IsEnum) actual = Convert.ChangeType(actual, Enum.GetUnderlyingType(type));
        if (actual.GetType().IsPrimitive) {
            object wanted = Decode(actual.GetType(), expected, path); count++;
            bool equal = actual is float f ? BitConverter.SingleToInt32Bits(f) == BitConverter.SingleToInt32Bits((float)wanted)
                : actual.Equals(wanted);
            if (!equal) differences.Add(new { path, native = wanted, replay = actual });
            return;
        }
        foreach (var field in type.GetFields(Fields)) Compare(field.GetValue(actual)!, expected.GetProperty(field.Name),
            path + "/" + field.Name, differences, ref count);
    }

    public static int Run(string entryPath, string exitPath, string output) {
        if (File.Exists(output)) throw new IOException("Refusing to overwrite evidence");
        exclusions.Clear();
        var game = typeof(GeometrySystem).Assembly;
        if (Hash(game.Location) != GameHash) throw new InvalidOperationException("Game hash changed");
        using var input = JsonDocument.Parse(File.ReadAllText(entryPath));
        using var expectedDocument = JsonDocument.Parse(File.ReadAllText(exitPath));
        var entry = input.RootElement; var exit = expectedDocument.RootElement;
        foreach (var capture in new[] { entry, exit }) {
            if (capture.GetProperty("schemaVersion").GetInt32() != 1 || !capture.GetProperty("complete").GetBoolean()
                || capture.GetProperty("job").GetString() != "Game.Net.GeometrySystem+CalculateEdgeGeometryJob"
                || capture.GetProperty("gameModuleVersionId").GetString() != game.ManifestModule.ModuleVersionId.ToString())
                throw new ArgumentException("Incomplete or incompatible native edge capture");
        }
        if (entry.GetProperty("phase").GetString() != "entry" || exit.GetProperty("phase").GetString() != "exit")
            throw new ArgumentException("Entry/exit phases required");
        foreach (var key in new[] { "operationId", "citySession", "pass" })
            if (entry.GetProperty(key).ToString() != exit.GetProperty(key).ToString()) throw new ArgumentException("Capture identity mismatch: " + key);
        var roots = entry.GetProperty("roots").EnumerateArray().Select(Id).ToArray();
        if (roots.Length == 0 || roots.Distinct().Count() != roots.Length
            || !roots.SequenceEqual(exit.GetProperty("roots").EnumerateArray().Select(Id)))
            throw new ArgumentException("Capture membership mismatch");
        var world = new ReplayWorld();
        var fields = entry.GetProperty("fields");
        var jobFields = typeof(CalculateEdgeGeometryJob).GetFields();
        var allowedTypes = jobFields.Where(f => f.FieldType.IsGenericType
                && f.FieldType.GetGenericTypeDefinition() is var definition
                && (definition == typeof(ReplayLookup<>) || definition == typeof(ReplayBufferLookup<>)))
            .ToDictionary(f => f.FieldType.GetGenericArguments()[0].FullName!, f =>
                f.FieldType.GetGenericTypeDefinition() == typeof(ReplayBufferLookup<>)
                    ? f.FieldType.GetGenericArguments()[0].MakeArrayType() : f.FieldType.GetGenericArguments()[0]);
        foreach (var (entity, cells) in Rows(entry)) foreach (var cell in cells.EnumerateObject()) {
            if (!allowedTypes.TryGetValue(cell.Name, out var type)) throw new ArgumentException("Unexpected component: " + cell.Name);
            string? presence = cell.Value.GetProperty("presence").GetString();
            if (presence == "absent") typeof(ReplayWorld).GetMethod("Absent")!.MakeGenericMethod(type).Invoke(world, new object[] { entity });
            else if (presence == "present") {
                object value = Decode(type, cell.Value.GetProperty("value"), entity + "/" + cell.Name);
                typeof(ReplayWorld).GetMethod("Record")!.MakeGenericMethod(type).Invoke(world, new[] { (object)entity, value });
            } else throw new ArgumentException("Uncaptured dependency: " + entity + "/" + cell.Name);
        }
        var job = WorldStageTests.Bind<CalculateEdgeGeometryJob>(world);
        job.m_Entities = new(i => roots[i], (_, _) => throw new InvalidOperationException("Read-only identity"), roots.Length);
        job.m_TerrainBounds = (Colossal.Mathematics.Bounds3)Decode(typeof(Colossal.Mathematics.Bounds3), fields.GetProperty("m_TerrainBounds"), "m_TerrainBounds");
        int writesBeforeProbe = world.Writes.Count;
        var seamProbe = EdgeSeamProbe.Run(job, roots);
        if (world.Writes.Count != writesBeforeProbe) throw new InvalidOperationException("Read-only seam probe wrote world state");
        int probeReadCount = world.Reads.Count;
        for (int i = 0; i < roots.Length; i++) job.Execute(i);
        var after = Rows(exit); var differences = new List<object>(); int count = 0;
        foreach (var entity in roots) {
            foreach (var type in new[] { typeof(EdgeGeometry), typeof(StartNodeGeometry), typeof(EndNodeGeometry) }) {
                var expected = after[entity].GetProperty(type.FullName!);
                if (expected.GetProperty("presence").GetString() != "present") throw new ArgumentException("Missing native output");
                var actual = typeof(ReplayWorld).GetMethod("Get")!.MakeGenericMethod(type).Invoke(world, new object[] { entity })!;
                Compare(actual, expected.GetProperty("value"), entity + "/" + type.Name, differences, ref count);
            }
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new {
            scope = "Bounded source-derived CalculateEdge; captured upstream input; sequential captured membership",
            passed = differences.Count == 0, gameSha256 = GameHash,
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            hardwareIntrinsicsOverride = Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic"),
            inputs = new { entryPath, sha256 = Hash(entryPath) }, expected = new { exitPath, sha256 = Hash(exitPath) },
            edges = roots.Length, comparedScalarFields = count, differences, excludedUnusedFields = exclusions,
            seamProbe, probeReadCount,
            reads = world.Reads, writes = world.Writes
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Native edge differential: {roots.Length} edges, {count} fields, {differences.Count} mismatches");
        return differences.Count == 0 ? 0 : 1;
    }
}
