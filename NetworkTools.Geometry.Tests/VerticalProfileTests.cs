using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;
using S = NetworkTools.Geometry.VerticalLinearProfile.Segment;
using H = NetworkTools.Geometry.VerticalLinearProfile.Heights;

static unsafe class VerticalProfileTests {
    static int checks;
    static void Near(double a,double b,string label,double epsilon=1e-9) {
        checks++;
        if (!double.IsFinite(b) || Math.Abs(a-b)>epsilon) throw new Exception(label+": "+a+" != "+b);
    }
    static bool Fit(S[] segments,double first,double last,bool start,bool end,out double[] nodes,out H[] curves,out double grade,double startGrade=.03,double endGrade=-.02) {
        nodes=new double[segments.Length+1];curves=new H[segments.Length];
        fixed(S* s=segments) fixed(double* n=nodes) fixed(H* c=curves)
            return VerticalLinearProfile.Fit(s,segments.Length,first,last,start,startGrade,end,endGrade,n,c,out grade);
    }
    public static void Run() {
        // Analytic straight, uneven segments with nonzero unequal offsets. Short/long
        // adjacency and translated absolute elevation must not alter fitted grade.
        var input=new[]{new S{Length=3,StartHandle=1,EndHandle=1,StartOffset=2,EndOffset=.5},
            new S{Length=300,StartHandle=100,EndHandle=100,StartOffset=-.25,EndOffset=1}};
        foreach(double first in new[]{0.0,600.0}) foreach(double rise in new[]{-30.0,0.0,60.0})
        foreach(bool smoothStart in new[]{false,true}) foreach(bool smoothEnd in new[]{false,true}) {
            if(!Fit(input,first,first+rise,smoothStart,smoothEnd,out var ns,out var cs,out var g))throw new Exception("Valid profile rejected");
            Near((rise-.25)/303,g,"analytic global grade");Near(first,ns[0],"fixed start");Near(first+rise,ns[2],"fixed end");
            for(int i=0;i<input.Length;i++) {
                var s=input[i];var c=cs[i];
                Near(s.StartOffset,c.A-ns[i],"start offset");Near(s.EndOffset,c.D-ns[i+1],"end offset");
                Near(g,(c.D-c.A)/s.Length,"secant grade");
                Near(i==0&&smoothStart?.03:g,(c.B-c.A)/s.StartHandle,"start derivative");
                Near(i==input.Length-1&&smoothEnd?-.02:g,(c.D-c.C)/s.EndHandle,"end derivative");
            }
            var reverse=new S[input.Length];
            for(int i=0;i<input.Length;i++){var segment=input[input.Length-1-i];reverse[i]=new S{Length=segment.Length,StartHandle=segment.EndHandle,EndHandle=segment.StartHandle,StartOffset=segment.EndOffset,EndOffset=segment.StartOffset};}
            if(!Fit(reverse,first+rise,first,smoothEnd,smoothStart,out var rn,out var rc,out var rg,.02,-.03))throw new Exception("Reverse rejected");
            Near(-g,rg,"reverse grade");for(int i=0;i<ns.Length;i++)Near(ns[i],rn[ns.Length-1-i],"reverse heights");
            for(int i=0;i<cs.Length;i++) {
                var r=rc[cs.Length-1-i];
                Near(cs[i].A,r.D,"reverse A");Near(cs[i].B,r.C,"reverse B");
                Near(cs[i].C,r.B,"reverse C");Near(cs[i].D,r.A,"reverse D");
            }
        }
        var line=new PlanarCubic(new P(0,0),new P(30,40),new P(60,80),new P(90,120));
        if(!VerticalLinearProfile.TryHorizontalLength(line,out var len))throw new Exception("Straight length rejected");Near(150,len,"analytic length");
        var bend=new PlanarCubic(new P(0,0),new P(0,100),new P(100,100),new P(100,0));
        if(!VerticalLinearProfile.TryHorizontalLength(bend,out len))throw new Exception("Bend rejected");Near(200,len,"analytic curved length",1e-6);
        if(VerticalLinearProfile.TryHorizontalLength(new PlanarCubic(),out _))throw new Exception("Collapsed curve accepted");
        foreach(double invalid in new[]{0.0,-1.0,double.NaN,double.PositiveInfinity}) {
            var bad=(S[])input.Clone();bad[0].Length=invalid;
            if(Fit(bad,0,1,false,false,out _,out _,out _))throw new Exception("Invalid length accepted");
        }
        var zeroHandle=(S[])input.Clone();zeroHandle[0].StartHandle=0;
        if(Fit(zeroHandle,0,1,false,false,out _,out _,out _))throw new Exception("Undefined grade accepted");
        // Fixed endpoint heights plus a conflicting boundary grade cannot yield an
        // everywhere-constant profile. The requested boundary derivative wins locally.
        var single=new[]{new S{Length=30,StartHandle=10,EndHandle=10}};
        if(!Fit(single,0,3,true,false,out _,out var conflict,out var common))throw new Exception("Boundary fit rejected");
        Near(.1,common,"mean rise grade");Near(.03,(conflict[0].B-conflict[0].A)/10,"boundary override");
        Near(.1175,(.25*(conflict[0].B-conflict[0].A)+.5*(conflict[0].C-conflict[0].B)+.25*(conflict[0].D-conflict[0].C))/10,"interior grade compensation");
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity}) {
            if(Fit(single,invalid,1,false,false,out _,out _,out _))throw new Exception("Invalid fixed height accepted");
            if(Fit(single,0,1,true,false,out _,out _,out _,invalid))throw new Exception("Invalid enabled anchor accepted");
            var badOffset=(S[])single.Clone();badOffset[0].EndOffset=invalid;
            if(Fit(badOffset,0,1,false,false,out _,out _,out _))throw new Exception("Invalid offset accepted");
        }
        Console.WriteLine("Vertical profile: "+checks+" analytic assertions plus invalid-input checks passed.");
    }
}