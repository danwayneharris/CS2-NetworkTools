using NetworkTools;
using NetworkTools.Systems.Tools.Parameters;
using Unity.Mathematics;

sealed class DoubleParameter : Parameter<double> {
    public DoubleParameter(double value) : base("double", value) { }
}
static class Program {
    static int checks;
    static void Require(bool value, string message) { ++checks; if (!value) throw new Exception(message); }
    static void Main() {
        var p = new FloatParameter("strength", .5f, 0, 1);
        int changes = 0; ChangeOrigin last = default;
        p.OnChanged += origin => { ++changes; last = origin; };
        foreach (var origin in new[] { ChangeOrigin.Code, ChangeOrigin.Handle, ChangeOrigin.Dependency })
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
                Require(!p.TrySetValue(bad, origin), "reject nonfinite at every mutation origin");
                Require(p.Value == .5f && changes == 0, "retain previous value without event");
            }
        p.Value = float.NaN;
        p.SetValue(float.NegativeInfinity, ChangeOrigin.Handle);
        Require(p.Value == .5f && changes == 0, "legacy property and setter use boundary");
        Require(p.TrySetValue(.5f, ChangeOrigin.Code) && changes == 0, "equal accepted value stays quiet");
        Require(p.TrySetValue(42f, ChangeOrigin.Handle) && p.Value == 42 && changes == 1 && last == ChangeOrigin.Handle,
            "finite handle value beyond UI metadata intentionally remains allowed");
        foreach (string bad in new[] { "NaN", "Infinity", "-Infinity", "1e9999", "garbage", "" }) {
            Require(!p.TryDeserializeValue(bad), "bad persisted input reports failure");
            Require(p.Value == 42 && changes == 1, "bad persistence retains previous valid value");
        }
        Require(p.TryDeserializeValue("0.75") && p.Value == .75f && changes == 2, "valid invariant-culture persistence");
        p.ResetToDefault(); Require(p.Value == .5f && changes == 3, "reset valid default notifies change");
        p.ResetToDefault(); Require(changes == 3, "unchanged reset stays quiet");
        Require(NetworkToolsMod.Instance.Log.Warnings.Count >= 17, "rejections/persistence failures emit diagnostics");
        var d = new DoubleParameter(2);
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Require(!d.TrySetValue(bad, ChangeOrigin.Code) && d.Value == 2, "double finite guard");
        Require(!d.TryDeserializeValue("NaN") && d.Value == 2, "double persistence finite guard");
        var v = new Float3Parameter("position", new float3(1, 2, 3)); int vectors = 0;
        v.OnChanged += _ => ++vectors;
        foreach (var bad in new[] { new float3(float.NaN,2,3), new float3(1,float.PositiveInfinity,3), new float3(1,2,float.NegativeInfinity) })
            Require(!v.TrySetValue(bad, ChangeOrigin.Handle) && v.Value == new float3(1,2,3) && vectors == 0, "every vector component guarded");
        var q = new QuaternionParameter("rotation", new quaternion(new float4(0,0,0,1)));
        foreach (var bad in new[] { new float4(float.NaN,0,0,1), new float4(0,float.NaN,0,1), new float4(0,0,float.NaN,1), new float4(0,0,0,float.NaN) })
            Require(!q.TrySetValue(new quaternion(bad), ChangeOrigin.Code) && q.Value.value.w == 1, "every quaternion component guarded");
        bool threw = false;
        try { new FloatParameter("bad", float.NaN, 0, 1); } catch (ArgumentException) { threw = true; }
        Require(threw, "invalid developer default rejected before instance can hold it");
        var integer = new IntParameter("count", 2, 0, 10); integer.Value = 20;
        Require(integer.Value == 20 && !integer.TryDeserializeValue("999999999999999999999"), "integer range policy unchanged; overflow rejected");
        Require(integer.Value == 20, "integer overflow does not mutate");
        // Persistence must not swallow a subscriber bug and falsely claim parsing failed
        // after the new value was already installed.
        p.OnChanged += _ => throw new InvalidOperationException("subscriber bug");
        threw = false;
        try { p.TryDeserializeValue("0.8"); } catch (InvalidOperationException) { threw = true; }
        Require(threw && p.Value == .8f, "subscriber errors are not mislabeled as parse rejection");
        Console.WriteLine($"Parameter production-source tests passed: {checks} assertions.");
    }
}
