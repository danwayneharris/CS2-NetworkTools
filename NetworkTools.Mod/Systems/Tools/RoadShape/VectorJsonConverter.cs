#if IS_DEBUG
namespace NetworkTools.Automation {
    using System;
    using Newtonsoft.Json;
    using Unity.Mathematics;

    // Unity vector swizzles recursively return vectors; serialize coordinates only.
    internal sealed class VectorJsonConverter : JsonConverter {
        public override bool CanConvert(Type t) => t == typeof(float2) || t == typeof(float3) || t == typeof(float4);
        public override bool CanRead => false;
        public override object ReadJson(JsonReader r, Type t, object v, JsonSerializer s) => throw new NotSupportedException();
        public override void WriteJson(JsonWriter w, object value, JsonSerializer s) {
            w.WriteStartArray();
            if (value is float2 a) { w.WriteValue(a.x); w.WriteValue(a.y); }
            else if (value is float3 b) { w.WriteValue(b.x); w.WriteValue(b.y); w.WriteValue(b.z); }
            else { var c = (float4)value; w.WriteValue(c.x); w.WriteValue(c.y); w.WriteValue(c.z); w.WriteValue(c.w); }
            w.WriteEndArray();
        }
        internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            Converters = { new VectorJsonConverter() }
        };
    }
}
#endif
