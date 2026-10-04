using System;
using NetworkTools.Geometry;
using S = NetworkTools.Geometry.VerticalLinearProfile.Segment;
using A = NetworkTools.Geometry.SectionedVerticalProfile.Anchor;
using H = NetworkTools.Geometry.VerticalLinearProfile.Heights;

internal static unsafe class BoundedVerticalProfileTests {
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static void Near(double expected, double actual, string message, double tolerance = 1e-7) =>
        Check(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance, message + $": {expected} != {actual}");
    private static S[] Segments(int count, double length = 30) {
        var result = new S[count];
        for (var i = 0; i < count; i++) result[i] = new S { Length = length, StartHandle = length / 3, EndHandle = length / 3 };
        return result;
    }
    private static A[] Anchors(int count, double first = 0, double last = 0) {
        var result = new A[count + 1];
        result[0] = new A { Fixed = true, Height = first };
        result[count] = new A { Fixed = true, Height = last };
        return result;
    }
    private static double[] Bounds(int count, double value) {
        var result = new double[count + 1]; Array.Fill(result, value); return result;
    }
    private static bool Fit(S[] s, A[] a, double[] lo, double[] hi, out double[] h, out H[] c,
        out BoundedVerticalProfile.Failure reason, int iterations = 0) {
        h = Bounds(s.Length, 123456); c = new H[s.Length];
        for (var i = 0; i < c.Length; i++) c[i] = new H { A = 123456, B = 123456, C = 123456, D = 123456 };
        fixed (S* sp = s) fixed (A* ap = a) fixed (double* lp = lo, up = hi, hp = h) fixed (H* cp = c) {
            var ok = BoundedVerticalProfile.Fit(sp, s.Length, ap, lp, up, hp, cp, out reason, out var residual, iterations);
            if (ok) Check(residual <= 1e-9, "successful projected KKT residual");
            else {
                foreach (var value in h) Near(123456, value, "failure leaves heights untouched");
                foreach (var value in c) { Near(123456, value.A, "failure leaves A"); Near(123456, value.B, "failure leaves B"); Near(123456, value.C, "failure leaves C"); Near(123456, value.D, "failure leaves D"); }
            }
            return ok;
        }
    }
    public static void Run() {
        var s = Segments(2); var a = Anchors(2); var lo = Bounds(2, double.NegativeInfinity); var hi = Bounds(2, double.PositiveInfinity);
        lo[1] = 9; hi[1] = 11;
        Check(Fit(s, a, lo, hi, out var h, out var c, out _), "lower-active fit");
        Near(9, h[1], "lower bound chosen by objective"); Near(4.5, c[0].B, "natural endpoint grade");
        Near(9, c[0].C, "shared middle grade incoming"); Near(9, c[1].B, "shared middle grade outgoing");
        lo[1] = -11; hi[1] = -9;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "upper-active fit"); Near(-9, h[1], "upper bound");
        lo[1] = hi[1] = 7;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "zero interval"); Near(7, h[1], "zero interval height");
        Near(0, (c[0].D - c[0].C) / s[0].EndHandle, "zero interval fitted grade");

        // An initially active bound must be released when its multiplier has the wrong sign.
        a = Anchors(2, 0, 10); lo[1] = 0; hi[1] = 20;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "release initial active bound"); Near(5, h[1], "released lower bound reaches interior optimum");
        lo[1] = -2; hi[1] = 2;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "enter bound along feasible Newton step"); Near(2, h[1], "upper bound hit from interior");
        var scaled = Segments(2, 30000);
        Check(Fit(scaled, a, lo, hi, out var scaledHeights, out var scaledCurves, out _), "uniform station rescaling");
        for (var i = 0; i < h.Length; i++) Near(h[i], scaledHeights[i], "normalized station heights");
        for (var i = 0; i < c.Length; i++) { Near(c[i].B, scaledCurves[i].B, "normalized station B"); Near(c[i].C, scaledCurves[i].C, "normalized station C"); }
        // Natural cubic spline through (0,0), (1,1), (3,0) has value 7/8 at x=2.
        // Clamping the unrestricted flat profile would incorrectly leave that node at 0.
        s = Segments(3, 1); a = Anchors(3); lo = Bounds(3, double.NegativeInfinity); hi = Bounds(3, double.PositiveInfinity);
        lo[1] = 1;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "coupled bound solution");
        Near(1, h[1], "active first junction"); Near(.875, h[2], "free neighbor must refit, not clamp");
        hi[2] = .5;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "multiple active junctions"); Near(1, h[1], "multiple lower"); Near(.5, h[2], "multiple upper");
        Check(Fit(s, a, lo, hi, out var repeated, out var repeatedCurves, out _), "deterministic evaluation");
        for (var i = 0; i < h.Length; i++) Check(h[i] == repeated[i], "bitwise deterministic heights");
        for (var i = 0; i < c.Length; i++) Check(c[i].B == repeatedCurves[i].B && c[i].C == repeatedCurves[i].C, "bitwise deterministic controls");

        // Fixed explicit anchors override bounds and preserve independent authored grades.
        a[1] = new A { Fixed = true, Height = 2, MatchIncoming = true, MatchOutgoing = true, IncomingGrade = .12, OutgoingGrade = -.07 };
        hi[1] = 1.5;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "explicit anchor takes precedence"); Near(2, h[1], "fixed anchor exact");
        Near(.12, (c[0].D - c[0].C) / s[0].EndHandle, "incoming prescribed grade");
        Near(-.07, (c[1].B - c[1].A) / s[1].StartHandle, "outgoing prescribed grade");

        s = Segments(3); s[0].StartOffset = .2; s[0].EndOffset = -.3; s[1].StartOffset = .5; s[2].EndOffset = -.1;
        s[1].Length = 65; s[1].StartHandle = 9; s[1].EndHandle = 17;
        a = Anchors(3, 100, 109); a[0].MatchOutgoing = true; a[0].OutgoingGrade = .04;
        lo = Bounds(3, double.NegativeInfinity); hi = Bounds(3, double.PositiveInfinity); lo[1] = 105; hi[2] = 107;
        Check(Fit(s, a, lo, hi, out h, out c, out _), "offset unequal-handle fit");
        for (var i = 0; i < s.Length; i++) { Near(h[i] + s[i].StartOffset, c[i].A, "original start offset"); Near(h[i+1] + s[i].EndOffset, c[i].D, "original end offset"); }
        for (var i = 1; i < s.Length; i++) Near((c[i-1].D-c[i-1].C)/s[i-1].EndHandle, (c[i].B-c[i].A)/s[i].StartHandle, "shared physical endpoint grade");
        var rs = new S[s.Length]; var ra = new A[a.Length]; var rl = new double[lo.Length]; var ru = new double[hi.Length];
        for (var i = 0; i < s.Length; i++) { var v = s[s.Length-1-i]; rs[i] = new S { Length=v.Length, StartHandle=v.EndHandle, EndHandle=v.StartHandle, StartOffset=v.EndOffset, EndOffset=v.StartOffset }; }
        for (var i = 0; i < a.Length; i++) { var v=a[a.Length-1-i]; ra[i]=new A { Fixed=v.Fixed, Height=v.Height, MatchIncoming=v.MatchOutgoing, MatchOutgoing=v.MatchIncoming, IncomingGrade=-v.OutgoingGrade, OutgoingGrade=-v.IncomingGrade }; rl[i]=lo[lo.Length-1-i]; ru[i]=hi[hi.Length-1-i]; }
        Check(Fit(rs, ra, rl, ru, out var rh, out var rc, out _), "reversed bounded fit");
        for (var i = 0; i < h.Length; i++) Near(h[i], rh[h.Length-1-i], "reverse node");
        for (var i = 0; i < c.Length; i++) { var r=rc[c.Length-1-i]; Near(c[i].A,r.D,"reverse A"); Near(c[i].B,r.C,"reverse B"); Near(c[i].C,r.B,"reverse C"); Near(c[i].D,r.A,"reverse D"); }
        a[0].Height += 10000; a[3].Height += 10000; lo[1] += 10000; hi[2] += 10000;
        Check(Fit(s,a,lo,hi,out rh,out rc,out _),"elevation translation"); for(var i=0;i<h.Length;i++) Near(h[i]+10000,rh[i],"translation invariant");

        s=Segments(2);a=Anchors(2);lo=Bounds(2,double.NegativeInfinity);hi=Bounds(2,double.PositiveInfinity);
        lo[1]=9;hi[1]=11;Check(Fit(s,a,lo,hi,out h,out c,out _),"first operation budget");
        lo[1]=h[1]-1;hi[1]=h[1]+1;Check(Fit(s,a,lo,hi,out h,out c,out _),"rebased next operation budget");Near(8,h[1],"per-operation bound permits accumulating movement");
        Check(!Fit(s,a,lo,hi,out _,out _,out var failure,1) && failure==BoundedVerticalProfile.Failure.IterationLimit,"bounded convergence rejects unfinished solve");
        s[1].Length=double.PositiveInfinity;Check(!Fit(s,a,lo,hi,out _,out _,out failure)&&failure==BoundedVerticalProfile.Failure.InvalidInput,"nonfinite length rejected");
        s[1].Length=0;Check(!Fit(s,a,lo,hi,out _,out _,out _),"degenerate span rejected");
        s[1].Length=30;lo[1]=double.NaN;Check(!Fit(s,a,lo,hi,out _,out _,out _),"NaN bound rejected");
        lo[1]=10;hi[1]=9;Check(!Fit(s,a,lo,hi,out _,out _,out _),"inverted bounds rejected");
        lo[1]=double.NegativeInfinity;hi[1]=double.PositiveInfinity;a[1].MatchIncoming=true;Check(!Fit(s,a,lo,hi,out _,out _,out _),"grade match requires explicit anchor");
        a[1].MatchIncoming=false;s[0].Length=.01;s[1].Length=1e12;a[2].Height=10;
        Check(!Fit(s,a,lo,hi,out _,out _,out failure)&&failure==BoundedVerticalProfile.Failure.IllConditioned,"extreme station conditioning rejected");
        // Regression for free-subproblem KKT release. The old movement-threshold
        // heuristic needed 12 iterations here; explicit free stationarity needs 10.
        s=Segments(64);a=Anchors(64,0,20);lo=Bounds(64,double.NegativeInfinity);hi=Bounds(64,double.PositiveInfinity);
        for(var i=8;i<64;i+=8){var original=20.0*i/64+5*Math.Sin(i*.43);lo[i]=original-1;hi[i]=original+1;}
        Check(Fit(s,a,lo,hi,out h,out c,out _,10),"free KKT multiplier release within bounded iteration budget");
        for(var i=8;i<64;i+=8)Check(h[i]>=lo[i]-1e-8&&h[i]<=hi[i]+1e-8,"performance regression remains feasible");
        var tooMany = BoundedVerticalProfile.MaximumSegments + 1;
        Check(!Fit(Segments(tooMany),Anchors(tooMany),Bounds(tooMany,double.NegativeInfinity),Bounds(tooMany,double.PositiveInfinity),out _,out _,out failure)
            && failure == BoundedVerticalProfile.Failure.CapacityExceeded,"interactive finite-bound capacity rejects explicitly");
        Console.WriteLine($"Bounded vertical profile: {checks} assertions passed");
    }
}
