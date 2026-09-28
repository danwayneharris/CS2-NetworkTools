using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

// Replays captured solver inputs, not the game's network rebuild or validation.
internal static class TraceReplay {
    private static P Point(JsonElement value) => new P(value[0].GetDouble(), value[2].GetDouble());

    public static unsafe void Run(string path) {
        var count = 0;
        var mismatches = 0;
        foreach (var line in File.ReadLines(path)) {
            const string marker = "[NetworkTools.SmoothTrace] ";
            var offset = line.IndexOf(marker, StringComparison.Ordinal);
            if (offset < 0) continue;
            using var document = JsonDocument.Parse(line.Substring(offset + marker.Length));
            var root = document.RootElement;
            if (!root.TryGetProperty("nodes", out var nodeData)) continue;
            var nodes = nodeData.EnumerateArray().Select(n => {
                var p = Point(n.GetProperty("input"));
                p.Fixed = n.GetProperty("pinned").GetBoolean();
                return p;
            }).ToArray();
            var curves = root.GetProperty("edges").EnumerateArray().Select(e => {
                var p = e.GetProperty("input").EnumerateArray().Select(Point).ToArray();
                if (!e.GetProperty("forward").GetBoolean()) Array.Reverse(p);
                return new PlanarCubic(p[0], p[1], p[2], p[3]);
            }).ToArray();
            var output = new P[nodes.Length];
            var outputCurves = new PlanarCubic[curves.Length];
            var scratch = new double[nodes.Length];
            var failure = SmoothFailure.PathCountMismatch;
            var index = -1;
            var valid = false;
            if (nodes.Length >= 2 && curves.Length == nodes.Length - 1) {
                fixed (P* n = nodes, o = output)
                fixed (PlanarCubic* c = curves, co = outputCurves)
                fixed (double* s = scratch) {
                    valid = PlanarPathTarget.Fit(n, c, nodes.Length, root.GetProperty("strength").GetDouble(),
                        o, co, s, out failure, out index);
                }
            }
            Console.WriteLine($"trace {root.GetProperty("id")}: recorded={root.GetProperty("valid")}, replay={valid}, reason={failure}, index={index}");
            if (valid != root.GetProperty("valid").GetBoolean()) mismatches++;
            count++;
        }
        if (count == 0) throw new Exception("No detailed SmoothTrace records found");
        Console.WriteLine($"Replayed {count} captures. Adapter and game validation are not replayed.");
        if (mismatches > 0) throw new Exception($"{mismatches} acceptance differences; inspect adapter checks and capture version");
    }
}
