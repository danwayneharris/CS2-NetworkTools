// Minimal managed test doubles. The production path search is linked unchanged.
// These do not verify native memory behavior, Unity safety checks, or heap tie order.
namespace Unity.Entities {
    public readonly record struct Entity(int Index, int Version = 1) {
        public static Entity Null => default;
    }
    public sealed class DynamicBuffer<T> : List<T> { public int Length => Count; }
    public sealed class EntityManager {
        readonly Dictionary<(Entity, Type), object> data = new();
        public void Set<T>(Entity entity, T value) => data[(entity, typeof(T))] = value;
        public bool TryGetComponent<T>(Entity entity, out T value) {
            if (data.TryGetValue((entity, typeof(T)), out var boxed)) { value = (T)boxed; return true; }
            value = default; return false;
        }
        public bool HasComponent<T>(Entity entity) => data.ContainsKey((entity, typeof(T)));
        public T GetComponentData<T>(Entity entity) => (T)data[(entity, typeof(T))];
        public bool HasBuffer<T>(Entity entity) => data.ContainsKey((entity, typeof(DynamicBuffer<T>)));
        public DynamicBuffer<T> GetBuffer<T>(Entity entity) => GetComponentData<DynamicBuffer<T>>(entity);
        public bool TryGetBuffer<T>(Entity entity, bool readOnly, out DynamicBuffer<T> buffer) => TryGetComponent(entity, out buffer);
    }
}
namespace Game.Net {
    public struct ConnectedEdge { public Unity.Entities.Entity m_Edge; }
    public struct Edge { public Unity.Entities.Entity m_Start, m_End; }
    public struct Curve { public float m_Length; }
}
namespace Game.Prefabs { public struct PrefabRef { public Unity.Entities.Entity m_Prefab; } }
namespace Colossal.Entities { } // Production extension methods are supplied by the manager double.
namespace Unity.Collections {
    public enum Allocator { Temp }
    public sealed class NativeList<T> : List<T>, IDisposable {
        public NativeList(int capacity, Allocator allocator) : base(capacity) { }
        public int Length => Count;
        public void Dispose() { }
    }
    public sealed class NativeQueue<T> : Queue<T>, IDisposable { public NativeQueue(Allocator a) { } public void Dispose() { } }
    public sealed class NativeHashSet<T> : HashSet<T>, IDisposable { public NativeHashSet(int n, Allocator a) { } public void Dispose() { } }
    public sealed class NativeHashMap<K,V> : Dictionary<K,V>, IDisposable { public NativeHashMap(int n, Allocator a) { } public void Dispose() { } }
}
namespace Colossal.Collections {
    public interface ILessThan<T> { bool LessThan(T other); }
    public sealed class NativeMinHeap<T> : IDisposable where T : ILessThan<T> {
        readonly List<T> items = new();
        public NativeMinHeap(int n, Unity.Collections.Allocator a) { }
        public int Length => items.Count;
        public void Insert(T item) => items.Add(item);
        public T Extract() {
            int best = 0;
            for (int i = 1; i < items.Count; ++i) if (items[i].LessThan(items[best])) best = i;
            var result = items[best]; items.RemoveAt(best); return result;
        }
        public void Dispose() { }
    }
}
namespace NetworkTools.Systems.Tools {
    public abstract partial class NT_PathSelectionToolSystem {
        protected Unity.Entities.EntityManager EntityManager = new();
        readonly Unity.Collections.NativeList<Unity.Entities.Entity> m_CurrentPathNodes = new(4, Unity.Collections.Allocator.Temp);
        readonly Log m_Log = new();
        sealed class Log { public void Debug(string message) { } }
    }
}
