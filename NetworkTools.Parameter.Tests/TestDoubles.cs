namespace Unity.Mathematics {
    public readonly record struct float3(float x, float y, float z);
    public readonly record struct float4(float x, float y, float z, float w);
    public readonly record struct quaternion(float4 value);
}
namespace Colossal.Logging {
    public interface ILog { void Debug(string message); void Warn(string message); }
    public sealed class TestLog : ILog {
        public readonly List<string> Warnings = new();
        public void Debug(string message) { }
        public void Warn(string message) => Warnings.Add(message);
    }
}
namespace NetworkTools {
    public sealed class NetworkToolsMod {
        public static NetworkToolsMod Instance { get; } = new();
        public Colossal.Logging.TestLog Log { get; } = new();
    }
}
namespace NetworkTools.Systems.Tools.Handles { public interface IHandleSpec<T> { } }
