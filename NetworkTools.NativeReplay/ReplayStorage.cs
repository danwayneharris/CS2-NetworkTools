using System.Runtime.InteropServices;
using Unity.Entities;

namespace NativeReplay;

// Unknown is distinct from an explicitly absent component. All reads are traced.
public sealed class ReplayWorld {
    public int ReadLimit { get; init; } = 100000;
    readonly Dictionary<(Entity, Type), object?> cells = new();
    public readonly List<string> Reads = new();
    public readonly List<string> Writes = new();
    public void Record<T>(Entity entity, T value) => cells[(entity, typeof(T))] = value;
    public void Absent<T>(Entity entity) => cells[(entity, typeof(T))] = null;
    public bool Try<T>(Entity entity, out T value) {
        if (Reads.Count >= ReadLimit) throw new InvalidOperationException("Captured-state read budget exceeded; possible cyclic references");
        Reads.Add($"{entity.Index}:{entity.Version}/{typeof(T).FullName}");
        if (!cells.TryGetValue((entity, typeof(T)), out var cell))
            throw new InvalidOperationException($"Not captured: {entity.Index}:{entity.Version}/{typeof(T).FullName}");
        value = cell is null ? default! : (T)cell;
        return cell is not null;
    }
    public T Get<T>(Entity entity) => Try<T>(entity, out var value) ? value
        : throw new InvalidOperationException($"Required component absent: {entity}/{typeof(T).FullName}");
    public void Set<T>(Entity entity, T value) {
        Get<T>(entity); // Job writes must target existing captured storage.
        cells[(entity, typeof(T))] = value;
        Writes.Add($"{entity.Index}:{entity.Version}/{typeof(T).FullName}");
    }
}

public struct ReplayLookup<T>(ReplayWorld world) {
    public bool HasComponent(Entity entity) => world.Try<T>(entity, out _);
    public bool TryGetComponent(Entity entity, out T value) => world.Try(entity, out value);
    public T this[Entity entity] { get => world.Get<T>(entity); set => world.Set(entity, value); }
}
public struct ReplayBuffer<T>(T[] values) {
    public int Length => values.Length;
    public T this[int index] => values[index];
}
public struct ReplayBufferLookup<T>(ReplayWorld world) {
    public ReplayBuffer<T> this[Entity entity] => new(world.Get<T[]>(entity));
    public bool TryGetBuffer(Entity entity, out ReplayBuffer<T> buffer) {
        bool found = world.Try<T[]>(entity, out var value);
        buffer = new(value);
        return found;
    }
}
public struct ReplayEntityType { }
public struct ReplayComponentType<T> { }
public sealed class ReplayArray<T>(Func<int, T> read, Action<int, T> write, int length) {
    public int Length => length;
    public T this[int i] { get => read(i); set => write(i, value); }
}
public readonly struct ReplayChunk(ReplayWorld world, Entity[] entities) {
    public ReplayArray<Entity> GetNativeArray(ReplayEntityType handle) {
        var capturedEntities = entities;
        return new(i => capturedEntities[i],
            (_, _) => throw new InvalidOperationException("Entity identity is read-only"), entities.Length);
    }
    public ReplayArray<T> GetNativeArray<T>(ref ReplayComponentType<T> _) {
        var capturedWorld = world;
        var capturedEntities = entities;
        return new(i => capturedWorld.Get<T>(capturedEntities[i]),
            (i, value) => capturedWorld.Set(capturedEntities[i], value), entities.Length);
    }
    public bool Has<T>(ref ReplayComponentType<T> handle) {
        if (entities.Length == 0) throw new InvalidOperationException("Empty chunk unsupported");
        bool present = world.Try<T>(entities[0], out _);
        foreach (var e in entities)
            if (world.Try<T>(e, out _) != present)
                throw new InvalidOperationException("Replay chunk must have homogeneous component presence");
        return present;
    }
}
public sealed class ReplayList<T> : IDisposable {
    readonly List<T> values;
    public ReplayList(int capacity, int _) { values = new(capacity); }
    public int Length => values.Count;
    public void Add(in T value) => values.Add(value);
    public T this[int index] => values[index];
    public ref T ElementAt(int index) => ref CollectionsMarshal.AsSpan(values)[index];
    public void Clear() => values.Clear();
    public void Dispose() { }
}
public sealed class ReplayMap<TKey, TValue> where TKey : notnull {
    public readonly Dictionary<TKey, TValue> Values = new();
    public bool TryGetValue(TKey key, out TValue value) => Values.TryGetValue(key, out value!);
    public ParallelWriter Writer => new(this);
    public readonly struct ParallelWriter(ReplayMap<TKey, TValue> map) {
        public bool TryAdd(TKey key, TValue value) => map.Values.TryAdd(key, value);
    }
}

public struct ReplayTerrainData { }
public static class ReplayTerrain {
    public static float SampleHeight(ref ReplayTerrainData data, Unity.Mathematics.float3 position) =>
        throw new InvalidOperationException("Not captured: terrain sampler required by FinishEdgeGeometry; terrain execution unsupported");
}
