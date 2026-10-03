using System;
using NetworkTools.Geometry;
using P=NetworkTools.Geometry.LanePlaneIntersection.Point;
using C=NetworkTools.Geometry.LanePlaneIntersection.Cubic;
using F=NetworkTools.Geometry.LanePlaneIntersection.Failure;
static class Program {
    static int checks;
    static void Check(bool b,string text) { checks++; if(!b) throw new Exception(text); }
    static C Polynomial(double a,double b,double c,double d) => new C(new P(d,0,0),new P(d+c/3,1,1),new P(d+2*c/3+b/3,2,2),new P(d+c+b+a,3,3));
    static void Test(C curve,F expected,double root=0) {
        bool ok=LanePlaneIntersection.IntersectCubic(curve,new P(),1,0,1e-8,.001,out var hit,out var failure);
        Check(ok==(expected==F.None),"success "+failure+" expected "+expected);
        Check(failure==expected,"reason "+failure+" expected "+expected);
        if(ok) { Check(Math.Abs(hit.Parameter-root)<1e-9,"root"); Check(Math.Abs(hit.Position.X)<1e-8,"plane residual"); }
        else Check(hit.Parameter==0&&hit.Position.X==0,"failure atomic");
    }
    static void Main() {
        Test(Polynomial(0,0,1,-.5),F.None,.5);
        Test(Polynomial(0,0,1,0),F.None,0);
        Test(Polynomial(0,0,1,-1),F.None,1);
        Test(Polynomial(0,0,-1,.5),F.None,.5);
        Test(Polynomial(0,0,1,2),F.NoIntersection);
        Test(Polynomial(0,0,0,0),F.CoplanarOrNearPlane);
        Test(Polynomial(0,1,-1,.25),F.TangentOrIllConditioned); // even multiplicity, no sign change
        Test(Polynomial(0,1,-1,.16),F.MultipleIntersections); // .2,.8
        Test(Polynomial(1,-1.5,.66,-.08),F.MultipleIntersections); // .2,.5,.8
        Test(Polynomial(1,-1.5,.75,-.125),F.TangentOrIllConditioned); // triple .5
        Test(Polynomial(0,0,1,1e-10),F.TangentOrIllConditioned); // near endpoint uncertainty
        Test(Polynomial(0,0,1e8,-5e7),F.TangentOrIllConditioned); // caller tolerance below conditioning envelope
        Test(Polynomial(0,1,-1,.250000001),F.TangentOrIllConditioned);
        Test(Polynomial(0,1,-1,.3),F.NoIntersection);
        Test(Polynomial(0,1,0,-.25),F.None,.5);
        Test(Polynomial(0,0,1,double.NaN),F.InvalidInput);
        for(int i=1;i<20;i++) {
            double root=i/20.0;
            Test(Polynomial(0,0,1,-root),F.None,root);
            Test(Polynomial(0,0,-1,1-root),F.None,1-root);
        }
        Test(Polynomial(1,0,1,-.625),F.None,.5); // genuinely cubic monotone crossing
        Test(Polynomial(0,1,-1,0),F.MultipleIntersections); // both endpoint roots
        Test(Polynomial(0,0,.00001,-.000005),F.TangentOrIllConditioned); // shallow physical crossing
        var rotated = new C(new P(10,0,-2),new P(11,1,-1),new P(12,2,0),new P(13,3,1));
        Check(LanePlaneIntersection.IntersectCubic(rotated,new P(10,99,0),0,8,1e-8,.1,out var rotatedHit,out var rotatedFailure),"normal rescaling/rotation");
        Check(Math.Abs(rotatedHit.Parameter-2.0/3)<1e-9&&Math.Abs(rotatedHit.Position.Y-2)<1e-9,"rotated plane ignores origin Y, interpolates curve Y");
        // Two crossings only 1e-4 apart: a coarse sample grid can miss both.
        // Near-plane extremum is explicitly rejected, never reported as unique .9 root.
        Test(Polynomial(1,-1.9003,1.15042002,-.225135018),F.TangentOrIllConditioned);
        bool Project(P p,P t,double cosine,double bound,out LanePlaneIntersection.Hit hit,out F reason)
            => LanePlaneIntersection.ProjectTangent(p,t,new P(),1,0,cosine,bound,out hit,out reason);
        Check(Project(new P(-2,3,5),new P(1,.5,0),.9,2,out var hit,out var reason),"bounded proposal");
        Check(hit.Position.X==0&&hit.Position.Y==4&&hit.Position.Z==5&&hit.Parameter==2,"proposal units and grade");
        Check(!Project(new P(-2,0,0),new P(1,0,0),.9,1.99,out _,out reason)&&reason==F.ExtrapolationExceeded,"extrapolation bound");
        Check(!Project(new P(-2,0,0),new P(.001,0,1),.1,10000,out _,out reason)&&reason==F.TangentOrIllConditioned,"angle bound");
        Check(!Project(new P(),new P(),.1,2,out _,out reason)&&reason==F.TangentOrIllConditioned,"zero tangent");
        Check(!Project(new P(),new P(1,0,0),0,2,out _,out reason)&&reason==F.InvalidInput,"explicit angle policy");
        Check(Project(new P(2,1,0),new P(1,0,0),.1,2,out hit,out reason)&&hit.Parameter==-2,"signed backward projection");
        Check(Project(new P(0,1,0),new P(1,0,0),.1,0,out hit,out reason)&&hit.Parameter==0,"zero extrapolation");
        Console.WriteLine("LanePlaneIntersection: "+checks+" assertions passed (offline only).");
    }
}

