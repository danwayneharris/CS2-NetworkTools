using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;
using C = NetworkTools.Geometry.PlanarCubic;
using H = NetworkTools.Geometry.VerticalLinearProfile.Heights;
using F = NetworkTools.Geometry.ConnectVerticalProfile.Failure;

internal static unsafe class Program {
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static void Near(double expected, double actual, string message, double tolerance = 1e-8) =>
        Check(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance, $"{message}: {expected} != {actual}");
    private static C Straight(double start, double end, double startHandle, double endHandle) =>
        new C(new P(start,0),new P(start+startHandle,0),new P(end-endHandle,0),new P(end,0));
    private static C Reverse(C c) => new C(c.D,c.C,c.B,c.A);
    private static double Span(P a,P b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    private static void EndpointGrades(C[] input,H[] h,double first,double last) {
        Near(first,(h[0].B-h[0].A)/Span(input[0].A,input[0].B),"physical start grade");
        var i=input.Length-1;Near(last,(h[i].D-h[i].C)/Span(input[i].C,input[i].D),"physical end grade");
        if(input.Length==2) {
            Near(h[0].D,h[1].A,"Complex shared height");
            Near((h[0].D-h[0].C)/Span(input[0].C,input[0].D),(h[1].B-h[1].A)/Span(input[1].A,input[1].B),"Complex shared physical grade");
        }
    }
    private static bool Fit(C[] input,double y0,double y1,double g0,double g1,out H[] h,out ConnectVerticalProfile.Result r,out F failure) {
        h=new H[input.Length];for(var i=0;i<h.Length;i++)h[i]=new H{A=123,B=234,C=345,D=456};
        var before=(C[])input.Clone();
        fixed(C* p=input)fixed(H* q=h) {
            var ok=ConnectVerticalProfile.Fit(p,input.Length,y0,y1,g0,g1,q,out r,out failure);
            for(var i=0;i<input.Length;i++)Check(input[i].Equals(before[i]),"horizontal input unchanged");
            if(!ok)foreach(var v in h){Near(123,v.A,"failure preserves A");Near(234,v.B,"failure preserves B");Near(345,v.C,"failure preserves C");Near(456,v.D,"failure preserves D");}
            return ok;
        }
    }
    private static void Main() {
        var simple=new[]{Straight(0,90,30,30)};
        Check(Fit(simple,10,10,0,0,out var h,out var r,out _),"flat Simple");
        Near(10,h[0].A,"flat A");Near(10,h[0].B,"flat B");Near(10,h[0].C,"flat C");Near(10,h[0].D,"flat D");
        EndpointGrades(simple,h,0,0);Near(90,r.TotalHorizontalLength,"Simple horizontal length");
        Check(Fit(simple,10,19,.1,.1,out h,out r,out _),"constant sloped Simple");Near(13,h[0].B,"linear B");Near(16,h[0].C,"linear C");Near(14.5,r.JoinHeight,"profile midpoint");EndpointGrades(simple,h,.1,.1);
        Check(Fit(simple,10,10,.2,-.2,out h,out r,out _),"opposing grades supported");Near(14.5,r.JoinHeight,"analytic crest midpoint");Near(0,r.JoinGrade,"crest grade");EndpointGrades(simple,h,.2,-.2);
        // Caller-selected endpoint heights are raw targets, not inferred terrain/node offsets.
        Check(Fit(simple,100.25,112.75,.04,.08,out h,out r,out _),"explicit caller height targets");Near(100.25,h[0].A,"raw target A");Near(112.75,h[0].D,"raw target D");EndpointGrades(simple,h,.04,.08);

        var complex=new[]{Straight(0,20,5,7),Straight(20,100,11,20)};
        Check(Fit(complex,10,20,0,0,out h,out r,out _),"asymmetric Complex");
        Near(.2,r.JoinStationFraction,"arcstation midpoint is not half");Near(11.04,r.JoinHeight,"analytic smoothstep midpoint");Near(.096,r.JoinGrade,"analytic midpoint derivative");EndpointGrades(complex,h,0,0);
        Near(11.04-.096*7,h[0].C,"incoming handle uses own span");Near(11.04+.096*11,h[1].B,"outgoing handle uses own span");
        var old=(H[])h.Clone();
        complex[0].C=new P(18,0);complex[1].B=new P(45,0);
        Check(Fit(complex,10,20,0,0,out h,out r,out _),"changed interior horizontal handles");Near(11.04,r.JoinHeight,"same station same midpoint");Near(.096,r.JoinGrade,"same physical midpoint grade");
        Check(h[0].C!=old[0].C&&h[1].B!=old[1].B,"vertical controls recomputed for handle lengths");EndpointGrades(complex,h,0,0);
        // Changing the actual curve shape changes arc length and therefore midpoint station.
        var bent=new[]{new C(new P(0,0),new P(0,15),new P(15,0),new P(20,0)),Straight(20,100,11,20)};
        Check(Fit(bent,10,20,.05,-.02,out h,out r,out _),"curved first span");Check(r.JoinStationFraction>.2,"curved horizontal arc changes station");EndpointGrades(bent,h,.05,-.02);
        var reversed=new[]{Reverse(bent[1]),Reverse(bent[0])};
        Check(Fit(reversed,20,10,.02,-.05,out var rh,out var rr,out _),"reversed Complex selection");Near(1-r.JoinStationFraction,rr.JoinStationFraction,"reverse station");Near(r.JoinHeight,rr.JoinHeight,"reverse shared height");Near(-r.JoinGrade,rr.JoinGrade,"reverse shared grade");
        for(var i=0;i<h.Length;i++){var v=rh[h.Length-1-i];Near(h[i].A,v.D,"reverse A");Near(h[i].B,v.C,"reverse B");Near(h[i].C,v.B,"reverse C");Near(h[i].D,v.A,"reverse D");}
        Check(Fit(bent,10,20,.05,-.02,out rh,out rr,out _),"deterministic repeat");for(var i=0;i<h.Length;i++)Check(h[i].Equals(rh[i]),"bitwise deterministic controls");
        Check(Fit(bent,10010,10020,.05,-.02,out rh,out rr,out _),"world-height translation");for(var i=0;i<h.Length;i++){Near(h[i].B+10000,rh[i].B,"translated B");Near(h[i].C+10000,rh[i].C,"translated C");}

        // Straight horizontal geometry with nonlinear parameter speed: P(s(t)) differs
        // inside the emitted cubic, though exact endpoint grades remain satisfied.
        var nonlinear=new[]{Straight(0,100,5,5)};
        Check(Fit(nonlinear,0,10,0,0,out h,out r,out _),"nonuniform speed illustration");
        var t=.25;var u=nonlinear[0].Evaluate(t).X/100;var stationY=10*(3*u*u-2*u*u*u);
        var emittedY=3*(1-t)*t*t*10+t*t*t*10;
        Check(Math.Abs(stationY-emittedY)>.1,"do not claim exact interior station-profile equality");EndpointGrades(nonlinear,h,0,0);

        var disconnected=new[]{Straight(0,20,5,5),Straight(21,100,10,10)};
        Check(!Fit(disconnected,0,10,0,0,out _,out _,out var why)&&why==F.DisconnectedJoin,"disconnected Complex rejects");
        var kink=new[]{Straight(0,20,5,5),new C(new P(20,0),new P(20,10),new P(90,0),new P(100,0))};
        Check(!Fit(kink,0,10,0,0,out _,out _,out why)&&why==F.HorizontalKink,"horizontal kink rejects");
        var backwards=new[]{Straight(0,20,5,5),new C(new P(20,0),new P(10,0),new P(90,0),new P(100,0))};
        Check(!Fit(backwards,0,10,0,0,out _,out _,out why)&&why==F.HorizontalKink,"reversed join tangent rejects");
        var bad=(C[])complex.Clone();bad[1].B=bad[1].A;
        Check(!Fit(bad,0,10,0,0,out _,out _,out why)&&why==F.DegenerateHandle,"later handle failure atomic");
        bad=(C[])complex.Clone();bad[1].D=new P(double.NaN,0);Check(!Fit(bad,0,10,0,0,out _,out _,out _),"later nonfinite coordinate atomic");
        Check(!Fit(simple,0,10,double.NaN,0,out _,out _,out _),"NaN grade rejected");
        Check(!Fit(simple,double.PositiveInfinity,10,0,0,out _,out _,out _),"infinite height rejected");
        Check(!Fit(simple,0,10,double.MaxValue,double.MaxValue,out _,out _,out why)&&why==F.NonfiniteOutput,"arithmetic overflow rejected");
        Check(!Fit(new[]{Straight(0,.005,.001,.001)},0,1,0,0,out _,out _,out why)&&why==F.HorizontalLength,"short span rejected");
        Check(!Fit(new[]{simple[0],simple[0],simple[0]},0,1,0,0,out _,out _,out why)&&why==F.UnsupportedSegmentCount,"only Simple or Complex supported");
        Console.WriteLine($"Connect vertical profile: {checks} assertions passed (pure authored geometry; no native validation)");
    }
}
