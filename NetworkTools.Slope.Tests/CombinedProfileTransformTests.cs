using System;
using Colossal.Mathematics;
using NetworkTools.Geometry;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Mathematics;

internal static unsafe class CombinedProfileTransformTests {
    private static int checks;
    private static void Check(bool ok,string why) { checks++; if(!ok) throw new Exception(why); }
    private static void Near(double a,double b,string why) => Check(Math.Abs(a-b)<1e-5,why);
    private static bool Fit(EdgeState[] e,NodeState[] n,EdgeState[] o,out CombinedLinearProfileTransform.Failure reason) {
        var s=new VerticalLinearProfile.Segment[e.Length];var a=new SectionedVerticalProfile.Anchor[n.Length];
        var h=new double[n.Length];var c=new VerticalLinearProfile.Heights[e.Length];
        fixed(EdgeState* ep=e) fixed(NodeState* np=n) fixed(EdgeState* op=o)
        fixed(VerticalLinearProfile.Segment* sp=s) fixed(SectionedVerticalProfile.Anchor* ap=a)
        fixed(double* hp=h) fixed(VerticalLinearProfile.Heights* cp=c)
            return CombinedLinearProfileTransform.Execute(ep,np,op,e.Length,default,default,sp,ap,hp,cp,out reason,out _);
    }
    public static void Run() {
        var original=new[]{new EdgeState{IsForward=true,Bezier=new Bezier4x3(new float3(0,0,0),new float3(10,1,0),new float3(20,2,0),new float3(30,3,0))},
            new EdgeState{IsForward=true,Bezier=new Bezier4x3(new float3(30,3,0),new float3(40,4,0),new float3(50,5,0),new float3(60,6,0))}};
        var nodes=new[]{new NodeState{OriginalPosition=new float3(0,0,0),Position=new float3(0,0,0)},
            new NodeState{OriginalPosition=new float3(30,3,0),Position=new float3(30,3,0),SmoothSplit=true},
            new NodeState{OriginalPosition=new float3(60,6,0),Position=new float3(60,6,0)}};
        var e=(EdgeState[])original.Clone();e[0].Bezier.c.x=25;e[1].Bezier.b.x=35;
        Check(Fit(e,nodes,original,out _),"compatible pin");Near(e[0].Bezier.c.y,2.5,"incoming handle follows new horizontal length");
        Near(e[1].Bezier.b.y,3.5,"outgoing handle follows new horizontal length");Near(nodes[1].Position.y,3,"pin height");
        var repeated=(EdgeState[])e.Clone();Check(Fit(e,nodes,original,out _),"deterministic repeat");
        Near(math.distance(e[0].Bezier.c,repeated[0].Bezier.c),0,"repeat control");
        var reverseOriginal=new EdgeState[original.Length];var reverseEdges=new EdgeState[e.Length];
        var reverseNodes=new NodeState[nodes.Length];
        for(var i=0;i<e.Length;i++) {
            reverseOriginal[i]=original[e.Length-1-i];reverseOriginal[i].IsForward=false;
            reverseEdges[i]=e[e.Length-1-i];reverseEdges[i].IsForward=false;
        }
        for(var i=0;i<nodes.Length;i++) reverseNodes[i]=nodes[nodes.Length-1-i];
        Check(Fit(reverseEdges,reverseNodes,reverseOriginal,out _),"reversed production fit");
        for(var i=0;i<e.Length;i++) {
            var c=reverseEdges[e.Length-1-i].Bezier;
            Near(math.distance(e[i].Bezier.a,c.a),0,"reversed A");Near(math.distance(e[i].Bezier.b,c.b),0,"reversed B");
            Near(math.distance(e[i].Bezier.c,c.c),0,"reversed C");Near(math.distance(e[i].Bezier.d,c.d),0,"reversed D");
        }
        original[1].Bezier.b.y=8;
        var before=(EdgeState[])e.Clone();
        Check(!Fit(e,nodes,original,out var why)&&why==CombinedLinearProfileTransform.Failure.SplitGradeConflict,"conflicting pin rejected");
        Near(math.distance(e[0].Bezier.c,before[0].Bezier.c),0,"failed fit does not publish controls");
        nodes[1].SmoothSplit=false;nodes[1].SmoothPinned=true;
        Check(Fit(e,nodes,original,out _),"junction distinct original branch grades");
        Near(e[1].Bezier.b.y,5.5,"junction outgoing grade preserved");Near(nodes[1].Position.y,3,"junction stays fixed");
        nodes[1].SmoothPinned=false;
        Check(Fit(e,nodes,original,out _),"free interior");Near(e[0].Bezier.c.y,2.5,"unconstrained constant grade");
        nodes[0].SmoothPinned=true;original[0].Bezier.b.y=9;
        Check(Fit(e,nodes,original,out _),"fixed dead end remains a height anchor only");
        Near(e[0].Bezier.b.y,1,"dead end does not accidentally preserve original grade");
        nodes[0].SmoothJunction=true;
        Check(Fit(e,nodes,original,out _),"terminal junction grade anchor");
        Near(e[0].Bezier.b.y,9,"terminal junction retains authored grade");
        var bad=(EdgeState[])original.Clone();bad[1].Bezier.d.y=float.NaN;
        before=(EdgeState[])e.Clone();Check(!Fit(e,nodes,bad,out _),"invalid later input");
        Near(math.distance(e[0].Bezier.c,before[0].Bezier.c),0,"late failure atomic");
        Console.WriteLine($"PASS {checks} combined transform assertions against production code");
    }
}
