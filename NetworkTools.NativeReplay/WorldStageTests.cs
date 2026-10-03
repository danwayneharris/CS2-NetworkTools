using System.Reflection;
using System.Text.Json;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using NativeReplay;
using Unity.Entities;
using Unity.Mathematics;

static class WorldStageTests {
    static Entity Id(int id) => new() { Index = id, Version = 1 };
    static int checks;
    static void Check(bool condition, string description) {
        if (!condition) throw new Exception(description);
        checks++;
    }
    internal static T Bind<T>(ReplayWorld world) where T : struct {
        object job = new T();
        foreach (var field in typeof(T).GetFields()) {
            var type = field.FieldType;
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(ReplayLookup<>)
                || type.GetGenericTypeDefinition() == typeof(ReplayBufferLookup<>)))
                field.SetValue(job, Activator.CreateInstance(type, world));
        }
        return (T)job;
    }
    static ReplayWorld Initialization(bool updated, bool omitUpdated = false) {
        var world = new ReplayWorld();
        var node = Id(1); var prefab = Id(100); var composition = Id(101);
        world.Record(node, new Node { m_Position = new float3(0, 10, 0) });
        world.Record(node, new NodeGeometry());
        world.Record(node, new PrefabRef { m_Prefab = prefab });
        world.Absent<Temp>(node);
        world.Record(prefab, new NetGeometryData { m_MergeLayers = (Layer)1, m_Flags = GeometryFlags.SmoothElevation });
        world.Record(composition, new NetCompositionData());
        world.Record(node, new[] { new ConnectedEdge { m_Edge = Id(2) }, new ConnectedEdge { m_Edge = Id(3) } });
        foreach (int i in new[] { 2, 3 }) {
            var edge = Id(i);
            world.Record(edge, new Edge { m_Start = node, m_End = Id(10 + i) });
            world.Record(edge, new PrefabRef { m_Prefab = prefab });
            world.Record(edge, new Composition { m_StartNode = composition, m_EndNode = composition, m_Edge = composition });
            world.Record(edge, new Curve { m_Bezier = new Bezier4x3(new float3(0, 10, 0),
                new float3(10, i == 2 ? 14 : 18, 0), new float3(20, 20, 0), new float3(30, 20, 0)) });
            if (!omitUpdated) {
                if (updated) world.Record(edge, new Updated()); else world.Absent<Updated>(edge);
            }
        }
        return world;
    }
    static EdgeGeometry Surface(float height, bool reverse = false) {
        float x = reverse ? 1 : -1;
        return new EdgeGeometry {
            m_Start = new Segment {
                m_Right = new Bezier4x3(new float3(x, height, 0), new float3(x, height + .5f, 1), default, default),
                m_Left = new Bezier4x3(new float3(-x, height, 0), new float3(-x, height + .5f, 1), default, default)
            }
        };
    }
    static ReplayWorld FlattenWorld(bool temporary, bool mixed, float sentinel = 0) {
        var world = new ReplayWorld(); var node = Id(1); var prefab = Id(100);
        world.Record(node, new NodeGeometry { m_Position = 1, m_Bounds = new Bounds3(new float3(sentinel, 0, 0), default) });
        if (temporary) {
            world.Record(node, new Temp { m_Original = Id(50) });
            world.Absent<ConnectedEdge[]>(Id(50));
        } else world.Absent<Temp>(node);
        world.Record(node, new[] { new ConnectedEdge { m_Edge = Id(2) }, new ConnectedEdge { m_Edge = Id(3) } });
        world.Record(prefab, new NetGeometryData { m_MergeLayers = (Layer)1, m_MaxSlopeSteepness = .2f });
        foreach (int i in new[] { 2, 3 }) {
            var edge = Id(i);
            world.Record(edge, new Edge { m_Start = node, m_End = Id(10 + i) });
            world.Record(edge, new PrefabRef { m_Prefab = prefab });
            world.Record(edge, Surface(i == 2 ? 2 : 0, i == 3));
            world.Absent<Hidden>(edge);
            if (temporary && (!mixed || i == 2)) {
                world.Record(edge, new Temp { m_Original = Id(20 + i) });
                world.Record(Id(20 + i), Surface(7));
            } else world.Absent<Temp>(edge);
        }
        return world;
    }
    static ReplayMap<int2, float4> Flatten(ReplayWorld world) {
        var job = Bind<FlattenNodeGeometryJob>(world);
        var map = new ReplayMap<int2, float4>();
        job.m_EdgeHeightMap = map.Writer;
        job.Execute(new ReplayChunk(world, new[] { Id(1) }));
        return map;
    }
    static FinishEdgeGeometryJob Finish(ReplayWorld world, ReplayMap<int2, float4> map) {
        foreach (int id in new[] { 2, 3 }) {
            world.Record(Id(id), new Composition { m_Edge = Id(101) });
            world.Absent<Owner>(Id(id));
        }
        world.Record(Id(101), new NetCompositionData { m_Width = 2, m_HeightRange = new Bounds1(-1, 2) });
        var job = Bind<FinishEdgeGeometryJob>(world);
        job.m_Entities = new(i => Id(i + 2), (_, _) => throw new Exception("Read-only"), 2);
        job.m_EdgeHeightMap = map;
        return job;
    }
    internal static int Run(string output) {
        if (File.Exists(output)) throw new IOException("Refusing to overwrite evidence");
        foreach (bool updated in new[] { false, true }) {
            var world = Initialization(updated);
            var job = Bind<InitializeNodeGeometryJob>(world);
            job.Execute(new ReplayChunk(world, new[] { Id(1) }));
            var node = world.Get<NodeGeometry>(Id(1));
            Check(node.m_Position == 13, "Source initialization weighted height result");
            Check(node.m_Bounds.min.x == (updated ? 0 : 1), "Updated-dependent retention sentinel");
            Check(world.Writes.Count == 1, "One node write");
        }
        var loaded = Initialization(false);
        var loadJob = Bind<InitializeNodeGeometryJob>(loaded); loadJob.m_Loaded = true;
        loadJob.Execute(new ReplayChunk(loaded, new[] { Id(1) }));
        Check(loaded.Get<NodeGeometry>(Id(1)).m_Bounds.min.x == 0, "Loaded bypasses Updated sentinel");
        bool rejected = false;
        try {
            var unknown = Initialization(false, true);
            Bind<InitializeNodeGeometryJob>(unknown).Execute(new ReplayChunk(unknown, new[] { Id(1) }));
        } catch (InvalidOperationException e) { rejected = e.Message.StartsWith("Not captured:"); }
        Check(rejected, "Unknown is not silently absent");
        var nullWorld = new ReplayWorld();
        Check(!nullWorld.Try<Node>(Entity.Null, out _), "Null component lookup reports nonexistent");
        Check(!nullWorld.Try<ConnectedEdge[]>(Entity.Null, out _), "Null buffer lookup reports nonexistent");
        rejected = false;
        try { nullWorld.Get<Node>(Entity.Null); }
        catch (InvalidOperationException e) { rejected = e.Message.StartsWith("Required component absent:"); }
        Check(rejected, "Required null lookup still fails");
        foreach (bool temp in new[] { false, true }) {
            var map = Flatten(FlattenWorld(temp, false));
            Check(map.Values.Count == 2, "Flatten emits both participants");
            foreach (var value in map.Values.Values)
                Check(value.y == 1 && value.w == 1 && value.x == 1.5f && value.z == 1.5f,
                    "Pairwise heights meet node height and preserve handle deltas");
        }
        Check(Flatten(FlattenWorld(false, false, 1)).Values.Count == 0, "Retention sentinel skips flattening");
        var mixed = Flatten(FlattenWorld(true, true));
        Check(mixed.Values.Count == 1 && mixed.Values.ContainsKey(new int2(2, 0)), "Mixed branch writes temporary participant only");
        Check(mixed.Values[new int2(2, 0)].y == 7 && mixed.Values[new int2(2, 0)].x == 7.5f,
            "Mixed branch retains original surface and current handle delta");
        foreach (bool temporary in new[] { false, true }) {
            var world = FlattenWorld(temporary, false);
            var map = Flatten(world);
            var finish = Finish(world, map);
            finish.Execute(0); finish.Execute(1);
            foreach (int id in new[] { 2, 3 }) {
                var surface = world.Get<EdgeGeometry>(Id(id));
                Check(surface.m_Start.m_Left.a.y == 1 && surface.m_Start.m_Right.a.y == 1,
                    "Finishing consumes computed flatten endpoints");
                Check(surface.m_Start.m_Left.b.y == 1.5f && surface.m_Start.m_Right.b.y == 1.5f,
                    "Finishing consumes computed flatten handles");
                Check(math.all(math.isfinite(surface.m_Start.m_Length)) && surface.m_Start.m_Length.x > 0,
                    "Finishing computes finite lengths");
                Check(surface.m_Bounds.min.y <= 0 && surface.m_Bounds.max.y >= 3,
                    "Finishing includes composition height range");
            }
            Check(world.Writes.Count == 2, "Finishing publishes two edge writes");
        }
        var terrainWorld = FlattenWorld(false, false);
        var terrainFinish = Finish(terrainWorld, Flatten(terrainWorld));
        terrainWorld.Record(Id(101), new NetCompositionData { m_Width = 2, m_State = CompositionState.LowerToTerrain });
        rejected = false;
        try { terrainFinish.Execute(0); }
        catch (InvalidOperationException e) { rejected = e.Message.Contains("terrain sampler"); }
        Check(rejected && terrainWorld.Writes.Count == 0, "Unsupported terrain cannot silently publish fabricated bounds");
        var cyclic = new ReplayWorld { ReadLimit = 20 };
        cyclic.Record(Id(1), new Temp { m_Original = Id(1) });
        cyclic.Record(Id(1), Array.Empty<ConnectedEdge>());
        rejected = false;
        try {
            var iterator = new ReplayEdgeIterator(Entity.Null, Id(1), new(cyclic), new(cyclic), new(cyclic), new(cyclic));
            iterator.GetNext(out _);
        } catch (InvalidOperationException e) { rejected = e.Message.Contains("read budget"); }
        Check(rejected, "Malformed cyclic original references terminate explicitly");
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed = true, assertions = checks,
            scope = "Synthetic explicit worlds; source-derived algorithms with managed storage adapters; not live differential qualification" },
            new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS {checks} source-world assertions");
        return 0;
    }
}
