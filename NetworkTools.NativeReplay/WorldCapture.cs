using System.Text.Json;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using NativeReplay;
using Unity.Entities;
using Unity.Mathematics;

// Schema 1 is an explicit field projection for the two supported stages, not a
// generic serializer. Fields not read by these stages are deliberately excluded.
static class WorldCapture {
    static JsonElement P(JsonElement e, string name) => e.GetProperty(name);
    static float F(JsonElement e) {
        float f = e.GetSingle();
        return float.IsFinite(f) ? f : throw new ArgumentException("Nonfinite field");
    }
    static float3 V(JsonElement e) {
        if (e.GetArrayLength() != 3) throw new ArgumentException("Expected XYZ");
        return new(F(e[0]), F(e[1]), F(e[2]));
    }
    static Entity E(JsonElement e) {
        if (e.GetArrayLength() != 2) throw new ArgumentException("Expected entity index/version");
        return new() { Index = e[0].GetInt32(), Version = e[1].GetInt32() };
    }
    static Bezier4x3 C(JsonElement e) {
        if (e.GetArrayLength() != 4) throw new ArgumentException("Expected four controls");
        return new(V(e[0]), V(e[1]), V(e[2]), V(e[3]));
    }
    static Segment S(JsonElement e) => new() { m_Left = C(P(e, "left")), m_Right = C(P(e, "right")) };
    static CompositionFlags Flags(JsonElement e) => new() {
        m_General = (CompositionFlags.General)P(e, "general").GetUInt64(),
        m_Left = (CompositionFlags.Side)P(e, "left").GetUInt32(),
        m_Right = (CompositionFlags.Side)P(e, "right").GetUInt32()
    };
    static void Put<T>(ReplayWorld world, Entity entity, JsonElement value, Func<JsonElement, T> decode) {
        if (value.ValueKind == JsonValueKind.Null) world.Absent<T>(entity);
        else world.Record(entity, decode(value));
    }
    static void Cell(ReplayWorld world, Entity entity, string name, JsonElement v) {
        switch (name) {
            case "Node": Put(world, entity, v, x => new Node { m_Position = V(P(x, "position")) }); break;
            case "NodeGeometry": Put(world, entity, v, x => new NodeGeometry {
                m_Position = F(P(x, "position")), m_Flatness = F(P(x, "flatness")), m_Offset = F(P(x, "offset")),
                m_Bounds = new Bounds3(V(P(x, "boundsMin")), V(P(x, "boundsMax"))) }); break;
            case "Edge": Put(world, entity, v, x => new Edge { m_Start = E(P(x, "start")), m_End = E(P(x, "end")) }); break;
            case "Curve": Put(world, entity, v, x => new Curve { m_Bezier = C(x) }); break;
            case "PrefabRef": Put(world, entity, v, x => new PrefabRef { m_Prefab = E(x) }); break;
            case "Composition": Put(world, entity, v, x => new Composition {
                m_Edge = E(P(x, "edge")), m_StartNode = E(P(x, "start")), m_EndNode = E(P(x, "end")) }); break;
            case "Temp": Put(world, entity, v, x => new Temp { m_Original = E(P(x, "original")), m_Flags = (TempFlags)P(x, "flags").GetInt32() }); break;
            case "Hidden": Put(world, entity, v, x => Tag<Hidden>(x)); break;
            case "Updated": Put(world, entity, v, x => Tag<Updated>(x)); break;
            case "Owner": Put(world, entity, v, x => new Owner { m_Owner = E(x) }); break;
            case "NetGeometryData": Put(world, entity, v, x => new NetGeometryData {
                m_MergeLayers = (Layer)P(x, "mergeLayers").GetUInt64(), m_Flags = (GeometryFlags)P(x, "flags").GetUInt64(),
                m_MaxSlopeSteepness = F(P(x, "maxSlope")) }); break;
            case "NetCompositionData": Put(world, entity, v, x => new NetCompositionData { m_Flags = Flags(x) }); break;
            case "ConnectedEdge": Put(world, entity, v, x => x.EnumerateArray().Select(n => new ConnectedEdge { m_Edge = E(n) }).ToArray()); break;
            case "EdgeGeometry": Put(world, entity, v, x => new EdgeGeometry { m_Start = S(P(x, "start")), m_End = S(P(x, "end")) }); break;
            default: throw new ArgumentException("Unsupported component projection: " + name);
        }
    }
    static T Tag<T>(JsonElement e) where T : struct {
        if (e.ValueKind != JsonValueKind.True) throw new ArgumentException("Tag must be true (present) or null (absent)");
        return new();
    }
    internal static int Run(string fixture, string output) {
        if (File.Exists(output)) throw new IOException("Refusing to overwrite evidence");
        using var document = JsonDocument.Parse(File.ReadAllText(fixture));
        var root = document.RootElement;
        if (P(root, "schemaVersion").GetInt32() != 1) throw new ArgumentException("Unsupported world schema");
        var assembly = typeof(GeometrySystem).Assembly;
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assembly.Location)));
        if (!string.Equals(hash, P(root, "gameSha256").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Game hash changed");
        var world = new ReplayWorld(); var seen = new HashSet<Entity>();
        foreach (var row in P(root, "entities").EnumerateArray()) {
            Entity id = E(P(row, "id"));
            if (!seen.Add(id)) throw new ArgumentException("Duplicate entity record");
            var components = new HashSet<string>();
            foreach (var field in P(row, "components").EnumerateObject()) {
                if (!components.Add(field.Name)) throw new ArgumentException("Duplicate component record");
                Cell(world, id, field.Name, field.Value);
            }
        }
        var nodes = P(root, "nodes").EnumerateArray().Select(E).ToArray();
        if (nodes.Length == 0 || nodes.Distinct().Count() != nodes.Length) throw new ArgumentException("Empty/duplicate node input");
        bool loaded = P(root, "loaded").GetBoolean();
        var reports = new List<object>();
        foreach (var stage in P(root, "stages").EnumerateArray()) {
            // One entity per replay chunk: homogeneous component layout by construction.
            // This does not infer native query membership or inter-chunk scheduling.
            var map = new ReplayMap<int2, float4>();
            foreach (var node in nodes) {
                var chunk = new ReplayChunk(world, new[] { node });
                switch (stage.GetString()) {
                    case "InitializeNodeGeometry":
                        var initialize = WorldStageTests.Bind<InitializeNodeGeometryJob>(world);
                        initialize.m_Loaded = loaded; initialize.Execute(chunk); break;
                    case "FlattenNodeGeometry":
                        var flatten = WorldStageTests.Bind<FlattenNodeGeometryJob>(world);
                        flatten.m_EdgeHeightMap = map.Writer; flatten.Execute(chunk); break;
                    default: throw new ArgumentException("Unsupported world stage");
                }
            }
            reports.Add(new { stage = stage.GetString(), nodes = nodes.Select(n => {
                var g = world.Get<NodeGeometry>(n);
                return new { id = new[] { n.Index, n.Version }, position = g.m_Position, flatness = g.m_Flatness,
                    offset = g.m_Offset, retentionSentinel = g.m_Bounds.min.x };
            }).ToArray(), heightMap = map.Values.Select(kv => new { key = new[] { kv.Key.x, kv.Key.y },
                value = new[] { kv.Value.x, kv.Value.y, kv.Value.z, kv.Value.w } }).ToArray() });
        }
        if (reports.Count == 0) throw new ArgumentException("No stages");
        File.WriteAllText(output, JsonSerializer.Serialize(new { schemaVersion = 1, gameSha256 = hash,
            scope = "Source-derived stage replay; explicit node list, no scheduler; projected inputs", stages = reports,
            reads = world.Reads, writes = world.Writes }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Executed {reports.Count} source world stages; {world.Reads.Count} captured-state reads");
        return 0;
    }
}
