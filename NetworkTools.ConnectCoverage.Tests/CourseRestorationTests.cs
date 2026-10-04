using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;
using C=NetworkTools.Geometry.ConnectProfileCoverage.Cubic;
using M=NetworkTools.Geometry.ConnectProfileCoverage.Mapping;
using P=NetworkTools.Geometry.PlanarFairing.Point;
using H=NetworkTools.Geometry.VerticalLinearProfile.Heights;

internal static unsafe class CourseRestorationTests {
    static int checks;
    static void Check(bool value,string name) { checks++; if(!value) throw new Exception(name); }
    static C Read(JsonElement e) {
        var p=e.EnumerateArray().Select(x=>x.EnumerateArray().Select(v=>v.GetDouble()).ToArray()).ToArray();
        return new C { Horizontal=new PlanarCubic(new P(p[0][0],p[0][2]),new P(p[1][0],p[1][2]),new P(p[2][0],p[2][2]),new P(p[3][0],p[3][2])),
            Vertical=new H { A=p[0][1],B=p[1][1],C=p[2][1],D=p[3][1] } };
    }
    static bool Restore(C[] a,C[] n,out C[] result) {
        result=Enumerable.Repeat(new C { Vertical=new H { A=1234,B=1234,C=1234,D=1234 } },n.Length).ToArray();
        var before=(C[])n.Clone();
        fixed(C* ap=a,np=n,rp=result) {
            var ok=ConnectProfileCoverage.RestoreHeights(ap,a.Length,np,n.Length,rp,out _,out _);
            for(var i=0;i<n.Length;i++) Check(n[i].Equals(before[i]),"input immutability");
            if(!ok) foreach(var c in result) Check(c.Vertical.A==1234&&c.Vertical.D==1234,"atomic refusal");
            return ok;
        }
    }
    static bool Valid(C[] a,C[] n) {
        var maps=new M[n.Length];
        fixed(C* ap=a,np=n) fixed(M* mp=maps) return ConnectProfileCoverage.Validate(ap,a.Length,np,n.Length,mp,out _,out _);
    }
    public static void Run() {
        Check(ConnectCourseElevation.Classify(3.99f,4,-100,100,false)==0,"ground threshold");
        Check(ConnectCourseElevation.Classify(4,4,-100,100,false)==4,"elevated boundary inclusive");
        Check(ConnectCourseElevation.Classify(-4,4,-100,100,false)==-4,"tunnel boundary inclusive");
        Check(ConnectCourseElevation.Classify(-8,4,0,100,false)==0,"prefab forbids negative elevation");
        Check(ConnectCourseElevation.Classify(8,4,-100,-4,false)==-4,"negative-only prefab clamp");
        Check(ConnectCourseElevation.Classify(8,4,-100,100,true)==0,"explicit endpoint side transition");
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures/connect-course-height.json")));
        var a=doc.RootElement.GetProperty("authored").EnumerateArray().Select(Read).ToArray();
        var n=doc.RootElement.GetProperty("native").EnumerateArray().Select(Read).ToArray();
        Check(!Valid(a,n),"captured native vertical regeneration must fail original strict oracle");
        Check(Restore(a,n,out var repaired),"captured horizontal subdivision is recoverable");
        Check(Valid(a,repaired),"restored captured case passes unmodified oracle");
        for(var i=0;i<n.Length;i++) Check(n[i].Horizontal.Equals(repaired[i].Horizontal),"native XZ bitwise preserved");
        Check(Restore(a,repaired,out var again),"idempotent second construction");
        Check(again.SequenceEqual(repaired),"idempotent bitwise outputs");
        var reverse=n.Reverse().Select(c=>new C { Horizontal=new PlanarCubic(c.Horizontal.D,c.Horizontal.C,c.Horizontal.B,c.Horizontal.A),Vertical=new H{A=c.Vertical.D,B=c.Vertical.C,C=c.Vertical.B,D=c.Vertical.A}}).ToArray();
        Check(Restore(a,reverse,out var r)&&Valid(a,r),"reversed shuffled subdivision");
        Check(!Restore(a,n.Skip(1).ToArray(),out _),"missing course refuses");
        Check(!Restore(a,n.Concat(new[]{n[0]}).ToArray(),out _),"duplicate course refuses");
        var bad=(C[])n.Clone();bad[2].Horizontal.B.Z+=.1;
        Check(!Restore(a,bad,out _),"changed horizontal control refuses");
        bad=(C[])n.Clone();bad[3].Vertical.C=double.NaN;
        Check(!Restore(a,bad,out _),"nonfinite later course refuses atomically");
        bad=(C[])repaired.Clone();bad[1].Vertical.B+=.051;
        Check(!Valid(a,bad),"correction does not weaken native acceptance");
        Console.WriteLine($"Connect course restoration: {checks} assertions passed (captured subdivision; not native execution)");
    }
}
