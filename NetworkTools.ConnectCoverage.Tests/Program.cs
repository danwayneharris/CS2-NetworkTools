using System;
using NetworkTools.Geometry;
using C=NetworkTools.Geometry.ConnectProfileCoverage.Cubic;
using M=NetworkTools.Geometry.ConnectProfileCoverage.Mapping;
using F=NetworkTools.Geometry.ConnectProfileCoverage.Failure;
using H=NetworkTools.Geometry.VerticalLinearProfile.Heights;
using P=NetworkTools.Geometry.PlanarFairing.Point;
internal static unsafe class Program {
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Near(double a,double b,string why){Check(double.IsFinite(b)&&Math.Abs(a-b)<1e-8,why);}
    static C Curve(double x0,double x1,double y0,double y1)=>new C{Horizontal=new PlanarCubic(new P(x0,0),new P(x0+(x1-x0)/3,10),new P(x1-(x1-x0)/3,10),new P(x1,0)),Vertical=new H{A=y0,B=y0+1,C=y1-1,D=y1}};
    static P Mix(P a,P b,double t)=>new P(a.X+(b.X-a.X)*t,a.Z+(b.Z-a.Z)*t);
    // Independent de Casteljau oracle: split horizontal and vertical control polygons.
    static void Split(C c,double t,out C left,out C right){
        var p=new[]{c.Horizontal.A,c.Horizontal.B,c.Horizontal.C,c.Horizontal.D};var y=new[]{c.Vertical.A,c.Vertical.B,c.Vertical.C,c.Vertical.D};
        var la=new P[4];var ra=new P[4];var ly=new double[4];var ry=new double[4];la[0]=p[0];ra[3]=p[3];ly[0]=y[0];ry[3]=y[3];
        for(var level=1;level<4;level++){for(var i=0;i<4-level;i++){p[i]=Mix(p[i],p[i+1],t);y[i]+= (y[i+1]-y[i])*t;}la[level]=p[0];ra[3-level]=p[3-level];ly[level]=y[0];ry[3-level]=y[3-level];}
        left=new C{Horizontal=new PlanarCubic(la[0],la[1],la[2],la[3]),Vertical=new H{A=ly[0],B=ly[1],C=ly[2],D=ly[3]}};
        right=new C{Horizontal=new PlanarCubic(ra[0],ra[1],ra[2],ra[3]),Vertical=new H{A=ry[0],B=ry[1],C=ry[2],D=ry[3]}};
    }
    static C Reverse(C c)=>new C{Horizontal=new PlanarCubic(c.Horizontal.D,c.Horizontal.C,c.Horizontal.B,c.Horizontal.A),Vertical=new H{A=c.Vertical.D,B=c.Vertical.C,C=c.Vertical.B,D=c.Vertical.A}};
    static bool Validate(C[] a,C[] n,out M[] mappings,out F failure,double tolerance=.05){
        mappings=new M[n.Length];for(var i=0;i<n.Length;i++)mappings[i]=new M{AuthoredIndex=-123,StartParameter=123,EndParameter=456};
        fixed(C* ap=a,np=n)fixed(M* mp=mappings){var ok=ConnectProfileCoverage.Validate(ap,a.Length,np,n.Length,mp,out failure,out _,tolerance,tolerance);if(!ok)foreach(var m in mappings){Check(m.AuthoredIndex==-123,"failure atomic identity");Near(123,m.StartParameter,"failure atomic start");Near(456,m.EndParameter,"failure atomic end");}return ok;}
    }
    static void Main(){
        CourseRestorationTests.Run();
        var c=Curve(0,100,20,30);Split(c,.37,out var first,out var last);
        Check(Validate(new[]{c},new[]{Reverse(last),first},out var m,out _),"split reversed shuffled coverage");Check(m[0].Reversed&&!m[1].Reversed,"orientation mapping");Near(.37,m[0].StartParameter,"split parameter");Near(1,m[0].EndParameter,"end parameter");Near(0,m[1].StartParameter,"start parameter");
        var c2=Curve(100,160,30,25);Split(c2,.6,out var secondA,out var secondB);
        Check(Validate(new[]{c,c2},new[]{secondB,Reverse(first),last,secondA},out m,out _),"two authored curves one-to-one coverage");Check(m[0].AuthoredIndex==1&&m[1].AuthoredIndex==0,"authored ownership");
        Check(Validate(new[]{c},new[]{c},out m,out _),"unsplit exact");
        var changed=c;changed.Vertical.B+=.049;changed.Horizontal.C.Z+=.049;
        Check(Validate(new[]{c},new[]{changed},out m,out _),"within 5cm whole-curve control bound");Near(.049,m[0].MaximumYControlError,"Y error measured");Near(.049,m[0].MaximumXZControlError,"XZ error measured");
        changed.Vertical.B+=.002;Check(!Validate(new[]{c},new[]{changed},out _,out var f)&&f==F.CurveMismatch,"over 5cm Y rejects");
        changed=c;changed.Horizontal.C.Z+=.051;Check(!Validate(new[]{c},new[]{changed},out _,out f)&&f==F.CurveMismatch,"over 5cm XZ rejects");
        Check(!Validate(new[]{c},new[]{first},out _,out f)&&f==F.Gap,"missing tail gap");Check(!Validate(new[]{c},new[]{last},out _,out f)&&f==F.Gap,"missing head gap");
        Check(!Validate(new[]{c},new[]{first,last,last},out _,out f)&&f==F.Overlap,"duplicate native interval rejects");
        Split(c,.45,out var overlapping,out _);Check(!Validate(new[]{c},new[]{overlapping,last},out _,out f)&&f==F.Overlap,"overlap rejects");
        Split(c,.2,out var shortFirst,out _);Check(!Validate(new[]{c},new[]{shortFirst,last},out _,out f)&&f==F.Gap,"internal interval gap rejects");
        var separated=last;separated.Vertical.A+=.01;Check(Validate(new[]{c},new[]{first,separated},out _,out _),"small Y join discrepancy accepted within geometric tolerance");
        separated=last;separated.Horizontal.A.Z+=.01;Check(Validate(new[]{c},new[]{first,separated},out _,out _),"small XZ join discrepancy accepted within geometric tolerance");
        var separatedFirst=first;separatedFirst.Vertical.D-=.03;separated=last;separated.Vertical.A+=.03;
        Check(!Validate(new[]{c},new[]{separatedFirst,separated},out _,out f)&&f==F.DisconnectedNative,"join discrepancy over 5cm rejects even though each curve differs by only 3cm");
        Check(!Validate(new[]{c,c2},new[]{c},out _,out f)&&f==F.Gap,"missing entire authored curve rejects");
        // Two traversals over the same authored geometry are ambiguous even with reversed orientation.
        Check(!Validate(new[]{c,Reverse(c)},new[]{c,Reverse(c)},out _,out f)&&f==F.AmbiguousMapping,"ambiguous authored ownership rejects");
        var backtracking=c;backtracking.Horizontal.B.X=150;backtracking.Horizontal.C.X=-50;
        Check(!Validate(new[]{backtracking},new[]{backtracking},out _,out f)&&f==F.UnsupportedHorizontalMapping,"nonmonotone projection explicit unsupported");
        var collapsed=c;collapsed.Horizontal.D=collapsed.Horizontal.A;Check(!Validate(new[]{collapsed},new[]{collapsed},out _,out f)&&f==F.UnsupportedHorizontalMapping,"closed chord unsupported");
        var nan=c;nan.Vertical.C=double.NaN;Check(!Validate(new[]{c},new[]{first,nan},out _,out f)&&f==F.InvalidInput,"late NaN leaves all mappings untouched");
        Check(!Validate(new[]{c},new[]{c},out _,out f,.051)&&f==F.InvalidInput,"cannot relax beyond 5cm");
        // Random deterministic partitioning against independent de Casteljau slices.
        var seed=new Random(4172);
        for(var trial=0;trial<50;trial++){
            var cut=.05+.9*seed.NextDouble();Split(c,cut,out first,out last);
            Check(Validate(new[]{c},new[]{Reverse(first),last},out m,out _),"random partition");Near(cut,m[0].EndParameter,"random recovered parameter");
        }
        Console.WriteLine($"Connect profile coverage: {checks} assertions passed (pure curve coverage; no lane/topology claim)");
    }
}
