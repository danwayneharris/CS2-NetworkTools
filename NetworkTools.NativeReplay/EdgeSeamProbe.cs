using System.Reflection;
using Colossal.Mathematics;
using NativeReplay;
using Unity.Entities;
using Unity.Mathematics;

// Isolates source adaptation from binary value methods on the SAME .NET runtime.
// These offsets are computed offline, not advertised as native intermediate truth.
static class EdgeSeamProbe {
    public static object Run(CalculateEdgeGeometryJob job, Entity[] roots) {
        var generated = typeof(CalculateEdgeGeometryJob);
        var binary = typeof(Game.Net.GeometrySystem).GetNestedType("CalculateEdgeGeometryJob", BindingFlags.NonPublic)!;
        var binaryJob = Activator.CreateInstance(binary)!;
        var offsets = generated.GetMethod("CalculateOffsets")!;
        var names = offsets.GetParameters().Select((p,i) => (p.Name!,i)).ToDictionary(x => x.Item1,x => x.i);
        var differences = new List<object>(); int values = 0, calls = 0;
        void Compare(object source, object native, string path) {
            if (source is float f) {
                values++;
                if (BitConverter.SingleToInt32Bits(f) != BitConverter.SingleToInt32Bits((float)native))
                    differences.Add(new { path, source = f, binary = native });
            } else foreach (var field in source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Compare(field.GetValue(source)!, field.GetValue(native)!, path + "/" + field.Name);
        }
        object Invoke(Type type, object instance, string method, object[] args) => type.GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance,args)!;
        foreach (var entity in roots) {
            object[] args = new object[offsets.GetParameters().Length]; args[0] = entity;
            offsets.Invoke(job,args);
            var startOffset=(float2)args[names["startOffsets"]]; var endOffset=(float2)args[names["endOffsets"]];
            var composition=(Game.Prefabs.NetCompositionData)args[names["edgeCompositionData"]];
            foreach (string side in new[] { "left", "right" }) {
                var start=(Bezier4x3)args[names[side+"StartCurve"]]; var end=(Bezier4x3)args[names[side+"EndCurve"]];
                float a=side=="left" ? startOffset.x : startOffset.y, b=side=="left" ? endOffset.x : endOffset.y;
                object[] inputs={start,end,a,b,composition.m_Width};
                object sourceCut=Invoke(generated,job,"CalculateCutOffset",inputs);
                object binaryCut=Invoke(binary,binaryJob,"CalculateCutOffset",inputs);
                Compare(sourceCut,binaryCut,entity+"/"+side+"/CalculateCutOffset"); calls++;
                // Use the SAME binary cut parameter in both bodies to isolate Cut itself.
                inputs[4]=binaryCut;
                Compare(Invoke(generated,job,"Cut",inputs),Invoke(binary,binaryJob,"Cut",inputs),entity+"/"+side+"/Cut"); calls++;
            }
        }
        return new { scope="Source-derived offset inputs; same-runtime source/binary pure-method differential; not native intermediate capture",
            calls, comparedFloat32Values=values, differences, passed=differences.Count==0 };
    }
}
