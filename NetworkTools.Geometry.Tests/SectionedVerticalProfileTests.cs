using System;
using NetworkTools.Geometry;
using S = NetworkTools.Geometry.VerticalLinearProfile.Segment;
using H = NetworkTools.Geometry.VerticalLinearProfile.Heights;
using A = NetworkTools.Geometry.SectionedVerticalProfile.Anchor;

internal static unsafe class SectionedVerticalProfileTests {
    private static int checks;
    private static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    private static void Near(double x, double y, string why) => Check(double.IsFinite(y) && Math.Abs(x-y)<1e-9, why);
    private static bool Fit(S[] s, A[] a, out double[] heights, out H[] curves) {
        heights=new double[s.Length+1]; curves=new H[s.Length];
        fixed(S* p=s) fixed(A* q=a) fixed(double* h=heights) fixed(H* c=curves)
            return SectionedVerticalProfile.Fit(p,s.Length,q,h,c,out _);
    }
    public static void Run() {
        var s=new[]{new S{Length=30,StartHandle=7,EndHandle=8,StartOffset=.2,EndOffset=-.1},
            new S{Length=70,StartHandle=15,EndHandle=20,StartOffset=.3,EndOffset=.1},
            new S{Length=15,StartHandle=4,EndHandle=3,StartOffset=-.2,EndOffset=.1},
            new S{Length=45,StartHandle=9,EndHandle=12,StartOffset=.1,EndOffset=-.1}};
        var a=new[]{new A{Fixed=true,Height=100},default(A),
            new A{Fixed=true,Height=111,MatchIncoming=true,MatchOutgoing=true,IncomingGrade=.04,OutgoingGrade=.04},
            default(A),new A{Fixed=true,Height=103}};
        Check(Fit(s,a,out var h,out var c),"pinned fit");
        Near(100,h[0],"first height");Near(111,h[2],"pin height");Near(103,h[4],"last height");
        Near(.04,(c[1].D-c[1].C)/s[1].EndHandle,"incoming pin grade");
        Near(.04,(c[2].B-c[2].A)/s[2].StartHandle,"outgoing pin grade");
        for(var i=0;i<s.Length;i++) {
            Near(h[i]+s[i].StartOffset,c[i].A,"start offset");
            Near(h[i+1]+s[i].EndOffset,c[i].D,"end offset");
        }
        var rev=new S[s.Length];var ra=new A[a.Length];
        for(var i=0;i<s.Length;i++){var t=s[s.Length-1-i];rev[i]=new S{Length=t.Length,StartHandle=t.EndHandle,EndHandle=t.StartHandle,StartOffset=t.EndOffset,EndOffset=t.StartOffset};}
        for(var i=0;i<a.Length;i++){var t=a[a.Length-1-i];ra[i]=new A{Fixed=t.Fixed,Height=t.Height,MatchIncoming=t.MatchOutgoing,MatchOutgoing=t.MatchIncoming,IncomingGrade=-t.OutgoingGrade,OutgoingGrade=-t.IncomingGrade};}
        Check(Fit(rev,ra,out var rh,out var rc),"reverse fit");
        for(var i=0;i<h.Length;i++)Near(h[i],rh[h.Length-1-i],"reverse heights");
        for(var i=0;i<c.Length;i++){var t=rc[c.Length-1-i];Near(c[i].A,t.D,"reverse A");Near(c[i].B,t.C,"reverse B");Near(c[i].C,t.B,"reverse C");Near(c[i].D,t.A,"reverse D");}
        Check(Fit(s,a,out var h2,out var c2),"deterministic fit");for(var i=0;i<c.Length;i++){Near(c[i].B,c2[i].B,"same B");Near(c[i].C,c2[i].C,"same C");}
        // Applying the result changes only free node heights; fixed anchors remain identical.
        for(var i=0;i<a.Length;i++){var t=a[i];t.Height=h[i];a[i]=t;}
        Check(Fit(s,a,out h2,out c2),"repeat fit");for(var i=0;i<h.Length;i++)Near(h[i],h2[i],"repeat height");
        // Shortening horizontal travel requires recomputing grade, not preserving old Y handles.
        var shortPath=(S[])s.Clone();shortPath[0].Length=20;
        Check(Fit(shortPath,a,out h2,out c2),"changed stationing");Check(Math.Abs(c[0].B-c2[0].B)>.01,"grade follows horizontal length");Near(111,h2[2],"changed station pin");
        var invalid=(A[])a.Clone();invalid[2].IncomingGrade=double.NaN;Check(!Fit(s,invalid,out _,out _),"invalid grade");
        invalid=(A[])a.Clone();invalid[0].Fixed=false;Check(!Fit(s,invalid,out _,out _),"missing outer anchor");
        invalid=(A[])a.Clone();invalid[1].MatchIncoming=true;Check(!Fit(s,invalid,out _,out _),"constraint on nonanchor");
        var bad=(S[])s.Clone();bad[3].Length=0;Check(!Fit(bad,a,out _,out _),"late span failure");
        // Different branch grades remain representable for junctions; ordinary split callers
        // must explicitly enforce their common-grade contract before invoking this primitive.
        invalid=(A[])a.Clone();invalid[2].OutgoingGrade=-.03;Check(Fit(s,invalid,out _,out c2),"explicit junction grades");Near(-.03,(c2[2].B-c2[2].A)/s[2].StartHandle,"no invented grade averaging");
        Console.WriteLine($"Sectioned vertical profile: {checks} assertions passed");
    }
}
