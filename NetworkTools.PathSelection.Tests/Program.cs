using Game.Net;
using Game.Prefabs;
using NetworkTools.Systems.Tools;
using Unity.Collections;
using Unity.Entities;

sealed class Graph : NT_PathSelectionToolSystem {
    public record Link(Entity Id, Entity A, Entity B, float Length, int Prefab);
    public readonly List<Link> Links = new();
    public Entity Add(int a, int b, float length, int prefab = 1, bool reverseStorage = false) {
        var id = new Entity(100 + Links.Count);
        Entity start = new(a), end = new(b);
        Links.Add(new(id, start, end, length, prefab));
        EntityManager.Set(id, new Edge { m_Start = reverseStorage ? end : start, m_End = reverseStorage ? start : end });
        EntityManager.Set(id, new Curve { m_Length = length });
        if (prefab != 0) EntityManager.Set(id, new PrefabRef { m_Prefab = new Entity(1000 + prefab) });
        foreach (var node in new[] { start, end }) {
            if (!EntityManager.HasBuffer<ConnectedEdge>(node)) EntityManager.Set(node, new DynamicBuffer<ConnectedEdge>());
            EntityManager.GetBuffer<ConnectedEdge>(node).Add(new ConnectedEdge { m_Edge = id });
        }
        return id;
    }
    public (bool Found, Entity[] Nodes, Entity[] Edges) Find(int start, int end) {
        var nodes = new NativeList<Entity>(4, Allocator.Temp);
        var edges = new NativeList<Entity>(4, Allocator.Temp);
        // Exercise stale output clearing too.
        nodes.Add(new Entity(-1)); edges.Add(new Entity(-1));
        var found = FindPathBetween(new Entity(start), new Entity(end), ref nodes, ref edges);
        return (found, nodes.ToArray(), edges.ToArray());
    }
    public float Cost(Entity[] edges) {
        float cost = 0; int previous = 0;
        foreach (var id in edges) {
            var link = Links.Single(e => e.Id == id);
            cost += link.Length + (previous != 0 && link.Prefab != 0 && previous != link.Prefab ? 9.9f : 0);
            previous = link.Prefab;
        }
        return cost;
    }
    // Independent exhaustive SIMPLE-path oracle, adequate for small graphs with
    // nonnegative lengths and known prefabs: cycling cannot improve this metric.
    public float Oracle(int start, int end) {
        float best = float.PositiveInfinity;
        var seen = new HashSet<int> { start };
        void Visit(int node, int prefab, float cost) {
            if (node == end) { best = Math.Min(best, cost); return; }
            foreach (var edge in Links.Where(e => e.A.Index == node || e.B.Index == node)) {
                int next = edge.A.Index == node ? edge.B.Index : edge.A.Index;
                if (!seen.Add(next)) continue;
                Visit(next, edge.Prefab, cost + edge.Length + (prefab != 0 && prefab != edge.Prefab ? 9.9f : 0));
                seen.Remove(next);
            }
        }
        Visit(start, 0, 0); return best;
    }
}

static class Program {
    static int checks;
    static void Require(bool ok, string message) { ++checks; if (!ok) throw new Exception(message); }
    static void Valid(Graph g, (bool Found, Entity[] Nodes, Entity[] Edges) r, int start, int end) {
        Require(r.Found && r.Nodes.Length == r.Edges.Length + 1, "path reconstruction counts");
        Require(r.Nodes[0].Index == start && r.Nodes[^1].Index == end, "path endpoints");
        for (int i = 0; i < r.Edges.Length; ++i) {
            var edge = g.Links.Single(e => e.Id == r.Edges[i]);
            Require((edge.A == r.Nodes[i] && edge.B == r.Nodes[i+1]) || (edge.B == r.Nodes[i] && edge.A == r.Nodes[i+1]), "edge/node correspondence");
            if (i != 0) Require(r.Edges[i] != r.Edges[i-1], "no immediate backtracking");
        }
    }
    static void Main() {
        // Audit F04: J reached at 10 on A or 11 on B; departing J on B costs 1.
        // Node-only settlement returns 20.9, arrival-aware settlement must return 12.
        foreach (bool reverse in new[] { false, true }) {
            var g = new Graph();
            g.Add(1, 2, 10, 1, reverse);
            var better = g.Add(1, 2, 11, 2, !reverse);
            var final = g.Add(2, 3, 1, 2, reverse);
            var r = g.Find(1, 3); Valid(g, r, 1, 3);
            Require(r.Edges.SequenceEqual(new[] { better, final }) && Math.Abs(g.Cost(r.Edges) - 12) < .0001, "F04 discarded-arrival counterexample");
            var back = g.Find(3, 1); Valid(g, back, 3, 1);
            Require(back.Edges.SequenceEqual(r.Edges.Reverse()), "reversed traversal picks same unique optimum");
        }
        var ties = new Graph();
        ties.Add(1, 2, 1); ties.Add(2, 4, 1); ties.Add(1, 3, 1); ties.Add(3, 4, 1);
        var tie = ties.Find(1, 4); Valid(ties, tie, 1, 4);
        Require(ties.Cost(tie.Edges) == 2, "equal-cost alternatives remain optimal (no native tie-order promise)");
        var disconnected = ties.Find(1, 99);
        Require(!disconnected.Found && disconnected.Nodes.Length == 0 && disconnected.Edges.Length == 0, "disconnected cyclic graph terminates with empty output");
        var same = ties.Find(1, 1);
        Require(same.Found && same.Nodes.Length == 0 && same.Edges.Length == 0, "same-node legacy empty success");
        var unknown = new Graph(); unknown.Add(1, 2, 2, 0); unknown.Add(2, 3, 3, 2);
        var u = unknown.Find(1, 3); Valid(unknown, u, 1, 3);
        Require(unknown.Cost(u.Edges) == 5, "missing prefab retains zero transition penalty");
        var rng = new Random(704);
        for (int trial = 0; trial < 100; ++trial) {
            var g = new Graph();
            for (int a = 1; a <= 6; ++a) for (int b = a+1; b <= 6; ++b)
                if (rng.Next(3) == 0) g.Add(a, b, rng.Next(0, 15), rng.Next(1, 4), rng.Next(2) == 0);
            foreach (var ends in new[] { (1,6), (6,1) }) {
                var expected = g.Oracle(ends.Item1, ends.Item2);
                var r = g.Find(ends.Item1, ends.Item2);
                Require(r.Found == !float.IsPositiveInfinity(expected), "exhaustive reachability");
                if (!r.Found) continue;
                Valid(g, r, ends.Item1, ends.Item2);
                Require(Math.Abs(g.Cost(r.Edges) - expected) < .001, "exhaustive minimum cost");
            }
        }
        Console.WriteLine($"Path selection production-source tests passed: {checks} assertions; 200 exhaustive small-graph comparisons.");
    }
}
