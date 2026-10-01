using System;
using System.Collections.Generic;
using NetworkTools.Geometry;

internal static class JunctionSearchTests {
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }
    internal static void Run() {
        var search = new JunctionCandidateSearch();
        var key = new object[] { "path", 7, 3.0 };
        search.Reset(key, false);
        Check(!search.WarmStart && search.Degrees == 0, "cold starts at zero");
        var seen = new HashSet<int>();
        do {
            Check(seen.Add(search.Degrees) && Math.Abs(search.Degrees) <= 15, "unique bounded candidates");
            if (search.Degrees == 12) search.RememberAccepted();
        } while (search.Advance());
        Check(seen.Count == 31, "complete cold search");
        search.Reset(key, false);
        Check(search.WarmStart && search.Degrees == 12, "warm start remembers accepted +12");
        seen.Clear();
        do { Check(seen.Add(search.Degrees), "warm duplicate"); } while (search.Advance());
        Check(seen.Count == 31, "warm fallback retains every candidate");
        search.Reset(new object[] { "path", 8, 3.0 }, false);
        Check(!search.WarmStart && search.Degrees == 0, "entity version invalidates hint");
        search.Reset(null, false);
        Check(!search.WarmStart, "missing source cannot warm start");
        search.Reset(key, true);
        Check(search.Degrees == 0 && !search.Advance(), "zero strength must never rotate");
        var stable = new StableSetObservation<int>();
        Check(!stable.Observe(new() { 1, 2 }) && !stable.Observe(new() { 2, 1 })
            && stable.Observe(new() { 1, 2 }), "three equal unordered observations");
        Check(!stable.Observe(new() { 1, 3 }), "changed lane set resets streak");
        stable.Observe(new() { 1, 3 });
        Check(!stable.Observe(null) && !stable.Observe(new() { 1, 3 }), "missing resets streak");
        stable.Reset();
        Check(!stable.Observe(new() { 1, 3 }), "new submission resets streak");
        Console.WriteLine("Junction candidate ordering and observation stability checks passed");
    }
}
