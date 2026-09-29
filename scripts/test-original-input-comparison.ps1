$ErrorActionPreference = 'Stop'
$source = Get-Content (Join-Path $PSScriptRoot '../NetworkTools.Mod/Geometry/OriginalInputComparison.cs') -Raw
$tests = @'
public static class OriginalComparisonChecks {
    private struct Value { public int Index, Version; public float Position; }
    private static void Check(object[] a, object[] b, string expected) {
        string actual = NetworkTools.Geometry.OriginalInputComparison.Compare(a, b);
        if (actual != expected) throw new System.Exception("Expected " + expected + ", got " + actual);
    }
    public static void Run() {
        var value = new Value { Index=12, Version=1, Position=5 };
        var submitted = new object[] { value, true };
        Check(submitted, new object[] { value, true }, "matches");
        value.Position=9;
        Check(submitted, new object[] { value, true }, "changed");
        value.Position=5; value.Version=2;
        Check(submitted, new object[] { value, true }, "changed");
        value.Version=1;
        Check(submitted, new object[] { value, false }, "changed");
        Check(submitted, new object[] { true, value }, "changed");
        Check(submitted, new object[0], "changed");
        Check(null, submitted, "unavailable");
        Check(submitted, null, "unavailable");
        if (NetworkTools.Geometry.OriginalInputComparison.Observe(2, 1, submitted, submitted) != "stale_revision")
            throw new System.Exception("Delayed revision was not rejected");
        if (NetworkTools.Geometry.OriginalInputComparison.Observe(0, 0, submitted, submitted) != "stale_revision")
            throw new System.Exception("Uninitialized revision was not rejected");
        if (NetworkTools.Geometry.OriginalInputComparison.Observe(2, 2, submitted, submitted) != "matches")
            throw new System.Exception("Current observation mismatch");
    }
}
'@
Add-Type -TypeDefinition ($source + "`n" + $tests)
[OriginalComparisonChecks]::Run()
Write-Output '11 original-input comparison checks passed (actual shared C# helper).'
