using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

static unsafe class NativeProfileReplayTests {
    static double Y(JsonElement point) => point.GetProperty("y").GetDouble();
    static P XZ(JsonElement point) => new P(point.GetProperty("x").GetDouble(),point.GetProperty("z").GetDouble());
    static double Distance(JsonElement a,JsonElement b) { var x=XZ(a);var y=XZ(b);return Math.Sqrt((x.X-y.X)*(x.X-y.X)+(x.Z-y.Z)*(x.Z-y.Z)); }
    public static void Run() {
        using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"offset-profile-native.json")));
        var root=data.RootElement;
        var input=root.GetProperty("beforeEdges").EnumerateArray().ToArray();
        var expected=root.GetProperty("afterEdges").EnumerateArray().ToArray();
        var nodes=root.GetProperty("beforeNodes").EnumerateArray().ToDictionary(n=>n.GetProperty("index").GetInt32(),n=>Y(n.GetProperty("position")));
        var expectedNodes=root.GetProperty("afterNodes").EnumerateArray().ToDictionary(n=>n.GetProperty("index").GetInt32(),n=>Y(n.GetProperty("position")));
        var segments=new VerticalLinearProfile.Segment[input.Length];
        var ids=new int[input.Length+1];
        for(int i=0;i<input.Length;i++) {
            var e=input[i];var c=e.GetProperty("curve").EnumerateArray().ToArray();var forward=e.GetProperty("pathForward").GetBoolean();
            ids[i]=e.GetProperty(forward?"startNode":"endNode").GetProperty("index").GetInt32();
            ids[i+1]=e.GetProperty(forward?"endNode":"startNode").GetProperty("index").GetInt32();
            if(!VerticalLinearProfile.TryHorizontalLength(new PlanarCubic(XZ(c[0]),XZ(c[1]),XZ(c[2]),XZ(c[3])),out var length))throw new Exception("Native replay length rejected");
            segments[i]=new VerticalLinearProfile.Segment{Length=length,StartHandle=Distance(c[0],c[1]),EndHandle=Distance(c[2],c[3]),StartOffset=Y(c[0])-nodes[ids[i]],EndOffset=Y(c[3])-nodes[ids[i+1]]};
        }
        var output=new VerticalLinearProfile.Heights[input.Length];var heights=new double[ids.Length];
        fixed(VerticalLinearProfile.Segment* s=segments) fixed(VerticalLinearProfile.Heights* c=output) fixed(double* h=heights)
            if(!VerticalLinearProfile.Fit(s,segments.Length,nodes[ids[0]],nodes[ids[ids.Length-1]],false,0,false,0,h,c,out _))throw new Exception("Native profile replay rejected");
        double maximum=0;
        for(int i=0;i<output.Length;i++) {
            var actual=new[]{output[i].A,output[i].B,output[i].C,output[i].D};var c=expected[i].GetProperty("curve").EnumerateArray().ToArray();
            for(int j=0;j<4;j++)maximum=Math.Max(maximum,Math.Abs(actual[j]-Y(c[j])));
        }
        for(int i=0;i<ids.Length;i++)maximum=Math.Max(maximum,Math.Abs(heights[i]-expectedNodes[ids[i]]));
        if(maximum>.001)throw new Exception("Offline/native profile replay differs by "+maximum+" m");
        Console.WriteLine("PASS: retained native five-edge profile replay, maximum error "+maximum+" m (not a new live test).");
    }
}
